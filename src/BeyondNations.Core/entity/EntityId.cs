using System;

namespace beyondnations {

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
            if (other == null) {
                return false;
            }

            return id.Equals(other.id);
        }

        public override int GetHashCode() {
            return id.GetHashCode();
        }

        public override string ToString() {
            return id.ToString();
        }
    }
}