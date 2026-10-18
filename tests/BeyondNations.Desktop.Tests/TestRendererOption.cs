using Xunit;

using beyondnations.desktop;

namespace beyondnationstests.desktop {

    /**
    * --renderer picks the world renderer (#274). The default must stay Silk:
    * the native renderer is opt-in until the parity plan passes.
    */
    public class TestRendererOption {

        [Fact]
        public void testRendererDefaultsToSilk() {
            // run
            GameOptions options = GameOptions.parse(new string[0]);

            // verify
            Assert.Equal(RendererKind.Silk, options.Renderer);
        }

        [Fact]
        public void testRendererNativeIsParsed() {
            // run
            GameOptions options = GameOptions.parse(new[] { "--renderer", "native" });

            // verify
            Assert.Equal(RendererKind.Native, options.Renderer);
        }

        [Fact]
        public void testRendererNameIsCaseInsensitiveAndSilkCanBeNamed() {
            Assert.Equal(RendererKind.Native, GameOptions.parse(new[] { "--renderer", "NATIVE" }).Renderer);
            Assert.Equal(RendererKind.Silk, GameOptions.parse(new[] { "--renderer", "silk" }).Renderer);
        }

        [Fact]
        public void testUsageTextDocumentsTheRendererSwitch() {
            Assert.Contains("--renderer", GameOptions.getUsageText());
        }

        // --- #280 ---
        [Fact]
        public void testNativeLightingDefaultsToSunAndLegacyCanBeAskedFor() {
            Assert.Equal(NativeLighting.Sun, GameOptions.parse(new string[0]).NativeLighting);
            Assert.Equal(NativeLighting.Legacy, GameOptions.parse(new[] { "--native-lighting", "legacy" }).NativeLighting);
            Assert.Equal(NativeLighting.Sun, GameOptions.parse(new[] { "--native-lighting", "SUN" }).NativeLighting);
        }

        [Fact]
        public void testTimeOfDayIsParsedAndUnsetByDefault() {
            Assert.True(GameOptions.parse(new string[0]).TimeOfDay < 0f);
            Assert.Equal(0.75f, GameOptions.parse(new[] { "--time-of-day", "0.75" }).TimeOfDay);
            Assert.Equal(0f, GameOptions.parse(new[] { "--time-of-day", "0" }).TimeOfDay);
        }

        [Fact]
        public void testUsageTextDocumentsTheLightingSwitches() {
            Assert.Contains("--native-lighting", GameOptions.getUsageText());
            Assert.Contains("--time-of-day", GameOptions.getUsageText());
        }

        [Fact]
        public void testTheSilkRendererStaysTheDefaultWhateverTheLighting() {
            Assert.Equal(RendererKind.Silk, GameOptions.parse(new[] { "--native-lighting", "sun", "--time-of-day", "0.5" }).Renderer);
        }
        // --- end #280 ---
    }
}
