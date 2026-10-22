
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
    
        /** One simulation step: the tick moves on and its messages share the line. */
        private static void step(TickCounter tickCounter, Status status) {
            status.endTick();
            tickCounter.increment();
            status.beginTick();
        }

        [Fact]
        public void testTwoMessagesInOneStep_AreBothShown() {
            TickCounter tickCounter = new TickCounter();
            Status status = new Status(tickCounter, 10);
            step(tickCounter, status);
            status.update("A new era: Testland is now a Town.");
            status.update("Objective complete: Reach the Town era.");
            Assert.Equal("A new era: Testland is now a Town. Objective complete: Reach the Town era.", status.getStatus());
            Assert.Equal("Objective complete: Reach the Town era.", status.getLatest());
        }

        [Fact]
        public void testAMessageInALaterStep_ReplacesTheLine() {
            TickCounter tickCounter = new TickCounter();
            Status status = new Status(tickCounter, 10);
            step(tickCounter, status);
            status.update("first.");
            status.update("second.");
            step(tickCounter, status);
            status.update("third.");
            Assert.Equal("third.", status.getStatus());
        }

        [Fact]
        public void testTheGameStartedLine_IsReplaced_NotJoined() {
            TickCounter tickCounter = new TickCounter();
            Status status = new Status(tickCounter, 10);
            status.beginTick();
            status.update("first.");
            Assert.Equal("first.", status.getStatus());
        }

        [Fact]
        public void testTheSameMessageTwiceInOneStep_IsShownOnce() {
            TickCounter tickCounter = new TickCounter();
            Status status = new Status(tickCounter, 10);
            step(tickCounter, status);
            status.update("A.");
            status.update("B.");
            status.update("B.");
            Assert.Equal("A. B.", status.getStatus());
        }

        [Fact]
        public void testCommandsBetweenSteps_ReplaceTheLine_AndRepeatsAreCounted() {
            // the world stands still under the crafting screen, so the tick
            // does not move between commands; they still replace the line
            TickCounter tickCounter = new TickCounter();
            Status status = new Status(tickCounter, 10);
            step(tickCounter, status);
            status.update("A new era.");
            status.endTick();
            status.update("Not enough wood.");
            Assert.Equal("Not enough wood.", status.getStatus());
            status.update("Not enough wood.");
            Assert.Equal("Not enough wood. (x2)", status.getStatus());
            status.update("Crafted a torch.");
            Assert.Equal("Crafted a torch.", status.getStatus());
        }

        [Fact]
        public void testAmend_ReplacesOnlyTheLatestMessage() {
            TickCounter tickCounter = new TickCounter();
            Status status = new Status(tickCounter, 10);
            step(tickCounter, status);
            status.update("A new era.");
            status.update("Alder has died.");
            step(tickCounter, status);
            status.amend("Alder and Birch have died.");
            Assert.Equal("A new era. Alder and Birch have died.", status.getStatus());
            Assert.Equal(2, status.getTickLastSet());
        }

        [Fact]
        public void testACombinedLine_SurvivesASave_AsOneLine() {
            Simulation simulation = new Simulation(new GameConfig());
            Status status = simulation.getPlayer().getStatus();
            status.beginTick();
            status.update("A.");
            status.update("B.");
            status.endTick();
            Simulation loaded = SaveGame.decode(SaveGame.encode(simulation, "test"));
            Status restored = loaded.getPlayer().getStatus();
            Assert.Equal("A. B.", restored.getStatus());
            // a message after loading starts a fresh line
            restored.beginTick();
            restored.update("C.");
            Assert.Equal("C.", restored.getStatus());
        }

        [Fact]
        public void testAfterASimulationStep_ACommandReplacesTheLine() {
            // fixedUpdate ends its step, so nothing it said is joined by a command
            Simulation simulation = new Simulation(new GameConfig());
            Status status = simulation.getPlayer().getStatus();
            simulation.fixedUpdate(0.02f);
            status.update("Said by a command.");
            Assert.Equal("Said by a command.", status.getStatus());
        }
    }
}
