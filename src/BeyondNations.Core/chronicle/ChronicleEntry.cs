using System.Collections.Generic;

namespace beyondnations {

    /**
    * One line of the world's chronicle: what happened, in words, and when, in
    * in-game time. It also remembers which nations and entities the line is
    * about, so a HUD can pick out the ones that concern the player.
    */
    public class ChronicleEntry {
        private readonly int sequence;
        private readonly EventType eventType;
        private readonly int tick;
        private readonly int day;
        private readonly string partOfDay;
        private readonly string text;
        private readonly List<NationId> nationIds;
        private readonly List<EntityId> entityIds;

        public ChronicleEntry(int sequence, EventType eventType, int tick, int day, string partOfDay, string text, List<NationId> nationIds, List<EntityId> entityIds) {
            this.sequence = sequence;
            this.eventType = eventType;
            this.tick = tick;
            this.day = day;
            this.partOfDay = partOfDay;
            this.text = text;
            this.nationIds = nationIds ?? new List<NationId>();
            this.entityIds = entityIds ?? new List<EntityId>();
        }

        /** 0 for the first entry the chronicle ever recorded, then 1, 2, ... */
        public int getSequence() { return sequence; }
        public EventType getEventType() { return eventType; }
        /** TickCounter.getTotalTicks() when the event happened. */
        public int getTick() { return tick; }
        /** The in-game day, counted from 1. */
        public int getDay() { return day; }
        /** "dawn", "morning", "midday", "afternoon", "dusk" or "night". */
        public string getPartOfDay() { return partOfDay; }
        /** The sentence on its own, without the time stamp. */
        public string getText() { return text; }

        /** "Day 3, dusk". */
        public string getTimestamp() {
            return "Day " + day + ", " + partOfDay;
        }

        /**
        * The full line, "Day 3, dusk - Mira founded the nation of Ironvale.".
        * A plain hyphen rather than a dash: the HUD font is ASCII only.
        */
        public string getLine() {
            return getTimestamp() + " - " + text;
        }

        public IReadOnlyList<NationId> getNationIds() { return nationIds; }
        public IReadOnlyList<EntityId> getEntityIds() { return entityIds; }

        public bool involvesNation(NationId nationId) {
            return nationId != null && nationIds.Contains(nationId);
        }

        public bool involvesEntity(EntityId entityId) {
            return entityId != null && entityIds.Contains(entityId);
        }

        /**
        * True when the entry is about the player or the nation the player
        * belongs to now. Asked at draw time, so joining a nation lights up its
        * past as well.
        */
        public bool concerns(Player player) {
            if (player == null) {
                return false;
            }
            return involvesEntity(player.getId()) || involvesNation(player.getNationId());
        }

        public override string ToString() {
            return getLine();
        }
    }
}
