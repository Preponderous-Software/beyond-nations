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
        // Where the controls screen goes back to when it is closed.
        private ScreenType controlsReturn = ScreenType.MAIN_MENU;

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
        * Whether the world should be drawn this frame. The inventory and pause
        * screens are laid over the world rather than replacing it, so the
        * world stays visible behind them even though it has stopped advancing.
        */
        public bool isWorldVisible() {
            return isLaidOverWorld(current)
                || (current == ScreenType.CONTROLS && isLaidOverWorld(controlsReturn));
        }

        private static bool isLaidOverWorld(ScreenType screen) {
            return screen == ScreenType.WORLD || screen == ScreenType.INVENTORY || screen == ScreenType.NATIONS || screen == ScreenType.PAUSE;
        }

        /**
        * The screen the controls screen returns to when it closes.
        */
        public ScreenType getControlsReturn() {
            return controlsReturn;
        }

        /**
        * The controls key, and the Controls buttons, open the list of key
        * bindings from the world, the pause screen or the main menu, and close
        * it again back to where it was opened. Every binding used to be
        * documented only in the README, so a player in the game had no way to
        * find out what the keys did. The key means nothing on other screens.
        */
        public void controlsPressed() {
            if (current == ScreenType.CONTROLS) {
                goTo(controlsReturn);
                return;
            }
            if (current == ScreenType.WORLD || current == ScreenType.PAUSE || current == ScreenType.MAIN_MENU) {
                controlsReturn = current;
                goTo(ScreenType.CONTROLS);
            }
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
        * The nations key opens the nations screen from the world and closes
        * it again, as the inventory key does for the inventory.
        */
        public void nationsPressed() {
            if (current == ScreenType.WORLD) {
                goTo(ScreenType.NATIONS);
            } else if (current == ScreenType.NATIONS) {
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
            if (current == ScreenType.INVENTORY || current == ScreenType.NATIONS) {
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
            if (current == ScreenType.CONTROLS) {
                goTo(controlsReturn);
                return true;
            }
            if (current == ScreenType.MAIN_MENU) {
                return false;
            }
            return true;
        }
    }
}
