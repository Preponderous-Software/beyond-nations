using System;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.render;

namespace beyondnationstests.desktop {

    /**
    * Mesh generation is pure arithmetic and needs no context, so the parts of
    * the renderer that can be checked without a window are checked here.
    *
    * Winding matters most: back-face culling is enabled, so a solid whose
    * triangles wind the wrong way is drawn inside out and nobody finds out
    * until somebody looks at a screen.
    */
    public class TestPrimitiveMesh {

        [Fact]
        public void testEveryPrimitiveKindHasAMesh() {
            foreach (PrimitiveKind kind in Enum.GetValues(typeof(PrimitiveKind))) {
                // run
                PrimitiveMesh mesh = PrimitiveMesh.forKind(kind);

                // verify
                Assert.NotNull(mesh);
                Assert.True(mesh.getVertexCount() > 0, kind + " has no vertices");
                Assert.True(mesh.getIndexCount() > 0, kind + " has no indices");
                Assert.Equal(0, mesh.getIndexCount() % 3);
                Assert.Equal(0, mesh.getVertices().Length % PrimitiveMesh.FloatsPerVertex);

                uint[] indices = mesh.getIndices();
                for (int i = 0; i < indices.Length; i++) {
                    Assert.True(indices[i] < mesh.getVertexCount(), kind + " has an out of range index");
                }
            }
        }

        [Fact]
        public void testCubeIsOneUnitWithAFlatNormalPerFace() {
            // run
            PrimitiveMesh mesh = PrimitiveMesh.createCube();

            // verify: six faces of four vertices, two triangles each
            Assert.Equal(24, mesh.getVertexCount());
            Assert.Equal(36, mesh.getIndexCount());
            assertBounds(mesh, 0.5f, 0.5f);
        }

        [Fact]
        public void testDimensionsMatchTheAppearanceDataTheyWereAuthoredFor() {
            // Scales in the simulation came from Unity primitives, where a
            // sphere is one unit across and a cylinder or capsule is two tall.
            assertBounds(PrimitiveMesh.forKind(PrimitiveKind.Sphere), 0.5f, 0.5f);
            assertBounds(PrimitiveMesh.forKind(PrimitiveKind.Cylinder), 0.5f, 1f);
            assertBounds(PrimitiveMesh.forKind(PrimitiveKind.Capsule), 0.5f, 1f);
        }

        [Fact]
        public void testEveryTriangleFacesOutwards() {
            foreach (PrimitiveKind kind in Enum.GetValues(typeof(PrimitiveKind))) {
                PrimitiveMesh mesh = PrimitiveMesh.forKind(kind);
                uint[] indices = mesh.getIndices();

                int checkedTriangles = 0;
                for (int i = 0; i < indices.Length; i += 3) {
                    Vector3 a = positionOf(mesh, indices[i]);
                    Vector3 b = positionOf(mesh, indices[i + 1]);
                    Vector3 c = positionOf(mesh, indices[i + 2]);

                    Vector3 face = Vector3.Cross(b - a, c - a);
                    if (face.Length() < 1e-6f) {
                        // A pole ring collapses to a point; those triangles
                        // have no area and therefore no facing.
                        continue;
                    }

                    // Every one of these solids is convex and centred on the
                    // origin, so a triangle faces outwards exactly when its
                    // normal agrees with the direction of its own centroid.
                    Vector3 centroid = (a + b + c) / 3f;
                    Assert.True(
                        Vector3.Dot(Vector3.Normalize(face), centroid) > 0f,
                        kind + " has a triangle winding inwards at index " + i);
                    checkedTriangles++;
                }

                Assert.True(checkedTriangles > 0, kind + " has no triangles with area");
            }
        }

        [Fact]
        public void testEveryNormalIsUnitLength() {
            foreach (PrimitiveKind kind in Enum.GetValues(typeof(PrimitiveKind))) {
                PrimitiveMesh mesh = PrimitiveMesh.forKind(kind);
                float[] vertices = mesh.getVertices();

                for (int i = 0; i < mesh.getVertexCount(); i++) {
                    int offset = i * PrimitiveMesh.FloatsPerVertex;
                    Vector3 normal = new Vector3(
                        vertices[offset + 3], vertices[offset + 4], vertices[offset + 5]);
                    Assert.True(
                        Math.Abs(normal.Length() - 1f) < 1e-4f,
                        kind + " has a normal of length " + normal.Length());
                }
            }
        }

        private static Vector3 positionOf(PrimitiveMesh mesh, uint index) {
            float[] vertices = mesh.getVertices();
            int offset = (int) index * PrimitiveMesh.FloatsPerVertex;
            return new Vector3(vertices[offset], vertices[offset + 1], vertices[offset + 2]);
        }

        private static void assertBounds(PrimitiveMesh mesh, float halfWidth, float halfHeight) {
            float maxX = 0f;
            float maxY = 0f;
            float maxZ = 0f;
            for (int i = 0; i < mesh.getVertexCount(); i++) {
                Vector3 position = positionOf(mesh, (uint) i);
                maxX = Math.Max(maxX, Math.Abs(position.X));
                maxY = Math.Max(maxY, Math.Abs(position.Y));
                maxZ = Math.Max(maxZ, Math.Abs(position.Z));
            }
            Assert.True(Math.Abs(maxX - halfWidth) < 1e-4f, "half width was " + maxX);
            Assert.True(Math.Abs(maxZ - halfWidth) < 1e-4f, "half depth was " + maxZ);
            Assert.True(Math.Abs(maxY - halfHeight) < 1e-4f, "half height was " + maxY);
        }
    }
}
