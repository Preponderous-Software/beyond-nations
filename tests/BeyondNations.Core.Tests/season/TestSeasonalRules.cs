using System.Collections.Generic;
using System.Numerics;
using Xunit;
using beyondnations;

namespace beyondnationstests {

    /**
    * What the seasons do: autumn's double harvest, winter's empty farms, slow
    * saplings and cold, chickens hatching in spring, pawns laying in food,
    * apples spoiling and bare trees -- and that together they make winter lean.
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
        public void testApples_SpoilOnlyBeyondTheKeptStore() {
            Assert.Equal(0, SeasonalRules.applesSpoiling(0, SeasonalRules.PAWN_APPLES_KEPT));
            Assert.Equal(0, SeasonalRules.applesSpoiling(SeasonalRules.PAWN_APPLES_KEPT, SeasonalRules.PAWN_APPLES_KEPT));
            // a single apple over the store rots: the share is rounded up
            Assert.Equal(1, SeasonalRules.applesSpoiling(SeasonalRules.PAWN_APPLES_KEPT + 1, SeasonalRules.PAWN_APPLES_KEPT));
            Assert.Equal(10, SeasonalRules.applesSpoiling(SeasonalRules.PAWN_APPLES_KEPT + 10 * SeasonalRules.SPOIL_DIVISOR, SeasonalRules.PAWN_APPLES_KEPT));
            // the store laid in for winter never rots
            Assert.True(SeasonalRules.PAWN_APPLES_KEPT >= SeasonalRules.WINTER_FOOD_STORE);
        }

        [Fact]
        public void testSpoilage_RotsAPawnsHoardAndAStallsStock_ButNotThePlayers() {
            Simulation simulation = createSimulation(42);
            removeAllButThePlayer(simulation);
            Pawn hoarder = new Pawn(new Vector3(5000, 1.5f, 5000), "Hoarder", new RandomSource(3));
            hoarder.getInventory().addItem(ItemType.APPLE, 200);
            Pawn frugal = new Pawn(new Vector3(5010, 1.5f, 5000), "Frugal", new RandomSource(4));
            frugal.getInventory().addItem(ItemType.APPLE, SeasonalRules.PAWN_APPLES_KEPT);
            simulation.getEntityRepository().addEntity(hoarder);
            simulation.getEntityRepository().addEntity(frugal);
            Settlement settlement = new Settlement(new Vector3(6000, 0, 6000), new NationId(), Rgba.Red, "Testland", simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            settlement.getMarket().createStall();
            settlement.getMarket().createStall();
            Stall pawnsStall = settlement.getMarket().getStalls()[0];
            Stall playersStall = settlement.getMarket().getStalls()[1];
            pawnsStall.setOwnerId(hoarder.getId());
            playersStall.setOwnerId(simulation.getPlayer().getId());
            pawnsStall.getInventory().addItem(ItemType.APPLE, 200);
            playersStall.getInventory().addItem(ItemType.APPLE, 200);
            Player player = simulation.getPlayer();
            int playersApples = player.getInventory().getNumItems(ItemType.APPLE) + 200;
            player.getInventory().addItem(ItemType.APPLE, 200);

            // three minutes of rounds
            for (int round = 0; round < 6; round++) {
                simulation.getSeasonalWorld().spoilApples();
            }

            int hoard = hoarder.getInventory().getNumItems(ItemType.APPLE);
            Assert.True(hoard < 100, "a hoard of 200 should have rotted to under half, not " + hoard);
            Assert.True(hoard > SeasonalRules.PAWN_APPLES_KEPT);
            Assert.Equal(SeasonalRules.PAWN_APPLES_KEPT, frugal.getInventory().getNumItems(ItemType.APPLE));
            Assert.True(pawnsStall.getInventory().getNumItems(ItemType.APPLE) < 100);
            Assert.Equal(200, playersStall.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(playersApples, player.getInventory().getNumItems(ItemType.APPLE));

            // it happens on its own every APPLE_SPOIL_INTERVAL_TICKS
            simulation.getSeasonalWorld().update(SeasonalRules.APPLE_SPOIL_INTERVAL_TICKS);
            Assert.True(hoarder.getInventory().getNumItems(ItemType.APPLE) < hoard);
        }

        private static void removeAllButThePlayer(Simulation simulation) {
            foreach (Entity entity in simulation.getEntityRepository().getEntities()) {
                if (entity.getType() != EntityType.PLAYER) {
                    simulation.getEntityRepository().removeEntity(entity);
                }
            }
        }

        [Fact]
        public void testTrees_AreBareForPawnsInWinter() {
            Assert.True(SeasonalRules.treesBearFruit(Season.SPRING));
            Assert.True(SeasonalRules.treesBearFruit(Season.SUMMER));
            Assert.True(SeasonalRules.treesBearFruit(Season.AUTUMN));
            Assert.False(SeasonalRules.treesBearFruit(Season.WINTER));
        }

        [Fact]
        public void testAWorld_GoesHungryInWinter_ButStaysPopulated() {
            // to the first day of the second spring: a whole year, winter included
            Simulation simulation = createSimulation(42);
            int ticks = (int) ((SeasonCalendar.DAYS_PER_YEAR - TimeOfDay.DefaultStartFraction) * TimeOfDay.DefaultTicksPerDay) + 100;
            int fewestPawns = int.MaxValue;
            float mostFoodPerPawn = 0;
            float foodPerPawnAtWinter = -1;
            int stallFoodAtWinter = -1;
            float foodPerPawnLateWinter = -1;
            int stallFoodLateWinter = -1;
            float emptyHandedLateWinter = -1;
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(1f / 50f);
                if (i % 3000 != 0 || i <= 30000) {
                    continue;
                }
                List<Entity> pawns = simulation.getEntityRepository().getEntitiesOfType(EntityType.PAWN);
                fewestPawns = System.Math.Min(fewestPawns, pawns.Count);
                int food = 0;
                int emptyHanded = 0;
                foreach (Entity pawn in pawns) {
                    int carried = SeasonalRules.countFood(pawn.getInventory());
                    food += carried;
                    if (carried == 0) {
                        emptyHanded++;
                    }
                }
                float foodPerPawn = (float) food / pawns.Count;
                mostFoodPerPawn = System.Math.Max(mostFoodPerPawn, foodPerPawn);
                SeasonCalendar calendar = simulation.getSeasonCalendar();
                if (calendar.getSeason() != Season.WINTER) {
                    continue;
                }
                if (foodPerPawnAtWinter < 0) {
                    foodPerPawnAtWinter = foodPerPawn;
                    stallFoodAtWinter = countStallFood(simulation);
                }
                if (calendar.getSeasonProgress() >= 0.75f) {
                    // the last quarter of winter: keep the latest sample
                    foodPerPawnLateWinter = foodPerPawn;
                    stallFoodLateWinter = countStallFood(simulation);
                    emptyHandedLateWinter = (float) emptyHanded / pawns.Count;
                }
            }
            Assert.Equal(Season.SPRING, simulation.getSeasonCalendar().getSeason());
            Assert.Equal(1, simulation.getSeasonCalendar().getYear());

            // measured for seed 42: at most ~23 food a pawn (in autumn; over 200 before
            // apples spoiled), ~14 a pawn as winter comes and under 1 by its end, with
            // two thirds of pawns empty-handed and the markets down from ~1,600 to ~200
            Assert.True(mostFoodPerPawn < 40, "pawns hoarded " + mostFoodPerPawn + " food each");
            Assert.True(foodPerPawnAtWinter > 0 && foodPerPawnLateWinter >= 0, "winter was sampled");
            Assert.True(foodPerPawnLateWinter < foodPerPawnAtWinter / 4,
                "pawns' stores went from " + foodPerPawnAtWinter + " to " + foodPerPawnLateWinter + " a pawn over winter");
            Assert.True(emptyHandedLateWinter >= 0.5f, "only " + emptyHandedLateWinter + " of pawns ran out of food by late winter");
            Assert.True(stallFoodLateWinter < stallFoodAtWinter / 2,
                "the markets went from " + stallFoodAtWinter + " to " + stallFoodLateWinter + " food over winter");

            // and still survivable: measured 100 pawns (the lag cap) all year and 12 deaths
            // (5 before a pawn's energy stopped at a full bar, so a well-fed pawn no longer
            // carries up to 34 spare energy into winter)
            Assert.True(fewestPawns >= 60, "the population fell to " + fewestPawns);
            Assert.True(simulation.getNumPawnDeaths() < 40, simulation.getNumPawnDeaths() + " pawns died in a year");
            Assert.True(simulation.getEntityRepository().getNumEntitiesOfType(EntityType.TREE) > 200, "spring brings the trees back");

            // and spring recovers rather than starving on: over the first half of the
            // second spring, measured 2 deaths (0 before pawn energy was capped at a
            // full bar; 6 before starving pawns foraged instead of walking back and
            // forth to a market that could not feed them)
            int deathsAtSpring = simulation.getNumPawnDeaths();
            for (int i = 0; i < TimeOfDay.DefaultTicksPerDay; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            Assert.Equal(Season.SPRING, simulation.getSeasonCalendar().getSeason());
            int springDeaths = simulation.getNumPawnDeaths() - deathsAtSpring;
            Assert.True(springDeaths <= 2, springDeaths + " pawns died in the first half of spring");
        }

        private static int countStallFood(Simulation simulation) {
            int food = 0;
            foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT)) {
                foreach (Stall stall in ((Settlement) entity).getMarket().getStalls()) {
                    food += SeasonalRules.countFood(stall.getInventory());
                }
            }
            return food;
        }
    }
}
