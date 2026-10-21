using System;
using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * Being a ruler on screen: the Laws tab, the war and peace buttons on
    * the nations screen, the succession and game-over screens, how the host
    * gets to them, and that an ended run is never saved. Read off the
    * --stage-diplomacy world, with no window.
    */
    public class TestRulerUi {

        private static Simulation staged(out DiplomacyShowcase.Staged result) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            result = DiplomacyShowcase.stage(simulation);
            return simulation;
        }

        [Fact]
        public void testLawsPanel_ListsEveryLaw_WithItsSettingAndWhatItCosts() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);

            List<LawsPanel.Row> rows = LawsPanel.getRows(simulation);

            Assert.Equal(4, rows.Count);
            Assert.Equal("Taxes: None", rows[0].title);
            Assert.Equal("No tax: members keep their coin.", rows[0].effect);
            Assert.Equal("Conscription: Off", rows[1].title);
            Assert.Equal("Borders: Closed", rows[2].title);
            Assert.Equal("Rationing: Off", rows[3].title);
            List<string> lines = LawsPanel.getLines(simulation);
            Assert.Equal("The laws of " + result.playerNation.getName() + ". Changes take 30 s to come in.", lines[0]);
            Assert.StartsWith("Defence 3", lines[1]);
        }

        [Fact]
        public void testLawsPanel_ShowsAChangeComingIn() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            new SetLawCommand(simulation.getNationRepository(), simulation.getRealm()).execute(simulation.getPlayer(), LawType.TAXES, 2);

            LawsPanel.Row taxes = LawsPanel.getRows(simulation)[0];

            Assert.Equal("Taxes: None (High in 30 s)", taxes.title);
            Assert.Equal(2, taxes.pending);
        }

        [Fact]
        public void testLawsPanel_WithoutANation_SaysSo() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);

            Assert.Equal(new List<string> { LawsPanel.NoNation }, LawsPanel.getLines(simulation));
            Assert.Empty(LawsPanel.getRows(simulation));
        }

        [Fact]
        public void testNationScreen_ShowsWarAndOffersPeaceToTheHostile() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            new DeclareWarCommand(simulation.getNationRepository(), simulation.getRealm()).execute(simulation.getPlayer(), result.hostile);

            List<NationScreen.Row> rows = NationScreen.getRows(simulation);

            Assert.True(rows[0].atWar);
            Assert.True(rows[0].canOfferPeace);
            Assert.Contains(", at war", rows[0].text);
            Assert.False(rows[1].atWar);
            Assert.False(rows[1].canOfferPeace);
        }

        [Fact]
        public void testSuccessionScreen_TellsWhoDiedAndWhoRulesNow() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);
            simulation.getPlayer().setEnergy(0.0001f);

            simulation.fixedUpdate(1f / 50f);
            Assert.True(RealmScreens.afterStep(simulation, screens));

            Assert.Equal(ScreenType.SUCCESSION, screens.getCurrent());
            Assert.True(screens.isWorldVisible());
            Assert.Equal(SuccessionScreen.SuccessionTitle, SuccessionScreen.getTitle(simulation));
            List<string> lines = SuccessionScreen.getLines(simulation);
            Assert.Equal("The founder died of hunger after a reign of less than an hour.", lines[0]);
            Assert.Equal("You go on as " + simulation.getPlayer().getName() + ", the longest sworn of your people.", lines[1]);
            Assert.Contains("1. The founder of " + result.playerNation.getName() + ", less than an hour", lines);
            Assert.Contains("2. " + simulation.getPlayer().getName() + ", reigning", lines);

            // shown once; the next step goes on as usual
            simulation.fixedUpdate(1f / 50f);
            Assert.False(RealmScreens.afterStep(simulation, screens));
            screens.escapePressed();
            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
        }

        [Fact]
        public void testTheEndOfTheLine_IsGameOver_AndIsNeverSaved() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(simulation.getPlayer());
            MemorySaveStorage storage = new MemorySaveStorage();
            SaveSession saves = new SaveSession(storage, "test", true);
            saves.beginNewWorld(simulation, new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
            Assert.True(saves.save(simulation, false));
            byte[] before = storage.Files[saves.getCurrentSlot() + ".bnsave"];
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);

            simulation.getPlayer().setEnergy(0f);
            simulation.fixedUpdate(1f / 50f);
            Assert.True(RealmScreens.afterStep(simulation, screens));

            // one end-of-run screen: the end of a line is a defeat (EraTracker)
            Assert.Equal(ScreenType.END_OF_RUN, screens.getCurrent());
            Assert.Equal(RunOutcome.DEFEAT, simulation.getEraTracker().getOutcome());
            Assert.Equal("Defeat", RunSummary.title(simulation.getEraTracker()));
            Assert.Contains("died with no heir", RunSummary.subtitle(simulation));
            Assert.Contains(EndOfRunScreen.LoadLabel, EndOfRunScreen.buttonLabels(RunOutcome.DEFEAT));
            Assert.False(saves.save(simulation, false));
            Assert.Equal(SaveSession.RunOverFeedback, saves.getFeedback());
            Assert.False(saves.isFeedbackError());
            Assert.Equal(before, storage.Files[saves.getCurrentSlot() + ".bnsave"]);
            // Escape does not skip past the end of a run; its buttons lead on
            screens.escapePressed();
            Assert.Equal(ScreenType.END_OF_RUN, screens.getCurrent());
        }

        [Fact]
        public void testOptions_OpenOnTheLawsTab_AndStageASuccession() {
            try {
                GameOptions options = GameOptions.parse(new string[] { "--start-screen", "laws", "--stage-succession" });
                Assert.Equal(ScreenType.NATIONS, options.StartScreen);
                Assert.True(NationScreen.startOnLaws);
                Assert.True(new NationScreen().isShowingLaws());
                Assert.True(options.StageDiplomacy);
                Assert.True(options.StageSuccession);
            } finally {
                NationScreen.startOnLaws = false;
            }
        }
    }
}
