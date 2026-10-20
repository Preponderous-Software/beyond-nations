using System.Collections.Generic;
using System.Linq;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Share > Export replay writes a usd-replay/1 layer as text. These check
    * it against the contract as far as text allows (ReplayChecks); the
    * History replay CI job runs usd-recorder's own validator on one.
    */
    public class TestReplayUsdaWriter {

        private static Simulation world(int seed) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            return new Simulation(config);
        }

        private static string record(int seed, int ticks, int interval, out Simulation simulation, out RunRecorder recorder) {
            simulation = world(seed);
            recorder = new RunRecorder(interval, 1000);
            recorder.begin(simulation);
            float dt = 1f / simulation.getGameConfig().getTicksPerSecond();
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(dt);
                recorder.observe(simulation);
            }
            return ReplayUsdaWriter.write(recorder, simulation, "0.3.0-test");
        }

        [Fact]
        public void testAnExportedRunHoldsToTheReplayContract() {
            string usda = record(42, 2400, 200, out Simulation simulation, out RunRecorder recorder);

            List<string> problems = ReplayChecks.problems(usda);
            Assert.True(problems.Count == 0, string.Join("\n", problems));

            ReplayChecks.Parsed parsed = ReplayChecks.parse(usda);
            Assert.Equal("usd-replay/1", parsed.meta["schema"]);
            Assert.Equal("beyond-nations", parsed.meta["sim"]);
            Assert.Equal("0.3.0-test", parsed.meta["simVersion"]);
            Assert.Equal("42", parsed.meta["seed"]);
            Assert.Equal("50", parsed.meta["tickRate"]);
            Assert.Equal(new[] { "pawns", "settlements", "player", "chickens" }, parsed.sets);
            Assert.Equal(new[] { "pawns", "settlements" }, parsed.vertexColoured);
            Assert.Equal(13, parsed.samples["Replay.replay:stateHash"].Count); // ticks 0, 200, ..., 2400
            Assert.Equal("0", parsed.stage["startTimeCode"]);
            Assert.Equal("2400", parsed.stage["endTimeCode"]);
            Assert.Equal(recorder.getSampleCount(), parsed.samples["player.ids"].Count);
            Assert.Contains("trees", parsed.scene);
        }

        [Fact]
        public void testTheReplayEndsWhereThePlayerIs() {
            Simulation simulation = world(7);
            RunRecorder recorder = new RunRecorder(1000, 1000);
            recorder.begin(simulation);
            float dt = 1f / 50f;
            for (int i = 0; i < 333; i++) {
                simulation.fixedUpdate(dt);
                recorder.observe(simulation);
            }
            string usda = ReplayUsdaWriter.write(recorder, simulation, "x");
            ReplayChecks.Parsed parsed = ReplayChecks.parse(usda);
            Assert.Equal(new[] { 0, 333 }, parsed.samples["Replay.replay:stateHash"].Keys.ToArray());
            Assert.Empty(ReplayChecks.problems(usda));
        }

        [Fact]
        public void testANegativeSeedIsWrittenAsDigits() {
            Simulation simulation = world(-5);
            RunRecorder recorder = new RunRecorder();
            recorder.begin(simulation);
            string usda = ReplayUsdaWriter.write(recorder, simulation, "x");

            ReplayChecks.Parsed parsed = ReplayChecks.parse(usda);
            Assert.Equal("4294967291", parsed.meta["seed"]);
            Assert.Contains("int worldSeed = -5", usda);
            Assert.Empty(ReplayChecks.problems(usda));
        }

        [Fact]
        public void testAFreshWorldWithOneSampleIsStillAReplay() {
            Simulation simulation = world(78);
            RunRecorder recorder = new RunRecorder();
            recorder.begin(simulation);
            string usda = ReplayUsdaWriter.write(recorder, simulation, "x");
            Assert.Empty(ReplayChecks.problems(usda));
        }

        [Fact]
        public void testStringsAreQuotedSafely() {
            Assert.Equal("\"a \\\"b\\\" \\\\ c\\nd\"", ReplayUsdaWriter.quote("a \"b\" \\ c\nd"));
        }

        [Fact]
        public void testTheFileIsNamedForItsSeedAndTick() {
            Assert.Equal("beyond-nations-seed-42-tick-9000.usda", ReplayUsdaWriter.fileName(42, 9000));
            Assert.Equal("beyond-nations-seed-m5-tick-1.usda", ReplayUsdaWriter.fileName(-5, 1));
        }

        [Fact]
        public void testColoursAreLinear() {
            Assert.Equal(0.0, ReplayUsdaWriter.srgbToLinear(0.0), 6);
            Assert.Equal(1.0, ReplayUsdaWriter.srgbToLinear(1.0), 6);
            Assert.Equal(0.214041, ReplayUsdaWriter.srgbToLinear(0.5), 5);
        }
    }
}
