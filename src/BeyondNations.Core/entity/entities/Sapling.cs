using System.Numerics;

namespace beyondnations {

    public class Sapling : Entity {
        private static readonly Rgba TrunkBrown = new Rgba(0.5f, 0.25f, 0f);

        private int height;

        private readonly TickCounter tickCounter;
        // The total tick it was planted on, and how long it takes to grow in
        // simulated seconds. Growth is measured in ticks, not on the wall
        // clock, so a seed grows the same trees on every run (#272).
        private readonly int plantedTick;
        private readonly int growTime;

        public Sapling(Vector3 position, int height, RandomSource random, TickCounter tickCounter) : base(EntityType.SAPLING, "Sapling") {
            this.height = height;
            setPosition(position);

            // A thinner trunk and a smaller cube of leaves than a grown tree.
            setAppearance(new Appearance()
                .addPart(PrimitiveKind.Cylinder, Vector3.Zero, new Vector3(0.5f, height, 0.5f), TrunkBrown)
                .addPart(PrimitiveKind.Cube, new Vector3(0, height - 1, 0), Vector3.One, Rgba.Green));

            getInventory().addItem(ItemType.WOOD, random.range(3, 6));
            getInventory().addItem(ItemType.APPLE, random.range(2, 4));
            getInventory().addItem(ItemType.SAPLING, random.range(1, 3));

            this.tickCounter = tickCounter;
            plantedTick = tickCounter.getTotalTicks();
            growTime = random.range(60, 600);
        }

        public int getHeight() {
            return height;
        }

        public int getGrowTime() {
            return growTime;
        }

        public bool isGrown() {
            long ticksSincePlanted = tickCounter.getTotalTicks() - plantedTick;
            return ticksSincePlanted > (long) growTime * tickCounter.getTicksPerSecond();
        }
    }
}
