namespace beyondnations {
    /// <summary>
    /// Represents an individual item with its type.
    /// Items are the basic building blocks that can be stacked into ItemStacks.
    /// </summary>
    public class Item {
        private ItemType type;

        public Item(ItemType type) {
            this.type = type;
        }

        public ItemType getType() {
            return type;
        }

        public bool isSameTypeAs(Item other) {
            if (other == null) {
                return false;
            }
            return this.type == other.type;
        }
    }
}
