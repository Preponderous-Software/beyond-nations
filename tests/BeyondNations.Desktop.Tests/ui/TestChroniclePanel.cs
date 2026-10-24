using System.Collections.Generic;
using Silk.NET.Input;
using Xunit;

using beyondnations;
using beyondnations.desktop.input;
using beyondnations.desktop.ui;
using beyondnationstests.desktop.input;

namespace beyondnationstests.desktop.ui {

    /**
    * The chronicle HUD's decisions -- which view is open, how entries fade,
    * how wide the strip is, how lines are cut and which entries are listed --
    * checked without a window.
    */
    public class TestChroniclePanel {

        private static Simulation createSimulation() {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(1234);
            return new Simulation(gameConfig);
        }

        [Fact]
        public void view_cyclesRecentFullHidden() {
            ChronicleView view = new ChronicleView();
            Assert.Equal(ChronicleMode.Recent, view.getMode());
            Assert.Equal(ChronicleMode.Full, view.cycle());
            Assert.Equal(ChronicleMode.Hidden, view.cycle());
            Assert.Equal(ChronicleMode.Recent, view.cycle());
        }

        [Fact]
        public void chronicleKey_cyclesTheControllersView_andSaysSo() {
            Simulation simulation = createSimulation();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            source.press(KeyBindings.ToggleChronicle);
            inputService.update();
            controller.update(simulation, inputService);
            // Held across frames: still one step.
            inputService.update();
            controller.update(simulation, inputService);

            Assert.Equal(ChronicleMode.Full, controller.getChronicleView().getMode());
            Assert.Equal("Chronicle: full history.", simulation.getPlayer().getStatus().getStatus());
        }

        [Fact]
        public void fade_fullWhileFresh_thenDownToTheFloor() {
            Assert.Equal(1f, ChroniclePanel.fadeAlpha(0));
            Assert.Equal(1f, ChroniclePanel.fadeAlpha(ChroniclePanel.FreshTicks));
            float middle = ChroniclePanel.fadeAlpha((ChroniclePanel.FreshTicks + ChroniclePanel.FadedTicks) / 2);
            Assert.True(middle < 1f && middle > ChroniclePanel.MinAlpha);
            Assert.Equal(ChroniclePanel.MinAlpha, ChroniclePanel.fadeAlpha(ChroniclePanel.FadedTicks));
            Assert.Equal(ChroniclePanel.MinAlpha, ChroniclePanel.fadeAlpha(1000000));
        }

        [Theory]
        [InlineData(1280f, 480f)]   // capped
        [InlineData(800f, 360f)]    // the gap between the 200 px box columns
        [InlineData(500f, 220f)]    // never narrower than readable...
        [InlineData(200f, 180f)]    // ...unless the screen itself is narrower
        public void compactWidth_fitsBetweenTheBoxColumns(float screen, float expected) {
            Assert.Equal(expected, ChroniclePanel.compactWidth(screen));
        }

        [Fact]
        public void compactTop_stacksUnderWhateverIsAbove() {
            Assert.Equal(ChroniclePanel.CompactTop, ChroniclePanel.compactTop(0f));
            Assert.Equal(80f + ChroniclePanel.StackGap, ChroniclePanel.compactTop(80f));
        }

        [Fact]
        public void fullSize_staysOnScreen() {
            System.Numerics.Vector2 large = ChroniclePanel.fullSize(1920f, 1080f);
            Assert.Equal(640f, large.X);
            Assert.Equal(520f, large.Y);
            System.Numerics.Vector2 small = ChroniclePanel.fullSize(480f, 320f);
            Assert.True(small.X <= 460f && small.Y <= 300f && small.Y >= 180f);
        }

        [Fact]
        public void fitToWidth_cutsWithAnEllipsis() {
            System.Func<string, float> measure = s => s.Length * 7f;
            Assert.Equal("short", ChroniclePanel.fitToWidth("short", 100f, measure));
            string cut = ChroniclePanel.fitToWidth("Day 3, dusk - Mira founded the nation of Ironvale.", 140f, measure);
            Assert.EndsWith("...", cut);
            Assert.True(measure(cut) <= 140f);
            Assert.StartsWith("Day 3, dusk", cut);
        }

        [Fact]
        public void selectAndDisplay_markThePlayersNation() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            Pawn rival = new Pawn(System.Numerics.Vector3.Zero, "Osric", simulation.getRandom());
            simulation.getEntityRepository().addEntity(rival);
            Nation rivalNation = new Nation("Ashford", rival.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(rivalNation);
            simulation.getEventProducer().produceNationCreationEvent(rivalNation);
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);

            IReadOnlyList<ChronicleEntry> entries = simulation.getChronicle().getEntries();
            Assert.Equal(2, entries.Count);
            Assert.Equal(2, ChroniclePanel.select(entries, player, false).Count);
            List<ChronicleEntry> mine = ChroniclePanel.select(entries, player, true);
            Assert.Single(mine);
            Assert.StartsWith(ChroniclePanel.MineMarker, ChroniclePanel.displayLine(mine[0], player));
            Assert.False(ChroniclePanel.displayLine(entries[0], player).StartsWith(ChroniclePanel.MineMarker));
        }

        [Fact]
        public void scope_startsOnYourNews_andToggles() {
            ChronicleView view = new ChronicleView();
            Assert.Equal(ChronicleScope.YourNews, view.getScope());
            Assert.Equal(ChronicleScope.AllNations, view.toggleScope());
            Assert.Equal(ChronicleScope.YourNews, view.toggleScope());
            // The mode is its own setting.
            Assert.Equal(ChronicleMode.Recent, view.getMode());
        }

        [Fact]
        public void scopeKey_togglesTheControllersView_andSaysSo() {
            Simulation simulation = createSimulation();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            source.press(KeyBindings.ToggleChronicleScope);
            inputService.update();
            controller.update(simulation, inputService);

            Assert.Equal(ChronicleScope.AllNations, controller.getChronicleView().getScope());
            Assert.Equal("Chronicle: showing all nations.", simulation.getPlayer().getStatus().getStatus());
        }

        [Fact]
        public void headingAndEmptyLine_nameTheScope() {
            Assert.Contains("your news", ChroniclePanel.recentHeading(ChronicleScope.YourNews));
            Assert.Contains("all nations", ChroniclePanel.recentHeading(ChronicleScope.AllNations));
            Assert.Contains("{ToggleChronicle}", ChroniclePanel.recentHeading(ChronicleScope.YourNews));
            Assert.Contains("neighbours", ChroniclePanel.emptyMessage(ChronicleScope.YourNews, false));
            Assert.Equal("Nothing worth telling has happened yet.", ChroniclePanel.emptyMessage(ChronicleScope.AllNations, false));
            Assert.Equal("Nothing about you or your nation yet.", ChroniclePanel.emptyMessage(ChronicleScope.YourNews, true));
        }

        [Fact]
        public void scopeButton_marksTheChosenOne_notByColourAlone() {
            Assert.Equal("> " + ChroniclePanel.YourNewsLabel, ChroniclePanel.scopeButtonLabel(ChroniclePanel.YourNewsLabel, true));
            Assert.Equal(ChroniclePanel.AllNationsLabel, ChroniclePanel.scopeButtonLabel(ChroniclePanel.AllNationsLabel, false));
        }

        [Fact]
        public void countLine_saysHowManyAreListed_whenFiltered() {
            Assert.Equal("3 entries.", ChroniclePanel.countLine(3, 3, 3));
            Assert.Equal("495 entries; the latest 200 are kept. Listing 12.", ChroniclePanel.countLine(495, 200, 12));
        }

        /** A world with the player's nation, a neighbour on its border and two far nations quarrelling. */
        private static (Simulation simulation, List<ChronicleEntry> entries) quarrellingWorld() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            addSettlement(simulation, own, 0f);
            Nation border = addNation(simulation, "Westham", 300f);
            Nation farA = addNation(simulation, "Aland", 6000f);
            Nation farB = addNation(simulation, "Borrow", 6300f);
            List<ChronicleEntry> entries = new List<ChronicleEntry> {
                new ChronicleEntry(0, EventType.NationRelationChange, 0, 1, "dawn", "Aland and Borrow have become rivals.", new List<NationId> { farA.getId(), farB.getId() }, null),
                new ChronicleEntry(1, EventType.NationRelationChange, 0, 1, "dawn", "Westham and Aland have become rivals.", new List<NationId> { border.getId(), farA.getId() }, null),
                new ChronicleEntry(2, EventType.NationRaid, 0, 1, "dawn", "Borrow raided Aland.", new List<NationId> { farB.getId(), farA.getId() }, null),
                new ChronicleEntry(3, EventType.Statecraft, 0, 1, "dawn", "You did something.", new List<NationId> { own.getId() }, new List<EntityId> { player.getId() }),
                new ChronicleEntry(4, EventType.NationRaid, 0, 1, "dawn", "Aland raided Borrow.", new List<NationId> { farA.getId(), farB.getId() }, null),
            };
            return (simulation, entries);
        }

        private static Nation addNation(Simulation simulation, string name, float x) {
            Pawn leader = new Pawn(System.Numerics.Vector3.Zero, name + " Lord", simulation.getRandom());
            simulation.getEntityRepository().addEntity(leader);
            Nation nation = new Nation(name, leader.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            leader.setNationId(nation.getId());
            addSettlement(simulation, nation, x);
            return nation;
        }

        private static void addSettlement(Simulation simulation, Nation nation, float x) {
            Settlement settlement = new Settlement(new System.Numerics.Vector3(x, 1, 0), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
        }

        [Fact]
        public void select_yourNewsDropsFarQuarrels_allNationsKeepsThem() {
            (Simulation simulation, List<ChronicleEntry> entries) = quarrellingWorld();
            Player player = simulation.getPlayer();
            ChronicleFocus focus = ChronicleFocus.of(simulation);

            List<ChronicleEntry> yours = ChroniclePanel.select(entries, player, focus, ChronicleScope.YourNews, false);
            Assert.Equal(new[] { 1, 3 }, yours.ConvertAll(e => e.getSequence()).ToArray());
            Assert.Equal(5, ChroniclePanel.select(entries, player, focus, ChronicleScope.AllNations, false).Count);
            List<ChronicleEntry> mine = ChroniclePanel.select(entries, player, focus, ChronicleScope.AllNations, true);
            Assert.Single(mine);
            Assert.Equal(3, mine[0].getSequence());
        }

        [Fact]
        public void recent_stripTakesTheLastInScope() {
            (Simulation simulation, List<ChronicleEntry> entries) = quarrellingWorld();
            ChronicleFocus focus = ChronicleFocus.of(simulation);

            List<ChronicleEntry> yours = ChroniclePanel.recent(entries, focus, ChronicleScope.YourNews, ChroniclePanel.RecentCount);
            Assert.Equal(new[] { 1, 3 }, yours.ConvertAll(e => e.getSequence()).ToArray());
            List<ChronicleEntry> all = ChroniclePanel.recent(entries, focus, ChronicleScope.AllNations, 2);
            Assert.Equal(new[] { 3, 4 }, all.ConvertAll(e => e.getSequence()).ToArray());
        }
    }
}
