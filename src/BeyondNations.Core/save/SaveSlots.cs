using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace beyondnations {

    /**
    * Where save files physically go. The desktop writes files under
    * AppDataPaths (FileSaveStorage); the browser writes localStorage. Either
    * way a write must be all-or-nothing: a reader sees the old bytes or the
    * new ones, never a mixture.
    */
    public interface ISaveStorage {
        /** The names of everything stored, in no particular order. */
        IReadOnlyList<string> listNames();

        /** The bytes stored under a name, or null if there are none. */
        byte[] read(string name);

        /** Replaces whatever is stored under a name, atomically and durably. */
        void writeAtomically(string name, byte[] data);

        /** Removes whatever is stored under a name; nothing if there is nothing. */
        void remove(string name);
    }

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

    /** One entry in the Load list: a slot and what its newest readable file says. */
    public class SaveSlotInfo {
        public string Slot { get; internal set; }
        /** Null when neither the save nor its backup can be read. */
        public SaveHeader Header { get; internal set; }
        /** Why the save cannot be read, or null when it can. */
        public string Problem { get; internal set; }
        /** True when the save itself is unreadable and the header shown is its backup's. */
        public bool FromBackup { get; internal set; }

        public bool isLoadable() {
            return Header != null;
        }
    }

    /** A loaded world, and whether it came from the backup because the save itself was damaged. */
    public class SaveLoadResult {
        public Simulation Simulation { get; internal set; }
        public SaveHeader Header { get; internal set; }
        public bool FromBackup { get; internal set; }
        /** Why the save itself could not be read, when the backup was used. */
        public string Problem { get; internal set; }
    }

    /**
    * Named save slots over an ISaveStorage (docs/SAVES.md). Each slot is one
    * world: "<slot>.bnsave", and "<slot>.bnsave.bak" holding the save before
    * the latest one.
    *
    * Integrity rules:
    *   - a new save is checked (header and checksum) before it is written,
    *     and read back and checked after;
    *   - the previous save becomes the backup only if it is itself sound, so
    *     a damaged save never pushes out a good backup;
    *   - loading never writes anything, and a save that cannot be loaded --
    *     damaged, or from a newer version -- is reported, not replaced.
    */
    public class SaveSlots {
        public const string Extension = ".bnsave";
        public const string BackupExtension = ".bnsave.bak";

        private readonly ISaveStorage storage;

        public SaveSlots(ISaveStorage storage) {
            this.storage = storage;
        }

        public ISaveStorage getStorage() {
            return storage;
        }

        /** A slot name for a new world: its seed and when it began, e.g. "world-42-20261018-142233". */
        public static string newSlotName(int seed, DateTime nowUtc) {
            return "world-" + seed.ToString(System.Globalization.CultureInfo.InvariantCulture).Replace('-', 'm')
                + "-" + nowUtc.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool isValidSlotName(string slot) {
            if (string.IsNullOrEmpty(slot) || slot.Length > 64) {
                return false;
            }
            foreach (char c in slot) {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!ok) {
                    return false;
                }
            }
            return true;
        }

        private static void requireValid(string slot) {
            if (!isValidSlotName(slot)) {
                throw new ArgumentException("'" + slot + "' is not a save slot name");
            }
        }

        /**
        * Writes a save into a slot, keeping the one it replaces as the
        * backup. Throws (and leaves the slot as it was) if the save does not
        * check out before or after writing.
        */
        public SaveHeader write(string slot, byte[] file) {
            requireValid(slot);
            SaveHeader header = SaveGame.check(file);

            byte[] previous = storage.read(slot + Extension);
            if (previous != null && isNewerFormat(previous)) {
                throw new SaveFormatException("The save in this slot was made by a newer version of Beyond Nations; it has been left alone.");
            }
            if (previous != null && isSound(previous)) {
                storage.writeAtomically(slot + BackupExtension, previous);
            }
            storage.writeAtomically(slot + Extension, file);

            byte[] written = storage.read(slot + Extension);
            if (written == null || !sameBytes(written, file)) {
                throw new IOException("The save did not read back as it was written.");
            }
            return header;
        }

        private static bool isSound(byte[] file) {
            try {
                SaveGame.check(file);
                return true;
            } catch (SaveFormatException) {
                return false;
            }
        }

        private static bool isNewerFormat(byte[] file) {
            try {
                SaveGame.readHeader(file);
                return false;
            } catch (SaveFormatException) {
                // readHeader refuses newer formats; tell that apart from damage
                // by reading the version number directly
                int newline = System.Array.IndexOf(file, (byte) '\n');
                if (newline <= 0) {
                    return false;
                }
                try {
                    using (System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(new System.ReadOnlyMemory<byte>(file, 0, newline))) {
                        System.Text.Json.JsonElement version;
                        return document.RootElement.TryGetProperty("formatVersion", out version)
                            && version.TryGetInt32(out int number) && number > SaveGame.CurrentFormatVersion;
                    }
                } catch (System.Text.Json.JsonException) {
                    return false;
                }
            }
        }

        private static bool sameBytes(byte[] a, byte[] b) {
            if (a.Length != b.Length) {
                return false;
            }
            for (int i = 0; i < a.Length; i++) {
                if (a[i] != b[i]) {
                    return false;
                }
            }
            return true;
        }

        /** Every slot, newest first; unreadable ones last, with the reason. */
        public List<SaveSlotInfo> list() {
            HashSet<string> slots = new HashSet<string>();
            foreach (string name in storage.listNames()) {
                string slot = null;
                if (name.EndsWith(BackupExtension, StringComparison.Ordinal)) {
                    slot = name.Substring(0, name.Length - BackupExtension.Length);
                } else if (name.EndsWith(Extension, StringComparison.Ordinal)) {
                    slot = name.Substring(0, name.Length - Extension.Length);
                }
                if (slot != null && isValidSlotName(slot)) {
                    slots.Add(slot);
                }
            }

            List<SaveSlotInfo> infos = new List<SaveSlotInfo>();
            foreach (string slot in slots) {
                infos.Add(describe(slot));
            }
            infos.Sort((a, b) => {
                if (a.isLoadable() != b.isLoadable()) {
                    return a.isLoadable() ? -1 : 1;
                }
                if (!a.isLoadable()) {
                    return string.CompareOrdinal(a.Slot, b.Slot);
                }
                int byTime = b.Header.SavedAtUtc.CompareTo(a.Header.SavedAtUtc);
                return byTime != 0 ? byTime : string.CompareOrdinal(a.Slot, b.Slot);
            });
            return infos;
        }

        private SaveSlotInfo describe(string slot) {
            SaveSlotInfo info = new SaveSlotInfo { Slot = slot };
            string problem = null;
            byte[] primary = storage.read(slot + Extension);
            if (primary != null) {
                try {
                    info.Header = SaveGame.check(primary);
                    return info;
                } catch (SaveFormatException e) {
                    problem = e.Message;
                }
            } else {
                problem = "The save is missing.";
            }
            byte[] backup = storage.read(slot + BackupExtension);
            if (backup != null) {
                try {
                    info.Header = SaveGame.check(backup);
                    info.FromBackup = true;
                } catch (SaveFormatException) {
                    // the save's own problem is the one worth reporting
                }
            }
            info.Problem = problem;
            return info;
        }

        /**
        * Deletes a world: its save and its backup. Only ever done because the
        * player asked to, from the Load list.
        */
        public void delete(string slot) {
            requireValid(slot);
            storage.remove(slot + Extension);
            storage.remove(slot + BackupExtension);
        }

        /** The most recently saved loadable slot, or null when there is none. */
        public SaveSlotInfo latest() {
            foreach (SaveSlotInfo info in list()) {
                if (info.isLoadable()) {
                    return info;
                }
            }
            return null;
        }

        /**
        * Loads a slot. Falls back to the backup when the save itself cannot
        * be read, and says so; throws SaveFormatException with the save's own
        * problem when neither can. Never writes.
        */
        public SaveLoadResult load(string slot) {
            requireValid(slot);
            byte[] primary = storage.read(slot + Extension);
            SaveFormatException primaryProblem;
            if (primary != null) {
                try {
                    return new SaveLoadResult { Simulation = SaveGame.decode(primary), Header = SaveGame.readHeader(primary) };
                } catch (SaveFormatException e) {
                    primaryProblem = e;
                }
            } else {
                primaryProblem = new SaveFormatException("The save is missing.");
            }
            byte[] backup = storage.read(slot + BackupExtension);
            if (backup != null) {
                try {
                    return new SaveLoadResult {
                        Simulation = SaveGame.decode(backup),
                        Header = SaveGame.readHeader(backup),
                        FromBackup = true,
                        Problem = primaryProblem.Message
                    };
                } catch (SaveFormatException) {
                    // report the save's own problem
                }
            }
            throw primaryProblem;
        }
    }
}
