using System.Collections.Generic;
using System.Numerics;
using Xunit;
using beyondnations;

namespace beyondnationstests {

    /**
    * Regions come in coherent patches, are a pure function of the seed (so a
    * loaded world rebuilds the same map), and shape what world generation
    * puts in each chunk.
    */
    public class TestRegionMap {

        [Fact]
        public void testTheSameSeed_GivesTheSameRegions() {
            RegionMap first = new RegionMap(42, 105f);
            RegionMap second = new RegionMap(42, 105f);
            for (int x = -20; x <= 20; x++) {
                for (int z = -20; z <= 20; z++) {
                    Assert.Equal(first.regionOfChunk(x, z), second.regionOfChunk(x, z));
                }
            }
        }

        /**
        * Saves rely on regions being rebuilt from the seed (docs/SAVES.md), so
        * the noise must not change by accident: these are seed 42's regions
        * as first released. Changing them changes every existing world.
        */
        [Fact]
        public void testSeed42sRegions_ArePinned() {
            string expected = pinnedRow();
            Assert.Equal(expected, rowOf(42, 0));
        }

        private static string pinnedRow() {
            return "FHHHFFFFFFPPMMMMPFFFHHHHHHHHFFFFFHHHPMMMPFFF";
        }

        private static string rowOf(int seed, int z) {
            char[] row = new char[44];
            for (int x = -22; x < 22; x++) {
                row[x + 22] = "PFHM"[(int) RegionMap.compute(seed, x, z)];
            }
            return new string(row);
        }

        /**
        * Regions and seasons are not in the save: an old save (format 1,
        * written before either existed) loads with the regions its seed
        * gives and the season its tick count gives.
        */
        [Fact]
        public void testAnOldSave_LoadsWithItsSeedsRegions_AndItsSeason() {
            Simulation loaded = SaveGame.decode(System.IO.File.ReadAllBytes(SaveFixtures.pathFor(1)));
            RegionMap fresh = new RegionMap(SaveFixtures.FixtureSeed, 105f);
            Assert.Equal(SaveFixtures.FixtureSeed, loaded.getRegionMap().getSeed());
            foreach (Chunk chunk in loaded.getEnvironment().getChunks()) {
                Assert.Equal(fresh.regionOf(chunk), loaded.getRegionMap().regionOf(chunk));
            }
            Assert.Equal(Season.SPRING, loaded.getSeasonCalendar().getSeason());
            for (int i = 0; i < 200; i++) {
                loaded.fixedUpdate(1f / 50f);
            }
        }

        [Fact]
        public void testEveryRegion_IsCommon_AndNoneDominates() {
            Dictionary<Region, int> counts = new Dictionary<Region, int>();
            int total = 0;
            for (int seed = 1; seed <= 60; seed++) {
                for (int x = -10; x < 10; x++) {
                    for (int z = -10; z < 10; z++) {
                        Region region = RegionMap.compute(seed, x, z);
                        counts[region] = (counts.TryGetValue(region, out int n) ? n : 0) + 1;
                        total++;
                    }
                }
            }
            foreach (Region region in new[] { Region.PLAINS, Region.FOREST, Region.HIGHLANDS, Region.MOOR }) {
                float share = (float) counts[region] / total;
                Assert.InRange(share, 0.10f, 0.45f);
            }
        }

        [Fact]
        public void testRegions_ComeInPatches() {
            int same = 0;
            int pairs = 0;
            for (int x = -30; x < 30; x++) {
                for (int z = -30; z < 30; z++) {
                    pairs++;
                    if (RegionMap.compute(7, x, z) == RegionMap.compute(7, x + 1, z)) {
                        same++;
                    }
                }
            }
            // a checkerboard would be about a quarter; patches are mostly unbroken
            Assert.True(same > pairs * 0.7f, same + " of " + pairs + " neighbours share a region");
        }

        [Fact]
        public void testRegionAt_RoundsDown_ForNegativeCoordinates() {
            RegionMap map = new RegionMap(9, 105f);
            Assert.Equal(-1, map.chunkCoordinate(-0.5f));
            Assert.Equal(0, map.chunkCoordinate(104.9f));
            Assert.Equal(map.regionOfChunk(-1, -2), map.regionAt(new Vector3(-10f, 3f, -110f)));
        }

        [Fact]
        public void testGeneration_PutsTreesInForests_AndRocksInTheHighlands() {
            Dictionary<Region, float> trees = new Dictionary<Region, float>();
            Dictionary<Region, float> rocks = new Dictionary<Region, float>();
            Dictionary<Region, int> chunks = new Dictionary<Region, int>();
            foreach (int seed in new[] { 1, 7, 42, 2026 }) {
                GameConfig config = new GameConfig();
                config.setWorldSeed(seed);
                Simulation simulation = new Simulation(config);
                simulation.fixedUpdate(1f / 50f);
                RegionMap map = simulation.getRegionMap();
                foreach (Chunk chunk in simulation.getEnvironment().getChunks()) {
                    if (chunk.getX() == 0 && chunk.getZ() == 0) {
                        continue; // the opening chunk is made bare, before generation
                    }
                    Region region = map.regionOf(chunk);
                    chunks[region] = (chunks.TryGetValue(region, out int n) ? n : 0) + 1;
                }
                foreach (Entity entity in simulation.getEntityRepository().getEntities()) {
                    Dictionary<Region, float> into = entity.getType() == EntityType.TREE ? trees : entity.getType() == EntityType.ROCK ? rocks : null;
                    if (into != null) {
                        Region region = map.regionAt(entity.getPosition());
                        into[region] = (into.TryGetValue(region, out float n) ? n : 0f) + 1f;
                    }
                }
            }
            float perChunk(Dictionary<Region, float> counts, Region region) {
                return (counts.TryGetValue(region, out float n) ? n : 0f) / chunks[region];
            }
            Assert.True(perChunk(trees, Region.FOREST) > 2f * perChunk(trees, Region.HIGHLANDS));
            Assert.True(perChunk(rocks, Region.HIGHLANDS) > 2f * perChunk(rocks, Region.FOREST));
            // stone everywhere: no region goes without
            foreach (Region region in chunks.Keys) {
                Assert.True(perChunk(rocks, region) >= 1.5f, RegionCatalog.name(region) + " has " + perChunk(rocks, region) + " rocks a chunk");
            }
        }
    }
}
