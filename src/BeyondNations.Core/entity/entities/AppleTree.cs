using System.Numerics;

namespace beyondnations {

    public class AppleTree : Entity {
        private static readonly Rgba TrunkBrown = new Rgba(0.5f, 0.25f, 0f);

        private int height;

        public AppleTree(Vector3 position, int height, RandomSource random) : base(EntityType.TREE, "Tree") {
            this.height = height;
            setPosition(position);

            // A cylinder trunk with a cube of leaves above it, as before. The
            // offsets are relative to the tree's own position.
            setAppearance(new Appearance()
                .addPart(PrimitiveKind.Cylinder, Vector3.Zero, new Vector3(1, height, 1), TrunkBrown)
                .addPart(PrimitiveKind.Cube, new Vector3(0, height - 1, 0), new Vector3(3, 3, 3), Rgba.Green));

            getInventory().addItem(ItemType.WOOD, random.range(3, 6));
            getInventory().addItem(ItemType.APPLE, random.range(0, 3));
            getInventory().addItem(ItemType.SAPLING, random.range(0, 3));
        }

        public int getHeight() {
            return height;
        }
    }
}
