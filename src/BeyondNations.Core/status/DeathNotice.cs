namespace beyondnations {

    /**
    * What the status line says when a pawn dies. One death names the pawn;
    * further deaths while that notice is still showing are folded into it,
    * so a hard winter reads "Alder and Birch have died." and then
    * "5 pawns have died." rather than a run of "X has died." lines, one per
    * pawn. Once the line expires (Status.clearStatusIfExpired) the next death
    * is named again.
    *
    * The count is read back off the status text, so nothing new is saved.
    */
    public static class DeathNotice {
        private const string ONE = " has died.";
        private const string TWO = " have died.";
        private const string MANY = " pawns have died.";

        /** The status line after the pawn called name dies, given the line showing now. */
        public static string combine(string current, string name) {
            int earlier = deathsIn(current);
            if (earlier == 0) {
                return name + ONE;
            }
            if (earlier == 1 && current != name + ONE) {
                return current.Substring(0, current.Length - ONE.Length) + " and " + name + TWO;
            }
            return (earlier + 1) + MANY;
        }

        /** How many pawn deaths a status line already reports: 0 if it is not a death notice. */
        public static int deathsIn(string status) {
            if (string.IsNullOrEmpty(status)) {
                return 0;
            }
            if (status.EndsWith(MANY)) {
                int count;
                return int.TryParse(status.Substring(0, status.Length - MANY.Length), out count) && count > 0 ? count : 0;
            }
            if (status.EndsWith(TWO) && status.Contains(" and ")) {
                return 2;
            }
            if (status.EndsWith(ONE) && status.Length > ONE.Length) {
                return 1;
            }
            return 0;
        }
    }
}
