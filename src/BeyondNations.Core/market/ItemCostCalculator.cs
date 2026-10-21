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

        /**
        * What one of these costs to buy at this market right now: the supply
        * curve at what its stalls hold, leaned on by the market's price
        * pressure (MarketPrices).
        */
        public static int calculateCostBasedOnSupply(ItemType itemType, Market market) {
            return calculateCostAtSupply(itemType, market, market.getQuantityAvailable(itemType));
        }

        /** The price at this market if its stalls held quantityAvailable of the item. */
        public static int calculateCostAtSupply(ItemType itemType, Market market, int quantityAvailable) {
            return applyPressure(calculateCost(getBaseCost(itemType), quantityAvailable), market.getPrices().getPressure(itemType));
        }

        /** A price leaned on by a pressure in percent (100 leaves it alone), never below one coin. */
        public static int applyPressure(int cost, int pressurePercent) {
            long pressed = (long) cost * pressurePercent / 100;
            return pressed < 1 ? 1 : (int) pressed;
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