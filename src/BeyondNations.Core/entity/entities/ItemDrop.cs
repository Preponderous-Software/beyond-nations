using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * An ItemStack that exists in the world as a physical entity.
    * Players and pawns can interact with ItemDrops to pick them up.
    */
    public class ItemDrop : Entity {
        private ItemStack itemStack;

        public ItemDrop(Vector3 position, ItemStack itemStack) : base(EntityType.ITEM_DROP, getItemDropName(itemStack)) {
            this.itemStack = itemStack;
            setPosition(position);
            setAppearance(new Appearance(PrimitiveKind.Sphere, new Vector3(0.5f, 0.5f, 0.5f), colorForItemType(itemStack.getItemType())));
        }

        private static string getItemDropName(ItemStack itemStack) {
            return itemStack.getItemType().ToString() + " x" + itemStack.getQuantity();
        }

        public ItemStack getItemStack() {
            return itemStack;
        }

        // Half the sphere's 0.5 scale, so a drop rests on the flat tile plane
        // (y = 0) rather than floating at whatever height its owner died at.
        public const float GroundHeight = 0.25f;

        /**
        * Turns everything in an inventory into drops at a position, one per
        * non-empty slot, and empties the inventory. This is what a death with
        * keep-inventory off leaves behind, where it used to leave nothing.
        */
        public static List<ItemDrop> dropContentsOf(Inventory inventory, Vector3 position) {
            List<ItemDrop> drops = new List<ItemDrop>();
            Vector3 dropPosition = new Vector3(position.X, GroundHeight, position.Z);
            foreach (ItemSlot slot in inventory.getSlots()) {
                if (!slot.isEmpty()) {
                    ItemStack stack = slot.getItemStack();
                    drops.Add(new ItemDrop(dropPosition, new ItemStack(stack.getItemType(), stack.getQuantity())));
                }
            }
            inventory.clear();
            return drops;
        }

        private static Rgba colorForItemType(ItemType itemType) {
            switch (itemType) {
                case ItemType.COIN:
                    return new Rgba(1f, 0.84f, 0f); // gold
                case ItemType.WOOD:
                    return new Rgba(0.6f, 0.4f, 0.2f); // brown
                case ItemType.STONE:
                    return Rgba.Gray;
                case ItemType.APPLE:
                    return Rgba.Red;
                case ItemType.SAPLING:
                    return Rgba.Green;
                default:
                    return Rgba.White;
            }
        }
    }
}
