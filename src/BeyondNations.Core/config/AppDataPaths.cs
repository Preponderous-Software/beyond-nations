namespace beyondnations {

    /**
    * Resolves where BeyondNations should write user data (currently just
    * screenshots) on the current platform.
    *
    * This intentionally avoids referencing a game engine, so it lives in
    * Core and is unit-testable without a window or a GL context. See #224:
    * the old Unity build hardcoded "C:\\BeyondNations", which does not exist
    * on Linux or macOS.
    *
    * .NET's own Environment.SpecialFolder.ApplicationData maps to
    * "~/.config" on Linux, which is a config location, not a data one. So
    * the base directory below follows the XDG Base Directory spec directly
    * on Linux (XDG_DATA_HOME, falling back to "~/.local/share") and defers
    * to Environment.GetFolderPath(SpecialFolder.ApplicationData) everywhere
    * else, which is the platform-correct roaming-data folder on both
    * Windows and macOS.
    */
    public static class AppDataPaths {

        private const string ApplicationFolderName = "BeyondNations";
        private const string ScreenshotsFolderName = "Screenshots";

        /**
        * The directory BeyondNations stores its data under, e.g.
        * "~/.local/share/BeyondNations" on Linux or
        * "%AppData%\BeyondNations" on Windows.
        */
        public static string getBaseDirectory() {
            bool isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Linux);
            string applicationDataFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string xdgDataHome = System.Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            string homeDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

            return resolveBaseDirectory(isLinux, applicationDataFolder, xdgDataHome, homeDirectory);
        }

        /**
        * The directory screenshots are written to, derived from
        * getBaseDirectory() rather than hardcoded.
        */
        public static string getScreenshotsDirectory() {
            return System.IO.Path.Combine(getBaseDirectory(), ScreenshotsFolderName);
        }

        /**
        * Pure resolution logic, split out from getBaseDirectory() so it can
        * be unit tested for every platform without depending on the OS the
        * tests happen to run on. Public so tests can exercise Windows,
        * macOS and Linux behaviour directly regardless of the host platform.
        */
        public static string resolveBaseDirectory(bool isLinux, string applicationDataFolder, string xdgDataHome, string homeDirectory) {
            string dataRoot;
            if (isLinux) {
                dataRoot = !string.IsNullOrEmpty(xdgDataHome)
                    ? xdgDataHome
                    : System.IO.Path.Combine(homeDirectory, ".local", "share");
            }
            else {
                // Windows: %AppData% (Roaming). macOS: .NET's own mapping,
                // which is the platform-correct roaming-data location there too.
                dataRoot = applicationDataFolder;
            }

            return System.IO.Path.Combine(dataRoot, ApplicationFolderName);
        }
    }
}
