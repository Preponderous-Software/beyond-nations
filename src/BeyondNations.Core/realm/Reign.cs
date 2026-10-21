using System.Text.Json;

namespace beyondnations {

    /**
    * One ruler in the player's line: who they were, which nation they
    * belonged to, when they took up the rule and when (and how) it ended.
    */
    public class Reign {
        private string name;
        private string nationName;
        private int startTick;
        private int endTick;
        private string cause;

        public Reign(string name, string nationName, int startTick, int endTick, string cause) {
            this.name = name;
            this.nationName = nationName;
            this.startTick = startTick;
            this.endTick = endTick;
            this.cause = cause;
        }

        public string getName() { return name; }
        public string getNationName() { return nationName; }
        public int getStartTick() { return startTick; }
        public int getEndTick() { return endTick; }
        public string getCause() { return cause; }

        internal void save(SaveWriter writer) {
            writer.write("name", name);
            writer.write("nationName", nationName);
            writer.write("startTick", startTick);
            writer.write("endTick", endTick);
            writer.write("cause", cause);
        }

        internal static Reign load(JsonElement element) {
            return new Reign(SaveReader.getString(element, "name"), SaveReader.getString(element, "nationName"),
                SaveReader.getInt(element, "startTick"), SaveReader.getInt(element, "endTick"), SaveReader.getString(element, "cause"));
        }
    }

    /**
    * What the host shows after the player dies in a nation: who died, who
    * took their place (or that nobody could), and why the heir was chosen.
    * The host takes it once (Realm.consumeNotice) and opens the succession
    * screen with it.
    */
    public class SuccessionNotice {
        private string previousName;
        private string heirName;
        private string nationName;
        private int reignTicks;
        private string reason;
        private bool lineEnded;

        public SuccessionNotice(string previousName, string heirName, string nationName, int reignTicks, string reason, bool lineEnded) {
            this.previousName = previousName;
            this.heirName = heirName;
            this.nationName = nationName;
            this.reignTicks = reignTicks;
            this.reason = reason;
            this.lineEnded = lineEnded;
        }

        public string getPreviousName() { return previousName; }
        /** Null when the line has ended. */
        public string getHeirName() { return heirName; }
        public string getNationName() { return nationName; }
        public int getReignTicks() { return reignTicks; }
        /** Why this heir ("of your family", "the most loyal of your people"), or why there was none. */
        public string getReason() { return reason; }
        public bool isLineEnded() { return lineEnded; }

        internal void save(SaveWriter writer) {
            writer.write("previousName", previousName);
            writer.write("heirName", heirName);
            writer.write("nationName", nationName);
            writer.write("reignTicks", reignTicks);
            writer.write("reason", reason);
            writer.write("lineEnded", lineEnded);
        }

        internal static SuccessionNotice load(JsonElement element) {
            return new SuccessionNotice(SaveReader.getString(element, "previousName"), SaveReader.getString(element, "heirName"),
                SaveReader.getString(element, "nationName"), SaveReader.getInt(element, "reignTicks"),
                SaveReader.getString(element, "reason"), SaveReader.getBool(element, "lineEnded"));
        }
    }
}
