using Xunit;

using beyondnations;
using beyondnations.desktop;

namespace beyondnationstests.desktop {

    /**
    * How the host reads its arguments, and what it tells a player it accepts.
    *
    * The parsing half needs no window, and neither does the usage text now that
    * it is returned rather than written straight to the console. That matters:
    * --no-labels, --debug-mode, --start-screen and --help itself had all gone
    * missing from the usage text unnoticed, because nothing could read it back
    * (#249).
    *
    * Arguments that fail to parse are deliberately not covered here. The
    * failure path calls System.Environment.Exit, which would take the test
    * runner down with it.
    */
    public class TestGameOptions {

        [Fact]
        public void testDefaultsNeedNoArguments() {
            // run
            GameOptions options = GameOptions.parse(new string[] { });

            // verify
            Assert.Equal(1280, options.Width);
            Assert.Equal(720, options.Height);
            Assert.Equal(50, options.TicksPerSecond);
            Assert.False(options.FirstPerson);
            Assert.Equal(ScreenType.TITLE, options.StartScreen);
        }

        [Fact]
        public void testSeedTakesANumberOrText() {
            GameOptions number = GameOptions.parse(new string[] { "--seed", "42" });
            Assert.Equal(42, number.Seed);
            Assert.Equal("42", number.SeedText);

            GameOptions text = GameOptions.parse(new string[] { "--seed", "Ironvale" });
            Assert.Equal(WorldSeed.fromText("Ironvale"), text.Seed);
            Assert.Equal(1450162717, text.Seed);
            Assert.Equal("Ironvale", text.SeedText);

            GameOptions none = GameOptions.parse(new string[] { });
            Assert.Equal(0, none.Seed);
            Assert.Null(none.SeedText);
        }

        [Fact]
        public void testTheNewWorldAndShareScreensCanBeOpenedOn() {
            Assert.Equal(ScreenType.NEW_WORLD, GameOptions.parse(new string[] { "--start-screen", "new-world" }).StartScreen);
            Assert.Equal(ScreenType.SHARE, GameOptions.parse(new string[] { "--start-screen", "share" }).StartScreen);
        }

        [Fact]
        public void testFirstPersonOpensInFirstPerson() {
            // run
            GameOptions options = GameOptions.parse(new string[] { "--first-person" });

            // verify
            Assert.True(options.FirstPerson);
        }

        [Fact]
        public void testStartScreenAcceptsTheInventoryScreen() {
            // run
            GameOptions options = GameOptions.parse(new string[] { "--start-screen", "inventory" });

            // verify
            Assert.Equal(ScreenType.INVENTORY, options.StartScreen);
        }

        [Fact]
        public void testStartScreenAcceptsTheNationsScreen_AndStageDiplomacyIsAFlag() {
            GameOptions options = GameOptions.parse(new string[] { "--start-screen", "nations", "--stage-diplomacy" });

            Assert.Equal(ScreenType.NATIONS, options.StartScreen);
            Assert.True(options.StageDiplomacy);
            Assert.False(GameOptions.parse(new string[] { }).StageDiplomacy);
        }

        [Fact]
        public void testStartScreenAcceptsTheCraftingScreen() {
            GameOptions options = GameOptions.parse(new string[] { "--start-screen", "crafting" });
            Assert.Equal(ScreenType.CRAFTING, options.StartScreen);
        }

        [Fact]
        public void testNoUsageReportingTurnsReportingOffForTheRun() {
            // run
            GameOptions defaults = GameOptions.parse(new string[] { });
            GameOptions options = GameOptions.parse(new string[] { "--no-usage-reporting" });

            // verify
            Assert.False(defaults.NoUsageReporting);
            Assert.True(options.NoUsageReporting);
        }

        [Fact]
        public void testUsageTextNamesEverySwitchThatParseAccepts() {
            // prepare: every case handled by GameOptions.parse, in the order it
            // handles them. A switch added to parse and forgotten in the usage
            // text is the defect this pins down.
            string[] switches = {
                "--width", "--height", "--ticks-per-second", "--exit-after-frames",
                "--screenshot-after-frames", "--seed", "--no-vsync", "--smoke-resize",
                "--render-stats", "--render-distance", "--no-culling", "--no-labels",
                "--debug-mode", "--first-person", "--no-usage-reporting", "--start-screen", "--ui-scale"
            };

            // run
            string usage = GameOptions.getUsageText();

            // verify
            foreach (string option in switches) {
                Assert.Contains(option, usage);
            }
            // Asserted whole, since "-h" on its own is a substring of --height
            // and would pass without the alias being documented at all.
            Assert.Contains("--help, -h", usage);
        }
    }
}
