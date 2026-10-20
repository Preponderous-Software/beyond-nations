namespace beyondnations {

    /**
    * One option on a world event's choice sheet: what it is called, what it
    * will cost and bring, and whether it can be taken right now (and if not,
    * why not -- "Needs 30 coins").
    */
    public class WorldEventChoice {
        private readonly string label;
        private readonly string consequence;
        private readonly string unavailableReason;

        public WorldEventChoice(string label, string consequence) : this(label, consequence, null) {
        }

        public WorldEventChoice(string label, string consequence, string unavailableReason) {
            this.label = label;
            this.consequence = consequence;
            this.unavailableReason = unavailableReason;
        }

        /** "Buy grain (30 coins)" */
        public string getLabel() { return label; }

        /** "Farms yield again at once." */
        public string getConsequence() { return consequence; }

        public bool isAvailable() { return unavailableReason == null; }

        /** Null when available. */
        public string getUnavailableReason() { return unavailableReason; }

        /** An option that can only be taken if a condition holds. */
        public static WorldEventChoice requiring(bool condition, string label, string consequence, string reasonIfNot) {
            return new WorldEventChoice(label, consequence, condition ? null : reasonIfNot);
        }
    }
}
