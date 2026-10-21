using Xunit;

using beyondnations;
using beyondnations.desktop;

namespace beyondnationstests.desktop {

    /**
    * The world is drawn by Strata only. --renderer and --native-lighting
    * (the switch between the Silk and Strata renderers, and the legacy light
    * the parity check used) are gone; --time-of-day (#280) remains.
    */
    public class TestTimeOfDayOption {

        [Fact]
        public void testTimeOfDayIsParsedAndUnsetByDefault() {
            Assert.True(GameOptions.parse(new string[0]).TimeOfDay < 0f);
            Assert.Equal(0.75f, GameOptions.parse(new[] { "--time-of-day", "0.75" }).TimeOfDay);
            Assert.Equal(0f, GameOptions.parse(new[] { "--time-of-day", "0" }).TimeOfDay);
        }

        [Fact]
        public void testSeasonIsParsedAndUnsetByDefault() {
            Assert.Null(GameOptions.parse(new string[0]).StartSeason);
            Assert.Equal(Season.WINTER, GameOptions.parse(new[] { "--season", "winter" }).StartSeason);
            Assert.Equal(Season.AUTUMN, GameOptions.parse(new[] { "--season", "Autumn" }).StartSeason);
        }

        [Fact]
        public void testSeasonOption_MovesANewWorldsClockToThatSeason() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            float fraction = simulation.getTimeOfDay().getFraction();
            simulation.getSeasonCalendar().advanceClockTo(simulation.getTickCounter(), Season.WINTER);
            Assert.True(simulation.getSeasonCalendar().isWinter());
            Assert.Equal(0, simulation.getSeasonCalendar().getDayOfSeason());
            Assert.Equal(fraction, simulation.getTimeOfDay().getFraction(), 4);
        }

        [Fact]
        public void testUsageTextDocumentsTheTimeOfDay() {
            Assert.Contains("--time-of-day", GameOptions.getUsageText());
        }

        [Fact]
        public void testUsageTextNoLongerOffersARendererChoice() {
            Assert.DoesNotContain("--renderer", GameOptions.getUsageText());
            Assert.DoesNotContain("--native-lighting", GameOptions.getUsageText());
        }
    }
}
