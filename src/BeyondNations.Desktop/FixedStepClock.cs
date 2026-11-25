namespace beyondnations.desktop {

    /**
    * How many fixed simulation steps each frame runs.
    *
    * In normal play the clock accumulates real elapsed time and hands out a
    * whole number of fixed steps, carrying the remainder over, so the tick rate
    * follows the wall clock and not the frame rate. The backlog is clamped at
    * maxCatchUpSteps, so a long stall cannot make one frame run thousands of
    * steps.
    *
    * In lockstep (lockstepSteps > 0) the wall clock is ignored: every frame
    * runs exactly lockstepSteps steps, however long it took. A staged
    * screenshot (--screenshot-after-frames with a --stage-* flag) then shows
    * the same scene for the same seed and frame count on a busy machine as on
    * an idle one; with the wall clock, a slow machine ran fewer ticks per frame
    * and the shot drifted.
    *
    * Use: beginFrame(deltaTime) once a frame, then take steps while
    * tryTakeStep() is true. A frame that stops early (the player died and the
    * heir's screen came up) leaves its untaken time in the accumulator; in
    * lockstep, untaken steps are dropped, since the next frame brings its own.
    */
    public class FixedStepClock {
        private readonly double fixedTimeStep;
        private readonly int maxCatchUpSteps;
        private readonly int lockstepSteps;
        private double accumulator;
        private int lockstepRemaining;

        public FixedStepClock(double fixedTimeStep, int maxCatchUpSteps, int lockstepSteps) {
            this.fixedTimeStep = fixedTimeStep;
            this.maxCatchUpSteps = maxCatchUpSteps;
            this.lockstepSteps = System.Math.Max(0, lockstepSteps);
        }

        public bool isLockstep() {
            return lockstepSteps > 0;
        }

        /** Starts a frame that took deltaTime seconds of real time. */
        public void beginFrame(double deltaTime) {
            if (isLockstep()) {
                lockstepRemaining = lockstepSteps;
                return;
            }
            accumulator += deltaTime;
            double maxBacklog = fixedTimeStep * maxCatchUpSteps;
            if (accumulator > maxBacklog) {
                accumulator = maxBacklog;
            }
        }

        /** True, and the step counted, when this frame has another fixed step to run. */
        public bool tryTakeStep() {
            if (isLockstep()) {
                if (lockstepRemaining <= 0) {
                    return false;
                }
                lockstepRemaining--;
                return true;
            }
            if (accumulator < fixedTimeStep) {
                return false;
            }
            accumulator -= fixedTimeStep;
            return true;
        }

        /** Forgets any time or steps owed: the world waited (a choice sheet), or was replaced (a load). */
        public void reset() {
            accumulator = 0;
            lockstepRemaining = 0;
        }
    }
}
