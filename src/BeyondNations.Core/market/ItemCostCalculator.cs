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

        /**
        * Each SUPPLY_SOFTNESS units on offer lower the price by one base
        * cost's worth of divisor. It was 10, and a stall stocked with a
        * dozen apples sold them for under half price on Day 1 (#369).
        */
        public const int SUPPLY_SOFTNESS = 20;

        /**
        * The price never falls below this percentage of the base cost, after
        * the market's pressure as well as the supply curve. It used to be a
        * quarter of the base before pressure, which a glut could halve again,
        * so apples fell from 42 to 8 within a game day (#369).
        */
        public const int PRICE_FLOOR_PERCENT = 40;

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
            int baseCost = getBaseCost(itemType);
            int pressed = applyPressure(calculateCost(baseCost, quantityAvailable), market.getPrices().getPressure(itemType));
            return System.Math.Max(pressed, floorOf(baseCost));
        }

        /** The lowest a price can go: PRICE_FLOOR_PERCENT of the base cost, and at least one coin. */
        public static int floorOf(int baseCost) {
            int floor = baseCost * PRICE_FLOOR_PERCENT / 100;
            return floor < 1 ? 1 : floor;
        }

        /** A price leaned on by a pressure in percent (100 leaves it alone), never below one coin. */
        public static int applyPressure(int cost, int pressurePercent) {
            long pressed = (long) cost * pressurePercent / 100;
            return pressed < 1 ? 1 : (int) pressed;
        }

        /**
        * A gentle supply curve: base / (1 + supply / 20), never below
        * PRICE_FLOOR_PERCENT of the base cost (and never below one coin).
        * Twenty units on offer halve the price; it used to be base / supply,
        * so forty apples sold for one coin each and selling was pointless.
        */
        public static int calculateCost(int baseCost, int quantityAvailable) {
            if (quantityAvailable < 0) {
                quantityAvailable = 0;
            }
            long cost = (long) baseCost * SUPPLY_SOFTNESS / (SUPPLY_SOFTNESS + quantityAvailable);
            int floor = floorOf(baseCost);
            return cost < floor ? floor : (int) cost;
        }

    }
}