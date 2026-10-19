namespace beyondnations {

    /**
    * A nation has raised a settlement. Produced by both the player's Found
    * Settlement command and a pawn leader's construct-settlement behaviour.
    */
    public class SettlementFoundedEvent : Event {
        private Settlement settlement;
        private Nation nation;
        private EntityId founderId;

        public SettlementFoundedEvent(Settlement settlement, Nation nation, EntityId founderId) : base(EventType.SettlementFounded, "A settlement of " + nation.getName() + " has been founded.") {
            this.settlement = settlement;
            this.nation = nation;
            this.founderId = founderId;
        }

        public Settlement getSettlement() {
            return settlement;
        }

        public Nation getNation() {
            return nation;
        }

        public EntityId getFounderId() {
            return founderId;
        }

        public override string ToString() {
            return "SettlementFoundedEvent [nation=" + nation.getName() + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}
