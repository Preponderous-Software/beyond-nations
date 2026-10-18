using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * #272: a sapling grows after its grow time in simulated seconds (ticks
    * divided by ticks per second), not after that many real seconds.
    */
    public class TestSapling {
        private readonly RandomSource random = new RandomSource(20260816);

        [Fact]
        public void testInstantiation() {
            Sapling sapling = new Sapling(new Vector3(1, 2, 3), 3, random, new TickCounter());

            Assert.Equal(EntityType.SAPLING, sapling.getType());
            Assert.Equal(3, sapling.getHeight());
            Assert.Equal(new Vector3(1, 2, 3), sapling.getPosition());
            Assert.InRange(sapling.getGrowTime(), 60, 599);
            Assert.False(sapling.isGrown());
        }

        [Fact]
        public void testGrowsOnceItsGrowTimeInSimulatedSecondsHasPassed() {
            TickCounter tickCounter = new TickCounter(50);
            Sapling sapling = new Sapling(Vector3.Zero, 3, random, tickCounter);
            int growTicks = sapling.getGrowTime() * 50;

            // Far more ticks than real seconds pass here, so on the wall clock
            // it would never grow; in simulated time it grows on schedule.
            for (int i = 0; i < growTicks; i++) {
                tickCounter.increment();
            }
            Assert.False(sapling.isGrown());

            tickCounter.increment();
            Assert.True(sapling.isGrown());
        }

        [Fact]
        public void testGrowthIsMeasuredFromThePlantingTick() {
            TickCounter tickCounter = new TickCounter(10);
            for (int i = 0; i < 1000; i++) {
                tickCounter.increment();
            }
            Sapling sapling = new Sapling(Vector3.Zero, 3, random, tickCounter);

            for (int i = 0; i < sapling.getGrowTime() * 10; i++) {
                tickCounter.increment();
            }
            Assert.False(sapling.isGrown());

            tickCounter.increment();
            Assert.True(sapling.isGrown());
        }

        [Fact]
        public void testSimulationReplacesAGrownSaplingWithATree() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(7); // no pawns, so nothing else plants
            Simulation simulation = new Simulation(config);
            Sapling sapling = new Sapling(new Vector3(0, 1, 0), 3, simulation.getRandom(), simulation.getTickCounter());
            simulation.getEntityRepository().addEntity(sapling);
            int growTicks = sapling.getGrowTime() * config.getTicksPerSecond();

            int tick = 0;
            while (simulation.getEntityRepository().getEntity(sapling.getId()) != null && tick < growTicks + 10) {
                simulation.fixedUpdate(1f / 50f);
                tick++;
            }

            Assert.Null(simulation.getEntityRepository().getEntity(sapling.getId()));
            Assert.InRange(tick, growTicks, growTicks + 2);
        }
    }
}
