using System;

namespace beyondnations {

    public class ChunkId {
        private Guid guid;

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