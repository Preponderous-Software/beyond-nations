using System.Text;
using Xunit;

using beyondnations;
using beyondnations.web;

namespace beyondnationstests {

    /**
    * Browser storage keeps saves gzipped behind a "gz:" marker. What comes
    * back must be the exact bytes that went in, or the save's checksum fails.
    */
    public class TestSaveStorageCodec {

        [Fact]
        public void testASave_RoundTripsByteForByte_AndShrinks() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            for (int i = 0; i < 1500; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            byte[] file = SaveGame.encode(simulation, "test");

            string stored = SaveStorageCodec.encode(file);
            Assert.StartsWith(SaveStorageCodec.GzipMarker, stored);
            Assert.True(stored.Length < file.Length / 3, stored.Length + " characters for " + file.Length + " bytes");
            byte[] back = SaveStorageCodec.decode(stored);
            Assert.Equal(file, back);
            Assert.Equal(1500, SaveGame.decode(back).getTickCounter().getTotalTicks());
        }

        [Fact]
        public void testAValueWithoutTheMarker_IsTakenAsTheSavesText() {
            Assert.Equal(Encoding.UTF8.GetBytes("{\"a\":1}\n{}"), SaveStorageCodec.decode("{\"a\":1}\n{}"));
        }
    }
}
