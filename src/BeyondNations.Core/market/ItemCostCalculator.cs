using System.Collections.Generic;

namespace beyondnations {

    public static class ItemCostCalculator {
        private static readonly Dictionary<ItemType, int> baseItemCosts = new Dictionary<ItemType, int>() {
            {ItemType.APPLE, 40},
            {ItemType.WOOD, 60},
            {ItemType.STONE, 30},
            {ItemType.SAPLING, 20},
            {ItemType.CHICKEN_MEAT, 50},
        };

        public static int getBaseCost(ItemType itemType) {
            return baseItemCosts[itemType];
        }

        public static int calculateCostBasedOnSupply(ItemType itemType, Market market) {
            int quantityAvailable = market.getQuantityAvailable(itemType);
            int baseCost = getBaseCost(itemType);
            if (quantityAvailable == 0) {
                return baseCost;
            }
            int cost = baseCost / quantityAvailable;
            if (cost < 1) {
                return 1;
            }
            return cost;
        }

    }
}