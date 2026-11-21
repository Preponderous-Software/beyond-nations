using System.Collections.Generic;
using System.Text;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * What every ISaveStorage promises (docs/SAVES.md), run against each
    * implementation the tests can build: the file storage the desktop uses
    * and the memory fake the host tests use. A new storage passes this by
    * adding one subclass that says how to make an empty one.
    */
    public abstract class SaveStorageContract {
        /** A storage with nothing in it. */
        protected abstract ISaveStorage emptyStorage();

        private static byte[] bytes(string text) {
            return Encoding.UTF8.GetBytes(text);
        }

        [Fact]
        public void testAnEmptyStorage_ListsNothing_AndReadsNull() {
            ISaveStorage storage = emptyStorage();
            Assert.Empty(storage.listNames());
            Assert.Null(storage.read("world-a.bnsave"));
        }

        [Fact]
        public void testAWrite_ReadsBackTheSameBytes_AndIsListedOnce() {
            ISaveStorage storage = emptyStorage();
            storage.writeAtomically("world-a.bnsave", bytes("first"));
            Assert.Equal(bytes("first"), storage.read("world-a.bnsave"));
            Assert.Equal(new List<string> { "world-a.bnsave" }, storage.listNames());
        }

        [Fact]
        public void testASecondWrite_ReplacesTheFirstWhole() {
            ISaveStorage storage = emptyStorage();
            storage.writeAtomically("world-a.bnsave", bytes("a much longer first save"));
            storage.writeAtomically("world-a.bnsave", bytes("short"));
            Assert.Equal(bytes("short"), storage.read("world-a.bnsave"));
            Assert.Single(storage.listNames());
        }

        [Fact]
        public void testNames_AreKeptApart() {
            ISaveStorage storage = emptyStorage();
            storage.writeAtomically("world-a.bnsave", bytes("save"));
            storage.writeAtomically("world-a.bnsave.bak", bytes("backup"));
            Assert.Equal(bytes("save"), storage.read("world-a.bnsave"));
            Assert.Equal(bytes("backup"), storage.read("world-a.bnsave.bak"));
            List<string> names = new List<string>(storage.listNames());
            names.Sort(string.CompareOrdinal);
            Assert.Equal(new List<string> { "world-a.bnsave", "world-a.bnsave.bak" }, names);
        }

        [Fact]
        public void testRemove_ForgetsTheName_AndRemovingNothingIsFine() {
            ISaveStorage storage = emptyStorage();
            storage.writeAtomically("world-a.bnsave", bytes("save"));
            storage.remove("world-a.bnsave");
            Assert.Null(storage.read("world-a.bnsave"));
            Assert.Empty(storage.listNames());
            storage.remove("world-a.bnsave");
            storage.remove("never-written.bnsave");
        }

        [Fact]
        public void testStoredBytes_DoNotShareTheCallersArrays() {
            ISaveStorage storage = emptyStorage();
            byte[] written = bytes("save");
            storage.writeAtomically("world-a.bnsave", written);
            written[0] = (byte) 'X';
            byte[] read = storage.read("world-a.bnsave");
            Assert.Equal(bytes("save"), read);
            read[0] = (byte) 'Y';
            Assert.Equal(bytes("save"), storage.read("world-a.bnsave"));
        }
    }
}
