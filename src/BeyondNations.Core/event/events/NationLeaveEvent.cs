namespace beyondnations {

    public class NationLeaveEvent : Event {
        private Nation nation;
        private EntityId entityId;

        public NationLeaveEvent(Nation nation, EntityId entityId) : base(EventType.NationLeave, "The entity " + entityId.ToString() + " has left the nation " + nation.getName() + ".") {
            this.nation = nation;
            this.entityId = entityId;
        }

        public Nation getNation() {
            return nation;
        }

        public EntityId getEntityId() {
            return entityId;
        }

        public override string ToString() {
            return "NationLeaveEvent [" + "nation=" + nation.getName() + ", entityId=" + entityId.ToString() + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}