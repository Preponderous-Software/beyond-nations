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
    * Whose news the chronicle lists (ChronicleFocus). Your news by default:
    * in the 0.6.2 playtest (docs/playtests/0.6.2, s04) AI nations' rivalries
    * and raids made up nearly all of 495 entries by day 2 and buried the
    * player's own.
    */
    public enum ChronicleScope {
        /** The player, their nation, its neighbours and world news. */
        YourNews,
        /** Every entry, as before. */
        AllNations
    }

    /**
    * Which chronicle view is open. The input layer owns one (the chronicle key
    * cycles it) and hands it to the HUD, which can also close the full view
    * from its own button, so there is one answer rather than a copy each.
    */
    public class ChronicleView {
        private ChronicleMode mode = ChronicleMode.Recent;
        private bool onlyMyNation;
        private ChronicleScope scope = ChronicleScope.YourNews;

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

        /**
        * Whose news both views list. A view setting, like the mode: it lives
        * with the input layer for the session and is not saved, so every run
        * opens on the player's news.
        */
        public ChronicleScope getScope() {
            return scope;
        }

        public void setScope(ChronicleScope scope) {
            this.scope = scope;
        }

        /** Your news -> All nations -> Your news. Returns the new scope. */
        public ChronicleScope toggleScope() {
            scope = scope == ChronicleScope.YourNews ? ChronicleScope.AllNations : ChronicleScope.YourNews;
            return scope;
        }

        /** "your news" or "all nations", for labels. */
        public static string scopeName(ChronicleScope scope) {
            return scope == ChronicleScope.AllNations ? "all nations" : "your news";
        }

        /** The status-line message shown when the scope key switches to scope. */
        public static string describe(ChronicleScope scope) {
            return "Chronicle: showing " + scopeName(scope) + ".";
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
