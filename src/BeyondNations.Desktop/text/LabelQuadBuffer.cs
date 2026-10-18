using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace beyondnations.desktop.text {

    /**
    * One corner of one glyph, laid out exactly as the vertex attributes read
    * it. Thirty-six bytes, so the array can go to the driver unconverted.
    */
    [StructLayout(LayoutKind.Sequential)]
    public struct LabelVertex {
        public Vector3 position;
        public Vector2 texCoord;
        public Vector4 color;
    }

    /**
    * Every glyph of every visible label, in one pair of arrays.
    *
    * This is the reason a hundred nametags cost one draw call rather than a
    * hundred. The Unity build made a Canvas and a Text component per label, so
    * the cost of showing names scaled with the number of pawns; here a label is
    * a handful of quads appended to a buffer that is uploaded once.
    *
    * The arrays are reused between frames and only ever grow, so once the
    * busiest frame has been drawn the label path allocates nothing either --
    * the same rule #218 set for the primitive renderer.
    *
    * There is no OpenGL in this file so that the packing can be asserted
    * against directly.
    */
    public class LabelQuadBuffer {
        private const int InitialQuads = 512;

        private LabelVertex[] vertices = new LabelVertex[InitialQuads * 4];
        private uint[] indices = new uint[InitialQuads * 6];

        private int quadCount;

        /**
        * How many indices were written for quads, which is the count handed to
        * glDrawElements.
        */
        private int builtIndexQuads;

        public LabelQuadBuffer() {
            buildIndicesUpTo(InitialQuads);
        }

        public int getQuadCount() { return quadCount; }
        public int getVertexCount() { return quadCount * 4; }
        public int getIndexCount() { return quadCount * 6; }
        public int getQuadCapacity() { return vertices.Length / 4; }

        public LabelVertex[] getVertices() { return vertices; }
        public uint[] getIndices() { return indices; }

        public bool isEmpty() { return quadCount == 0; }

        public void clear() {
            quadCount = 0;
        }

        /**
        * Appends one glyph. Corners are given in the order the font hands them
        * over; the two triangles are 0-1-2 and 0-2-3, which is the winding the
        * index table below was built for.
        */
        public void addQuad(
                ref LabelVertex topLeft,
                ref LabelVertex topRight,
                ref LabelVertex bottomRight,
                ref LabelVertex bottomLeft) {
            if (quadCount == getQuadCapacity()) {
                grow();
            }

            int at = quadCount * 4;
            vertices[at + 0] = topLeft;
            vertices[at + 1] = topRight;
            vertices[at + 2] = bottomRight;
            vertices[at + 3] = bottomLeft;
            quadCount++;
        }

        private void grow() {
            int newQuads = getQuadCapacity() * 2;

            LabelVertex[] largerVertices = new LabelVertex[newQuads * 4];
            Array.Copy(vertices, largerVertices, vertices.Length);
            vertices = largerVertices;

            uint[] largerIndices = new uint[newQuads * 6];
            Array.Copy(indices, largerIndices, indices.Length);
            indices = largerIndices;

            buildIndicesUpTo(newQuads);
        }

        /**
        * Quad indices never change, so they are written once when the buffer
        * grows rather than rebuilt every frame.
        */
        private void buildIndicesUpTo(int quads) {
            for (int quad = builtIndexQuads; quad < quads; quad++) {
                uint vertex = (uint) (quad * 4);
                int at = quad * 6;
                indices[at + 0] = vertex + 0;
                indices[at + 1] = vertex + 1;
                indices[at + 2] = vertex + 2;
                indices[at + 3] = vertex + 0;
                indices[at + 4] = vertex + 2;
                indices[at + 5] = vertex + 3;
            }
            builtIndexQuads = quads;
        }
    }
}
