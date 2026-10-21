using System;

namespace beyondnations {

    public class ChunkId {
        private Guid guid;

        /** The same identity, rebuilt from a save (docs/SAVES.md). */
        internal ChunkId(Guid value) {
            this.guid = value;
        }

        internal Guid getGuid() {
            return guid;
        }

        public ChunkId() {
            guid = Guid.NewGuid();
        }

        public override string ToString() {
            return guid.ToString();
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            ChunkId other = (ChunkId) obj;
            return guid.Equals(other.guid);
        }

        public override int GetHashCode() {
            return guid.GetHashCode();
        }
    }
}