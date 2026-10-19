using System;
using System.IO;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The committed save fixtures (tests/BeyondNations.Core.Tests/save/fixtures).
    * Each is a save as a released format wrote it, and must load for as long
    * as the game exists -- that is what makes a migration step trustworthy.
    *
    * A fixture is written once, when its format is current, and never
    * regenerated. To write the fixture for a new format version, run the
    * tests once with BN_WRITE_SAVE_FIXTURE=1 and commit the new file.
    */
    public static class SaveFixtures {
        public const int FixtureSeed = 42;
        public const int FixtureTicks = 2400;

        public static string directory() {
            // tests run from bin/<config>/net8.0; the fixtures live beside the source
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 6 && dir != null; i++) {
                string candidate = Path.Combine(dir, "save", "fixtures");
                if (Directory.Exists(candidate)) {
                    return candidate;
                }
                dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
            }
            throw new DirectoryNotFoundException("save/fixtures not found above " + AppContext.BaseDirectory);
        }

        public static string pathFor(int formatVersion) {
            return Path.Combine(directory(), "v" + formatVersion + "-seed" + FixtureSeed + "-tick" + FixtureTicks + ".bnsave");
        }

        /** The world every fixture holds: seed 42 just after its first nation and settlement. */
        public static Simulation fixtureWorld() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(FixtureSeed);
            Simulation simulation = new Simulation(config);
            for (int i = 0; i < FixtureTicks; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            return simulation;
        }
    }

    public class TestWriteSaveFixture {
        [Fact]
        public void writeTheCurrentFormatsFixture_WhenAskedTo() {
            if (System.Environment.GetEnvironmentVariable("BN_WRITE_SAVE_FIXTURE") != "1") {
                return;
            }
            string path = SaveFixtures.pathFor(SaveGame.CurrentFormatVersion);
            Assert.False(File.Exists(path), path + " exists; fixtures are never regenerated");
            File.WriteAllBytes(path, SaveGame.encode(SaveFixtures.fixtureWorld(), "0.3.0-alpha",
                new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc)));
        }
    }
}
