using System.Collections.Generic;

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
}
