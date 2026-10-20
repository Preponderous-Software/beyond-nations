using Xunit;

using beyondnations.historyexport;

namespace beyondnations.historyexport.tests {

    /**
    * --game-replay: the replay Share > Export replay writes, for a seeded
    * world run headless. CI validates one with usd-recorder; these hold it
    * to being reproducible and to the shape that validator reads.
    */
    public class TestGameReplay {

        [Fact]
        public void testTheSameSeedGivesTheSameReplay() {
            string first = GameReplay.export(42, 2400, 50, 200, "test");
            string second = GameReplay.export(42, 2400, 50, 200, "test");
            Assert.Equal(first, second);
            Assert.NotEqual(first, GameReplay.export(78, 2400, 50, 200, "test"));
        }

        [Fact]
        public void testItIsAUsdReplayLayer() {
            string usda = GameReplay.export(42, 1000, 50, 250, "test");
            Assert.StartsWith("#usda 1.0\n", usda);
            Assert.Contains("string schema = \"usd-replay/1\"", usda);
            Assert.Contains("defaultPrim = \"Replay\"", usda);
            Assert.Contains("upAxis = \"Z\"", usda);
            Assert.Contains("startTimeCode = 0\n", usda);
            Assert.Contains("endTimeCode = 1000\n", usda);
            Assert.Contains("def Points \"pawns\"", usda);
        }
    }
}
