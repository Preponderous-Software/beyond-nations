using System;
using System.Collections.Generic;

namespace beyondnations {

    /**
    * Living prices: every settlement's market leans its prices toward what
    * the settlement has and what its people need.
    *
    * A stall's own stock already moves a price the moment it changes (the
    * supply curve in ItemCostCalculator). On top of that each market keeps a
    * pressure per item (MarketPrices) that drifts, a few points every
    * pricing round, toward
    *
    *     100 * (demand + K) / (stock + K)        clamped to 50..200
    *
    * where stock is what the stalls and the settlement's stockpile hold, and
    * demand grows with the people who live there: food for every mouth, wood
    * and stone for the next house. So a crowded town with an empty larder
    * pays well over the odds for apples, a camp sitting on a woodpile pays
    * little for more wood, and goods carried from one to the other -- by the
    * player or by a caravan -- sell for more than they cost.
    *
    * Nothing here is random, and it is all whole numbers, so prices are part
    * of the deterministic world.
    */
    public static class MarketPricing {
        /** How often prices drift: every ten seconds at 50 ticks a second. */
        public const int ROUND_TICKS = 500;

        /** The most a pressure moves in one round, in percentage points. */
        public const int DRIFT_STEP = 5;

        /** Softens the demand/stock ratio, so a handful of goods does not swing a price wildly. */
        public const int STOCK_SOFTNESS = 20;

        /** Every item a market prices: everything but the coin it is priced in. */
        public static readonly ItemType[] PricedItems = {
            ItemType.APPLE, ItemType.CHICKEN_MEAT, ItemType.COOKED_MEAT,
            ItemType.WOOD, ItemType.STONE, ItemType.PLANKS, ItemType.BRICKS,
            ItemType.AXE, ItemType.PICKAXE, ItemType.SAPLING,
        };

        /** Runs a pricing round when one is due. Called once a tick. */
        public static void update(EntityRepository entityRepository, int totalTicks, TradeNeeds needs = null) {
            if (totalTicks <= 0 || totalTicks % ROUND_TICKS != 0) {
                return;
            }
            runRound(entityRepository, needs);
        }

        /** Drifts every settlement's prices one step toward where they ought to be. */
        public static void runRound(EntityRepository entityRepository, TradeNeeds needs = null) {
            int count = entityRepository.getEntityCount();
            for (int i = 0; i < count; i++) {
                Settlement settlement = entityRepository.getEntityAt(i) as Settlement;
                if (settlement == null || settlement.isMarkedForDeletion()) {
                    continue;
                }
                driftPrices(settlement, needs);
            }
        }

        public static void driftPrices(Settlement settlement, TradeNeeds needs = null) {
            Market market = settlement.getMarket();
            MarketPrices prices = market.getPrices();
            foreach (ItemType itemType in PricedItems) {
                // the price as it stands is what the trend arrow compares against next
                prices.setReference(itemType, market.getBuyPrice(itemType));
                prices.driftToward(itemType, getTargetPressure(settlement, itemType, needs), DRIFT_STEP);
            }
        }

        /** Where this settlement's pressure on an item is heading, in percent. */
        public static int getTargetPressure(Settlement settlement, ItemType itemType, TradeNeeds needs = null) {
            long demand = getDemand(settlement, itemType, needs);
            long stock = getStock(settlement, itemType);
            long target = 100L * (demand + STOCK_SOFTNESS) / (stock + STOCK_SOFTNESS);
            return MarketPrices.clampPressure((int) Math.Min(target, int.MaxValue));
        }

        /**
        * How much of an item a settlement wants to have about: some for
        * everyone, more for every resident. Foods count together, since a
        * hungry town is as glad of meat as of apples.
        */
        public static int getDemand(Settlement settlement, ItemType itemType, TradeNeeds needs = null) {
            int residents = Math.Max(0, settlement.getResidentCount());
            int demand;
            if (FoodItems.isFood(itemType)) {
                demand = 3 + 2 * residents;
            }
            else {
                switch (itemType) {
                    case ItemType.WOOD: demand = 10 + 2 * residents; break;
                    case ItemType.STONE: demand = 6 + residents; break;
                    case ItemType.PLANKS:
                    case ItemType.BRICKS: demand = 2 + residents / 2; break;
                    case ItemType.AXE:
                    case ItemType.PICKAXE: demand = 1 + residents / 4; break;
                    default: demand = 2; break;
                }
            }
            // a nation settled on land short of this (NationResources, #318)
            // wants half as much again; one with plenty of it, a third less
            return TradeNeeds.scaleByStanding(demand, TradeNeeds.getStanding(needs, settlement, itemType), true);
        }

        /** What a settlement holds of an item: its stalls and its stockpile. Foods count together. */
        public static int getStock(Settlement settlement, ItemType itemType) {
            if (FoodItems.isFood(itemType)) {
                int food = 0;
                foreach (ItemType foodType in FoodItems.getFoodInDescendingEnergyOrder()) {
                    food += stockOf(settlement, foodType);
                }
                return food;
            }
            return stockOf(settlement, itemType);
        }

        private static int stockOf(Settlement settlement, ItemType itemType) {
            return settlement.getMarket().getQuantityAvailable(itemType) + Math.Max(0, settlement.getInventory().getNumItems(itemType));
        }

        /**
        * -1, 0 or +1: whether an item's price here has fallen, held or risen
        * since the last pricing round.
        */
        public static int getTrend(Market market, ItemType itemType) {
            int reference = market.getPrices().getReference(itemType);
            if (reference == MarketPrices.NO_REFERENCE) {
                return 0;
            }
            return Math.Sign(market.getBuyPrice(itemType) - reference);
        }
    }
}
