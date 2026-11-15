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
    *
    * The world waits while the screen is open, so the objectives are
    * checked the moment the player enters: "Enter your settlement" is done
    * however they then leave, by Exit Settlement on the screen or by
    * staying inside (playtest 0.8.0 s01, s07, s08: Exit Settlement on the
    * screen left the objective open, as no tick ever saw the player inside).
    * And when the screen closes with the player still inside, the status
    * line says where they are and how to walk on (s02, s08: they stood
    * still, unable to move, with nothing saying why).
    */
    public static class SettlementScreens {
        /** Said when the settlement screen closes and the player is still inside. */
        public const string InsideMessage = "You are inside the settlement. Its buttons are below; Exit Settlement walks you back out.";

        private static Simulation seen;
        private static EntityId wasIn;
        private static ScreenType wasOn;

        /** Returns true when it opened the screen. */
        public static bool beforeDraw(Simulation simulation, ScreenState screens) {
            if (simulation == null) {
                seen = null;
                wasIn = null;
                wasOn = ScreenType.TITLE;
                return false;
            }
            EntityId now = simulation.getPlayer().getCurrentSettlementId();
            if (simulation != seen) {
                seen = simulation;
            } else {
                if (now != null && now != wasIn) {
                    // the world stops while the screen is open: count the entering now
                    simulation.getObjectiveTracker().evaluate(simulation);
                    if (screens.getCurrent() == ScreenType.WORLD) {
                        SettlementScreen.requestOpen(now);
                    }
                }
                if (wasOn == ScreenType.SETTLEMENT && screens.getCurrent() == ScreenType.WORLD && now != null) {
                    simulation.getPlayer().getStatus().prompt(InsideMessage);
                }
            }
            wasIn = now;
            wasOn = screens.getCurrent();
            if (!SettlementScreen.consumeOpenRequest()) {
                return false;
            }
            screens.openSettlement();
            wasOn = screens.getCurrent();
            return screens.getCurrent() == ScreenType.SETTLEMENT;
        }
    }
}
