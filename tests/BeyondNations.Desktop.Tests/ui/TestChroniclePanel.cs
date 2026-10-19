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
    }
}
