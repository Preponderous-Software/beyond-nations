using System;
using System.Numerics;

namespace beyondnations {

    /**
    * What a tree or a sapling looks like: a trunk and a canopy of layered
    * leaf blobs, in one of a few shapes (#140).
    *
    * Which shape, how big and which way its lobes lean are worked out from
    * where the tree stands, not drawn from the world's random stream, so
    * world generation and every seed's pacing are untouched, the same tree
    * looks the same on every run, and a loaded tree is rebuilt with the
    * look it had (AppleTree.loadState) without the save having to say.
    *
    * Leaves are pure green scaled by a shade (0, g, 0): WorldPalette turns
    * every such part the season's leaf colour at that shade, so the lower,
    * shadowed layers stay darker in every season. Everything is drawn from
    * the four primitives Strata already instances, so a tree adds instances
    * to existing draw calls, never draw calls.
    *
    * Offsets are relative to the entity's position, which world generation
    * puts one unit above its ground tile: the ground's top is at y = -0.5.
    */
    public static class TreeShape {
        public static readonly Rgba TrunkBrown = new Rgba(0.5f, 0.25f, 0f);
        public static readonly Rgba TrunkDark = new Rgba(0.38f, 0.2f, 0.05f);

        /** How many canopy shapes there are. */
        public const int ShapeCount = 3;

        /** Where the ground's top is, relative to a tree's position. */
        public const float GroundY = -0.5f;

        /** A round broadleaf crown: one big blob, two lower side lobes and a cap. */
        public const int Round = 0;
        /** A layered crown: three flattened tiers, widest at the bottom. */
        public const int Tiered = 1;
        /** A tall oval crown with one low lobe, on a longer trunk. */
        public const int Tall = 2;

        /** A grown tree of the given height (5 for every tree the game makes). */
        public static Appearance tree(Vector3 position, int height) {
            uint seed = seedFor(position);
            return build(height, shapeOf(seed), sizeOf(seed, 0.85f, 1.2f), angleOf(seed), 1f);
        }

        /** A sapling: a thin stem with a small crown, about a third of a tree. */
        public static Appearance sapling(Vector3 position, int height) {
            uint seed = seedFor(position);
            return build(height, shapeOf(seed), sizeOf(seed, 0.8f, 1.1f) * 0.45f, angleOf(seed), 0.6f);
        }

        /** Which of the shapes a tree standing here takes. */
        public static int shapeAt(Vector3 position) {
            return shapeOf(seedFor(position));
        }

        private static Appearance build(int height, int shape, float size, float angle, float trunkThickness) {
            Appearance a = new Appearance();
            float trunkTop = Math.Max(1f, height * size * (shape == Tall ? 0.9f : 0.72f));
            float trunkWidth = 0.55f * Math.Max(0.6f, size) * trunkThickness;
            // the trunk stands on the ground: a cylinder is two units tall at scale 1
            float trunkHalf = (trunkTop - GroundY) * 0.5f;
            a.addPart(PrimitiveKind.Cylinder, new Vector3(0, GroundY + trunkHalf, 0),
                new Vector3(trunkWidth, trunkHalf, trunkWidth), TrunkBrown);

            float s = size * 1.15f;
            Vector3 lean = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle));
            Vector3 across = new Vector3(-lean.Z, 0, lean.X);
            switch (shape) {
                case Round:
                    leaf(a, new Vector3(0, trunkTop + 0.4f * s, 0), new Vector3(3.2f, 2.8f, 3.2f) * s, 0.85f);
                    leaf(a, new Vector3(0, trunkTop - 0.2f * s, 0) + lean * 1.1f * s, new Vector3(2.2f, 1.8f, 2.2f) * s, 0.7f);
                    leaf(a, new Vector3(0, trunkTop + 0.1f * s, 0) - lean * 0.6f * s + across * 0.9f * s, new Vector3(2.0f, 1.7f, 2.0f) * s, 0.75f);
                    leaf(a, new Vector3(0, trunkTop + 1.5f * s, 0) - lean * 0.2f * s, new Vector3(1.8f, 1.5f, 1.8f) * s, 1f);
                    break;
                case Tiered:
                    leaf(a, new Vector3(0, trunkTop - 0.6f * s, 0), new Vector3(3.6f, 1.3f, 3.6f) * s, 0.7f);
                    leaf(a, new Vector3(0, trunkTop + 0.4f * s, 0) + lean * 0.15f * s, new Vector3(2.7f, 1.2f, 2.7f) * s, 0.85f);
                    leaf(a, new Vector3(0, trunkTop + 1.35f * s, 0), new Vector3(1.7f, 1.2f, 1.7f) * s, 1f);
                    break;
                default:
                    leaf(a, new Vector3(0, trunkTop + 0.9f * s, 0), new Vector3(2.3f, 3.8f, 2.3f) * s, 0.9f);
                    leaf(a, new Vector3(0, trunkTop - 0.5f * s, 0) + lean * 0.9f * s, new Vector3(1.6f, 1.6f, 1.6f) * s, 0.7f);
                    break;
            }
            return a;
        }

        private static void leaf(Appearance a, Vector3 offset, Vector3 scale, float shade) {
            a.addPart(PrimitiveKind.Sphere, offset, scale, new Rgba(0f, shade, 0f));
        }

        // --- the per-tree seed: a hash of where it stands ---

        private static uint seedFor(Vector3 position) {
            // rounded to a tenth of a unit, so float noise in a saved
            // position cannot change the look on load
            uint x = (uint) (int) MathF.Round(position.X * 10f);
            uint z = (uint) (int) MathF.Round(position.Z * 10f);
            uint h = x * 0x9E3779B1u ^ (z + 0x7F4A7C15u) * 0x85EBCA77u;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return h;
        }

        private static int shapeOf(uint seed) {
            return (int) (seed % ShapeCount);
        }

        private static float sizeOf(uint seed, float min, float max) {
            return min + (max - min) * (((seed >> 8) & 0xFFFF) / 65535f);
        }

        private static float angleOf(uint seed) {
            return ((seed >> 24) & 0xFF) / 256f * MathF.PI * 2f;
        }
    }
}
