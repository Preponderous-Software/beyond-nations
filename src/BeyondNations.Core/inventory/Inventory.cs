using System.Collections.Generic;

namespace beyondnations {

    /// <summary>
    /// Represents an inventory containing a list of item slots.
    /// Each slot can contain an ItemStack of items.
    /// </summary>
    public class Inventory {
        private List<ItemSlot> slots;

        // Uses already spent on the tool in hand, per tool type (see Tools).
        // Forgotten whenever the last of that tool leaves the inventory, so a
        // fresh tool always starts unworn.
        private Dictionary<ItemType, int> toolWear = new Dictionary<ItemType, int>();

        public Inventory(int numGoldCoins) {
            slots = new List<ItemSlot>();
            
            // Initialize slots for each item type
            // Coins slot
            if (numGoldCoins > 0) {
                slots.Add(new ItemSlot(new ItemStack(ItemType.COIN, numGoldCoins)));
            } else {
                slots.Add(new ItemSlot());
            }
            
            // Other item slots (initially empty)
            slots.Add(new ItemSlot()); // WOOD
            slots.Add(new ItemSlot()); // STONE
            slots.Add(new ItemSlot()); // APPLE
            slots.Add(new ItemSlot()); // SAPLING
        }
        
        public List<ItemSlot> getSlots() {
            return slots;
        }
        
        public int getNumItems(ItemType itemType) {
            foreach (ItemSlot slot in slots) {
                if (slot.hasItemOfType(itemType)) {
                    return slot.getQuantity();
                }
            }
            return 0;
        }

        public void addItem(ItemType itemType, int numItems) {
            // Try to find existing slot with this item type
            foreach (ItemSlot slot in slots) {
                if (slot.hasItemOfType(itemType)) {
                    slot.getItemStack().addQuantity(numItems);
                    return;
                }
            }
            
            // Try to find an empty slot
            foreach (ItemSlot slot in slots) {
                if (slot.isEmpty()) {
                    slot.setItemStack(new ItemStack(itemType, numItems));
                    return;
                }
            }
            
            // Add a new slot if no empty slot was found
            slots.Add(new ItemSlot(new ItemStack(itemType, numItems)));
        }

        /// <summary>
        /// Removes the specified number of items from the inventory.
        /// Note: The slot will be cleared if the quantity becomes zero or negative after removal.
        /// </summary>
        public void removeItem(ItemType itemType, int numItems) {
            foreach (ItemSlot slot in slots) {
                if (slot.hasItemOfType(itemType)) {
                    slot.getItemStack().removeQuantity(numItems);
                    // Clear the slot if empty (quantity <= 0)
                    if (slot.getItemStack().isEmpty()) {
                        slot.clear();
                        toolWear.Remove(itemType);
                    }
                    return;
                }
            }
        }

        public bool hasItem(ItemType itemType) {
            return getNumItems(itemType) > 0;
        }

        public void setNumItems(ItemType itemType, int numItems) {
            // Try to find existing slot with this item type
            foreach (ItemSlot slot in slots) {
                if (slot.hasItemOfType(itemType)) {
                    if (numItems > 0) {
                        slot.getItemStack().setQuantity(numItems);
                    } else {
                        slot.clear();
                        toolWear.Remove(itemType);
                    }
                    return;
                }
            }
            
            // If not found and numItems > 0, add to empty slot or create new slot
            if (numItems > 0) {
                foreach (ItemSlot slot in slots) {
                    if (slot.isEmpty()) {
                        slot.setItemStack(new ItemStack(itemType, numItems));
                        return;
                    }
                }
                slots.Add(new ItemSlot(new ItemStack(itemType, numItems)));
            }
        }

        /// <summary>
        /// Clears all items from the inventory, including any coins.
        /// After clearing, all item counts will be 0.
        /// </summary>
        public void clear() {
            foreach (ItemSlot slot in slots) {
                slot.clear();
            }
            toolWear.Clear();
        }

        /** Uses already spent on the tool of this type in hand; 0 for a fresh one. */
        public int getToolWear(ItemType toolType) {
            int wear;
            return toolWear.TryGetValue(toolType, out wear) ? wear : 0;
        }

        public void setToolWear(ItemType toolType, int wear) {
            if (wear <= 0) {
                toolWear.Remove(toolType);
            } else {
                toolWear[toolType] = wear;
            }
        }

        /**
        * Transfers the contents of the other inventory to this inventory.
        * 
        * @param otherInventory
        */
        public void transferContentsOfInventory(Inventory otherInventory) {
            foreach (ItemSlot otherSlot in otherInventory.getSlots()) {
                if (!otherSlot.isEmpty()) {
                    ItemType itemType = otherSlot.getItemStack().getItemType();
                    int quantity = otherSlot.getQuantity();
                    addItem(itemType, quantity);
                }
            }
            otherInventory.clear();
        }

        /** Tradeable items (anything but coin) that count as worth a trip to market. */
        public const int ABUNDANCE_THRESHOLD = 20;

        /**
        * Whether this holds enough tradeable goods -- any mix of wood, stone,
        * food and saplings -- to be worth taking to market. It used to need
        * more than ten each of wood, stone and apples at once, which with one
        * stone per rock almost never happened, so pawns never traded.
        */
        public bool containsAbundanceOfResources() {
            return getTotalNumItems() - getNumItems(ItemType.COIN) >= ABUNDANCE_THRESHOLD;
        }

        public int getTotalNumItems() {
            int total = 0;
            foreach (ItemSlot slot in slots) {
                if (!slot.isEmpty()) {
                    total += slot.getQuantity();
                }
            }
            return total;
        }
    }
}