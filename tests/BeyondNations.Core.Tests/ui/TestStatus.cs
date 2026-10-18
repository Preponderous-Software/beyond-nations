
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestStatus {

        
        [Fact]
        public void testInstantiation() {
            // prepare
            TickCounter tickCounter = new TickCounter();
            int ticksToExpire = 10;

            // execute
            Status status = new Status(tickCounter, ticksToExpire);

            // verify
            Assert.Equal(ticksToExpire, status.getTicksToExpire());
            Assert.Equal(0, status.getTickLastSet());
            Assert.Equal("Game started.", status.getStatus());

        }

        [Fact]
        public void testUpdate() {
            // prepare
            TickCounter tickCounter = new TickCounter();
            int ticksToExpire = 10;
            Status status = new Status(tickCounter, ticksToExpire);

            // execute
            status.update("test");

            // verify
            Assert.Equal(0, status.getTickLastSet());
            Assert.Equal("test", status.getStatus());

        }

        [Fact]
        public void testClearStatusIfExpiredNotExpired() {
            // prepare
            TickCounter tickCounter = new TickCounter();
            int ticksToExpire = 10;
            Status status = new Status(tickCounter, ticksToExpire);
            status.update("test");

            // execute
            tickCounter.increment();
            status.clearStatusIfExpired();

            // verify
            Assert.Equal("test", status.getStatus());

        }

        [Fact]
        public void testClearStatusIfExpiredExpired() {
            // prepare
            TickCounter tickCounter = new TickCounter();
            int ticksToExpire = 10;
            Status status = new Status(tickCounter, ticksToExpire);
            status.update("test");

            // execute
            for (int i = 0; i < ticksToExpire + 1; i++) {
                tickCounter.increment();
            }
            status.clearStatusIfExpired();

            // verify
            Assert.Equal("", status.getStatus());

        }
    }
}