using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.OpenGL;
using beyondnations;

namespace beyondnations.desktop.render {

    /**
    * Draws a world snapshot as a handful of instanced draw calls.
    *
    * One batch exists per PrimitiveKind, and every item of that kind in the
    * snapshot -- ground tile, rock, pawn, tree trunk, dropped apple -- becomes a
    * row in that batch's instance buffer. A frame is therefore at most four draw
    * calls no matter how large the world is, against the one draw call per
    * GameObject that the Unity build could not escape.
    *
    * The render loop allocates nothing once the batches have reached their
    * high-water mark: the snapshot lists are indexed rather than enumerated,
    * instance data is written into arrays that are reused between frames, and
    * uniform locations were looked up at startup.
    */
    public unsafe class PrimitiveRenderer : IDisposable {

        /**
        * A single directional light, pointing down and slightly across, so that
        * the top of a cube reads differently from its sides.
        */
        private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.35f, -0.85f, -0.4f));
        private const float Ambient = 0.35f;

        private readonly GL gl;
        private readonly InstancedPrimitiveShader shader;
        private readonly PrimitiveBatch[] batches;

        private int drawCalls;
        private int instancesDrawn;
        private int instancesCulled;

        public PrimitiveRenderer(GL gl) {
            this.gl = gl;
            this.shader = new InstancedPrimitiveShader(gl);

            // Driven off the enum rather than a hand-written list, so a new
            // primitive kind cannot be added to the simulation without a mesh
            // being demanded for it here.
            Array kinds = Enum.GetValues(typeof(PrimitiveKind));
            int highest = 0;
            foreach (PrimitiveKind kind in kinds) {
                if ((int) kind > highest) {
                    highest = (int) kind;
                }
            }

            batches = new PrimitiveBatch[highest + 1];
            foreach (PrimitiveKind kind in kinds) {
                batches[(int) kind] = new PrimitiveBatch(gl, kind, PrimitiveMesh.forKind(kind));
            }

            // Solid, opaque, convex primitives: the far side of one is never
            // wanted, and the near side must win wherever two overlap.
            gl.Enable(EnableCap.DepthTest);
            gl.DepthFunc(DepthFunction.Less);
            gl.Enable(EnableCap.CullFace);
            gl.CullFace(TriangleFace.Back);
            gl.FrontFace(FrontFaceDirection.Ccw);

            Log.info("primitive renderer ready: " + batches.Length + " instanced batches");
        }

        public int getDrawCallCount() { return drawCalls; }
        public int getInstancesDrawn() { return instancesDrawn; }

        /**
        * How many snapshot items the culler rejected on the last frame, and so
        * how much work the instance buffers were spared (#219).
        */
        public int getInstancesCulled() { return instancesCulled; }

        public int getInstanceCount(PrimitiveKind kind) {
            return batches[(int) kind].getInstanceCount();
        }

        public int getInstanceCapacity(PrimitiveKind kind) {
            return batches[(int) kind].getInstanceCapacity();
        }

        /**
        * Draws everything in the snapshot. The snapshot is read and never
        * written, so the simulation cannot be disturbed by having been drawn.
        */
        public void render(WorldSnapshot snapshot, Matrix4x4 view, Matrix4x4 projection) {
            render(snapshot, view, projection, null);
        }

        /**
        * As above, but with a culler (#219) deciding what reaches an instance
        * buffer at all. A null culler draws everything, which is what the
        * renderer did before the camera existed.
        */
        public void render(WorldSnapshot snapshot, Matrix4x4 view, Matrix4x4 projection, RenderCuller culler) {
            for (int i = 0; i < batches.Length; i++) {
                batches[i].clear();
            }

            instancesCulled = 0;
            appendAll(snapshot.getGroundItems(), culler);
            appendAll(snapshot.getEntityItems(), culler);

            shader.use();

            // System.Numerics rows become GLSL columns, so these go up
            // untransposed and are multiplied on the left in the shader.
            Matrix4x4 viewCopy = view;
            Matrix4x4 projectionCopy = projection;
            gl.UniformMatrix4(shader.getViewLocation(), 1, false, (float*) &viewCopy);
            gl.UniformMatrix4(shader.getProjectionLocation(), 1, false, (float*) &projectionCopy);
            gl.Uniform3(shader.getLightDirectionLocation(), LightDirection.X, LightDirection.Y, LightDirection.Z);
            gl.Uniform1(shader.getAmbientLocation(), Ambient);

            drawCalls = 0;
            instancesDrawn = 0;
            for (int i = 0; i < batches.Length; i++) {
                instancesDrawn += batches[i].getInstanceCount();
                drawCalls += batches[i].uploadAndDraw();
            }

            gl.BindVertexArray(0);
        }

        /**
        * Indexed rather than foreach on purpose: enumerating an IReadOnlyList
        * through its interface allocates an enumerator every frame, which is
        * exactly the per-object allocation #218 forbids.
        */
        private void appendAll(IReadOnlyList<RenderItem> items, RenderCuller culler) {
            int count = items.Count;
            for (int i = 0; i < count; i++) {
                RenderItem item = items[i];
                // The culling decision is taken here, before the instance is
                // written, so a culled item costs a distance compare rather
                // than eighty bytes of buffer and a share of a draw call.
                if (culler != null && !culler.shouldDraw(ref item)) {
                    instancesCulled++;
                    continue;
                }
                batches[(int) item.kind].add(ref item);
            }
        }

        public void Dispose() {
            for (int i = 0; i < batches.Length; i++) {
                if (batches[i] != null) {
                    batches[i].Dispose();
                }
            }
            shader.Dispose();
        }
    }
}
