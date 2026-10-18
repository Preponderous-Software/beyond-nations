using System;
using System.Threading;
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
            Assert.Equal(0, tickCounter.getTotalTicks());
            Assert.Equal(TickCounter.DefaultTicksPerSecond, tickCounter.getTicksPerSecond());
        }

        [Fact]
        public void testIncrement() {
            // prepare
            TickCounter tickCounter = new TickCounter();

            // run
            tickCounter.increment();

            // check
            Assert.Equal(1, tickCounter.getTick());
            Assert.Equal(1, tickCounter.getTotalTicks());
        }

        [Fact]
        public void testTickWrapsEveryTicksPerSecondTicks() {
            TickCounter tickCounter = new TickCounter(50);

            for (int i = 1; i <= 10000; i++) {
                tickCounter.increment();
                Assert.Equal(i % 50, tickCounter.getTick());
            }
            Assert.Equal(10000, tickCounter.getTotalTicks());
        }

        [Fact]
        public void testTickWrapsAtTheConfiguredRate() {
            TickCounter tickCounter = new TickCounter(20);

            for (int i = 0; i < 45; i++) {
                tickCounter.increment();
            }

            Assert.Equal(5, tickCounter.getTick());
        }

        [Fact]
        public void testTickDoesNotFollowTheWallClock() {
            // #272: the tick within the second used to reset whenever a real
            // second had passed, so a pause mid-second moved the reset.
            TickCounter tickCounter = new TickCounter(50);
            for (int i = 0; i < 25; i++) {
                tickCounter.increment();
            }

            Thread.Sleep(1100);
            for (int i = 0; i < 25; i++) {
                tickCounter.increment();
            }

            Assert.Equal(0, tickCounter.getTick());
            Assert.Equal(50, tickCounter.getTotalTicks());
        }

        [Fact]
        public void testNonPositiveTicksPerSecond_Throws() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickCounter(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickCounter(-1));
        }

    }
}
