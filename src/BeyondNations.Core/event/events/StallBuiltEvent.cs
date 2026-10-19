namespace beyondnations {

    /**
    * A market stall has been built in a settlement. Produced by both the
    * player's Build Stall command and a pawn leader's construct-stall
    * behaviour.
    */
    public class StallBuiltEvent : Event {
        private Settlement settlement;
        private Nation nation;
        private EntityId builderId;

        public StallBuiltEvent(Settlement settlement, Nation nation, EntityId builderId) : base(EventType.StallBuilt, "A stall has been built in a settlement of " + nation.getName() + ".") {
            this.settlement = settlement;
            this.nation = nation;
            this.builderId = builderId;
        }

        public Settlement getSettlement() {
            return settlement;
        }

        public Nation getNation() {
            return nation;
        }

        public EntityId getBuilderId() {
            return builderId;
        }

        public override string ToString() {
            return "StallBuiltEvent [nation=" + nation.getName() + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}
