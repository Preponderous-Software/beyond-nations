using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using beyondnations;

namespace beyondnations.desktop.render {

    /**
    * What one instance of a primitive needs on the GPU: where it is and what
    * colour it is. Eighty bytes, laid out exactly as the vertex attributes read
    * it, so the array can be handed to the driver without any conversion step.
    */
    [StructLayout(LayoutKind.Sequential)]
    public struct PrimitiveInstance {
        public Matrix4x4 model;
        public Vector4 color;
    }

    /**
    * Every instance of one primitive kind, drawn in a single call.
    *
    * This is the whole point of #218. The Unity build created a GameObject and a
    * material instance for each of the hundreds of thousands of tiles it could
    * be asked to show, and LagPreventer existed to delete things at random when
    * that got out of hand. Here a tile is eighty bytes in an array, and every
    * cube in the world -- ground, rocks and tree leaves alike -- is one
    * glDrawElementsInstanced.
    *
    * The instance array is reused between frames and only ever grows, so once
    * the world has reached its largest the render loop allocates nothing at all.
    */
    public unsafe class PrimitiveBatch : IDisposable {
        private const int InitialCapacity = 1024;

        private readonly GL gl;
        private readonly PrimitiveKind kind;
        private readonly int indexCount;

        private readonly uint vertexArray;
        private readonly uint vertexBuffer;
        private readonly uint indexBuffer;
        private readonly uint instanceBuffer;

        private PrimitiveInstance[] instances = new PrimitiveInstance[InitialCapacity];
        private int instanceCount;
        private int uploadedCapacity;

        public PrimitiveBatch(GL gl, PrimitiveKind kind, PrimitiveMesh mesh) {
            this.gl = gl;
            this.kind = kind;
            this.indexCount = mesh.getIndexCount();

            vertexArray = gl.GenVertexArray();
            gl.BindVertexArray(vertexArray);

            float[] vertices = mesh.getVertices();
            vertexBuffer = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBuffer);
            fixed (float* data = vertices) {
                gl.BufferData(
                    BufferTargetARB.ArrayBuffer,
                    (nuint) (vertices.Length * sizeof(float)),
                    data,
                    BufferUsageARB.StaticDraw);
            }

            uint[] indices = mesh.getIndices();
            indexBuffer = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indexBuffer);
            fixed (uint* data = indices) {
                gl.BufferData(
                    BufferTargetARB.ElementArrayBuffer,
                    (nuint) (indices.Length * sizeof(uint)),
                    data,
                    BufferUsageARB.StaticDraw);
            }

            uint vertexStride = (uint) (PrimitiveMesh.FloatsPerVertex * sizeof(float));
            gl.EnableVertexAttribArray(InstancedPrimitiveShader.AttributePosition);
            gl.VertexAttribPointer(
                InstancedPrimitiveShader.AttributePosition, 3, VertexAttribPointerType.Float, false, vertexStride, (void*) 0);
            gl.EnableVertexAttribArray(InstancedPrimitiveShader.AttributeNormal);
            gl.VertexAttribPointer(
                InstancedPrimitiveShader.AttributeNormal, 3, VertexAttribPointerType.Float, false, vertexStride, (void*) (3 * sizeof(float)));

            instanceBuffer = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, instanceBuffer);
            uint instanceStride = (uint) sizeof(PrimitiveInstance);

            // A mat4 attribute is four vec4 slots, each advanced once per
            // instance rather than once per vertex.
            for (uint column = 0; column < 4; column++) {
                uint location = InstancedPrimitiveShader.AttributeModelColumn0 + column;
                gl.EnableVertexAttribArray(location);
                gl.VertexAttribPointer(
                    location, 4, VertexAttribPointerType.Float, false, instanceStride, (void*) (column * 4 * sizeof(float)));
                gl.VertexAttribDivisor(location, 1);
            }
            gl.EnableVertexAttribArray(InstancedPrimitiveShader.AttributeColor);
            gl.VertexAttribPointer(
                InstancedPrimitiveShader.AttributeColor, 4, VertexAttribPointerType.Float, false, instanceStride, (void*) (16 * sizeof(float)));
            gl.VertexAttribDivisor(InstancedPrimitiveShader.AttributeColor, 1);

            gl.BindVertexArray(0);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        }

        public PrimitiveKind getKind() { return kind; }
        public int getInstanceCount() { return instanceCount; }
        public int getInstanceCapacity() { return instances.Length; }

        public void clear() {
            instanceCount = 0;
        }

        /**
        * Adds one instance. The item is already in world space, and only ever
        * scaled and translated, so the matrix is written directly instead of
        * being multiplied out of a scale and a translation.
        */
        public void add(ref RenderItem item) {
            if (instanceCount == instances.Length) {
                grow();
            }

            // Assigning into the array by index writes through to the existing
            // storage; nothing is boxed and nothing is copied out and back.
            instances[instanceCount].model = new Matrix4x4(
                item.scale.X, 0f, 0f, 0f,
                0f, item.scale.Y, 0f, 0f,
                0f, 0f, item.scale.Z, 0f,
                item.position.X, item.position.Y, item.position.Z, 1f);
            instances[instanceCount].color = new Vector4(
                item.color.r, item.color.g, item.color.b, item.color.a);
            instanceCount++;
        }

        /**
        * Uploads this frame's instances and draws all of them at once. Returns
        * the number of draw calls issued, which is one, or none when there is
        * nothing of this kind in the world.
        */
        public int uploadAndDraw() {
            if (instanceCount == 0) {
                return 0;
            }

            gl.BindVertexArray(vertexArray);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, instanceBuffer);

            int stride = sizeof(PrimitiveInstance);
            fixed (PrimitiveInstance* data = instances) {
                if (instances.Length != uploadedCapacity) {
                    // The store is sized to the array rather than to this
                    // frame's count, so a frame that grows by one instance does
                    // not reallocate it.
                    gl.BufferData(
                        BufferTargetARB.ArrayBuffer,
                        (nuint) (instances.Length * stride),
                        null,
                        BufferUsageARB.StreamDraw);
                    uploadedCapacity = instances.Length;
                }
                gl.BufferSubData(
                    BufferTargetARB.ArrayBuffer,
                    0,
                    (nuint) (instanceCount * stride),
                    data);
            }

            gl.DrawElementsInstanced(
                PrimitiveType.Triangles,
                (uint) indexCount,
                DrawElementsType.UnsignedInt,
                (void*) 0,
                (uint) instanceCount);

            return 1;
        }

        private void grow() {
            PrimitiveInstance[] larger = new PrimitiveInstance[instances.Length * 2];
            Array.Copy(instances, larger, instances.Length);
            instances = larger;
        }

        public void Dispose() {
            gl.DeleteBuffer(instanceBuffer);
            gl.DeleteBuffer(indexBuffer);
            gl.DeleteBuffer(vertexBuffer);
            gl.DeleteVertexArray(vertexArray);
        }
    }
}
