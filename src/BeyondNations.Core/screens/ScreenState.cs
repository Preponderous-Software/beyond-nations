namespace beyondnations {

    /**
    * Which screen the game is showing.
    *
    * This lived in BeyondNations.cs, the single MonoBehaviour in the project,
    * alongside a Unity Update() that switched on it. That class is dropped
    * rather than adapted, as #216 requires, and the state machine becomes host
    * state here.
    *
    * The transitions are the ones the Unity version had: any key leaves the
    * title screen, Escape moves between the main menu, the world and the pause
    * screen, and the config screen is reached from the main menu. The
    * inventory screen, which the Unity version never had, is opened from the
    * world with I and left with I or Escape.
    */
    public class ScreenState {
        private ScreenType current = ScreenType.TITLE;
        private ScreenType previous = ScreenType.TITLE;

        public ScreenType getCurrent() {
            return current;
        }

        public ScreenType getPrevious() {
            return previous;
        }

        public bool isWorldActive() {
            return current == ScreenType.WORLD;
        }

        /**
        * Whether the world should be drawn this frame. The inventory screen
        * is laid over the world rather than replacing it, so the world stays
        * visible behind it even though it has stopped advancing.
        */
        public bool isWorldVisible() {
            return current == ScreenType.WORLD || current == ScreenType.INVENTORY;
        }

        /**
        * The world keeps ticking behind the pause screen only if the host says
        * so; by default the simulation advances only while the world is shown.
        */
        public bool shouldAdvanceSimulation() {
            return current == ScreenType.WORLD;
        }

        public void goTo(ScreenType screen) {
            if (screen == current) {
                return;
            }
            previous = current;
            current = screen;
            Log.info("screen: " + previous + " -> " + current);
        }

        /**
        * Any key press leaves the title screen, as before.
        */
        public void anyKeyPressed() {
            if (current == ScreenType.TITLE) {
                goTo(ScreenType.MAIN_MENU);
            }
        }

        /**
        * The inventory key opens the inventory screen from the world and
        * closes it again (#181). It means nothing on any other screen.
        */
        public void inventoryPressed() {
            if (current == ScreenType.WORLD) {
                goTo(ScreenType.INVENTORY);
            } else if (current == ScreenType.INVENTORY) {
                goTo(ScreenType.WORLD);
            }
        }

        /**
        * Escape, which means something different on each screen. Returns false
        * when the press should quit the game, which is what Escape did on the
        * main menu.
        */
        public bool escapePressed() {
            if (current == ScreenType.WORLD) {
                goTo(ScreenType.PAUSE);
                return true;
            }
            if (current == ScreenType.INVENTORY) {
                goTo(ScreenType.WORLD);
                return true;
            }
            if (current == ScreenType.PAUSE) {
                goTo(ScreenType.WORLD);
                return true;
            }
            if (current == ScreenType.CONFIG) {
                goTo(ScreenType.MAIN_MENU);
                return true;
            }
            if (current == ScreenType.MAIN_MENU) {
                return false;
            }
            return true;
        }
    }
}
