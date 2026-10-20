using System.Text.Json;

namespace beyondnations {

    /**
    * One happening of a world event: which event, when, to whom, and the
    * details it was prepared with. Events are stateless, so this is all a
    * save needs to bring a pending choice back exactly as it was offered.
    *
    * The fields are general on purpose, so a new event needs no new save
    * format: a nation it happens to, another nation it involves, a
    * settlement, one entity (a claimant, say) and a number whose meaning is
    * the event's own (how many wanderers, which way the map points).
    */
    public class WorldEventInstance {
        private string eventId;
        private int sequence;
        private int startTick;
        private int deadlineTick;
        private bool forPlayer;
        private NationId nationId;
        private NationId otherNationId;
        private EntityId settlementId;
        private EntityId entityId;
        private int magnitude;

        public WorldEventInstance(string eventId, int sequence, int startTick, bool forPlayer) {
            this.eventId = eventId;
            this.sequence = sequence;
            this.startTick = startTick;
            this.forPlayer = forPlayer;
        }

        public string getEventId() { return eventId; }
        /** How many world events had happened before this one. */
        public int getSequence() { return sequence; }
        public int getStartTick() { return startTick; }

        /** The tick at which, still unanswered, the default choice is taken. 0 for a nation's own event. */
        public int getDeadlineTick() { return deadlineTick; }
        public void setDeadlineTick(int deadlineTick) { this.deadlineTick = deadlineTick; }

        /** Whether the player is the one choosing. */
        public bool isForPlayer() { return forPlayer; }

        /** The nation it happens to; null for a personal event. */
        public NationId getNationId() { return nationId; }
        public void setNationId(NationId nationId) { this.nationId = nationId; }

        public NationId getOtherNationId() { return otherNationId; }
        public void setOtherNationId(NationId otherNationId) { this.otherNationId = otherNationId; }

        public EntityId getSettlementId() { return settlementId; }
        public void setSettlementId(EntityId settlementId) { this.settlementId = settlementId; }

        public EntityId getEntityId() { return entityId; }
        public void setEntityId(EntityId entityId) { this.entityId = entityId; }

        public int getMagnitude() { return magnitude; }
        public void setMagnitude(int magnitude) { this.magnitude = magnitude; }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.write("eventId", eventId);
            writer.write("sequence", sequence);
            writer.write("startTick", startTick);
            writer.write("deadlineTick", deadlineTick);
            writer.write("forPlayer", forPlayer);
            writer.write("nationId", nationId);
            writer.write("otherNationId", otherNationId);
            writer.write("settlementId", settlementId);
            writer.write("entityId", entityId);
            writer.write("magnitude", magnitude);
        }

        internal static WorldEventInstance load(JsonElement element, SaveReader reader) {
            WorldEventInstance instance = new WorldEventInstance(SaveReader.getString(element, "eventId"), SaveReader.getInt(element, "sequence"),
                SaveReader.getInt(element, "startTick"), SaveReader.getBool(element, "forPlayer"));
            instance.deadlineTick = SaveReader.getInt(element, "deadlineTick");
            instance.nationId = reader.getNationId(element, "nationId");
            instance.otherNationId = reader.getNationId(element, "otherNationId");
            instance.settlementId = reader.getEntityId(element, "settlementId");
            instance.entityId = reader.getEntityId(element, "entityId");
            instance.magnitude = SaveReader.getInt(element, "magnitude");
            if (string.IsNullOrEmpty(instance.eventId)) {
                throw new SaveFormatException("a world event with no id");
            }
            return instance;
        }
    }
}
