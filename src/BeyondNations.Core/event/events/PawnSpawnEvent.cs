using System.Numerics;

namespace beyondnations {

    public class PawnSpawnEvent : Event {
        private Vector3 position;
        private Pawn pawn;

        public PawnSpawnEvent(Vector3 position, Pawn pawn) : base(EventType.PawnSpawn, "A pawn has spawned at (" + position.X + ", " + position.Y + ", " + position.Z + ").") {
            this.position = position;
            this.pawn = pawn;
        }

        public Vector3 getPosition() {
            return position;
        }

        public Pawn getPawn() {
            return pawn;
        }

        public override string ToString() {
            return "PawnSpawnEvent [position=" + position + ", pawn=" + pawn + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}