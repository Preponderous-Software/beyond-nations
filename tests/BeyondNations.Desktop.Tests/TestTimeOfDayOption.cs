using Xunit;

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
