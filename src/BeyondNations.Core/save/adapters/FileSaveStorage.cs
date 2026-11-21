using System.Collections.Generic;
using System.IO;

namespace beyondnations {

    /**
    * Save files in a directory. A write goes to a temporary file that is
    * flushed to disk (fsync) and then renamed over the real one, so a crash or
    * power cut mid-write leaves the previous save intact.
    */
    public class FileSaveStorage : ISaveStorage {
        private readonly string directory;

        public FileSaveStorage(string directory) {
            this.directory = directory;
        }

        public string getDirectory() {
            return directory;
        }

        public IReadOnlyList<string> listNames() {
            List<string> names = new List<string>();
            if (!Directory.Exists(directory)) {
                return names;
            }
            foreach (string path in Directory.GetFiles(directory)) {
                names.Add(Path.GetFileName(path));
            }
            return names;
        }

        public byte[] read(string name) {
            string path = Path.Combine(directory, name);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        public void writeAtomically(string name, byte[] data) {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, name);
            string temporary = path + ".tmp";
            using (FileStream stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None)) {
                stream.Write(data, 0, data.Length);
                stream.Flush(true);
            }
            File.Move(temporary, path, true);
        }

        public void remove(string name) {
            string path = Path.Combine(directory, name);
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }
}
