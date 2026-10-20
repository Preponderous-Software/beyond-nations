using System;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * Which region (forest, highlands, plains, moor) every chunk of the world
    * belongs to.
    *
    * A chunk's region is a pure function of the world seed and the chunk's
    * coordinates: two smooth value-noise fields, "elevation" and "moisture",
    * sampled on a grid CELL_CHUNKS chunks across, so regions come in coherent
    * patches several chunks wide rather than a checkerboard. Nothing is drawn
    * from the world's random streams, so adding regions never shifted what
    * else a seed generates, and the map needs no saving: a loaded world
    * rebuilds it from its seed and gets the same regions (docs/SAVES.md).
    *
    * Because old saves rely on that, the noise below is part of the save
    * contract: changing it changes the regions of every existing world.
    * TestRegionMap pins a handful of chunks for seed 42 so it cannot change
    * by accident.
    */
    public class RegionMap {

        /** Width of one noise cell, in chunks: a region patch is about this wide. */
        public const int CELL_CHUNKS = 4;

        // Thresholds on the two noise fields, tuned so that over many seeds
        // about 30% of land is plains, 26% forest, 24% highlands and 19% moor.
        public const float HIGHLANDS_ELEVATION = 0.63f;
        public const float FOREST_MOISTURE = 0.58f;
        public const float MOOR_MOISTURE = 0.37f;

        private readonly int seed;
        private readonly float chunkLength;
        // a memo of regions already worked out; derived, so never saved
        private readonly Dictionary<long, Region> cache = new Dictionary<long, Region>();

        /**
        * chunkLength is the width of a chunk in world units (chunk size times
        * location scale), for looking regions up by position.
        */
        public RegionMap(int seed, float chunkLength) {
            if (chunkLength <= 0f) {
                throw new ArgumentOutOfRangeException(nameof(chunkLength), chunkLength, "a chunk must have some width");
            }
            this.seed = seed;
            this.chunkLength = chunkLength;
        }

        public int getSeed() { return seed; }

        /** The region of the chunk at these chunk coordinates. */
        public Region regionOfChunk(int chunkX, int chunkZ) {
            long key = ((long) chunkX << 32) ^ (uint) chunkZ;
            Region region;
            if (!cache.TryGetValue(key, out region)) {
                region = compute(seed, chunkX, chunkZ);
                cache[key] = region;
            }
            return region;
        }

        public Region regionOf(Chunk chunk) {
            return regionOfChunk(chunk.getX(), chunk.getZ());
        }

        /** The region of the land at a world position (height is ignored). */
        public Region regionAt(Vector3 position) {
            return regionOfChunk(chunkCoordinate(position.X), chunkCoordinate(position.Z));
        }

        /** The chunk coordinate a world coordinate falls in, rounding down (so -1 is in chunk -1, not 0). */
        public int chunkCoordinate(float worldCoordinate) {
            return (int) MathF.Floor(worldCoordinate / chunkLength);
        }

        // --- the noise ---

        /** The region a seed gives a chunk, with no memo. */
        public static Region compute(int seed, int chunkX, int chunkZ) {
            float elevation = noise(seed, 0x51EE7, chunkX, chunkZ);
            float moisture = noise(seed, 0x3A1F, chunkX, chunkZ);
            if (elevation > HIGHLANDS_ELEVATION) {
                return Region.HIGHLANDS;
            }
            if (moisture > FOREST_MOISTURE) {
                return Region.FOREST;
            }
            if (moisture < MOOR_MOISTURE) {
                return Region.MOOR;
            }
            return Region.PLAINS;
        }

        /**
        * Smooth value noise in [0, 1): a random value at every cell corner,
        * blended across the cell with a smoothstep, plus a finer octave so
        * edges wander instead of running straight.
        */
        public static float noise(int seed, int channel, int chunkX, int chunkZ) {
            float coarse = valueNoise(seed, channel, chunkX, chunkZ, CELL_CHUNKS);
            float fine = valueNoise(seed, channel ^ 0x7F4A7C15, chunkX, chunkZ, Math.Max(1, CELL_CHUNKS / 2));
            return coarse * 0.8f + fine * 0.2f;
        }

        private static float valueNoise(int seed, int channel, int x, int z, int cell) {
            int cx = floorDiv(x, cell);
            int cz = floorDiv(z, cell);
            float fx = (float) (x - cx * cell) / cell;
            float fz = (float) (z - cz * cell) / cell;
            float sx = fx * fx * (3f - 2f * fx);
            float sz = fz * fz * (3f - 2f * fz);
            float v00 = hash01(seed, channel, cx, cz);
            float v10 = hash01(seed, channel, cx + 1, cz);
            float v01 = hash01(seed, channel, cx, cz + 1);
            float v11 = hash01(seed, channel, cx + 1, cz + 1);
            float top = v00 + (v10 - v00) * sx;
            float bottom = v01 + (v11 - v01) * sx;
            return top + (bottom - top) * sz;
        }

        private static int floorDiv(int a, int b) {
            int q = a / b;
            return (a % b != 0 && (a < 0) != (b < 0)) ? q - 1 : q;
        }

        /** A well-mixed value in [0, 1) for a corner (SplitMix64's finaliser). */
        private static float hash01(int seed, int channel, int x, int z) {
            unchecked {
                ulong h = (ulong) (uint) seed;
                h = h * 0x9E3779B97F4A7C15UL + (ulong) (uint) channel;
                h = h * 0x9E3779B97F4A7C15UL + (ulong) (uint) x;
                h = h * 0x9E3779B97F4A7C15UL + (ulong) (uint) z;
                h ^= h >> 30;
                h *= 0xBF58476D1CE4E5B9UL;
                h ^= h >> 27;
                h *= 0x94D049BB133111EBUL;
                h ^= h >> 31;
                return (float) ((h >> 40) / (double) (1UL << 24));
            }
        }
    }
}
