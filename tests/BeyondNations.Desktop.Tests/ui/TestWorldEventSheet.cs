using Xunit;

using beyondnations;
using beyondnations.desktop;
using beyondnations.desktop.ui;

namespace beyondnationstests.desktop.ui {

    /**
    * What the world event choice sheet shows (worldevents/), and the flag
    * that stages one. None of it needs a window.
    */
    public class TestWorldEventSheet {

        private static Simulation newSimulation() {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(42);
            return new Simulation(gameConfig);
        }

        [Fact]
        public void testNoSheet_WhileNothingIsWaitingOnThePlayer() {
            Assert.Null(WorldEventSheet.getView(newSimulation()));
            Assert.Null(WorldEventSheet.getView(null));
        }

        [Fact]
        public void testTheSheet_ShowsTheEventAndEveryChoice_GreyingOutWhatCannotBeTaken() {
            Simulation simulation = newSimulation();
            simulation.getPlayer().getInventory().setNumItems(ItemType.COIN, 0);
            simulation.getPlayer().getInventory().setNumItems(ItemType.WOOD, 25);
            Assert.True(simulation.getWorldEvents().startPlayerEventNow(simulation, RareMerchantEvent.Id));

            WorldEventSheet.View view = WorldEventSheet.getView(simulation);
            Assert.NotNull(view);
            Assert.Equal("A travelling merchant", view.Title);
            Assert.Contains("axe and a pickaxe", view.Body);
            Assert.Equal(3, view.Rows.Count);
            Assert.False(view.Rows[0].Available);
            Assert.StartsWith("Needs " + RareMerchantEvent.TOOLS_COINS + " coins", view.Rows[0].Detail);
            Assert.True(view.Rows[1].Available);
            Assert.True(view.Rows[2].Available);

            // choosing closes it
            Assert.NotNull(simulation.getWorldEvents().choose(simulation, 2));
            Assert.Null(WorldEventSheet.getView(simulation));
        }

        [Fact]
        public void testSmallerLines_NeverDrawnBelowTheFontAtlas() {
            // at 960x540 the sheet's font scale is about 1.04; its 0.9 detail
            // lines were drawn at 0.94 and lost the row the hyphens of
            // "Nothing now -- but" sit on (playtest 0.7.1 winter s03)
            float smallScreen = 1.04f;
            Assert.Equal(1f, WorldEventSheet.smallerScale(smallScreen, 0.9f));
            Assert.Equal(1f, WorldEventSheet.smallerScale(smallScreen, 0.8f));
            // a larger screen keeps the proportion
            Assert.Equal(1.35f, WorldEventSheet.smallerScale(1.5f, 0.9f), 3);
            Assert.Equal(1f, Overlay.atLeastAtlasSize(0.5f));
            Assert.Equal(2f, Overlay.atLeastAtlasSize(2f));
        }

        [Fact]
        public void testStageEvent_IsAFlag() {
            Assert.True(GameOptions.parse(new string[] { "--stage-event" }).StageEvent);
            Assert.False(GameOptions.parse(new string[] { }).StageEvent);
        }
    
        /** #502: once a choice has waited a while, the sheet says the world has stood still, for how long, and why. */
        [Fact]
        public void testPausedNote_SaysHowLongTheWorldHasStoodStill_OnceTheWaitIsLong() {
            Assert.Equal(WorldEventSheet.PausedNote, WorldEventSheet.pausedNote(0));
            Assert.Equal(WorldEventSheet.PausedNote, WorldEventSheet.pausedNote(WorldEventSheet.LongWaitSeconds - 0.1));
            Assert.Equal("The world has stood still for 40 seconds, waiting for your choice. Nothing moves until you choose.",
                WorldEventSheet.pausedNote(40.7));
            Assert.Equal("The world has stood still for 2 minutes, waiting for your choice. Nothing moves until you choose.",
                WorldEventSheet.pausedNote(130));
            Assert.Null(TestTouchText.namedKey(WorldEventSheet.pausedNote(130)));
        }

        [Fact]
        public void testDescribeWait() {
            Assert.Equal("1 second", WorldEventSheet.describeWait(1.2));
            Assert.Equal("59 seconds", WorldEventSheet.describeWait(59.9));
            Assert.Equal("1 minute", WorldEventSheet.describeWait(60));
            Assert.Equal("1 minute", WorldEventSheet.describeWait(119));
            Assert.Equal("25 minutes", WorldEventSheet.describeWait(25 * 60 + 5));
            Assert.Equal("0 seconds", WorldEventSheet.describeWait(-3));
        }

        /** #505: a centred line is shrunk to fit, but never below the atlas size (it wraps instead). */
        [Fact]
        public void testCentredLines_NeverShrinkBelowTheFontAtlas() {
            // fits: unchanged
            Assert.Equal(1.5f, Overlay.centredLineScale(1.5f, 300f, 380f));
            // a little too wide: shrunk in proportion while that stays at or above 1
            Assert.Equal(1.5f * 380f / 500f, Overlay.centredLineScale(1.5f, 500f, 380f), 4);
            // far too wide: held at the atlas size (textCentered wraps it)
            Assert.Equal(1f, Overlay.centredLineScale(1.5f, 900f, 380f));
            // asked for below the atlas size (the crafting screen's close hint at 960x540): raised to it
            Assert.Equal(1f, Overlay.centredLineScale(0.8f, 100f, 380f));
        }

        /** #505: the crafting screen's 0.9x lines go through the atlas floor. */
        [Fact]
        public void testCraftingScreen_SmallLinesUseTheAtlasFloor() {
            string root = System.AppContext.BaseDirectory;
            while (root != null && !System.IO.File.Exists(System.IO.Path.Combine(root, "BeyondNations.sln"))) {
                root = System.IO.Path.GetDirectoryName(root);
            }
            Assert.NotNull(root);
            string source = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "BeyondNations.Desktop", "ui", "screens", "CraftingScreen.cs"));
            Assert.DoesNotContain("SetWindowFontScale(scale * 0.", source);
            Assert.Contains("Overlay.atLeastAtlasSize(scale * 0.9f)", source);
        }
    }
}
