using System.Collections.Generic;
using Xunit;

using beyondnations.desktop;

namespace beyondnationstests.desktop {

    /**
    * The host's fixed-step clock: wall-clock time in normal play, and a fixed
    * number of steps a frame in lockstep, so a staged screenshot does not
    * drift with machine load.
    */
    public class TestFixedStepClock {
        private const double Step = 1.0 / 50.0;

        private static int stepsFor(FixedStepClock clock, double deltaTime) {
            clock.beginFrame(deltaTime);
            int steps = 0;
            while (clock.tryTakeStep()) {
                steps++;
            }
            return steps;
        }

        [Fact]
        public void testRealTimeRunsTheStepsTheElapsedTimeAllows() {
            FixedStepClock clock = new FixedStepClock(Step, 5, 0);
            Assert.False(clock.isLockstep());
            Assert.Equal(0, stepsFor(clock, Step * 0.5));
            // the half step left over carries into the next frame
            Assert.Equal(1, stepsFor(clock, Step * 0.6));
            Assert.Equal(3, stepsFor(clock, Step * 3.0));
        }

        [Fact]
        public void testRealTimeClampsALongStall() {
            FixedStepClock clock = new FixedStepClock(Step, 5, 0);
            // at most the five steps of the clamp (floating point may leave the last a hair short)
            Assert.InRange(stepsFor(clock, 10.0), 4, 5);
        }

        [Fact]
        public void testLockstepIgnoresHowLongAFrameTook() {
            FixedStepClock clock = new FixedStepClock(Step, 5, 1);
            Assert.True(clock.isLockstep());
            List<int> steps = new List<int>();
            foreach (double delta in new[] { 0.0001, Step, 0.25, 10.0 }) {
                steps.Add(stepsFor(clock, delta));
            }
            Assert.Equal(new[] { 1, 1, 1, 1 }, steps);
        }

        [Fact]
        public void testTheSameFramesRunTheSameStepsWhateverTheLoad() {
            // an idle machine at 60 frames a second and a loaded one at 9
            FixedStepClock idle = new FixedStepClock(Step, 5, 2);
            FixedStepClock loaded = new FixedStepClock(Step, 5, 2);
            int idleSteps = 0;
            int loadedSteps = 0;
            for (int frame = 0; frame < 120; frame++) {
                idleSteps += stepsFor(idle, 1.0 / 60.0);
                loadedSteps += stepsFor(loaded, 1.0 / 9.0);
            }
            Assert.Equal(240, idleSteps);
            Assert.Equal(idleSteps, loadedSteps);
        }

        [Fact]
        public void testResetDropsWhatIsOwed() {
            FixedStepClock clock = new FixedStepClock(Step, 5, 0);
            clock.beginFrame(Step * 0.9);
            clock.reset();
            Assert.Equal(0, stepsFor(clock, Step * 0.5));

            FixedStepClock lockstep = new FixedStepClock(Step, 5, 3);
            lockstep.beginFrame(Step);
            Assert.True(lockstep.tryTakeStep());
            lockstep.reset();
            Assert.False(lockstep.tryTakeStep());
        }

        [Fact]
        public void testLockstepIsOnForScreenshotsOnly() {
            Assert.Equal(0, GameOptions.parse(new string[] { }).lockstepStepsPerFrame());
            Assert.Equal(0, GameOptions.parse(new[] { "--playtest-out", "/tmp/x" }).lockstepStepsPerFrame());
            Assert.Equal(1, GameOptions.parse(new[] { "--screenshot-after-frames", "60" }).lockstepStepsPerFrame());
            // staged play without a screenshot follows the clock
            Assert.Equal(0, GameOptions.parse(new[] { "--stage-caravan" }).lockstepStepsPerFrame());
            Assert.Equal(1, GameOptions.parse(new[] { "--stage-caravan", "--screenshot-after-frames", "60" }).lockstepStepsPerFrame());
            Assert.Equal(0, GameOptions.parse(new[] { "--stage-succession" }).lockstepStepsPerFrame());
            Assert.Equal(0, GameOptions.parse(new[] { "--stage-trade", "--screenshot-after-frames", "60", "--lockstep", "0" }).lockstepStepsPerFrame());
            Assert.Equal(3, GameOptions.parse(new[] { "--lockstep", "3" }).lockstepStepsPerFrame());
        }
    }
}
