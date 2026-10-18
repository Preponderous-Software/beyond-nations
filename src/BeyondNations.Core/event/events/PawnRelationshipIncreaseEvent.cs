namespace beyondnations {

    /**
     * An event that is fired when a relationship between two pawns increases.
     */
    public class PawnRelationshipIncreaseEvent : Event {
        private Pawn pawn1;
        private Entity entity2;
        private int increase;

        public PawnRelationshipIncreaseEvent(Pawn pawn1, Entity entity2, int increase) : base(EventType.PawnRelationshipIncrease, "The relationship between " + pawn1.getName() + " and " + entity2.getId() + "has increased by " + increase + ".") {
            this.pawn1 = pawn1;
            this.entity2 = entity2;
            this.increase = increase;
        }

        public Pawn getPawn1() {
            return pawn1;
        }

        public Entity getEntity2() {
            return entity2;
        }

        public int getIncrease() {
            return increase;
        }
        
        public override string ToString() {
            return "PawnRelationshipIncreaseEvent[" + pawn1.getName() + ", " + entity2.getId() + ", " + increase + "]";
        }
    }
}