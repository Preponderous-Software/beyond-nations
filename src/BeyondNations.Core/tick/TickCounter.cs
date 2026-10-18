using System;

namespace beyondnations {

    public class TickCounter {
        private int tick = 0;
        
        // measured ticks per second
        private int mtps = 0;
        private int totalTicks = 0;
        
        // last time tick was updated
        private DateTime lastTickUpdate = DateTime.Now;

        public int getTick() {
            return tick;
        }

        public void increment() {
            tick++;
            totalTicks++;

            if (DateTime.Now - lastTickUpdate >= TimeSpan.FromSeconds(1)) {
                mtps = tick;
                tick = 0;
                lastTickUpdate = DateTime.Now;
            }
        }

        public int getMtps() {
            return mtps;
        }

        public int getTotalTicks() {
            return totalTicks;
        }
    }
}