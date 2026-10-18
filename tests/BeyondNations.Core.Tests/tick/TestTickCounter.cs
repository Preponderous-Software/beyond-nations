
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestTickCounter {


        [Fact]
        public void testInstantiation() {
            // run
            TickCounter tickCounter = new TickCounter();

            // check
            Assert.Equal(0, tickCounter.getTick());
        }

        [Fact]
        public void testIncrement() {
            // prepare
            TickCounter tickCounter = new TickCounter();

            // run
            tickCounter.increment();

            // check
            Assert.Equal(1, tickCounter.getTick());
        }
        
    }
}