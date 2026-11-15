using System.Text.Json;
using System.Numerics;

namespace beyondnations {

    public class Sapling : Entity {
        private int height;

        private readonly TickCounter tickCounter;
        // The total tick it was planted on, and how long it takes to grow in
        // simulated seconds. Growth is measured in ticks, not on the wall
        // clock, so a seed grows the same trees on every run (#272).
        private int plantedTick;
        private int growTime;

        public Sapling(Vector3 position, int height, RandomSource random, TickCounter tickCounter) : base(EntityType.SAPLING, "Sapling") {
            this.height = height;
            setPosition(position);

            // A thin stem and a small crown, in the shape it will grow into (#140).
            setAppearance(TreeShape.sapling(position, height));

            getInventory().addItem(ItemType.WOOD, random.range(3, 6));
            getInventory().addItem(ItemType.APPLE, random.range(2, 4));
            getInventory().addItem(ItemType.SAPLING, random.range(1, 3));

            this.tickCounter = tickCounter;
            plantedTick = tickCounter.getTotalTicks();
            // one to five simulated minutes, so a planted grove regrows within a session
            growTime = random.range(60, 300);
        }

        public int getHeight() {
            return height;
        }

        public int getGrowTime() {
            return growTime;
        }

        public bool isGrown() {
            return isGrown(1);
        }

        /**
        * Whether it has grown, when it needs growthTimeFactor times as long as
        * usual -- winter slows growth (SeasonalRules.saplingGrowthFactor).
        */
        public bool isGrown(int growthTimeFactor) {
            long ticksSincePlanted = tickCounter.getTotalTicks() - plantedTick;
            return ticksSincePlanted > (long) growTime * growthTimeFactor * tickCounter.getTicksPerSecond();
        }

        // --- saving (docs/SAVES.md) ---

        internal override void saveState(SaveWriter writer) {
            writer.write("height", height);
            writer.write("plantedTick", plantedTick);
            writer.write("growTime", growTime);
        }

        internal override void loadState(JsonElement element, SaveReader reader) {
            height = SaveReader.getInt(element, "height");
            plantedTick = SaveReader.getInt(element, "plantedTick");
            growTime = SaveReader.getInt(element, "growTime");
            // derived from where it stands, as AppleTree.loadState does
            setAppearance(TreeShape.sapling(getPosition(), height));
        }
    }
}
