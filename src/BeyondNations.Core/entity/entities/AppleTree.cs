using System.Text.Json;
using System.Numerics;

namespace beyondnations {

    public class AppleTree : Entity {
        private int height;

        public AppleTree(Vector3 position, int height, RandomSource random) : base(EntityType.TREE, "Tree") {
            this.height = height;
            setPosition(position);

            // A trunk and a layered canopy, shaped by where it stands (#140).
            setAppearance(TreeShape.tree(position, height));

            // 4-6 wood and 1-3 apples: about ten trees found a settlement and
            // every tree feeds you a little.
            getInventory().addItem(ItemType.WOOD, random.range(4, 7));
            getInventory().addItem(ItemType.APPLE, random.range(1, 4));
            getInventory().addItem(ItemType.SAPLING, random.range(0, 3));
        }

        public int getHeight() {
            return height;
        }

        // --- saving (docs/SAVES.md) ---

        internal override void saveState(SaveWriter writer) {
            writer.write("height", height);
        }

        internal override void loadState(JsonElement element, SaveReader reader) {
            height = SaveReader.getInt(element, "height");
            // The look is derived, not state: rebuild it from where the tree
            // stands, so a tree from an older save takes today's shape and
            // the saved appearance never has to be trusted.
            setAppearance(TreeShape.tree(getPosition(), height));
        }
    }
}
