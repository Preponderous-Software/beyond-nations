using System;

namespace beyondnations {

    /**
    * Counts simulation ticks. Everything the simulation decides from it is
    * derived from the tick count and the configured ticks per second, never
    * from the wall clock, so a seed gives the same world history however fast
    * the ticks actually run (#272): in the windowed game at its fixed step, or
    * in a headless run thousands of times faster.
    *
    * Only getMtps() reads the wall clock, because it is a measurement of how
    * many ticks really ran in the last second, shown in the debug overlay.
    */
    public class TickCounter {
        public const int DefaultTicksPerSecond = 50;

        private readonly int ticksPerSecond;
        private int totalTicks = 0;

        // measured ticks per second (wall clock; display only)
        private int mtps = 0;
        private int ticksSinceMeasurement = 0;
        private DateTime lastMeasurement = DateTime.Now;

        public TickCounter() : this(DefaultTicksPerSecond) {
        }

        public TickCounter(int ticksPerSecond) {
            if (ticksPerSecond <= 0) {
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), "must be positive");
            }
            this.ticksPerSecond = ticksPerSecond;
        }

        /**
        * The tick within the current simulated second, 0 to ticksPerSecond - 1.
        * It wraps to 0 on every ticksPerSecond-th tick.
        */
        public int getTick() {
            return totalTicks % ticksPerSecond;
        }

        public void increment() {
            totalTicks++;
            ticksSinceMeasurement++;

            DateTime now = DateTime.Now;
            if (now - lastMeasurement >= TimeSpan.FromSeconds(1)) {
                mtps = ticksSinceMeasurement;
                ticksSinceMeasurement = 0;
                lastMeasurement = now;
            }
        }

        public int getMtps() {
            return mtps;
        }

        public int getTotalTicks() {
            return totalTicks;
        }

        public int getTicksPerSecond() {
            return ticksPerSecond;
        }

        // --- saving (docs/SAVES.md) ---

        /** The tick a loaded world resumes at. The wall-clock measurement starts afresh. */
        internal void restoreTotalTicks(int totalTicks) {
            if (totalTicks < 0) {
                throw new SaveFormatException("a negative tick count");
            }
            this.totalTicks = totalTicks;
        }
    }
}
