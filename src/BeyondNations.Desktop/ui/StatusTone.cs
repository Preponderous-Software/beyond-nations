namespace beyondnations.desktop.ui {

    /**
    * Whether a status message reports something that did not happen.
    *
    * Every command already explains a refusal on the status line ("Not
    * enough wood. Need 20.", "You are not in a nation."), but the line drew
    * a refusal exactly like a success, in small white text that vanished a
    * few seconds later, so a player pressing B with too little wood saw
    * nothing they recognised as "no". The HUD now draws a failure in its own
    * colour. Status carries no kind, and the messages come from many
    * commands in Core, so the wording is classified here: every refusal the
    * commands produce starts with one of these phrases.
    */
    public static class StatusTone {
        private static readonly string[] FailurePrefixes = {
            "Cannot ",
            "Can't ",
            "No ",
            "Not enough",
            "You are not ",
            "You are already ",
            "You can only ",
            "You cannot ",
            "You can't ",
            "You do not ",
            "You don't ",
            "You died",
            "There are no ",
            "There is no ",
            "Only the ",
            "The stockpile is empty",
            "You have nothing ",
            "You need to ",
            "This settlement does not ",
            "This settlement has no ",
            "Too close ",
            "Max number ",
            "Market is full",
            "Settlement has no ",
            "Your stall has no ",
            "Debug mode must ",
            "Screenshot capture failed",
            "Could not save",
            "Could not load",
        };

        // Refusals that start with a name: "Mira is already a merchant.",
        // and pawns turning the player down until they know them better.
        private static readonly string[] FailureFragments = {
            " is already a ",
            ". Need ",
            " has no home settlement",
            "(needs ",
        };

        public static bool isFailure(string message) {
            if (string.IsNullOrEmpty(message)) {
                return false;
            }
            foreach (string fragment in FailureFragments) {
                if (message.Contains(fragment, System.StringComparison.Ordinal)) {
                    return true;
                }
            }
            foreach (string prefix in FailurePrefixes) {
                if (message.StartsWith(prefix, System.StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }
    }
}
