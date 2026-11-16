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
    }
}
