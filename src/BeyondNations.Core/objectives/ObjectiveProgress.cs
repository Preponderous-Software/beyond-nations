namespace beyondnations {

    /**
    * How far the player is towards one objective: whether it is met, and
    * optionally a counter to show ("Wood 12/40"). The counter need not be the
    * completion test itself -- founding a settlement shows the wood it costs
    * but is met by the settlement existing -- so the two are kept apart.
    */
    public readonly struct ObjectiveProgress {
        private readonly bool complete;
        private readonly string label;
        private readonly int current;
        private readonly int target;

        private ObjectiveProgress(bool complete, string label, int current, int target) {
            this.complete = complete;
            this.label = label;
            this.current = current;
            this.target = target;
        }

        /** A yes-or-no objective with no counter to show. */
        public static ObjectiveProgress flag(bool complete) {
            return new ObjectiveProgress(complete, null, 0, 0);
        }

        /** Met once current reaches target; the counter is shown. */
        public static ObjectiveProgress count(string label, int current, int target) {
            return new ObjectiveProgress(current >= target, label, current, target);
        }

        /** Met by its own test, with a counter shown alongside. */
        public static ObjectiveProgress counterWith(bool complete, string label, int current, int target) {
            return new ObjectiveProgress(complete, label, current, target);
        }

        public bool isComplete() { return complete; }
        public string getLabel() { return label; }
        public int getCurrent() { return current; }
        public int getTarget() { return target; }
        public bool hasCounter() { return label != null; }

        /**
        * "Wood 12/40", with the current value capped at the target so a full
        * counter never reads 140/40; null when there is no counter.
        */
        public string describe() {
            if (label == null) {
                return null;
            }
            int shown = current > target ? target : current;
            return label + " " + shown + "/" + target;
        }
    }
}
