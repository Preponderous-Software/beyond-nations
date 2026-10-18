namespace beyondnations {
    /// <summary>
    /// Represents a stack of items of the same type with a quantity.
    /// ItemStacks can exist in ItemSlots within an Inventory or as world drops.
    /// </summary>
    public class ItemStack {
        private Item item;
        private int quantity;

        public ItemStack(Item item, int quantity) {
            this.item = item;
            this.quantity = quantity;
        }

        public ItemStack(ItemType itemType, int quantity) {
            this.item = new Item(itemType);
            this.quantity = quantity;
        }

        public Item getItem() {
            return item;
        }

        public ItemType getItemType() {
            return item.getType();
        }

        public int getQuantity() {
            return quantity;
        }

        public void setQuantity(int quantity) {
            this.quantity = quantity;
        }

        /// <summary>
        /// Adds the specified amount to the quantity. Can accept negative values to decrease quantity.
        /// </summary>
        public void addQuantity(int amount) {
            this.quantity += amount;
        }

        /// <summary>
        /// Removes the specified amount from the quantity. This can result in negative quantities if more is removed than available.
        /// </summary>
        public void removeQuantity(int amount) {
            this.quantity -= amount;
        }

        public bool isEmpty() {
            return quantity <= 0;
        }

        public bool canStackWith(ItemStack other) {
            return other != null && this.item.isSameTypeAs(other.item);
        }
    }
}
