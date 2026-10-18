
namespace beyondnations {

    public class PawnDeathEvent : Event {
        private Pawn pawn;

        public PawnDeathEvent(Pawn pawn) : base(EventType.PawnDeath, pawn.getName() + " has died.") {
            this.pawn = pawn;
        }

        public Pawn getPawn() {
            return pawn;
        }

        public override string ToString() {
            return "PawnDeathEvent{" +
                    "pawn=" + pawn.getName() +
                    '}';
        }
    }
}