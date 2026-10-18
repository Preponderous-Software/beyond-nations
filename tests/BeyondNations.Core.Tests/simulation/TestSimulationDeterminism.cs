using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * #272: a seed is a recording. The same seed run for the same number of
    * ticks gives the same world however fast the ticks ran, so a headless run
    * matches the windowed game and every rerun matches the first.
    */
    public class TestSimulationDeterminism {
        // Seed 42 founds its first nation at tick 2250.
        private const int Seed = 42;
        private const int Ticks = 2400;

        private static Simulation run(int pauseAtTick) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(Seed);
            Simulation simulation = new Simulation(config);
            for (int i = 0; i < Ticks; i++) {
                if (i == pauseAtTick) {
                    // A real second passes mid-run, as it would in the game.
                    Thread.Sleep(1100);
                }
                simulation.fixedUpdate(1f / 50f);
            }
            return simulation;
        }

        private static string fingerprint(Simulation simulation) {
            StringBuilder text = new StringBuilder();
            foreach (Entity entity in simulation.getEntityRepository().getEntities()) {
                text.Append(entity.getType()).Append(' ').Append(entity.getName()).Append(' ')
                    .Append(entity.getPosition().X.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(entity.getPosition().Y.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(entity.getPosition().Z.ToString("R", CultureInfo.InvariantCulture));
                Pawn pawn = entity as Pawn;
                if (pawn != null) {
                    text.Append(' ').Append(pawn.getCurrentBehaviorType()).Append(' ')
                        .Append(pawn.getEnergy().ToString("R", CultureInfo.InvariantCulture));
                }
                text.Append('\n');
            }
            foreach (Nation nation in simulation.getNationRepository().getNations()) {
                text.Append("nation ").Append(nation.getName()).Append('\n');
            }
            return text.ToString();
        }

        [Fact]
        public void testSameSeed_RunAtDifferentRealSpeeds_GivesTheSameWorld() {
            Simulation uninterrupted = run(-1);
            Simulation paused = run(1025);

            Assert.NotEmpty(uninterrupted.getNationRepository().getNations());
            Assert.Equal(Ticks, paused.getTickCounter().getTotalTicks());
            Assert.Equal(fingerprint(uninterrupted), fingerprint(paused));
        }
    }
}
