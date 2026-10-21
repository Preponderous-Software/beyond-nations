using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The run recorder behind Share > Export replay (docs/SHARING.md): it
    * must sample on its interval, stay bounded on a long run, record the same
    * thing for the same seed, and never change the world it watches.
    */
    public class TestRunRecorder {
        // Seed 42 founds its first nation at tick 2000 (TestHistoryExporter).
        private const int Seed = 42;
        private const int Ticks = 2400;

        private static Simulation world(int seed = Seed) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            return new Simulation(config);
        }

        private static void run(Simulation simulation, int ticks, RunRecorder recorder) {
            float dt = 1f / simulation.getGameConfig().getTicksPerSecond();
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(dt);
                recorder?.observe(simulation);
            }
        }

        [Fact]
        public void testSamplesOnItsIntervalFromTheFirstTick() {
            Simulation simulation = world();
            RunRecorder recorder = new RunRecorder(100, 1000);
            recorder.begin(simulation);
            run(simulation, 1000, recorder);

            List<int> ticks = recorder.getSamples().Select(s => s.tick).ToList();
            Assert.Equal(0, ticks[0]);
            Assert.Equal(Enumerable.Range(0, 11).Select(i => i * 100).ToList(), ticks);
            // the player is always there
            Assert.All(recorder.getSamples(), s => Assert.Equal(1, s.count(RunRecorder.PlayerSet)));
        }

        [Fact]
        public void testALongRunStaysWithinMaxSamplesAndStillReachesNow() {
            Simulation simulation = world();
            RunRecorder recorder = new RunRecorder(10, 20);
            recorder.begin(simulation);
            run(simulation, 1000, recorder);
            recorder.recordNow(simulation);

            Assert.True(recorder.getSampleCount() <= 21, "kept " + recorder.getSampleCount());
            Assert.True(recorder.getThinnings() > 0);
            Assert.True(recorder.getInterval() > 10);
            Assert.Equal(0, recorder.getFirstTick());
            Assert.Equal(1000, recorder.getLastTick());
            List<int> ticks = recorder.getSamples().Select(s => s.tick).ToList();
            Assert.Equal(ticks.OrderBy(t => t).Distinct().ToList(), ticks);
        }

        [Fact]
        public void testTheSameSeedRecordsTheSameRun() {
            Simulation a = world();
            Simulation b = world();
            RunRecorder ra = new RunRecorder(200, 1000);
            RunRecorder rb = new RunRecorder(200, 1000);
            ra.begin(a);
            rb.begin(b);
            run(a, Ticks, ra);
            run(b, Ticks, rb);

            Assert.Equal(ra.getSamples().Select(s => s.stateHash), rb.getSamples().Select(s => s.stateHash));
            Assert.Equal(ReplayUsdaWriter.write(ra, a, "test"), ReplayUsdaWriter.write(rb, b, "test"));
            // and it saw the world change: a nation was founded along the way
            Assert.True(ra.getSamples().Last().nations > ra.getSamples().First().nations
                || ra.getSamples().Last().count(RunRecorder.Settlements) > 0);
        }

        [Fact]
        public void testRecordingNeverChangesTheWorld() {
            Simulation watched = world();
            Simulation alone = world();
            RunRecorder recorder = new RunRecorder(50, 1000);
            recorder.begin(watched);
            run(watched, Ticks, recorder);
            ReplayUsdaWriter.write(recorder, watched, "test");
            run(alone, Ticks, null);

            Assert.Equal(positions(alone), positions(watched));
            Assert.Equal(alone.getNationRepository().getNumberOfNations(), watched.getNationRepository().getNumberOfNations());
            Assert.Equal(alone.getRandom().range(0, 1000000), watched.getRandom().range(0, 1000000));
        }

        private static List<Vector3> positions(Simulation simulation) {
            List<Vector3> list = new List<Vector3>();
            EntityRepository repository = simulation.getEntityRepository();
            for (int i = 0; i < repository.getEntityCount(); i++) {
                list.Add(repository.getEntityAt(i).getPosition());
            }
            return list;
        }

        [Fact]
        public void testBeginStartsAFreshRecordingAtTheLoadedTick() {
            Simulation simulation = world();
            RunRecorder recorder = new RunRecorder(100, 1000);
            recorder.begin(simulation);
            run(simulation, 500, recorder);
            Assert.Equal(6, recorder.getSampleCount());

            recorder.begin(simulation);
            Assert.Equal(1, recorder.getSampleCount());
            Assert.Equal(500, recorder.getFirstTick());
        }

        [Fact]
        public void testRecordNowDoesNotRepeatATick() {
            Simulation simulation = world();
            RunRecorder recorder = new RunRecorder(100, 1000);
            recorder.begin(simulation);
            recorder.recordNow(simulation);
            recorder.recordNow(simulation);
            Assert.Equal(1, recorder.getSampleCount());
        }

        [Fact]
        public void testPositionsAreZUp() {
            Vector3 p = RunRecorder.toReplaySpace(new Vector3(1f, 2f, 3f), 10f);
            Assert.Equal(new Vector3(1f, -3f, 7f), p);
        }
    }
}
