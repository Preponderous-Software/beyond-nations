namespace beyondnations.desktop.ui {

    /** How much of the chronicle the world HUD shows. */
    public enum ChronicleMode {
        /** The last few entries, compact, at the top of the screen. */
        Recent,
        /** The whole kept chronicle in a scrollable window. */
        Full,
        /** Nothing. */
        Hidden
    }

    /**
    * Which chronicle view is open. The input layer owns one (the chronicle key
    * cycles it) and hands it to the HUD, which can also close the full view
    * from its own button, so there is one answer rather than a copy each.
    */
    public class ChronicleView {
        private ChronicleMode mode = ChronicleMode.Recent;
        private bool onlyMyNation;

        public ChronicleMode getMode() {
            return mode;
        }

        public void setMode(ChronicleMode mode) {
            this.mode = mode;
        }

        /** Recent -> Full -> Hidden -> Recent. Returns the new mode. */
        public ChronicleMode cycle() {
            switch (mode) {
                case ChronicleMode.Recent: mode = ChronicleMode.Full; break;
                case ChronicleMode.Full: mode = ChronicleMode.Hidden; break;
                default: mode = ChronicleMode.Recent; break;
            }
            return mode;
        }

        /** The full view's filter: only entries about the player or their nation. */
        public bool isOnlyMyNation() {
            return onlyMyNation;
        }

        public void setOnlyMyNation(bool onlyMyNation) {
            this.onlyMyNation = onlyMyNation;
        }

        /** The status-line message shown when the key switches to mode. */
        public static string describe(ChronicleMode mode) {
            switch (mode) {
                case ChronicleMode.Recent: return "Chronicle: recent entries.";
                case ChronicleMode.Full: return "Chronicle: full history.";
                default: return "Chronicle hidden.";
            }
        }
    }
}
