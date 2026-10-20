using System.Collections.Generic;
using System.Numerics;
using Xunit;
using beyondnations;

namespace beyondnationstests {

    /**
    * The surplus/shortage query trade and diplomacy read: a nation settled
    * in the highlands has stone to spare and wants wood and food; a forest
    * nation has wood and wants stone.
    */
    public class TestNationResources {
        private const float ChunkLength = 105f;
        private const int Seed = 42;

        private static Vector3 centreOfFirst(Region region) {
            // the middle of a 3x3 block of one region, so the land around is all that region
            for (int x = -60; x < 60; x++) {
                for (int z = -60; z < 60; z++) {
                    bool all = true;
                    for (int dx = -1; dx <= 1 && all; dx++) {
                        for (int dz = -1; dz <= 1 && all; dz++) {
                            all = RegionMap.compute(Seed, x + dx, z + dz) == region;
                        }
                    }
                    if (all) {
                        return new Vector3((x + 0.5f) * ChunkLength, 0f, (z + 0.5f) * ChunkLength);
                    }
                }
            }
            throw new Xunit.Sdk.XunitException("no block of " + region);
        }

        private static Nation settleIn(EntityRepository entities, Region region) {
            Nation nation = new Nation(RegionCatalog.name(region) + "land", new EntityId(), new RandomSource(1));
            Settlement settlement = new Settlement(centreOfFirst(region), nation.getId(), Rgba.White, nation.getName(), new RandomSource(2));
            entities.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return nation;
        }

        [Fact]
        public void testAHighlandNation_HasStone_AndLacksWoodAndFood() {
            EntityRepository entities = new EntityRepository(new RandomSource(0));
            NationResources resources = new NationResources(entities, new RegionMap(Seed, ChunkLength));
            Nation nation = settleIn(entities, Region.HIGHLANDS);

            Assert.Equal(new List<Commodity> { Commodity.STONE }, resources.getSurpluses(nation));
            Assert.Equal(new List<Commodity> { Commodity.WOOD, Commodity.FOOD }, resources.getShortages(nation));
            Assert.Equal(RegionCatalog.rockFactor(Region.HIGHLANDS), resources.getAbundance(nation, Commodity.STONE), 3);
        }

        [Fact]
        public void testAForestNation_AndAHighlandNation_HaveSomethingToTrade() {
            EntityRepository entities = new EntityRepository(new RandomSource(0));
            NationResources resources = new NationResources(entities, new RegionMap(Seed, ChunkLength));
            Nation forest = settleIn(entities, Region.FOREST);
            Nation highlands = settleIn(entities, Region.HIGHLANDS);

            Assert.Contains(Commodity.WOOD, resources.getSurpluses(forest));
            Assert.Contains(Commodity.STONE, resources.getShortages(forest));
            Assert.True(resources.canSupply(forest, highlands, Commodity.WOOD));
            Assert.True(resources.canSupply(highlands, forest, Commodity.STONE));
            Assert.False(resources.canSupply(forest, highlands, Commodity.STONE));
        }

        [Fact]
        public void testANationWithNoLand_HasNoSurplusOrShortage() {
            EntityRepository entities = new EntityRepository(new RandomSource(0));
            NationResources resources = new NationResources(entities, new RegionMap(Seed, ChunkLength));
            Nation nation = new Nation("Nowhere", new EntityId(), new RandomSource(1));
            Assert.Empty(resources.getSurpluses(nation));
            Assert.Empty(resources.getShortages(nation));
            Assert.Equal(ResourceStanding.NORMAL, resources.getStanding(nation, Commodity.FOOD));
        }

        [Fact]
        public void testCommodities_NameTheirItems() {
            Assert.Equal(new[] { ItemType.WOOD }, NationResources.itemTypesOf(Commodity.WOOD));
            Assert.Contains(ItemType.APPLE, NationResources.itemTypesOf(Commodity.FOOD));
        }

        [Fact]
        public void testRealWorlds_HaveNationsThatDiffer() {
            // the point of regions: across a populated world, not every nation is the same
            GameConfig config = new GameConfig();
            config.setWorldSeed(Seed);
            Simulation simulation = new Simulation(config);
            for (int i = 0; i < 30000; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            HashSet<string> profiles = new HashSet<string>();
            foreach (Nation nation in simulation.getNationRepository().getNations()) {
                NationResources resources = simulation.getNationResources();
                profiles.Add(string.Join(",", resources.getSurpluses(nation)) + "/" + string.Join(",", resources.getShortages(nation)));
            }
            Assert.True(profiles.Count >= 2, "every nation had the same surpluses and shortages");
        }
    }
}
