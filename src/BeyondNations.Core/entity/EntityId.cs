using System;

namespace beyondnations {

    /**
    * An entity's identity. Two EntityIds naming the same entity are equal
    * by Equals and by == alike, so a comparison such as
    * stall.getOwnerId() == pawn.getId() holds whichever object each side
    * holds (a load shares one object per id, but nothing else promises to).
    */
    public class EntityId {
        private Guid id;

        /** The same identity, rebuilt from a save (docs/SAVES.md). */
        internal EntityId(Guid value) {
            this.id = value;
        }

        internal Guid getGuid() {
            return id;
        }

        public EntityId() {
            this.id = Guid.NewGuid();
        }

        public Guid getId() {
            return id;
        }

        public override bool Equals(object obj) {
            if (obj == null) {
                return false;
            }

            EntityId other = obj as EntityId;
            if (other is null) {
                return false;
            }

            return id.Equals(other.id);
        }

        public override int GetHashCode() {
            return id.GetHashCode();
        }

        public static bool operator ==(EntityId a, EntityId b) {
            if (ReferenceEquals(a, b)) {
                return true;
            }
            if (a is null || b is null) {
                return false;
            }
            return a.id.Equals(b.id);
        }

        public static bool operator !=(EntityId a, EntityId b) {
            return !(a == b);
        }

        public override string ToString() {
            return id.ToString();
        }
    }
}