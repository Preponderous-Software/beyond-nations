using System.Text.Json;
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
        // An optional second counter, for a cost paid in two things
        // ("Wood 12/20 · Stone 3/5"); null label when there is none.
        private readonly string secondLabel;
        private readonly int secondCurrent;
        private readonly int secondTarget;

        private ObjectiveProgress(bool complete, string label, int current, int target,
                                  string secondLabel = null, int secondCurrent = 0, int secondTarget = 0) {
            this.complete = complete;
            this.label = label;
            this.current = current;
            this.target = target;
            this.secondLabel = secondLabel;
            this.secondCurrent = secondCurrent;
            this.secondTarget = secondTarget;
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

        /** Met by its own test, with two counters shown alongside. */
        public static ObjectiveProgress countersWith(bool complete, string label, int current, int target,
                                                     string secondLabel, int secondCurrent, int secondTarget) {
            return new ObjectiveProgress(complete, label, current, target, secondLabel, secondCurrent, secondTarget);
        }

        public bool isComplete() { return complete; }
        public string getLabel() { return label; }
        public int getCurrent() { return current; }
        public int getTarget() { return target; }
        public bool hasCounter() { return label != null; }
        public bool hasSecondCounter() { return secondLabel != null; }
        public string getSecondLabel() { return secondLabel; }
        public int getSecondCurrent() { return secondCurrent; }
        public int getSecondTarget() { return secondTarget; }

        /**
        * How full the counters are, 0 to 1, for a progress bar: each counter
        * is capped at its target and the two are pooled, so neither can make
        * up for the other being short.
        */
        public float fraction() {
            int total = target + (secondLabel != null ? secondTarget : 0);
            if (label == null || total <= 0) {
                return complete ? 1f : 0f;
            }
            int have = capped(current, target) + (secondLabel != null ? capped(secondCurrent, secondTarget) : 0);
            return (float) have / total;
        }

        private static int capped(int value, int max) {
            return value < 0 ? 0 : value > max ? max : value;
        }

        /**
        * "Wood 12/40", with the current value capped at the target so a full
        * counter never reads 140/40; null when there is no counter.
        */
        public string describe() {
            if (label == null) {
                return null;
            }
            string text = label + " " + capped(current, target) + "/" + target;
            if (secondLabel != null) {
                text += " \u00b7 " + secondLabel + " " + capped(secondCurrent, secondTarget) + "/" + secondTarget;
            }
            return text;
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.write("complete", complete);
            writer.write("label", label);
            writer.write("current", current);
            writer.write("target", target);
            writer.write("secondLabel", secondLabel);
            writer.write("secondCurrent", secondCurrent);
            writer.write("secondTarget", secondTarget);
        }

        internal static ObjectiveProgress load(JsonElement element) {
            return new ObjectiveProgress(SaveReader.getBool(element, "complete"), SaveReader.getString(element, "label"),
                SaveReader.getInt(element, "current"), SaveReader.getInt(element, "target"),
                SaveReader.getString(element, "secondLabel"), SaveReader.getInt(element, "secondCurrent"),
                SaveReader.getInt(element, "secondTarget"));
        }
    }
}
