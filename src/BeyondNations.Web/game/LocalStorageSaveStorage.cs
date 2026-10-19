using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using beyondnations;
using beyondnations.web.host;

namespace beyondnations.web {

    /**
    * Saves in the browser's localStorage (docs/SAVES.md), one key per file
    * under "beyond-nations/saves/". localStorage.setItem replaces a value
    * whole or not at all, which is the atomic write ISaveStorage asks for;
    * SaveSlots reads every write back and checks it.
    *
    * The arcade serves every game from one origin, which shares one
    * localStorage quota (about 5 MB, counted in UTF-16 code units), and a
    * save of a world played for twenty minutes is a couple of megabytes of
    * JSON. So each file is stored gzipped and base64-encoded, behind a "gz:"
    * marker -- about a sixth of the size -- and turned back into the exact
    * bytes on read, so the checksum SaveGame verifies is the checksum of the
    * file as it was written. A full quota is reported to the player as such.
    */
    public class LocalStorageSaveStorage : ISaveStorage {
        public const string Prefix = "beyond-nations/saves/";

        public IReadOnlyList<string> listNames() {
            List<string> names = new List<string>();
            using (JsonDocument document = JsonDocument.Parse(WebPage.storageKeys(Prefix))) {
                foreach (JsonElement key in document.RootElement.EnumerateArray()) {
                    names.Add(key.GetString());
                }
            }
            return names;
        }

        public byte[] read(string name) {
            string value = WebPage.storageGet(Prefix + name);
            if (value == null) {
                return null;
            }
            try {
                return SaveStorageCodec.decode(value);
            } catch (Exception e) when (e is FormatException || e is InvalidDataException) {
                // a mangled value reads as a damaged save, which SaveGame reports
                Log.warning("save storage: " + name + " could not be decoded: " + e.Message);
                return Encoding.UTF8.GetBytes(value);
            }
        }

        public void writeAtomically(string name, byte[] data) {
            string problem = WebPage.storageSet(Prefix + name, SaveStorageCodec.encode(data));
            if (!string.IsNullOrEmpty(problem)) {
                throw new IOException(problem);
            }
        }

        public void remove(string name) {
            WebPage.storageRemove(Prefix + name);
        }
    }
}
