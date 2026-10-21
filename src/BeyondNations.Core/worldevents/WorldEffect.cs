using System.Text.Json;

namespace beyondnations {

    /** The lasting consequences a world event can leave behind. */
    public enum WorldEffectKind {
        /** A nation's farms bring in nothing until it ends. */
        DROUGHT,
        /**
        * Bandits rob a settlement every BANDIT_THEFT_SECONDS until they leave,
        * are driven off or are paid off (WorldEvents.offerStandingBanditCamp).
        */
        BANDITS,
    }

    /**
    * Something a world event left running, with its timer: a drought over a
    * nation, a bandit camp outside a settlement. WorldEvents ticks them and
    * ends them; a save keeps them, timers and all.
    */
    public class WorldEffect {
        private WorldEffectKind kind;
        private NationId nationId;
        private EntityId settlementId;
        private int startTick;
        private int endTick;
        private int nextTick;

        public WorldEffect(WorldEffectKind kind, NationId nationId, EntityId settlementId, int startTick, int endTick, int nextTick) {
            this.kind = kind;
            this.nationId = nationId;
            this.settlementId = settlementId;
            this.startTick = startTick;
            this.endTick = endTick;
            this.nextTick = nextTick;
        }

        public WorldEffectKind getKind() { return kind; }
        public NationId getNationId() { return nationId; }
        public EntityId getSettlementId() { return settlementId; }
        public int getStartTick() { return startTick; }
        /** The tick it ends on. */
        public int getEndTick() { return endTick; }
        public void setEndTick(int endTick) { this.endTick = endTick; }
        /** When it next does something (a bandit raid). */
        public int getNextTick() { return nextTick; }
        public void setNextTick(int nextTick) { this.nextTick = nextTick; }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.writeEnum("kind", kind);
            writer.write("nationId", nationId);
            writer.write("settlementId", settlementId);
            writer.write("startTick", startTick);
            writer.write("endTick", endTick);
            writer.write("nextTick", nextTick);
        }

        internal static WorldEffect load(JsonElement element, SaveReader reader) {
            return new WorldEffect(SaveReader.getEnum<WorldEffectKind>(element, "kind"), reader.getNationId(element, "nationId"),
                reader.getEntityId(element, "settlementId"), SaveReader.getInt(element, "startTick"), SaveReader.getInt(element, "endTick"),
                SaveReader.getInt(element, "nextTick"));
        }
    }
}
