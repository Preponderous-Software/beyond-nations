using System;

using beyondnations;

namespace beyondnationstests {

    /** A clock that says whatever the test sets, and stands still otherwise. */
    public class FixedClock : IClock {
        public DateTime Now;

        public FixedClock(DateTime nowUtc) {
            Now = nowUtc;
        }

        public DateTime utcNow() {
            return Now;
        }
    }
}
