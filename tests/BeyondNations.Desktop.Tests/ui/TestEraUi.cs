using System.Collections.Generic;
using System.Linq;
using Xunit;

using beyondnations;
using beyondnations.desktop;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.boxes;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests {

    /**
    * The era box on the HUD and the end-of-run screen, decided without an
    * ImGui frame: what they say, which buttons are offered, how a drag
    * scrolls the history, and how the screens get there.
    */
    public class TestEraUi {

        private static Simulation makeSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            return new Simulation(config);
        }

        [Fact]
        public void testEraBox_ANewRun_IsTheCampWithTheVillageAhead() {
            Simulation simulation = makeSimulation();
            Assert.Equal("Era: Camp (1/4)", EraBox.headline(simulation));
            Assert.Equal("Village: 0/4", EraBox.progressLabel(simulation));
            Assert.Equal(1f / EraRules.VillageDay / 4f, EraBox.progressFraction(simulation), 4); // day 1 of the days the village waits for
            Assert.Null(EraBox.collapseWarning(simulation));
            List<KeyValuePair<string, bool>> lines = EraBox.detailLines(simulation);
            Assert.Equal("To reach the Village era:", lines[0].Key);
            Assert.Contains(lines, line => line.Key == "[ ] Members 0/" + EraRules.VillageMembers);
            Assert.Contains(lines, line => line.Key.StartsWith("Unlocks: Workshop"));
            // victories are not shown until they are within reach
            Assert.DoesNotContain(lines, line => line.Key == "Victories:");
        }

        [Fact]
        public void testEraBox_ATown_ShowsTheVictories() {
            Simulation simulation = makeSimulation();
            SettlementShowcase.stage(simulation);
            simulation.getEraTracker().stageEnding(simulation, Era.TOWN, RunOutcome.NONE);
            Assert.Equal("Era: Town (3/4)", EraBox.headline(simulation));
            Assert.StartsWith("Kingdom: ", EraBox.progressLabel(simulation));
            List<KeyValuePair<string, bool>> lines = EraBox.detailLines(simulation);
            Assert.Contains(lines, line => line.Key == "Victories:");
            Assert.Contains(lines, line => line.Key.StartsWith("Dominion: "));
            Assert.Contains(lines, line => line.Key.StartsWith("Prosperity: "));
            Assert.Contains(lines, line => line.Key.StartsWith("Concord: "));
            Assert.True(EraBox.progressFraction(simulation) > 0f);
        }

        [Fact]
        public void testEraBox_TapsOpenAndClosed() {
            EraBox box = new EraBox();
            Assert.False(box.isExpanded());
            box.setExpanded(true);
            Assert.True(box.isExpanded());
        }

        [Fact]
        public void testEndOfRun_AVictory_OffersToPlayOn() {
            Assert.Equal(new[] { EndOfRunScreen.ContinueLabel, EndOfRunScreen.NewWorldLabel, EndOfRunScreen.MainMenuLabel },
                EndOfRunScreen.buttonLabels(RunOutcome.PROSPERITY));
            Assert.Equal(new[] { EndOfRunScreen.NewWorldLabel, EndOfRunScreen.LoadLabel, EndOfRunScreen.MainMenuLabel },
                EndOfRunScreen.buttonLabels(RunOutcome.DEFEAT));
        }

        [Fact]
        public void testEndOfRun_TheStagedVictory_TellsItsStory() {
            Simulation simulation = makeSimulation();
            EndingShowcase.stage(simulation, true);
            EraTracker eras = simulation.getEraTracker();
            Assert.True(eras.shouldShowOutcome());
            Assert.Equal("Victory: Prosperity", RunSummary.title(eras));
            Assert.Equal("The History of " + eras.getNationName(), RunSummary.historyTitle(eras));
            List<string> stats = RunSummary.statLines(simulation);
            Assert.Equal("Era reached: Kingdom (4 of 4)", stats[0]);
            Assert.StartsWith("Days survived: ", stats[1]);
            Assert.Contains(stats, line => line.StartsWith("Members: "));
            List<HistoryEntry> history = EndOfRunScreen.historyLines(simulation);
            Assert.Contains(history, entry => entry.getText().Contains("was born"));
            Assert.Equal(eras.getNationName() + " won a Prosperity victory.", history.Last().getText());
            Assert.True(history.Last().isMilestone());
        }

        [Fact]
        public void testEndOfRun_TheStagedDefeat() {
            Simulation simulation = makeSimulation();
            EndingShowcase.stage(simulation, false);
            Assert.Equal("Defeat", RunSummary.title(simulation.getEraTracker()));
            Assert.Contains("collapsed", RunSummary.subtitle(simulation.getEraTracker()));
        }

        [Fact]
        public void testHistory_DragsWithTheFinger_AndStopsAtTheEnds() {
            Assert.Equal(90f, EndOfRunScreen.dragScroll(100f, 500f, 10f)); // finger down: text down, scroll back
            Assert.Equal(110f, EndOfRunScreen.dragScroll(100f, 500f, -10f));
            Assert.Equal(0f, EndOfRunScreen.dragScroll(5f, 500f, 40f));
            Assert.Equal(500f, EndOfRunScreen.dragScroll(495f, 500f, -40f));
            Assert.Equal(0f, EndOfRunScreen.dragScroll(0f, 0f, -40f));
        }

        [Fact]
        public void testScreens_ARunEnds_OnlyFromTheWorld_AndEscapeDoesNotSkipIt() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.PAUSE);
            screens.runEnded();
            Assert.Equal(ScreenType.PAUSE, screens.getCurrent());
            screens.goTo(ScreenType.WORLD);
            screens.runEnded();
            Assert.Equal(ScreenType.END_OF_RUN, screens.getCurrent());
            Assert.True(screens.isWorldVisible());
            Assert.False(screens.shouldAdvanceSimulation());
            Assert.True(screens.escapePressed());
            Assert.Equal(ScreenType.END_OF_RUN, screens.getCurrent());
        }

        [Fact]
        public void testStageFlags_Parse() {
            Assert.True(GameOptions.parse(new[] { "--stage-victory" }).StageVictory);
            Assert.True(GameOptions.parse(new[] { "--stage-defeat" }).StageDefeat);
            Assert.False(GameOptions.parse(new string[0]).StageVictory);
        }

        [Fact]
        public void testBuildBuildingHint_NamesTheCommandBarOnATouchScreen() {
            Assert.Equal("a Build button", ObjectiveKeyNames.ofTouch(HintKey.BuildBuilding));
        }

        [Fact]
        public void testBuildBuildingHint_NamesTheBuildKey() {
            Assert.Equal(beyondnations.desktop.input.KeyBindings.BuildBuilding.ToString(), ObjectiveKeyNames.of(HintKey.BuildBuilding));
        }
    }
}
