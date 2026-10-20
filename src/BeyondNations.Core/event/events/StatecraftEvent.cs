namespace beyondnations {

    /**
    * Something a ruler did or that befell their line -- a law coming into
    * force, a war declared, a peace made, a deserter, a succession -- told
    * already as the line of history it makes (Realm writes the text).
    */
    public class StatecraftEvent : Event {
        private Nation nation;
        private Nation otherNation;
        private EntityId entityId;

        public StatecraftEvent(string text, Nation nation, Nation otherNation, EntityId entityId) : base(EventType.Statecraft, text) {
            this.nation = nation;
            this.otherNation = otherNation;
            this.entityId = entityId;
        }

        public Nation getNation() { return nation; }
        /** The other nation the act concerns, for war and peace; otherwise null. */
        public Nation getOtherNation() { return otherNation; }
        public EntityId getEntityId() { return entityId; }

        public override string ToString() {
            return "StatecraftEvent [description=" + getDescription() + ", type=" + getType() + ", date=" + getDate() + "]";
        }
    }
}
