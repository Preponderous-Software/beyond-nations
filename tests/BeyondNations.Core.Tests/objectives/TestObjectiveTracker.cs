using System.Collections.Generic;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestObjectiveProgress {

        [Fact]
        public void testCount_BelowTarget_IncompleteWithCounter() {
            ObjectiveProgress progress = ObjectiveProgress.count("Wood", 12, 40);
            Assert.False(progress.isComplete());
            Assert.True(progress.hasCounter());
            Assert.Equal("Wood 12/40", progress.describe());
        }

        [Fact]
        public void testCount_PastTarget_CompleteAndCapped() {
            ObjectiveProgress progress = ObjectiveProgress.count("Wood", 140, 40);
            Assert.True(progress.isComplete());
            Assert.Equal("Wood 40/40", progress.describe());
        }

        [Fact]
        public void testTwoCounters_DescribedTogetherAndPooledForTheBar() {
            ObjectiveProgress progress = ObjectiveProgress.countersWith(false, "Wood", 12, 20, "Stone", 9, 5);
            Assert.Equal("Wood 12/20 \u00b7 Stone 5/5", progress.describe());
            Assert.True(progress.hasSecondCounter());
            // 12 of 20 wood plus 5 of 5 stone (the extra 4 stone does not count)
            Assert.Equal(17f / 25f, progress.fraction(), 3);
        }

        [Fact]
        public void testFraction_SingleCounterAndFlag() {
            Assert.Equal(0.25f, ObjectiveProgress.count("Wood", 10, 40).fraction(), 3);
            Assert.Equal(1f, ObjectiveProgress.flag(true).fraction());
            Assert.Equal(0f, ObjectiveProgress.flag(false).fraction());
        }

        [Fact]
        public void testFlag_HasNoCounter() {
            Assert.True(ObjectiveProgress.flag(true).isComplete());
            Assert.False(ObjectiveProgress.flag(false).isComplete());
            Assert.Null(ObjectiveProgress.flag(true).describe());
        }

        [Fact]
        public void testCounterWith_CompletionIndependentOfCounter() {
            ObjectiveProgress progress = ObjectiveProgress.counterWith(true, "Wood", 0, 100);
            Assert.True(progress.isComplete());
            Assert.Equal("Wood 0/100", progress.describe());
        }
    }

    public class TestObjectiveReward {

        [Fact]
        public void testDescribe_PluralisesCountableItems() {
            ObjectiveReward reward = ObjectiveReward.coins(25).and(ItemType.APPLE, 2).and(ItemType.WOOD, 10).and(ItemType.SAPLING, 1);
            Assert.Equal("+25 coins, +2 apples, +10 wood, +1 sapling", reward.describe());
        }

        [Fact]
        public void testGrantTo_AddsEveryStack() {
            Inventory inventory = new Inventory(0);
            ObjectiveReward.coins(5).and(ItemType.CHICKEN_MEAT, 3).grantTo(inventory);
            Assert.Equal(5, inventory.getNumItems(ItemType.COIN));
            Assert.Equal(3, inventory.getNumItems(ItemType.CHICKEN_MEAT));
        }

        [Fact]
        public void testZeroQuantity_IsDropped() {
            Assert.True(ObjectiveReward.coins(0).isEmpty());
            Assert.Equal("", ObjectiveReward.None.describe());
        }
    }

    public class TestObjectiveHint {

        [Fact]
        public void testResolve_ReplacesEveryKnownToken() {
            string resolved = ObjectiveHint.resolve("Press {CreateNewNation} or {JoinNation}; {Unknown} stays.", key => "<" + key + ">");
            Assert.Equal("Press <CreateNewNation> or <JoinNation>; {Unknown} stays.", resolved);
        }

        [Fact]
        public void testResolve_NoTokens_Unchanged() {
            Assert.Equal("Explore.", ObjectiveHint.resolve("Explore.", key => "X"));
            Assert.Equal("", ObjectiveHint.resolve("", key => "X"));
        }
    }

    public class TestObjectiveTracker {
        private const float FixedDeltaTime = 1f / 50f;

        private static Simulation makeSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(20261018);
            return new Simulation(config);
        }

        private static Objective switchObjective(string id, bool[] done, ObjectiveReward reward = null) {
            return new Objective(id, "Do " + id, s => ObjectiveProgress.flag(done[0]), "hint " + id, reward);
        }

        [Fact]
        public void testEvaluate_Incomplete_StaysOnCurrent() {
            Simulation simulation = makeSimulation();
            bool[] done = { false };
            ObjectiveTracker tracker = new ObjectiveTracker(new[] { switchObjective("a", done) }, null);

            Assert.False(tracker.evaluate(simulation));
            Assert.Equal("a", tracker.getCurrent().getId());
            Assert.Equal(0, tracker.getCompletedCount());
            Assert.Null(tracker.getLastCompleted());
            Assert.Equal(-1, tracker.getLastCompletedTick());
        }

        [Fact]
        public void testEvaluate_Complete_GrantsRewardAnnouncesAndAdvances() {
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            int coins = player.getInventory().getNumItems(ItemType.COIN);
            bool[] done = { true };
            bool[] notDone = { false };
            ObjectiveTracker tracker = new ObjectiveTracker(new[] {
                switchObjective("a", done, ObjectiveReward.coins(30)),
                switchObjective("b", notDone),
            }, null);

            Assert.True(tracker.evaluate(simulation));

            Assert.Equal(coins + 30, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Objective complete: Do a. +30 coins.", player.getStatus().getStatus());
            Assert.Equal("b", tracker.getCurrent().getId());
            Assert.Equal("a", tracker.getLastCompleted().getId());
            Assert.Equal(1, tracker.getCompletedCount());
            Assert.False(tracker.getCurrentProgress().isComplete());
        }

        [Fact]
        public void testEvaluate_SeveralAlreadyMet_CompletesOnePerEvaluation() {
            Simulation simulation = makeSimulation();
            bool[] done = { true };
            ObjectiveTracker tracker = new ObjectiveTracker(new[] {
                switchObjective("a", done), switchObjective("b", done), switchObjective("c", done),
            }, null);

            tracker.evaluate(simulation);
            Assert.Equal(1, tracker.getCompletedCount());
            // the next one's progress is already known, so the HUD can show it
            Assert.True(tracker.getCurrentProgress().isComplete());
            tracker.evaluate(simulation);
            Assert.Equal(2, tracker.getCompletedCount());
            tracker.evaluate(simulation);
            Assert.Equal(3, tracker.getCompletedCount());
            Assert.Null(tracker.getCurrent());
            Assert.False(tracker.evaluate(simulation));
        }

        [Fact]
        public void testUpdate_EvaluatesOncePerSecondOfGameTime() {
            Simulation simulation = makeSimulation();
            int evaluations = 0;
            Objective counting = new Objective("count", "Count", s => { evaluations++; return ObjectiveProgress.flag(false); }, "", null);
            ObjectiveTracker tracker = new ObjectiveTracker(new[] { counting }, null);

            int ticksPerSecond = simulation.getTickCounter().getTicksPerSecond();
            tracker.update(simulation); // the first call always evaluates
            for (int i = 0; i < ticksPerSecond * 3; i++) {
                simulation.getTickCounter().increment();
                tracker.update(simulation);
            }

            Assert.Equal(4, evaluations);
        }

        [Fact]
        public void testAmbitions_AppendedToTheSameListWhenTheArcRunsOut() {
            Simulation simulation = makeSimulation();
            bool[] done = { true };
            ObjectiveTracker tracker = new ObjectiveTracker(new[] { switchObjective("arc", done) },
                index => index < 2 ? new Objective("amb" + index, "Ambition " + index, s => ObjectiveProgress.flag(true), "", null, true) : null);

            Assert.False(tracker.isOnAmbitions());
            Assert.Single(tracker.getObjectives());

            tracker.evaluate(simulation);
            Assert.True(tracker.isOnAmbitions());
            Assert.Equal("amb0", tracker.getCurrent().getId());
            Assert.Equal(2, tracker.getObjectives().Count);

            tracker.evaluate(simulation);
            tracker.evaluate(simulation);
            Assert.Null(tracker.getCurrent());
            Assert.Equal(3, tracker.getObjectives().Count);
        }

        [Fact]
        public void testSimulation_RecordsCompletionTickForTheToast() {
            Simulation simulation = makeSimulation();
            // full, so the apples gather-wood rewards are not eaten and "Eat something" stays put
            simulation.getPlayer().setEnergy(100f);
            simulation.getPlayer().getInventory().addItem(ItemType.WOOD, ObjectiveCatalog.firstWoodTarget());
            int ticksPerSecond = simulation.getTickCounter().getTicksPerSecond();
            for (int i = 0; i < ticksPerSecond; i++) {
                simulation.fixedUpdate(FixedDeltaTime);
            }
            ObjectiveTracker tracker = simulation.getObjectiveTracker();
            Assert.Equal("gather-wood", tracker.getLastCompleted().getId());
            Assert.Equal(1, tracker.getLastCompletedTick());
        }
    }
}
