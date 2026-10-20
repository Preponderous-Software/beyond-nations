namespace beyondnations.desktop.ui {

    /**
    * The one thing the hosts do about succession (realm/Succession): after
    * each simulation step, if the player has just died in a nation, show
    * the succession screen -- or, when their line has ended, the
    * end-of-run screen, where EraTracker has recorded it as a defeat.
    * Shared by the desktop and browser hosts.
    */
    public static class RealmScreens {

        /** Returns true when it changed the screen, so the host stops stepping the world this frame. */
        public static bool afterStep(Simulation simulation, ScreenState screens) {
            if (simulation == null) {
                return false;
            }
            SuccessionNotice notice = simulation.getRealm().consumeNotice();
            if (notice == null) {
                return false;
            }
            if (notice.isLineEnded()) {
                screens.runEnded();
            } else {
                screens.goTo(ScreenType.SUCCESSION);
            }
            return true;
        }
    }
}
