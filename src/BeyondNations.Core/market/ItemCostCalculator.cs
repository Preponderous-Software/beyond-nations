using System.Collections.Generic;

namespace beyondnations {

    public static class ItemCostCalculator {
        private static readonly Dictionary<ItemType, int> baseItemCosts = new Dictionary<ItemType, int>() {
            {ItemType.APPLE, 40},
            {ItemType.WOOD, 60},
            {ItemType.STONE, 30},
            {ItemType.SAPLING, 20},
            {ItemType.CHICKEN_MEAT, 50},
            // Crafted trade goods and tools are worth more than what went
            // into them, so crafting to sell is a profit: planks 2 x 120 from
            // 3 wood (180), bricks 2 x 110 from 3 stone and a wood (150), an
            // axe 360 from 300 of materials, a pickaxe 330 from 270. Cooked
            // meat is for eating, priced at the same coins per energy as raw.
            {ItemType.AXE, 360},
            {ItemType.PICKAXE, 330},
            {ItemType.COOKED_MEAT, 90},
            {ItemType.PLANKS, 120},
            {ItemType.BRICKS, 110},
        };

        public static int getBaseCost(ItemType itemType) {
            return baseItemCosts[itemType];
        }

        /** Each SUPPLY_SOFTNESS units on offer lower the price by one base cost's worth of divisor. */
        public const int SUPPLY_SOFTNESS = 10;

        /** The price never falls below this percentage of the base cost. */
        public const int PRICE_FLOOR_PERCENT = 25;

        public static int calculateCostBasedOnSupply(ItemType itemType, Market market) {
            return calculateCost(getBaseCost(itemType), market.getQuantityAvailable(itemType));
        }

        /**
        * A gentle supply curve: base / (1 + supply / 10), never below a quarter
        * of the base cost (and never below one coin). Ten units on offer halve
        * the price; it used to be base / supply, so forty apples sold for one
        * coin each and selling was pointless.
        */
        public static int calculateCost(int baseCost, int quantityAvailable) {
            if (quantityAvailable < 0) {
                quantityAvailable = 0;
            }
            long cost = (long) baseCost * SUPPLY_SOFTNESS / (SUPPLY_SOFTNESS + quantityAvailable);
            int floor = baseCost * PRICE_FLOOR_PERCENT / 100;
            if (floor < 1) {
                floor = 1;
            }
            return cost < floor ? floor : (int) cost;
        }

    }
}