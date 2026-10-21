using System;

namespace beyondnations {
    
    public class LocationId {
        private Guid guid;

        /** The same identity, rebuilt from a save (docs/SAVES.md). */
        internal LocationId(Guid value) {
            this.guid = value;
        }

        internal Guid getGuid() {
            return guid;
        }

        public LocationId() {
            guid = Guid.NewGuid();
        }

        public override string ToString() {
            return guid.ToString();
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            LocationId other = (LocationId) obj;
            return guid.Equals(other.guid);
        }

        public override int GetHashCode() {
            return guid.GetHashCode();
        }
    }
}