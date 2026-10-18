using System.Collections.Generic;

namespace beyondnations {

    /// <summary>
    /// The items that can be eaten, and how much energy each one restores.
    /// Energy is the only measure of hunger the simulation keeps, so this table is
    /// the single place that decides what counts as food -- the player, the pawn
    /// energy step and the pawn behavior calculator all read it rather than naming
    /// an item type of their own.
    /// </summary>
    public static class FoodItems {

        /// <summary>
        /// Food in the order it should be eaten, most nourishing first. Kept as an
        /// ordered list rather than derived from the table below so that the choice
        /// is deterministic; TestFoodItems asserts the two stay consistent.
        /// </summary>
        private static readonly ItemType[] foodInDescendingEnergyOrder = new ItemType[] {
            ItemType.CHICKEN_MEAT,
            ItemType.APPLE,
        };

        private static readonly Dictionary<ItemType, int> energyRestoredByFood = new Dictionary<ItemType, int>() {
            {ItemType.CHICKEN_MEAT, 25},
            {ItemType.APPLE, 10},
        };

        public static bool isFood(ItemType itemType) {
            return energyRestoredByFood.ContainsKey(itemType);
        }

        /// <summary>
        /// The energy eating one of these restores, or zero for anything that is
        /// not food.
        /// </summary>
        public static int getEnergyRestored(ItemType itemType) {
            int energy;
            if (energyRestoredByFood.TryGetValue(itemType, out energy)) {
                return energy;
            }
            return 0;
        }

        public static bool hasFood(Inventory inventory) {
            foreach (ItemType foodType in foodInDescendingEnergyOrder) {
                if (inventory.getNumItems(foodType) > 0) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The most nourishing food the inventory holds, or null when it holds none.
        /// </summary>
        public static ItemType? getMostNourishingFood(Inventory inventory) {
            foreach (ItemType foodType in foodInDescendingEnergyOrder) {
                if (inventory.getNumItems(foodType) > 0) {
                    return foodType;
                }
            }
            return null;
        }

        /// <summary>
        /// Removes one of the most nourishing food the inventory holds and returns
        /// the energy it restores. Returns zero, and consumes nothing, when the
        /// inventory holds no food at all.
        /// </summary>
        public static int consumeMostNourishingFood(Inventory inventory) {
            foreach (ItemType foodType in foodInDescendingEnergyOrder) {
                if (inventory.getNumItems(foodType) > 0) {
                    inventory.removeItem(foodType, 1);
                    return getEnergyRestored(foodType);
                }
            }
            return 0;
        }

        /// <summary>
        /// Every edible item type, most nourishing first. Exposed so that the
        /// ordering can be verified against the energy table.
        /// </summary>
        public static IReadOnlyList<ItemType> getFoodInDescendingEnergyOrder() {
            return foodInDescendingEnergyOrder;
        }
    }
}
