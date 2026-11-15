using beyondnations.desktop.ui.screens;

namespace beyondnations.desktop.ui {

    /**
    * What the hosts do about the settlement screen (#178), before each
    * frame's drawing: open it when the player has just entered a
    * settlement, as the issue asks, and when a button has asked for it
    * (the command bar's Settlement button, or Open on the nations screen's
    * Manage tab). Shared by the desktop and browser hosts.
    *
    * Entering is seen as the player's settlement changing from one frame
    * to the next while the world is shown, so a world loaded or staged
    * with the player already inside opens on the world, not on the screen.
    */
    public static class SettlementScreens {
        private static Simulation seen;
        private static EntityId wasIn;

        /** Returns true when it opened the screen. */
        public static bool beforeDraw(Simulation simulation, ScreenState screens) {
            if (simulation == null) {
                seen = null;
                wasIn = null;
                return false;
            }
            EntityId now = simulation.getPlayer().getCurrentSettlementId();
            if (simulation != seen) {
                seen = simulation;
            } else if (now != null && now != wasIn && screens.getCurrent() == ScreenType.WORLD) {
                SettlementScreen.requestOpen(now);
            }
            wasIn = now;
            if (!SettlementScreen.consumeOpenRequest()) {
                return false;
            }
            screens.openSettlement();
            return screens.getCurrent() == ScreenType.SETTLEMENT;
        }
    }
}
