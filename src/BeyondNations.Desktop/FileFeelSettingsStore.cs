using System;
using System.IO;
using beyondnations.desktop.ui.feel;

namespace beyondnations.desktop {

    /**
    * Sound and tips settings (FeelSettings) as a small text file beside the
    * saves, under AppDataPaths. Written through a temporary file and a move,
    * so a crash mid-write leaves the old file rather than half of a new one.
    */
    public class FileFeelSettingsStore : IFeelSettingsStore {
        public const string FileName = "feel-settings.txt";

        private readonly string path;

        public FileFeelSettingsStore(string directory) {
            this.path = Path.Combine(directory, FileName);
        }

        public string getPath() {
            return path;
        }

        public string read() {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void write(string text) {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) {
                Directory.CreateDirectory(directory);
            }
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, text);
            File.Move(temporary, path, true);
        }
    }
}
