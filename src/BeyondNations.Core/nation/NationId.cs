using System;

namespace beyondnations {

    public class NationId {
        private Guid guid;

        /** The same identity, rebuilt from a save (docs/SAVES.md). */
        internal NationId(Guid value) {
            this.guid = value;
        }

        internal Guid getGuid() {
            return guid;
        }

        public NationId() {
            guid = Guid.NewGuid();
        }

        public override string ToString() {
            return guid.ToString();
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            NationId other = (NationId) obj;
            return guid.Equals(other.guid);
        }

        public override int GetHashCode() {
            return guid.GetHashCode();
        }
    }
}