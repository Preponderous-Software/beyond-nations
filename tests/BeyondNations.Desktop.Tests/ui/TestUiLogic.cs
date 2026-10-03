using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.boxes;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests {

    /**
    * The parts of the user interface that decide something rather than draw
    * something. Nothing here needs a GL context or an ImGui frame.
    */
    public class TestConfigValues {

        [Fact]
        public void testValuesRoundTripThroughGameConfig() {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setChunkSize(9);
            gameConfig.setLocationScale(11);
            gameConfig.setRespawnPawns(true);
            gameConfig.setKeepInventoryOnDeath(false);
            gameConfig.setLagPreventionEnabled(false);

            ConfigValues values = ConfigValues.readFrom(gameConfig);

            Assert.Equal(9, values.ChunkSize);
            Assert.Equal(11, values.LocationScale);
            Assert.True(values.RespawnPawns);
            Assert.False(values.KeepInventoryOnDeath);
            Assert.False(values.LagPreventionEnabled);
        }

        [Fact]
        public void testEditsApplyToTheRunningConfig() {
            GameConfig gameConfig = new GameConfig();
            ConfigValues values = ConfigValues.readFrom(gameConfig);

            values.ChunkSize = 13;
            values.LocationScale = 15;
            values.RespawnPawns = true;
            values.KeepInventoryOnDeath = false;
            values.LagPreventionEnabled = false;
            values.applyTo(gameConfig);

            // The config screen edits the same GameConfig the simulation holds,
            // so a change made on that screen is live immediately.
            Assert.Equal(13, gameConfig.getChunkSize());
            Assert.Equal(15, gameConfig.getLocationScale());
            Assert.True(gameConfig.getRespawnPawns());
            Assert.False(gameConfig.getKeepInventoryOnDeath());
            Assert.False(gameConfig.getLagPreventionEnabled());
        }

        [Fact]
        public void testTheSimulationSeesAConfigChangeMadeAfterItStarted() {
            GameConfig gameConfig = new GameConfig();
            Simulation simulation = new Simulation(gameConfig);

            ConfigValues values = ConfigValues.readFrom(gameConfig);
            values.LagPreventionEnabled = !gameConfig.getLagPreventionEnabled();
            values.applyTo(gameConfig);

            Assert.Equal(gameConfig.getLagPreventionEnabled(),
                         simulation.getGameConfig().getLagPreventionEnabled());
        }

        [Fact]
        public void testBoundsAreOrderedAndSane() {
            Assert.True(ConfigValues.MinChunkSize < ConfigValues.MaxChunkSize);
            Assert.True(ConfigValues.MinLocationScale < ConfigValues.MaxLocationScale);
        }
    }

    /**
    * The debug overlay, which #221 requires to report the same fields the
    * Unity one did.
    */
    public class TestDebugInfoBox {

        private static Simulation newSimulation() {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(42);
            return new Simulation(gameConfig);
        }

        [Fact]
        public void testOverlayReportsItsFullSetOfFields() {
            DebugInfoBox box = new DebugInfoBox(newSimulation());

            List<string> lines = box.getLines();

            Assert.Equal(DebugInfoBox.NumDataPoints, lines.Count);
        }

        [Fact]
        public void testOverlayReportsRenderDistance() {
            Simulation simulation = newSimulation();
            simulation.getPlayer().decreaseRenderDistance();
            int expected = simulation.getPlayer().getRenderDistance();

            DebugInfoBox box = new DebugInfoBox(simulation);
            List<string> lines = box.getLines();

            // #219 records the render distance; this is the overlay that
            // surfaces it, which is that issue's remaining acceptance criterion.
            Assert.Contains(lines, line => line.Contains(expected.ToString()));
        }

        [Fact]
        public void testOverlayReportsTheFrameRateItIsGiven() {
            DebugInfoBox box = new DebugInfoBox(newSimulation());
            box.setFramesPerSecond(144);

            List<string> lines = box.getLines();

            Assert.Contains(lines, line => line.Contains("144"));
        }

        [Fact]
        public void testEveryLineIsPopulated() {
            DebugInfoBox box = new DebugInfoBox(newSimulation());

            foreach (string line in box.getLines()) {
                Assert.False(string.IsNullOrWhiteSpace(line));
            }
        }
    }

    /**
    * The text of the inventory screen (#181), which needs no ImGui frame.
    */
    public class TestInventoryScreen {

        [Fact]
        public void testListsOccupiedSlotsInSlotOrder() {
            Inventory inventory = new Inventory(7);
            inventory.addItem(ItemType.STONE, 3);
            inventory.addItem(ItemType.CHICKEN_MEAT, 2);

            List<string> lines = InventoryScreen.getLines(inventory);

            Assert.Equal(new List<string> { "Coins: 7", "Stone: 3", "Chicken Meat: 2" }, lines);
        }

        [Fact]
        public void testSkipsSlotsEmptiedByRemoval() {
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.WOOD, 4);
            inventory.addItem(ItemType.APPLE, 1);
            inventory.removeItem(ItemType.WOOD, 4);

            List<string> lines = InventoryScreen.getLines(inventory);

            Assert.Equal(new List<string> { "Apples: 1" }, lines);
        }

        [Fact]
        public void testSaysSoWhenTheInventoryIsEmpty() {
            List<string> lines = InventoryScreen.getLines(new Inventory(0));

            Assert.Equal(new List<string> { InventoryScreen.EmptyLine }, lines);
        }

        [Fact]
        public void testEveryItemTypeHasAName() {
            foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType))) {
                Assert.NotEqual(itemType.ToString(), InventoryScreen.getItemName(itemType));
            }
        }
    }

    /**
    * The screen state machine as the user interface drives it.
    */
    public class TestUiScreenFlow {

        [Fact]
        public void testTitleLeadsToMainMenuAndOnToTheWorld() {
            ScreenState screens = new ScreenState();

            screens.anyKeyPressed();
            Assert.Equal(ScreenType.MAIN_MENU, screens.getCurrent());

            screens.goTo(ScreenType.WORLD);
            Assert.True(screens.isWorldActive());
            Assert.True(screens.shouldAdvanceSimulation());
        }

        [Fact]
        public void testPauseStopsTheSimulationAdvancing() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);

            screens.escapePressed();

            Assert.Equal(ScreenType.PAUSE, screens.getCurrent());
            Assert.False(screens.shouldAdvanceSimulation());
        }

        [Fact]
        public void testConfigIsReachableAndReturns() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.MAIN_MENU);

            screens.goTo(ScreenType.CONFIG);
            Assert.Equal(ScreenType.CONFIG, screens.getCurrent());

            Assert.True(screens.escapePressed());
            Assert.Equal(ScreenType.MAIN_MENU, screens.getCurrent());
        }
    }
}
