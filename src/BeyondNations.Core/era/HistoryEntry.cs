using System.Text.Json;

namespace beyondnations {

    /**
    * One line of the history of the player's nation, as the end-of-run
    * screen tells it: a chronicle entry about the player or their nation,
    * kept here because the chronicle itself keeps only its latest 200, or a
    * milestone of the run (an era reached, a victory, a collapse).
    */
    public class HistoryEntry {
        private readonly int tick;
        private readonly int day;
        private readonly string partOfDay;
        private readonly string text;
        private readonly bool milestone;

        public HistoryEntry(int tick, int day, string partOfDay, string text, bool milestone) {
            this.tick = tick;
            this.day = day;
            this.partOfDay = partOfDay;
            this.text = text;
            this.milestone = milestone;
        }

        public static HistoryEntry of(ChronicleEntry entry) {
            return new HistoryEntry(entry.getTick(), entry.getDay(), entry.getPartOfDay(), entry.getText(), false);
        }

        public int getTick() { return tick; }
        public int getDay() { return day; }
        public string getPartOfDay() { return partOfDay; }
        public string getText() { return text; }
        /** True for an era, a victory or a collapse rather than a chronicle line. */
        public bool isMilestone() { return milestone; }

        /** "Day 3, dusk". */
        public string getTimestamp() {
            return "Day " + day + ", " + partOfDay;
        }

        public string getLine() {
            return getTimestamp() + " - " + text;
        }

        public override string ToString() {
            return getLine();
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.write("tick", tick);
            writer.write("day", day);
            writer.write("partOfDay", partOfDay);
            writer.write("text", text);
            writer.write("milestone", milestone);
        }

        internal static HistoryEntry load(JsonElement element) {
            return new HistoryEntry(SaveReader.getInt(element, "tick"), SaveReader.getInt(element, "day"),
                SaveReader.getString(element, "partOfDay"), SaveReader.getString(element, "text"), SaveReader.getBool(element, "milestone"));
        }
    }
}
