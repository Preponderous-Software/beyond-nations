using System;
using Xunit;

using beyondnations;
using beyondnations.desktop.input;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.boxes;

namespace beyondnationstests {

    /**
    * What the objective tracker shows, decided without an ImGui frame.
    */
    // reads text that names keys or buttons by TouchLayout, which another test may switch on
    [Collection("TouchLayout")]
    public class TestObjectiveTrackerBox {
        private const float FixedDeltaTime = 1f / 50f;

        private static Simulation makeSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            return new Simulation(config);
        }

        private static void tick(Simulation simulation, int count) {
            for (int i = 0; i < count; i++) {
                simulation.fixedUpdate(FixedDeltaTime);
            }
        }

        [Fact]
        public void testKeyNames_ComeFromTheBindingTable() {
            Assert.Equal(KeyBindings.Interact.ToString(), ObjectiveKeyNames.of(HintKey.Interact));
            Assert.Equal(KeyBindings.CreateNewNation.ToString(), ObjectiveKeyNames.of(HintKey.CreateNewNation));
            Assert.Equal(KeyBindings.FoundSettlement.ToString(), ObjectiveKeyNames.of(HintKey.FoundSettlement));
            Assert.Equal(KeyBindings.BuildStall.ToString(), ObjectiveKeyNames.of(HintKey.BuildStall));
        }

        [Fact]
        public void testEveryHintKey_HasABinding() {
            foreach (HintKey key in (HintKey[]) Enum.GetValues(typeof(HintKey))) {
                Assert.NotEqual(key.ToString(), ObjectiveKeyNames.of(key));
            }
        }

        [Fact]
        public void testEveryObjectiveHint_ResolvesToNoLeftoverTokens() {
            Simulation simulation = makeSimulation();
            foreach (Objective objective in ObjectiveCatalog.firstArc()) {
                string hint = ObjectiveKeyNames.resolve(objective.getHint(simulation));
                Assert.DoesNotContain("{", hint);
            }
            for (int i = 0; i < ObjectiveCatalog.AmbitionCategoryCount; i++) {
                Assert.DoesNotContain("{", ObjectiveKeyNames.resolve(ObjectiveCatalog.ambition(i).getHint(simulation)));
            }
        }

        [Fact]
        public void testOpeningHint_NamesTheInteractKey() {
            Simulation simulation = makeSimulation();
            Assert.Contains("press " + KeyBindings.Interact + " to chop", ObjectiveTrackerBox.hint(simulation));
        }

        [Fact]
        public void testHeading_NumbersTheArcThenSaysAmbition() {
            Simulation simulation = makeSimulation();
            ObjectiveTracker tracker = simulation.getObjectiveTracker();
            Assert.Equal("Objective 1", ObjectiveTrackerBox.heading(tracker));

            ObjectiveTracker ambitionsOnly = new ObjectiveTracker(new Objective[0], ObjectiveCatalog.ambition);
            Assert.Equal("Ambition", ObjectiveTrackerBox.heading(ambitionsOnly));
        }

        [Fact]
        public void testToast_ShowsAfterCompletionThenExpires() {
            Simulation simulation = makeSimulation();
            int ticksPerSecond = simulation.getTickCounter().getTicksPerSecond();
            Assert.Null(ObjectiveTrackerBox.toastText(simulation));

            simulation.getPlayer().getInventory().addItem(ItemType.WOOD, ObjectiveCatalog.firstWoodTarget());
            tick(simulation, 1);
            string toast = ObjectiveTrackerBox.toastText(simulation);
            Assert.NotNull(toast);
            Assert.StartsWith("Gather wood", toast);
            Assert.Contains("+2 apples", toast);

            tick(simulation, (int) (ObjectiveTrackerBox.ToastSeconds * ticksPerSecond));
            Assert.Null(ObjectiveTrackerBox.toastText(simulation));
        }

        [Fact]
        public void testWidth_FitsSmallAndLargeWindows() {
            Assert.Equal(ObjectiveTrackerBox.MinWidth, ObjectiveTrackerBox.widthFor(320));
            Assert.Equal(ObjectiveTrackerBox.MaxWidth, ObjectiveTrackerBox.widthFor(1920));
            float at1024 = ObjectiveTrackerBox.widthFor(1024);
            Assert.InRange(at1024, ObjectiveTrackerBox.MinWidth, ObjectiveTrackerBox.MaxWidth);
        }
    }
}
