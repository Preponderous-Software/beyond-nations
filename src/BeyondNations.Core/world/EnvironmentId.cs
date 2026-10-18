using System;

namespace beyondnations {

    public class EnvironmentId {
        private Guid guid;

        public EnvironmentId() {
            guid = Guid.NewGuid();
        }

        public override string ToString() {
            return guid.ToString();
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            EnvironmentId other = (EnvironmentId) obj;
            return guid.Equals(other.guid);
        }

        public override int GetHashCode() {
            return guid.GetHashCode();
        }
    }
}