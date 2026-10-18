namespace beyondnations {
    /// <summary>
    /// Represents a slot that can hold an ItemStack.
    /// ItemSlots are the containers within an Inventory.
    /// </summary>
    public class ItemSlot {
        private ItemStack itemStack;

        public ItemSlot() {
            this.itemStack = null;
        }

        public ItemSlot(ItemStack itemStack) {
            this.itemStack = itemStack;
        }

        public bool isEmpty() {
            return itemStack == null || itemStack.isEmpty();
        }

        public ItemStack getItemStack() {
            return itemStack;
        }

        public void setItemStack(ItemStack itemStack) {
            this.itemStack = itemStack;
        }

        public void clear() {
            this.itemStack = null;
        }

        public bool hasItemOfType(ItemType type) {
            return !isEmpty() && itemStack.getItemType() == type;
        }

        public int getQuantity() {
            if (isEmpty()) {
                return 0;
            }
            return itemStack.getQuantity();
        }
    }
}
