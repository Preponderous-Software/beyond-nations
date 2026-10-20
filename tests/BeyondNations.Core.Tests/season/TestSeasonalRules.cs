using System.Numerics;
using Xunit;
using beyondnations;

namespace beyondnationstests {

    /**
    * What the seasons do: autumn's double harvest, winter's empty farms, slow
    * saplings and cold, chickens hatching in spring, and pawns laying in food.
    */
    public class TestSeasonalRules {

        private static Simulation createSimulation(int seed) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            return new Simulation(config);
        }

        /** Moves the clock on without running the world, to the first tick of the season. */
        private static void jumpTo(Simulation simulation, Season season) {
            int guard = 0;
            while (simulation.getSeasonCalendar().getSeason() != season) {
                simulation.getTickCounter().increment();
                Assert.True(++guard < 10_000_000);
            }
        }

        [Fact]
        public void testFarms_YieldDoubleInAutumn_AndNothingInWinter() {
            Assert.Equal(1, SeasonalRules.farmYield(Season.SPRING));
            Assert.Equal(1, SeasonalRules.farmYield(Season.SUMMER));
            Assert.Equal(2, SeasonalRules.farmYield(Season.AUTUMN));
            Assert.Equal(0, SeasonalRules.farmYield(Season.WINTER));
            Assert.Equal(1, RegionCatalog.farmBonus(Region.PLAINS));
            Assert.Equal(0, RegionCatalog.farmBonus(Region.HIGHLANDS));
        }

        [Fact]
        public void testSaplings_GrowSlowlyInWinter() {
            TickCounter ticks = new TickCounter();
            Sapling sapling = new Sapling(Vector3.Zero, 3, new RandomSource(1), ticks);
            long grow = (long) sapling.getGrowTime() * ticks.getTicksPerSecond();
            for (long i = 0; i <= grow; i++) {
                ticks.increment();
            }
            Assert.True(sapling.isGrown(SeasonalRules.saplingGrowthFactor(Season.SUMMER)));
            Assert.False(sapling.isGrown(SeasonalRules.saplingGrowthFactor(Season.WINTER)));
            for (long i = 0; i < grow * 2; i++) {
                ticks.increment();
            }
            Assert.True(sapling.isGrown(SeasonalRules.saplingGrowthFactor(Season.WINTER)));
        }

        [Fact]
        public void testWinterCold_BurnsAPawnsEnergyFasterOutdoors() {
            float springBurn = burnOverOneTick(Season.SPRING);
            float winterBurn = burnOverOneTick(Season.WINTER);
            Assert.Equal(springBurn * SeasonalRules.WINTER_COLD_MULTIPLIER, winterBurn, 4);
        }

        private static float burnOverOneTick(Season season) {
            Simulation simulation = createSimulation(42);
            jumpTo(simulation, season);
            Pawn pawn = new Pawn(new Vector3(5000, 1.5f, 5000), "Test", new RandomSource(3));
            pawn.setEnergy(95f);
            simulation.getEntityRepository().addEntity(pawn);
            // off the behaviour cadences, so the pawn neither rethinks nor acts this tick
            while (simulation.getTickCounter().getTotalTicks() % 5 == 4) {
                simulation.getTickCounter().increment();
            }
            simulation.fixedUpdate(1f / 50f);
            return 95f - pawn.getEnergy();
        }

        [Fact]
        public void testChickens_HatchInSpring_ButNotInWinter() {
            Simulation spring = createSimulation(42);
            spring.fixedUpdate(1f / 50f);
            removeChickens(spring);
            int hatched = 0;
            for (int i = 0; i < 40; i++) {
                if (spring.getSeasonalWorld().hatchChicken()) {
                    hatched++;
                }
            }
            Assert.True(hatched > 0);
            Assert.Equal(hatched, spring.getEntityRepository().getNumEntitiesOfType(EntityType.CHICKEN));

            Assert.True(SeasonalRules.chickensHatch(Season.SPRING));
            Assert.True(SeasonalRules.chickensHatch(Season.SUMMER));
            Assert.False(SeasonalRules.chickensHatch(Season.AUTUMN));
            Assert.False(SeasonalRules.chickensHatch(Season.WINTER));
        }

        private static void removeChickens(Simulation simulation) {
            foreach (Entity chicken in simulation.getEntityRepository().getEntitiesOfType(EntityType.CHICKEN)) {
                simulation.getEntityRepository().removeEntity(chicken);
            }
        }

        [Fact]
        public void testPawns_LayInFoodFromAutumn() {
            Pawn pawn = new Pawn(Vector3.Zero, "Test", new RandomSource(3));
            Assert.False(SeasonalRules.wantsStores(pawn, Season.SUMMER));
            Assert.True(SeasonalRules.wantsStores(pawn, Season.AUTUMN));
            Assert.True(SeasonalRules.wantsStores(pawn, Season.WINTER));
            pawn.getInventory().addItem(ItemType.APPLE, SeasonalRules.WINTER_FOOD_STORE);
            Assert.False(SeasonalRules.wantsStores(pawn, Season.AUTUMN));
        }

        [Fact]
        public void testAWorld_StaysPopulatedThroughItsFirstWinter() {
            // to the first day of the second spring: a whole year, winter included
            Simulation simulation = createSimulation(42);
            int ticks = (int) ((SeasonCalendar.DAYS_PER_YEAR - TimeOfDay.DefaultStartFraction) * TimeOfDay.DefaultTicksPerDay) + 100;
            int fewestPawns = int.MaxValue;
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(1f / 50f);
                if (i % 3000 == 0 && i > 30000) {
                    fewestPawns = System.Math.Min(fewestPawns, simulation.getEntityRepository().getNumEntitiesOfType(EntityType.PAWN));
                }
            }
            Assert.Equal(Season.SPRING, simulation.getSeasonCalendar().getSeason());
            Assert.Equal(1, simulation.getSeasonCalendar().getYear());
            // measured: 100 pawns (the lag cap) all year and 6 deaths for seed 42
            Assert.True(fewestPawns >= 60, "the population fell to " + fewestPawns);
            Assert.True(simulation.getNumPawnDeaths() < 40, simulation.getNumPawnDeaths() + " pawns died in a year");
            Assert.True(simulation.getEntityRepository().getNumEntitiesOfType(EntityType.TREE) > 200, "spring brings the trees back");
        }
    }
}
