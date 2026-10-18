using System;
using System.Numerics;

namespace beyondnations {

    public class Sapling : Entity {
        private static readonly Rgba TrunkBrown = new Rgba(0.5f, 0.25f, 0f);

        private int height;

        private DateTime planted;
        private int growTime;

        public Sapling(Vector3 position, int height, RandomSource random) : base(EntityType.SAPLING, "Sapling") {
            this.height = height;
            setPosition(position);

            // A thinner trunk and a smaller cube of leaves than a grown tree.
            setAppearance(new Appearance()
                .addPart(PrimitiveKind.Cylinder, Vector3.Zero, new Vector3(0.5f, height, 0.5f), TrunkBrown)
                .addPart(PrimitiveKind.Cube, new Vector3(0, height - 1, 0), Vector3.One, Rgba.Green));

            getInventory().addItem(ItemType.WOOD, random.range(3, 6));
            getInventory().addItem(ItemType.APPLE, random.range(2, 4));
            getInventory().addItem(ItemType.SAPLING, random.range(1, 3));

            planted = DateTime.Now;
            growTime = random.range(60, 600);
        }

        public int getHeight() {
            return height;
        }

        public bool isGrown() {
            return DateTime.Now.Subtract(planted).TotalSeconds > growTime;
        }
    }
}
