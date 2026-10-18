
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestNationNameGenerator {
        private readonly RandomSource random = new RandomSource(20260816);


        [Fact]
        public void testGenerate() {
            // run
            string name = new NationNameGenerator(random).generate();

            // verify
            Assert.NotNull(name);
            Assert.True(name.Length > 0);
        }
    }
}