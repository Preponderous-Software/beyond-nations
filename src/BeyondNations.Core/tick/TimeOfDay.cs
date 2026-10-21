using System;

namespace beyondnations {

    /**
    * The time of day, derived from the simulation's total tick count (engine
    * phase B2, #279). The game owns time; a renderer only reads it.
    *
    * The fraction runs over [0, 1): 0.25 is sunrise, 0.5 noon, 0.75 sunset,
    * which is what Strata's sun lighting expects. It is a pure function of
    * TickCounter.getTotalTicks(), the ticks per day and the fraction the world
    * started at -- no wall clock -- so the same tick always gives the same
    * time. One rule reads it: pawns go home at night (PawnBehaviorCalculator).
    */
    public class TimeOfDay {

        /** Ten minutes of play at the default 50 ticks per second. */
        public const int DefaultTicksPerDay = 30000;

        /** Mid-morning, so a new world opens in daylight. */
        public const float DefaultStartFraction = 0.3f;

        private readonly TickCounter tickCounter;
        private readonly int ticksPerDay;
        private float startFraction;

        public TimeOfDay(TickCounter tickCounter, int ticksPerDay = DefaultTicksPerDay, float startFraction = DefaultStartFraction) {
            if (tickCounter == null) {
                throw new ArgumentNullException(nameof(tickCounter));
            }
            if (ticksPerDay <= 0) {
                throw new ArgumentOutOfRangeException(nameof(ticksPerDay), ticksPerDay, "ticks per day must be positive");
            }
            this.tickCounter = tickCounter;
            this.ticksPerDay = ticksPerDay;
            setStartFraction(startFraction);
        }

        public int getTicksPerDay() { return ticksPerDay; }
        public float getStartFraction() { return startFraction; }

        /** The time of day the world is at when no tick has run. */
        public void setStartFraction(float fraction) {
            if (float.IsNaN(fraction) || fraction < 0f || fraction >= 1f) {
                throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "the start fraction must be in [0, 1)");
            }
            startFraction = fraction;
        }

        /** The current time of day in [0, 1). */
        public float getFraction() {
            return fractionAt(tickCounter.getTotalTicks(), ticksPerDay, startFraction);
        }

        /** Night runs from a little after sunset to a little before sunrise. */
        public const float NightStartFraction = 0.8f;
        public const float NightEndFraction = 0.2f;

        public bool isNight() {
            return isNightAt(getFraction());
        }

        public static bool isNightAt(float fraction) {
            return fraction >= NightStartFraction || fraction < NightEndFraction;
        }

        /** Days completed since the world started: 0 on the first day. */
        public int getDay() {
            return dayAt(tickCounter.getTotalTicks(), ticksPerDay, startFraction);
        }

        public static float fractionAt(long totalTicks, int ticksPerDay, float startFraction) {
            double day = startFraction + (double) totalTicks / ticksPerDay;
            double fraction = day - Math.Floor(day);
            // A fraction a hair below 1 can round up to 1f; keep the range half-open.
            float result = (float) fraction;
            return result >= 1f ? 0f : result;
        }

        public static int dayAt(long totalTicks, int ticksPerDay, float startFraction) {
            return (int) Math.Floor(startFraction + (double) totalTicks / ticksPerDay);
        }
    }
}
