
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestAppDataPaths {

        [Fact]
        public void testLinuxUsesXdgDataHomeWhenSet() {
            // run
            string result = AppDataPaths.resolveBaseDirectory(
                isLinux: true,
                applicationDataFolder: "/home/someone/.config",
                xdgDataHome: "/custom/data",
                homeDirectory: "/home/someone");

            // check
            Assert.Equal(System.IO.Path.Combine("/custom/data", "BeyondNations"), result);
        }

        [Fact]
        public void testLinuxFallsBackToDotLocalShareWhenXdgUnset() {
            // run
            string result = AppDataPaths.resolveBaseDirectory(
                isLinux: true,
                applicationDataFolder: "/home/someone/.config",
                xdgDataHome: null,
                homeDirectory: "/home/someone");

            // check
            string expected = System.IO.Path.Combine("/home/someone", ".local", "share", "BeyondNations");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void testLinuxFallsBackToDotLocalShareWhenXdgEmpty() {
            // run
            string result = AppDataPaths.resolveBaseDirectory(
                isLinux: true,
                applicationDataFolder: "/home/someone/.config",
                xdgDataHome: "",
                homeDirectory: "/home/someone");

            // check
            string expected = System.IO.Path.Combine("/home/someone", ".local", "share", "BeyondNations");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void testNonLinuxUsesApplicationDataFolder() {
            // run: simulates Windows, where ApplicationData is %AppData% (Roaming)
            string result = AppDataPaths.resolveBaseDirectory(
                isLinux: false,
                applicationDataFolder: @"C:\Users\someone\AppData\Roaming",
                xdgDataHome: null,
                homeDirectory: @"C:\Users\someone");

            // check
            string expected = System.IO.Path.Combine(@"C:\Users\someone\AppData\Roaming", "BeyondNations");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void testNonLinuxIgnoresXdgDataHomeEvenIfSet() {
            // run: simulates macOS, where .NET's ApplicationData mapping should win,
            // not an incidentally-set XDG_DATA_HOME
            string result = AppDataPaths.resolveBaseDirectory(
                isLinux: false,
                applicationDataFolder: "/Users/someone/.config",
                xdgDataHome: "/should/be/ignored",
                homeDirectory: "/Users/someone");

            // check
            string expected = System.IO.Path.Combine("/Users/someone/.config", "BeyondNations");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void testResultContainsNoBackslashesOnNonWindowsHost() {
            // run
            string result = AppDataPaths.resolveBaseDirectory(
                isLinux: true,
                applicationDataFolder: "/home/someone/.config",
                xdgDataHome: null,
                homeDirectory: "/home/someone");

            // check: this test runs on Linux/macOS CI, so Path.Combine must not
            // introduce a hardcoded backslash regardless of platform branch taken
            Assert.DoesNotContain("\\", result);
            Assert.DoesNotContain(":", result);
        }

        [Fact]
        public void testGetBaseDirectoryReturnsNonEmptyAbsolutePath() {
            // run
            string result = AppDataPaths.getBaseDirectory();

            // check
            Assert.False(string.IsNullOrWhiteSpace(result));
            Assert.True(System.IO.Path.IsPathRooted(result));
            Assert.EndsWith("BeyondNations", result);
        }

        [Fact]
        public void testGetScreenshotsDirectoryIsDerivedFromBaseDirectory() {
            // run
            string baseDirectory = AppDataPaths.getBaseDirectory();
            string screenshotsDirectory = AppDataPaths.getScreenshotsDirectory();

            // check
            Assert.Equal(System.IO.Path.Combine(baseDirectory, "Screenshots"), screenshotsDirectory);
        }

        [Fact]
        public void testGetBaseDirectoryHasNoHardcodedDriveLetterOnThisHost() {
            // run
            string result = AppDataPaths.getBaseDirectory();

            // check: this test suite runs on Linux/macOS, so there should never be
            // a literal "C:\" style prefix baked into the resolved path
            Assert.DoesNotContain("C:\\", result);
        }
    }
}
