
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestLocation {
        private readonly RandomSource random = new RandomSource(20260816);

        

        [Fact]
        public void testInitialization() {
            // run
            Location location = new Location(0, 0, 1, random);

            // verify
            Assert.NotNull(location);
            Assert.NotNull(location.getId());
            Assert.Equal(new Vector3(0, 0, 0), location.getPosition());
            Assert.Equal(1, location.getScale());
            Assert.Equal(new Vector3(1, 1, 1), location.getScaleVector());
            Assert.Equal(0, location.getNumberOfEntities());

            // clean up
        }
    }

}