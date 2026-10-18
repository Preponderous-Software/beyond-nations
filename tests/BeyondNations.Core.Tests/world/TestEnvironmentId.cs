
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestEnvironmentId {


        [Fact]
        public void testInitialization() {
            // run
            EnvironmentId environmentId = new EnvironmentId();

            // verify
            Assert.NotNull(environmentId);
        }

        [Fact]
        public void testEquality() {
            // prepare
            EnvironmentId environmentId1 = new EnvironmentId();
            EnvironmentId environmentId2 = new EnvironmentId();

            // verify
            Assert.NotEqual(environmentId2, environmentId1);
        }
    }
}