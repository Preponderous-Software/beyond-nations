namespace beyondnations.desktop.ui {

    /**
    * The frames-per-second figure the debug overlay reports.
    *
    * The Unity overlay printed (int)(1 / Time.smoothDeltaTime), where
    * smoothDeltaTime is a smoothed frame time rather than the raw one, so the
    * number does not flicker. Unity is gone, so the smoothing is done here:
    * an exponential moving average of the frame delta, which is what
    * smoothDeltaTime is.
    *
    * Nothing here touches a graphics context, so the reported figure can be
    * checked in a test.
    */
    public class FrameRateCounter {
        // Unity's smoothing factor for smoothDeltaTime.
        private const double SmoothingFactor = 0.2;

        private double smoothedDeltaSeconds;
        private bool hasSample;

        public void record(double deltaSeconds) {
            if (deltaSeconds <= 0) {
                return;
            }
            if (!hasSample) {
                smoothedDeltaSeconds = deltaSeconds;
                hasSample = true;
                return;
            }
            smoothedDeltaSeconds = smoothedDeltaSeconds + (deltaSeconds - smoothedDeltaSeconds) * SmoothingFactor;
        }

        public double getSmoothedDeltaSeconds() {
            return smoothedDeltaSeconds;
        }

        /**
        * Zero until a frame has been recorded, which is what the very first
        * frame of a run reports.
        */
        public int getFramesPerSecond() {
            if (!hasSample || smoothedDeltaSeconds <= 0) {
                return 0;
            }
            return (int) (1.0 / smoothedDeltaSeconds);
        }
    }
}
