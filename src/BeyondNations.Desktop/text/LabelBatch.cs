using System;
using System.Numerics;
using Silk.NET.OpenGL;

namespace beyondnations.desktop.text {

    /**
    * The GPU side of the label buffer: one vertex array, one vertex buffer, one
    * index buffer, and one glDrawElements for the lot.
    *
    * Both buffers are orphaned and refilled when the CPU arrays grow, and only
    * the used prefix is uploaded each frame, so a frame with three labels does
    * not pay for the high-water mark of a frame with three hundred.
    */
    public unsafe class LabelBatch : IDisposable {

        private readonly GL gl;
        private readonly uint vertexArray;
        private readonly uint vertexBuffer;
        private readonly uint indexBuffer;

        private int uploadedVertexCapacity;
        private int uploadedIndexCapacity;

        public LabelBatch(GL gl) {
            this.gl = gl;

            vertexArray = gl.GenVertexArray();
            gl.BindVertexArray(vertexArray);

            vertexBuffer = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBuffer);

            indexBuffer = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indexBuffer);

            uint stride = (uint) sizeof(LabelVertex);
            gl.EnableVertexAttribArray(LabelShader.AttributePosition);
            gl.VertexAttribPointer(
                LabelShader.AttributePosition, 3, VertexAttribPointerType.Float, false, stride, (void*) 0);
            gl.EnableVertexAttribArray(LabelShader.AttributeTexCoord);
            gl.VertexAttribPointer(
                LabelShader.AttributeTexCoord, 2, VertexAttribPointerType.Float, false, stride, (void*) (3 * sizeof(float)));
            gl.EnableVertexAttribArray(LabelShader.AttributeColor);
            gl.VertexAttribPointer(
                LabelShader.AttributeColor, 4, VertexAttribPointerType.Float, false, stride, (void*) (5 * sizeof(float)));

            gl.BindVertexArray(0);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }

        /**
        * Uploads the buffer's used prefix and draws all of it. Returns the
        * number of draw calls issued: one, or none when nothing is visible.
        */
        public int uploadAndDraw(LabelQuadBuffer buffer) {
            if (buffer.isEmpty()) {
                return 0;
            }

            gl.BindVertexArray(vertexArray);

            LabelVertex[] vertices = buffer.getVertices();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBuffer);
            fixed (LabelVertex* data = vertices) {
                if (vertices.Length != uploadedVertexCapacity) {
                    gl.BufferData(
                        BufferTargetARB.ArrayBuffer,
                        (nuint) (vertices.Length * sizeof(LabelVertex)),
                        null,
                        BufferUsageARB.StreamDraw);
                    uploadedVertexCapacity = vertices.Length;
                }
                gl.BufferSubData(
                    BufferTargetARB.ArrayBuffer,
                    0,
                    (nuint) (buffer.getVertexCount() * sizeof(LabelVertex)),
                    data);
            }

            uint[] indices = buffer.getIndices();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indexBuffer);
            if (indices.Length != uploadedIndexCapacity) {
                // Indices only change when the buffer grows, so this is the
                // only time they are sent at all.
                fixed (uint* data = indices) {
                    gl.BufferData(
                        BufferTargetARB.ElementArrayBuffer,
                        (nuint) (indices.Length * sizeof(uint)),
                        data,
                        BufferUsageARB.StaticDraw);
                }
                uploadedIndexCapacity = indices.Length;
            }

            gl.DrawElements(
                PrimitiveType.Triangles,
                (uint) buffer.getIndexCount(),
                DrawElementsType.UnsignedInt,
                (void*) 0);

            return 1;
        }

        public void Dispose() {
            gl.DeleteBuffer(indexBuffer);
            gl.DeleteBuffer(vertexBuffer);
            gl.DeleteVertexArray(vertexArray);
        }
    }
}
