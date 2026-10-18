using System;
using System.Diagnostics;
using System.Globalization;

namespace beyondnations.desktop.render {

    /**
    * Measures what #218 claims, rather than leaving it asserted.
    *
    * Two of the acceptance criteria for the instanced renderer -- that frame
    * time stays stable at scale, and that the render loop allocates nothing per
    * object -- are only meaningful as numbers, and numbers nobody can produce
    * are not evidence. This records both while the host runs and prints them on
    * shutdown, so any run can be checked with --render-stats.
    *
    * The first frames are treated as warm-up and reported separately: batches
    * are still growing towards their high-water mark then, and that growth is
    * the one allocation the design does permit.
    */
    public class RenderStatsRecorder {
        private const int WarmUpFrames = 30;
        private const int MaxRecordedFrames = 100000;

        private readonly double[] frameMilliseconds = new double[MaxRecordedFrames];
        private int recordedFrames;

        private long frameStartTimestamp;
        private long frameStartAllocatedBytes;

        private long warmUpAllocatedBytes;
        private long steadyAllocatedBytes;
        private int steadyFrames;

        private int collectionsAtStart0;
        private int collectionsAtStart1;
        private int collectionsAtStart2;
        private bool collectionsCaptured;

        private int lastDrawCalls;
        private int lastInstances;
        private int peakDrawCalls;
        private int peakInstances;

        // --- #219 camera and culling ---
        private int lastRenderDistance;
        private int lastCulled;
        private int peakCulled;
        // --- end #219 ---

        public void beginFrame() {
            if (!collectionsCaptured) {
                collectionsAtStart0 = GC.CollectionCount(0);
                collectionsAtStart1 = GC.CollectionCount(1);
                collectionsAtStart2 = GC.CollectionCount(2);
                collectionsCaptured = true;
            }
            frameStartAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
            frameStartTimestamp = Stopwatch.GetTimestamp();
        }

        public void endFrame(int drawCalls, int instances) {
            long elapsedTicks = Stopwatch.GetTimestamp() - frameStartTimestamp;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - frameStartAllocatedBytes;

            double milliseconds = elapsedTicks * 1000.0 / Stopwatch.Frequency;
            if (recordedFrames < MaxRecordedFrames) {
                frameMilliseconds[recordedFrames] = milliseconds;
            }
            recordedFrames++;

            if (recordedFrames <= WarmUpFrames) {
                warmUpAllocatedBytes += allocated;
            } else {
                steadyAllocatedBytes += allocated;
                steadyFrames++;
            }

            lastDrawCalls = drawCalls;
            lastInstances = instances;
            if (drawCalls > peakDrawCalls) {
                peakDrawCalls = drawCalls;
            }
            if (instances > peakInstances) {
                peakInstances = instances;
            }
        }

        public bool hasData() {
            return recordedFrames > 0;
        }

        public int getLastDrawCalls() { return lastDrawCalls; }
        public int getLastInstances() { return lastInstances; }
        public long getSteadyAllocatedBytes() { return steadyAllocatedBytes; }

        // --- #219 camera and culling ---
        /**
        * The render distance in force and how many snapshot items the culler
        * rejected this frame. Until the debug overlay lands (#221), a run with
        * --render-stats is where the render distance can be read.
        */
        public void recordCamera(int renderDistance, int instancesCulled) {
            lastRenderDistance = renderDistance;
            lastCulled = instancesCulled;
            if (instancesCulled > peakCulled) {
                peakCulled = instancesCulled;
            }
        }

        public int getLastRenderDistance() { return lastRenderDistance; }
        public int getLastCulled() { return lastCulled; }
        public int getPeakCulled() { return peakCulled; }
        // --- end #219 ---

        /**
        * One line per fact, so a run can be pasted into a pull request without
        * anybody having to interpret it.
        */
        public string summarize() {
            int sampled = Math.Min(recordedFrames, MaxRecordedFrames);
            if (sampled == 0) {
                return "render stats: no frames were drawn";
            }

            double[] sorted = new double[sampled];
            Array.Copy(frameMilliseconds, sorted, sampled);
            Array.Sort(sorted);

            double total = 0;
            for (int i = 0; i < sampled; i++) {
                total += sorted[i];
            }

            double median = sorted[sampled / 2];
            double p95 = sorted[(int) (sampled * 0.95) < sampled ? (int) (sampled * 0.95) : sampled - 1];
            double perSteadyFrame = steadyFrames > 0 ? (double) steadyAllocatedBytes / steadyFrames : 0;

            CultureInfo culture = CultureInfo.InvariantCulture;
            return string.Format(culture,
                "render stats:\n"
                + "  frames drawn            {0}\n"
                + "  draw calls, last frame  {1}\n"
                + "  draw calls, peak        {2}\n"
                + "  instances, last frame   {3}\n"
                + "  instances, peak         {4}\n"
                + "  render distance         {18}\n"
                + "  instances culled, last frame  {19}\n"
                + "  instances culled, peak        {20}\n"
                + "  frame time min/med/p95/max ms  {5:F3} / {6:F3} / {7:F3} / {8:F3}\n"
                + "  frame time mean ms      {9:F3}\n"
                + "  allocated during first {10} frames, bytes  {11}\n"
                + "  allocated over the remaining {12} frames, bytes  {13}\n"
                + "  allocated per steady frame, bytes  {14:F1}\n"
                + "  gc collections gen0/gen1/gen2  {15} / {16} / {17}",
                recordedFrames,
                lastDrawCalls,
                peakDrawCalls,
                lastInstances,
                peakInstances,
                sorted[0], median, p95, sorted[sampled - 1],
                total / sampled,
                WarmUpFrames, warmUpAllocatedBytes,
                steadyFrames, steadyAllocatedBytes,
                perSteadyFrame,
                GC.CollectionCount(0) - collectionsAtStart0,
                GC.CollectionCount(1) - collectionsAtStart1,
                GC.CollectionCount(2) - collectionsAtStart2,
                lastRenderDistance,
                lastCulled,
                peakCulled);
        }
    }
}
