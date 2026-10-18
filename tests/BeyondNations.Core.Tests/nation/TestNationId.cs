
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestNationId {


        [Fact]
        public void testInitialization() {
            // run
            NationId id = new NationId();

            // verify
            Assert.NotNull(id);
        }

        [Fact]
        public void testEquality() {
            // prepare
            NationId id1 = new NationId();
            NationId id2 = new NationId();

            // verify
            Assert.NotEqual(id2, id1);
        }
    }
}