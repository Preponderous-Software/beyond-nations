using System;

namespace beyondnations {

    /** The machine's clock: what every build uses outside the tests. */
    public sealed class SystemClock : IClock {
        public DateTime utcNow() {
            return DateTime.UtcNow;
        }
    }
}
