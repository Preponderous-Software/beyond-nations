using beyondnations;

namespace beyondnationstests {

    /** The host tests' storage keeps the same promises as the real ones. */
    public class TestMemorySaveStorage : SaveStorageContract {
        protected override ISaveStorage emptyStorage() {
            return new MemorySaveStorage();
        }
    }
}
