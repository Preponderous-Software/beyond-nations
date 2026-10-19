using System.Collections.Generic;
using System.Numerics;
using Xunit;
using beyondnations;

namespace beyondnationstests {

    /**
    * Stone was every settlement's bottleneck: one or two rocks a chunk, three
    * stone each, and never coming back once mined. These pin the density,
    * the yield and the regrowth that replaced that.
    */
    public class TestRockSupply {

        private static Simulation createSimulation(int seed) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            return new Simulation(config);
        }

        [Fact]
        public void testFreshWorld_HasAtLeastTwoRocksPerChunk() {
            Simulation simulation = createSimulation(42);
            simulation.fixedUpdate(1f / 50f);

            int rocks = simulation.getEntityRepository().getNumEntitiesOfType(EntityType.ROCK);
            int chunks = simulation.getEnvironment().getNumChunks();
            Assert.True(rocks >= 2 * chunks, rocks + " rocks in " + chunks + " chunks");
        }

        [Fact]
        public void testRegrowth_AddsARockOnTheInterval_WhenBelowTarget() {
            Simulation simulation = createSimulation(42);
            simulation.fixedUpdate(1f / 50f);
            EntityRepository entities = simulation.getEntityRepository();
            foreach (Entity rock in entities.getEntitiesOfType(EntityType.ROCK)) {
                entities.removeEntity(rock);
            }

            Assert.False(simulation.getWorldGenerator().regrowRocks(WorldGenerator.ROCK_REGROW_INTERVAL_TICKS + 1));
            bool added = false;
            // a regrowth attempt can land on an occupied location and skip, so allow a few
            for (int attempt = 1; attempt <= 20 && !added; attempt++) {
                added = simulation.getWorldGenerator().regrowRocks(attempt * WorldGenerator.ROCK_REGROW_INTERVAL_TICKS);
            }
            Assert.True(added);
            Assert.Equal(1, entities.getNumEntitiesOfType(EntityType.ROCK));
        }

        [Fact]
        public void testRegrowth_StopsAtTheTarget() {
            Simulation simulation = createSimulation(42);
            simulation.fixedUpdate(1f / 50f);
            EntityRepository entities = simulation.getEntityRepository();
            int target = simulation.getEnvironment().getNumChunks() * WorldGenerator.TARGET_ROCKS_PER_CHUNK;
            while (entities.getNumEntitiesOfType(EntityType.ROCK) < target) {
                entities.addEntity(new Rock(new Vector3(9999, 1, entities.getNumEntitiesOfType(EntityType.ROCK))));
            }

            for (int attempt = 1; attempt <= 20; attempt++) {
                Assert.False(simulation.getWorldGenerator().regrowRocks(attempt * WorldGenerator.ROCK_REGROW_INTERVAL_TICKS));
            }
        }

        [Fact]
        public void testMinedOutWorld_RecoversStoneOverADay() {
            Simulation simulation = createSimulation(42);
            simulation.fixedUpdate(1f / 50f);
            EntityRepository entities = simulation.getEntityRepository();
            foreach (Entity rock in entities.getEntitiesOfType(EntityType.ROCK)) {
                entities.removeEntity(rock);
            }

            for (int i = 0; i < TimeOfDay.DefaultTicksPerDay; i++) {
                simulation.fixedUpdate(1f / 50f);
            }

            Assert.True(entities.getNumEntitiesOfType(EntityType.ROCK) > 0, "no rock came back in a day");
        }

        [Fact]
        public void testSettlerBetweenTwoSettlements_HeadsOutOfTheCluster_NotTowardTheOther() {
            // A settler just east of A, with B further east. Walking away from
            // only the nearest (A) would send it east, toward B; the old
            // behaviour then turned round at B and never got clear.
            List<Entity> settlements = new List<Entity> { settlementAt(0, 0), settlementAt(200, 0), settlementAt(100, -150) };
            Vector3 away = PawnBehaviorExecutor.computeAwayFromSettlements(new Vector3(100, 1, 10), settlements);

            Assert.True(away.Z > 0.9f, "expected to head out (+z) away from all three, got " + away);
        }

        [Fact]
        public void testSeekingAwayFromOneSettlement_GoesStraightAway() {
            List<Entity> settlements = new List<Entity> { settlementAt(0, 0) };
            Vector3 away = PawnBehaviorExecutor.computeAwayFromSettlements(new Vector3(10, 1, 0), settlements);

            Assert.Equal(1f, away.X, 3);
            Assert.Equal(0f, away.Z, 3);
        }

        [Fact]
        public void testSeekingWithNoSettlements_StandsStill() {
            Assert.Equal(Vector3.Zero, PawnBehaviorExecutor.computeAwayFromSettlements(Vector3.Zero, new List<Entity>()));
        }

        private static Settlement settlementAt(float x, float z) {
            return new Settlement(new Vector3(x, 1, z), new NationId(), Rgba.White, "S", new RandomSource(1));
        }
    }
}
