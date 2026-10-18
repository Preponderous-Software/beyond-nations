
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestLocationId {
        

        [Fact]
        public void testInitialization() {
            // run
            LocationId locationId = new LocationId();

            // verify
            Assert.NotNull(locationId);
        }

        [Fact]
        public void testEquality() {
            // prepare
            LocationId locationId1 = new LocationId();
            LocationId locationId2 = new LocationId();

            // verify
            Assert.NotEqual(locationId2, locationId1);
        }
    }
}