using System;
using System.Collections.Generic;
using beyondnations;

namespace beyondnations.desktop.render {

    /**
    * A primitive mesh, generated in memory rather than imported.
    *
    * The project has never owned a mesh file and does not start now: every shape
    * the simulation asks for is one of four analytic solids, so each is built
    * from its own definition at startup. Vertices are interleaved as position
    * then normal, three floats each, which is all the shader needs.
    *
    * Dimensions deliberately match the Unity primitives the appearance data was
    * authored against, so that existing scales keep their meaning:
    *
    *   Cube      one unit on every side, centred on the origin
    *   Sphere    one unit across, centred on the origin
    *   Cylinder  one unit across, two units tall, centred on the origin
    *   Capsule   one unit across, two units tall, centred on the origin
    *
    * That is why a tree trunk of scale (1, height, 1) reaches y = height and its
    * cube of leaves sits at y = height - 1: the numbers came from Unity and the
    * mesh has to agree with them.
    *
    * Triangles wind counter-clockwise when seen from outside the solid, which is
    * what lets back faces be culled.
    */
    public class PrimitiveMesh {
        public const int FloatsPerVertex = 6;

        private readonly float[] vertices;
        private readonly uint[] indices;

        public PrimitiveMesh(float[] vertices, uint[] indices) {
            this.vertices = vertices;
            this.indices = indices;
        }

        public float[] getVertices() { return vertices; }
        public uint[] getIndices() { return indices; }
        public int getVertexCount() { return vertices.Length / FloatsPerVertex; }
        public int getIndexCount() { return indices.Length; }

        /**
        * Builds the mesh for a kind. Every member of PrimitiveKind is handled;
        * an unknown one is a programming error rather than something to draw as
        * a fallback, so it throws.
        */
        public static PrimitiveMesh forKind(PrimitiveKind kind) {
            switch (kind) {
                case PrimitiveKind.Cube:     return createCube();
                case PrimitiveKind.Sphere:   return createSphere(16, 24);
                case PrimitiveKind.Cylinder: return createCylinder(24);
                case PrimitiveKind.Capsule:  return createCapsule(16, 24);
                default:
                    throw new ArgumentOutOfRangeException("kind", "no mesh is defined for primitive kind " + kind);
            }
        }

        /**
        * A unit cube with one normal per face, so the faces stay flat rather
        * than being smoothed into each other by shared vertices.
        */
        public static PrimitiveMesh createCube() {
            MeshBuilder builder = new MeshBuilder(24, 36);
            const float h = 0.5f;

            builder.addQuad(-h, -h,  h,   h, -h,  h,   h,  h,  h,  -h,  h,  h,   0f,  0f,  1f);
            builder.addQuad( h, -h, -h,  -h, -h, -h,  -h,  h, -h,   h,  h, -h,   0f,  0f, -1f);
            builder.addQuad( h, -h,  h,   h, -h, -h,   h,  h, -h,   h,  h,  h,   1f,  0f,  0f);
            builder.addQuad(-h, -h, -h,  -h, -h,  h,  -h,  h,  h,  -h,  h, -h,  -1f,  0f,  0f);
            builder.addQuad(-h,  h,  h,   h,  h,  h,   h,  h, -h,  -h,  h, -h,   0f,  1f,  0f);
            builder.addQuad(-h, -h, -h,   h, -h, -h,   h, -h,  h,  -h, -h,  h,   0f, -1f,  0f);

            return builder.build();
        }

        /**
        * A sphere of unit diameter, as a latitude and longitude grid. The polar
        * rings collapse to a point, which costs a row of degenerate triangles
        * and saves the special-casing that a fan would need.
        */
        public static PrimitiveMesh createSphere(int stacks, int sectors) {
            MeshBuilder builder = new MeshBuilder((stacks + 1) * sectors, stacks * sectors * 6);

            for (int i = 0; i <= stacks; i++) {
                double theta = Math.PI * i / stacks;
                float sinTheta = (float) Math.Sin(theta);
                float cosTheta = (float) Math.Cos(theta);
                for (int j = 0; j < sectors; j++) {
                    double phi = 2.0 * Math.PI * j / sectors;
                    float nx = sinTheta * (float) Math.Cos(phi);
                    float ny = cosTheta;
                    float nz = sinTheta * (float) Math.Sin(phi);
                    builder.addVertex(nx * 0.5f, ny * 0.5f, nz * 0.5f, nx, ny, nz);
                }
            }

            addGridIndices(builder, stacks + 1, sectors);
            return builder.build();
        }

        /**
        * A capsule of unit diameter and two units tall: a hemisphere, a straight
        * wall, and a second hemisphere. The ring at the equator is emitted twice,
        * once for each hemisphere centre, and the quads between those two copies
        * are the wall.
        */
        public static PrimitiveMesh createCapsule(int stacks, int sectors) {
            if (stacks % 2 != 0) {
                stacks++;
            }
            int half = stacks / 2;
            int rings = stacks + 2;

            MeshBuilder builder = new MeshBuilder(rings * sectors, (rings - 1) * sectors * 6);

            for (int ring = 0; ring < rings; ring++) {
                // Rings 0..half belong to the top cap, half+1..rings-1 to the
                // bottom one, so the two copies of the equator differ only in
                // which hemisphere centre they are offset from.
                int i = ring <= half ? ring : ring - 1;
                float centreY = ring <= half ? 0.5f : -0.5f;

                double theta = Math.PI * i / stacks;
                float sinTheta = (float) Math.Sin(theta);
                float cosTheta = (float) Math.Cos(theta);

                for (int j = 0; j < sectors; j++) {
                    double phi = 2.0 * Math.PI * j / sectors;
                    float nx = sinTheta * (float) Math.Cos(phi);
                    float ny = cosTheta;
                    float nz = sinTheta * (float) Math.Sin(phi);
                    builder.addVertex(nx * 0.5f, ny * 0.5f + centreY, nz * 0.5f, nx, ny, nz);
                }
            }

            addGridIndices(builder, rings, sectors);
            return builder.build();
        }

        /**
        * A cylinder of unit diameter and two units tall, with flat caps whose
        * normals point along the axis rather than being shared with the wall.
        */
        public static PrimitiveMesh createCylinder(int sectors) {
            MeshBuilder builder = new MeshBuilder(sectors * 4 + 2, sectors * 12);

            // Wall: a bottom ring then a top ring, both with outward normals.
            for (int ring = 0; ring < 2; ring++) {
                float y = ring == 0 ? -1f : 1f;
                for (int j = 0; j < sectors; j++) {
                    double phi = 2.0 * Math.PI * j / sectors;
                    float nx = (float) Math.Cos(phi);
                    float nz = (float) Math.Sin(phi);
                    builder.addVertex(nx * 0.5f, y, nz * 0.5f, nx, 0f, nz);
                }
            }
            for (int j = 0; j < sectors; j++) {
                int next = (j + 1) % sectors;
                uint bottom = (uint) j;
                uint bottomNext = (uint) next;
                uint top = (uint) (sectors + j);
                uint topNext = (uint) (sectors + next);
                builder.addTriangle(bottom, top, topNext);
                builder.addTriangle(bottom, topNext, bottomNext);
            }

            // Caps, each a fan around its own centre vertex.
            uint topRingStart = (uint) builder.getVertexCount();
            for (int j = 0; j < sectors; j++) {
                double phi = 2.0 * Math.PI * j / sectors;
                builder.addVertex((float) Math.Cos(phi) * 0.5f, 1f, (float) Math.Sin(phi) * 0.5f, 0f, 1f, 0f);
            }
            uint topCentre = (uint) builder.getVertexCount();
            builder.addVertex(0f, 1f, 0f, 0f, 1f, 0f);

            uint bottomRingStart = (uint) builder.getVertexCount();
            for (int j = 0; j < sectors; j++) {
                double phi = 2.0 * Math.PI * j / sectors;
                builder.addVertex((float) Math.Cos(phi) * 0.5f, -1f, (float) Math.Sin(phi) * 0.5f, 0f, -1f, 0f);
            }
            uint bottomCentre = (uint) builder.getVertexCount();
            builder.addVertex(0f, -1f, 0f, 0f, -1f, 0f);

            for (int j = 0; j < sectors; j++) {
                uint next = (uint) ((j + 1) % sectors);
                builder.addTriangle(topCentre, topRingStart + next, topRingStart + (uint) j);
                builder.addTriangle(bottomCentre, bottomRingStart + (uint) j, bottomRingStart + next);
            }

            return builder.build();
        }

        /**
        * Stitches a ring-by-sector grid of vertices into triangles, wrapping the
        * last sector back onto the first. Longitude runs before latitude so that
        * the result faces outwards.
        */
        private static void addGridIndices(MeshBuilder builder, int rings, int sectors) {
            for (int i = 0; i < rings - 1; i++) {
                for (int j = 0; j < sectors; j++) {
                    int next = (j + 1) % sectors;
                    uint a = (uint) (i * sectors + j);
                    uint b = (uint) (i * sectors + next);
                    uint c = (uint) ((i + 1) * sectors + next);
                    uint d = (uint) ((i + 1) * sectors + j);
                    builder.addTriangle(a, b, c);
                    builder.addTriangle(a, c, d);
                }
            }
        }

        /**
        * Accumulates interleaved vertices and indices while a mesh is built.
        * Startup only; nothing here runs while frames are being drawn.
        */
        private class MeshBuilder {
            private readonly List<float> vertices;
            private readonly List<uint> indices;

            public MeshBuilder(int expectedVertices, int expectedIndices) {
                vertices = new List<float>(expectedVertices * FloatsPerVertex);
                indices = new List<uint>(expectedIndices);
            }

            public int getVertexCount() {
                return vertices.Count / FloatsPerVertex;
            }

            public void addVertex(float x, float y, float z, float nx, float ny, float nz) {
                vertices.Add(x);
                vertices.Add(y);
                vertices.Add(z);
                vertices.Add(nx);
                vertices.Add(ny);
                vertices.Add(nz);
            }

            public void addTriangle(uint a, uint b, uint c) {
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
            }

            public void addQuad(
                    float ax, float ay, float az,
                    float bx, float by, float bz,
                    float cx, float cy, float cz,
                    float dx, float dy, float dz,
                    float nx, float ny, float nz) {
                uint start = (uint) getVertexCount();
                addVertex(ax, ay, az, nx, ny, nz);
                addVertex(bx, by, bz, nx, ny, nz);
                addVertex(cx, cy, cz, nx, ny, nz);
                addVertex(dx, dy, dz, nx, ny, nz);
                addTriangle(start, start + 1, start + 2);
                addTriangle(start, start + 2, start + 3);
            }

            public PrimitiveMesh build() {
                return new PrimitiveMesh(vertices.ToArray(), indices.ToArray());
            }
        }
    }
}
