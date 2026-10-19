using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using beyondnations;
using beyondnations.desktop.render;
using beyondnations.web.host;

namespace beyondnations.web {

    /**
    * The browser's world renderer for now: a WebGL2 port of the Silk renderer
    * the desktop used before it became Strata-only. It is replaced by Strata
    * linked into the page once that link is proven (docs/WEB.md).
    *
    * Same meshes (PrimitiveMesh, shared with the desktop), same 80-byte
    * instance (model matrix + colour), same light and ambient, same shader
    * with only the GLSL version line changed (330 core -> 300 es, see
    * wwwroot/webhost/gfx.js), and the same one instanced draw per primitive
    * kind. Culling is the shared RenderCuller, applied before an instance is
    * written, exactly as on the desktop.
    *
    * --renderer native (the Strata engine) is not available in the browser
    * yet; when Strata's WebGL2 backend lands it becomes a second
    * IWorldRenderer here, chosen by the same option.
    */
    public unsafe class WebGl2WorldRenderer : IWorldRenderer {

        // The Silk renderer's fixed light direction and ambient.
        private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.35f, -0.85f, -0.4f));
        private const float Ambient = 0.35f;

        [StructLayout(LayoutKind.Sequential)]
        private struct Instance {
            public Matrix4x4 model;
            public Vector4 color;
        }

        private class Batch {
            public int mesh;
            public Instance[] instances = new Instance[1024];
            public int count;
        }

        private readonly Batch[] batches;
        private readonly float[] matrices = new float[32];

        private int drawCalls;
        private int instancesDrawn;
        private int instancesCulled;

        public WebGl2WorldRenderer() {
            Array kinds = Enum.GetValues(typeof(PrimitiveKind));
            int highest = 0;
            foreach (PrimitiveKind kind in kinds) {
                highest = Math.Max(highest, (int) kind);
            }
            batches = new Batch[highest + 1];
            foreach (PrimitiveKind kind in kinds) {
                PrimitiveMesh mesh = PrimitiveMesh.forKind(kind);
                float[] vertices = mesh.getVertices();
                uint[] indices = mesh.getIndices();
                Batch batch = new Batch();
                fixed (float* v = vertices)
                fixed (uint* i = indices) {
                    batch.mesh = WebGpu.createMesh((int) v, vertices.Length, (int) i, indices.Length);
                }
                batches[(int) kind] = batch;
            }
            Log.info("primitive renderer ready (WebGL2): " + batches.Length + " instanced batches");
        }

        public string getName() { return "webgl2"; }

        public void setViewportSize(int width, int height) { }

        public int getDrawCallCount() { return drawCalls; }
        public int getInstancesDrawn() { return instancesDrawn; }
        public int getInstancesCulled() { return instancesCulled; }

        public int getInstanceCount(PrimitiveKind kind) {
            return batches[(int) kind].count;
        }

        public void render(WorldSnapshot snapshot, Matrix4x4 view, Matrix4x4 projection, RenderCuller culler) {
            for (int i = 0; i < batches.Length; i++) {
                batches[i].count = 0;
            }

            instancesCulled = 0;
            appendAll(snapshot.getGroundItems(), culler);
            appendAll(snapshot.getEntityItems(), culler);

            write(view, 0);
            write(projection, 16);
            fixed (float* m = matrices) {
                WebGpu.beginPrimitives((int) m, LightDirection.X, LightDirection.Y, LightDirection.Z, Ambient);
            }

            drawCalls = 0;
            instancesDrawn = 0;
            for (int i = 0; i < batches.Length; i++) {
                Batch batch = batches[i];
                if (batch.count == 0) {
                    continue;
                }
                fixed (Instance* data = batch.instances) {
                    WebGpu.drawInstances(batch.mesh, (int) data, batch.count);
                }
                instancesDrawn += batch.count;
                drawCalls++;
            }
        }

        private void appendAll(IReadOnlyList<RenderItem> items, RenderCuller culler) {
            int count = items.Count;
            for (int i = 0; i < count; i++) {
                RenderItem item = items[i];
                if (culler != null && !culler.shouldDraw(ref item)) {
                    instancesCulled++;
                    continue;
                }
                Batch batch = batches[(int) item.kind];
                if (batch.count == batch.instances.Length) {
                    Array.Resize(ref batch.instances, batch.instances.Length * 2);
                }
                // Scaled and translated only, so the matrix is written directly.
                batch.instances[batch.count].model = new Matrix4x4(
                    item.scale.X, 0f, 0f, 0f,
                    0f, item.scale.Y, 0f, 0f,
                    0f, 0f, item.scale.Z, 0f,
                    item.position.X, item.position.Y, item.position.Z, 1f);
                batch.instances[batch.count].color = new Vector4(item.color.r, item.color.g, item.color.b, item.color.a);
                batch.count++;
            }
        }

        private void write(Matrix4x4 m, int at) {
            matrices[at + 0] = m.M11; matrices[at + 1] = m.M12; matrices[at + 2] = m.M13; matrices[at + 3] = m.M14;
            matrices[at + 4] = m.M21; matrices[at + 5] = m.M22; matrices[at + 6] = m.M23; matrices[at + 7] = m.M24;
            matrices[at + 8] = m.M31; matrices[at + 9] = m.M32; matrices[at + 10] = m.M33; matrices[at + 11] = m.M34;
            matrices[at + 12] = m.M41; matrices[at + 13] = m.M42; matrices[at + 14] = m.M43; matrices[at + 15] = m.M44;
        }

        public void Dispose() { }
    }
}
