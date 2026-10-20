using System;
using System.IO;
using System.Text;
using Silk.NET.Input;
using beyondnations.desktop.ui;

namespace beyondnations.desktop {

    /**
    * Sharing on the desktop (docs/SHARING.md): the link goes to the system
    * clipboard, through GLFW's, and exports are written under
    * AppDataPaths.getExportsDirectory() -- "~/.local/share/BeyondNations/Exports"
    * on Linux -- with the full path on the Share screen so they can be found.
    */
    public class DesktopSharePlatform : ISharePlatform {
        private readonly IInputContext input;
        private readonly string exportsDirectory;

        public DesktopSharePlatform(IInputContext input) : this(input, AppDataPaths.getExportsDirectory()) {
        }

        public DesktopSharePlatform(IInputContext input, string exportsDirectory) {
            this.input = input;
            this.exportsDirectory = exportsDirectory;
        }

        public string getShareLinkLabel() {
            return "Copy link";
        }

        public void shareLink(string url, string title, Action<string, bool> done) {
            if (input == null || input.Keyboards.Count == 0) {
                done("No clipboard here; the link is " + url, true);
                return;
            }
            input.Keyboards[0].ClipboardText = url;
            done("Link copied: " + url, false);
        }

        public string saveFile(string fileName, string mimeType, string content) {
            return "Saved to " + write(exportsDirectory, fileName, content);
        }

        /** Writes content (UTF-8, no byte order mark) to directory/fileName, creating the directory; returns the path. */
        public static string write(string directory, string fileName, string content) {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName);
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return path;
        }

        public bool promptsForText() {
            return false;
        }

        public string promptText(string message, string current) {
            return null;
        }
    }
}
