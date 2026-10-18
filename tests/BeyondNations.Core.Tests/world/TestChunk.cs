
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestChunk {
        private readonly RandomSource random = new RandomSource(20260816);



        [Fact]
        public void testInitialization() {
            // run
            Chunk chunk = new Chunk(0, 0, 1, 1, random);

            // verify
            Assert.NotNull(chunk);
            Assert.NotNull(chunk.getId());
            Assert.Equal(1, chunk.getSize());
            Assert.Equal(0, chunk.getX());
            Assert.Equal(0, chunk.getZ());
            Assert.Equal(new Vector3(0, 0, 0), chunk.getPosition());
            Assert.NotNull(chunk.getLocations());
            Assert.Equal(1, chunk.getLocations().GetLength(0) * chunk.getLocations().GetLength(1));
            Assert.NotNull(chunk.getLocations()[0, 0]);

            // clean up
        }

        [Fact]
        public void testGetLocation() {
            // prepare
            Chunk chunk = new Chunk(0, 0, 1, 1, random);

            // run
            Location location = chunk.getLocation(0, 0);

            // verify
            Assert.NotNull(location);

            // clean up
        }

        [Fact]
        public void testGetLocations() {
            // prepare
            Chunk chunk = new Chunk(0, 0, 1, 1, random);

            // run
            Location[,] locations = chunk.getLocations();

            // verify
            Assert.NotNull(locations);
            Assert.Equal(1, locations.GetLength(0) * locations.GetLength(1));
            Assert.NotNull(locations[0, 0]);

            // clean up
        }

        [Fact]
        public void testGetGameObject() {
            // prepare
            Chunk chunk = new Chunk(0, 0, 1, 1, random);

            // run
            string name = chunk.getName();

            // verify
            Assert.Equal("Chunk_0_0", name);

            // clean up
        }

        [Fact]
        public void testGetPosition() {
            // prepare
            Chunk chunk = new Chunk(0, 0, 1, 1, random);

            // run
            Vector3 position = chunk.getPosition();

            // verify
            Assert.Equal(new Vector3(0, 0, 0), position);

            // clean up
        }

        [Fact]
        public void testGetSize() {
            // prepare
            Chunk chunk = new Chunk(0, 0, 1, 1, random);

            // run
            int size = chunk.getSize();

            // verify
            Assert.Equal(1, size);

            // clean up
        }

        [Fact]
        public void testGetX() {
            // prepare
            Chunk chunk = new Chunk(0, 0, 1, 1, random);

            // run
            int x = chunk.getX();

            // verify
            Assert.Equal(0, x);

            // clean up
        }

        [Fact]
        public void testGetZ() {
            // prepare
            Chunk chunk = new Chunk(0, 0, 1, 1, random);

            // run
            int z = chunk.getZ();

            // verify
            Assert.Equal(0, z);

            // clean up
        }
    }
}