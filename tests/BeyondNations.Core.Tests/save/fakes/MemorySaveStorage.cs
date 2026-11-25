using System.Collections.Generic;
using System.IO;

using beyondnations;

namespace beyondnationstests {

    /** Saves held in memory, as localStorage holds them in the browser. */
    public class MemorySaveStorage : ISaveStorage {
        public readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
        public string FailWritesWith;

        public IReadOnlyList<string> listNames() {
            return new List<string>(Files.Keys);
        }

        public byte[] read(string name) {
            byte[] data;
            return Files.TryGetValue(name, out data) ? (byte[]) data.Clone() : null;
        }

        public void writeAtomically(string name, byte[] data) {
            if (FailWritesWith != null) {
                throw new IOException(FailWritesWith);
            }
            Files[name] = (byte[]) data.Clone();
        }

        public void remove(string name) {
            Files.Remove(name);
        }
    }
}
