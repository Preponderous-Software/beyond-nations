using System;
using System.IO;
using System.Text;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The desktop's storage: the shared ISaveStorage contract, plus what only
    * files have -- a directory made on the first write, and no temporary
    * file left behind.
    */
    public class TestFileSaveStorage : SaveStorageContract, IDisposable {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "bn-storage-tests-" + Guid.NewGuid().ToString("N"));

        protected override ISaveStorage emptyStorage() {
            return new FileSaveStorage(directory);
        }

        public void Dispose() {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void testTheDirectory_IsMadeOnTheFirstWrite_NotBefore() {
            FileSaveStorage storage = new FileSaveStorage(directory);
            Assert.Equal(directory, storage.getDirectory());
            storage.listNames();
            storage.read("world-a.bnsave");
            storage.remove("world-a.bnsave");
            Assert.False(Directory.Exists(directory));
            storage.writeAtomically("world-a.bnsave", Encoding.UTF8.GetBytes("save"));
            Assert.True(File.Exists(Path.Combine(directory, "world-a.bnsave")));
        }

        [Fact]
        public void testAWrite_LeavesNoTemporaryFile() {
            FileSaveStorage storage = new FileSaveStorage(directory);
            storage.writeAtomically("world-a.bnsave", Encoding.UTF8.GetBytes("first"));
            storage.writeAtomically("world-a.bnsave", Encoding.UTF8.GetBytes("second"));
            Assert.Equal(new[] { Path.Combine(directory, "world-a.bnsave") }, Directory.GetFiles(directory));
        }
    }
}
