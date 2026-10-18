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
    }
}
