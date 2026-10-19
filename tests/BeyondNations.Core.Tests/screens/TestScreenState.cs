using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The screen state machine, which #216 moved out of BeyondNations.cs. These
    * are the transitions the Unity version had, now checkable without a window.
    */
    public class TestScreenState {

        [Fact]
        public void testStartsOnTitle() {
            ScreenState screens = new ScreenState();

            Assert.Equal(ScreenType.TITLE, screens.getCurrent());
            Assert.False(screens.isWorldActive());
            Assert.False(screens.shouldAdvanceSimulation());
        }

        [Fact]
        public void testAnyKeyLeavesTitleForMainMenu() {
            ScreenState screens = new ScreenState();

            screens.anyKeyPressed();

            Assert.Equal(ScreenType.MAIN_MENU, screens.getCurrent());
        }

        [Fact]
        public void testAnyKeyDoesNothingOffTitle() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);

            screens.anyKeyPressed();

            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
        }

        [Fact]
        public void testEscapePausesTheWorldAndResumesIt() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);

            Assert.True(screens.escapePressed());
            Assert.Equal(ScreenType.PAUSE, screens.getCurrent());

            Assert.True(screens.escapePressed());
            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
        }

        [Fact]
        public void testEscapeLeavesConfigForMainMenu() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.CONFIG);

            Assert.True(screens.escapePressed());
            Assert.Equal(ScreenType.MAIN_MENU, screens.getCurrent());
        }

        [Fact]
        public void testEscapeOnMainMenuAsksToQuit() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.MAIN_MENU);

            // false means the host should close the window, which is what
            // Escape on the main menu did in the Unity build.
            Assert.False(screens.escapePressed());
        }

        [Fact]
        public void testSimulationAdvancesOnlyInTheWorld() {
            ScreenState screens = new ScreenState();

            screens.goTo(ScreenType.WORLD);
            Assert.True(screens.shouldAdvanceSimulation());

            screens.goTo(ScreenType.PAUSE);
            Assert.False(screens.shouldAdvanceSimulation());

            screens.goTo(ScreenType.CONFIG);
            Assert.False(screens.shouldAdvanceSimulation());
        }

        [Fact]
        public void testPreviousScreenIsRemembered() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);
            screens.goTo(ScreenType.PAUSE);

            Assert.Equal(ScreenType.WORLD, screens.getPrevious());
        }

        [Fact]
        public void testGoingToTheCurrentScreenIsANoOp() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);
            screens.goTo(ScreenType.WORLD);

            Assert.Equal(ScreenType.TITLE, screens.getPrevious());
        }

        [Fact]
        public void testInventoryKeyOpensAndClosesTheInventoryFromTheWorld() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);

            screens.inventoryPressed();
            Assert.Equal(ScreenType.INVENTORY, screens.getCurrent());

            screens.inventoryPressed();
            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
        }

        [Fact]
        public void testInventoryKeyDoesNothingOffTheWorld() {
            ScreenState screens = new ScreenState();

            screens.goTo(ScreenType.PAUSE);
            Assert.True(screens.isWorldVisible());

            foreach (ScreenType screen in new ScreenType[] { ScreenType.TITLE, ScreenType.MAIN_MENU, ScreenType.CONFIG }) {
                screens.goTo(screen);
                screens.inventoryPressed();
                Assert.Equal(screen, screens.getCurrent());
            }
        }

        [Fact]
        public void testEscapeClosesTheInventoryRatherThanPausing() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);
            screens.inventoryPressed();

            Assert.True(screens.escapePressed());
            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
        }

        [Fact]
        public void testInventoryPausesTheWorldButLeavesItVisible() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.INVENTORY);

            Assert.False(screens.shouldAdvanceSimulation());
            Assert.False(screens.isWorldActive());
            Assert.True(screens.isWorldVisible());
        }

        [Fact]
        public void testWorldIsVisibleOnlyOnTheWorldInventoryAndPauseScreens() {
            ScreenState screens = new ScreenState();

            screens.goTo(ScreenType.WORLD);
            Assert.True(screens.isWorldVisible());

            screens.goTo(ScreenType.PAUSE);
            Assert.True(screens.isWorldVisible());

            foreach (ScreenType screen in new ScreenType[] { ScreenType.TITLE, ScreenType.MAIN_MENU, ScreenType.CONFIG }) {
                screens.goTo(screen);
                Assert.False(screens.isWorldVisible());
            }
        }
    
        [Fact]
        public void testControlsOpenFromTheWorldAndCloseBackToIt() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);

            screens.controlsPressed();
            Assert.Equal(ScreenType.CONTROLS, screens.getCurrent());
            Assert.True(screens.isWorldVisible());
            Assert.False(screens.shouldAdvanceSimulation());

            screens.controlsPressed();
            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
        }

        [Fact]
        public void testEscapeClosesControlsBackToWhereTheyWereOpened() {
            ScreenState screens = new ScreenState();
            screens.goTo(ScreenType.PAUSE);
            screens.controlsPressed();

            Assert.True(screens.escapePressed());
            Assert.Equal(ScreenType.PAUSE, screens.getCurrent());

            screens.goTo(ScreenType.MAIN_MENU);
            screens.controlsPressed();
            Assert.False(screens.isWorldVisible());
            Assert.True(screens.escapePressed());
            Assert.Equal(ScreenType.MAIN_MENU, screens.getCurrent());
        }

        [Fact]
        public void testControlsKeyDoesNothingOnTitleConfigOrInventory() {
            ScreenState screens = new ScreenState();

            foreach (ScreenType screen in new ScreenType[] { ScreenType.TITLE, ScreenType.CONFIG, ScreenType.INVENTORY }) {
                screens.goTo(screen);
                screens.controlsPressed();
                Assert.Equal(screen, screens.getCurrent());
            }
        }
    }
}
