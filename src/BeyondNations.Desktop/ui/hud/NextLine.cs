namespace beyondnations.desktop.ui.hud {

    /**
    * The one line on the HUD saying what to do next (Atman's NEXT line):
    * the current objective's title and, when it counts something, how far
    * along it is ("Next: Gather wood - Wood 3/10"). The hint that says how
    * stays in the Journal's Goals tab, so the line never names a key and
    * reads the same by touch.
    */
    public static class NextLine {
        public const string Prefix = "Next: ";

        /** The line, or null when there is nothing to do next (no objective at all). */
        public static string text(string title, string counter) {
            if (string.IsNullOrEmpty(title)) {
                return null;
            }
            return Prefix + title + (string.IsNullOrEmpty(counter) ? "" : " - " + counter);
        }

        /** The line for an objective tracker as it stands. */
        public static string of(ObjectiveTracker tracker) {
            Objective current = tracker.getCurrent();
            if (current == null) {
                return null;
            }
            ObjectiveProgress progress = tracker.getCurrentProgress();
            return text(current.getTitle(), progress.hasCounter() ? progress.describe() : null);
        }

        /** How full the current objective is, 0 to 1 (the line's progress bar). */
        public static float fraction(ObjectiveTracker tracker) {
            return tracker.getCurrent() == null ? 0f : tracker.getCurrentProgress().fraction();
        }
    }
}
