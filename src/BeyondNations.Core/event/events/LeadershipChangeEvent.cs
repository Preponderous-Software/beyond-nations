namespace beyondnations {

    /**
    * A nation's leadership has passed from one member to another: the leader
    * died (Simulation, #267) or the player stepped down by leaving.
    */
    public class LeadershipChangeEvent : Event {
        private Nation nation;
        private EntityId previousLeaderId;
        private EntityId newLeaderId;

        public LeadershipChangeEvent(Nation nation, EntityId previousLeaderId, EntityId newLeaderId) : base(EventType.LeadershipChange, "The leadership of " + nation.getName() + " has changed hands.") {
            this.nation = nation;
            this.previousLeaderId = previousLeaderId;
            this.newLeaderId = newLeaderId;
        }

        public Nation getNation() {
            return nation;
        }

        public EntityId getPreviousLeaderId() {
            return previousLeaderId;
        }

        public EntityId getNewLeaderId() {
            return newLeaderId;
        }

        public override string ToString() {
            return "LeadershipChangeEvent [nation=" + nation.getName() + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}
