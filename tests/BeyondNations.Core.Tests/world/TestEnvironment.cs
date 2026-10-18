
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestEnvironment {
        private readonly RandomSource random = new RandomSource(20260816);



        [Fact]
        public void testInitialization() {
            // run
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(1, 1, entityRepository, random);

            // verify
            Assert.NotNull(environment);
            Assert.NotNull(environment.getChunkAtPosition(new Vector3(0, 0, 0)));

            // clean up
        }

        // TODO: write more tests
    }
}