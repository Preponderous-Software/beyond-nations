
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestChunkId {
        

        [Fact]
        public void testInitialization() {
            // run
            ChunkId chunkId = new ChunkId();

            // verify
            Assert.NotNull(chunkId);
        }

        [Fact]
        public void testEquality() {
            // prepare
            ChunkId chunkId1 = new ChunkId();
            ChunkId chunkId2 = new ChunkId();

            // verify
            Assert.NotEqual(chunkId2, chunkId1);
        }
    }
}