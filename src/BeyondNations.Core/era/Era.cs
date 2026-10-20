namespace beyondnations {

    /**
    * How far the player's nation has come in a run: a camp, a village, a town
    * and finally a kingdom. Eras only go forward. What it takes to leave each
    * one, and what reaching the next one unlocks, is in EraRules.
    *
    * Saved by name, so adding an era never shifts the meaning of a save.
    */
    public enum Era {
        CAMP,
        VILLAGE,
        TOWN,
        KINGDOM
    }

    public static class Eras {
        public const Era First = Era.CAMP;
        public const Era Last = Era.KINGDOM;

        public static string getDisplayName(Era era) {
            switch (era) {
                case Era.VILLAGE: return "Village";
                case Era.TOWN: return "Town";
                case Era.KINGDOM: return "Kingdom";
                default: return "Camp";
            }
        }

        /** The era after this one, or null in the last. */
        public static Era? next(Era era) {
            return era == Last ? (Era?) null : era + 1;
        }

        /** 1 for the camp up to 4 for the kingdom: "Era 2 of 4". */
        public static int number(Era era) {
            return (int) era + 1;
        }

        public static int count() {
            return (int) Last + 1;
        }
    }
}
