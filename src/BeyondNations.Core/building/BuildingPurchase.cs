namespace beyondnations {

    /**
    * Buying a building with coins: what the wood and stone the builder is
    * short of cost in coins, so a purse has something to buy besides a stall
    * and a gift. (The fun pass found a player who does not court sitting on
    * 4,500 to 6,600 coins at 30 minutes with nothing to spend them on.)
    *
    * Only what is missing is bought: the wood and stone the builder carries
    * and the settlement's stockpile go in first, as they do for Build, and
    * each unit still short costs
    *
    *     base price * (BasePercent + PercentPerBuilding * buildings standing) / 100
    *
    * coins, the base price being the market's (ItemCostCalculator: wood 60,
    * stone 30). So a first House bought outright costs 1,980 coins, the sixth
    * building 2,805 for a House, and the twelfth about 3,800: real money in a
    * Village or Town purse, more for each building the settlement already has.
    * Gathering stays the cheaper road -- it costs nothing but time, and a
    * unit bought costs more than one sold at the market's base price fetches.
    *
    * Nothing here is random and nothing is stored: a bought building is an
    * ordinary building, so saves are unchanged.
    */
    public static class BuildingPurchase {

        /** What a missing unit costs, as a percentage of its market base price, in a settlement with no buildings. */
        public const int BasePercent = 120;

        /** How much dearer, in percentage points, each building already standing makes the next. */
        public const int PercentPerBuilding = 10;

        /** The percentage of the base price a missing unit costs in this settlement. */
        public static int getPercent(Settlement settlement) {
            return BasePercent + PercentPerBuilding * settlement.getNumBuildings();
        }

        /** Coins for one missing unit of wood or stone in this settlement. */
        public static int getCoinsPerUnit(Settlement settlement, ItemType itemType) {
            return ItemCostCalculator.getBaseCost(itemType) * getPercent(settlement) / 100;
        }

        /** How much of an item the builder and the stockpile are short of for this building. */
        public static int getShortfall(Inventory builder, Inventory stockpile, BuildingType type, ItemType itemType) {
            int cost = itemType == ItemType.WOOD ? BuildingCatalog.getWoodCost(type) : BuildingCatalog.getStoneCost(type);
            int available = builder.getNumItems(itemType) + (stockpile == null ? 0 : stockpile.getNumItems(itemType));
            return System.Math.Max(0, cost - available);
        }

        /**
        * The coins this builder pays for this building here: the missing wood
        * and stone at this settlement's rate. Zero when what the builder
        * carries and the stockpile already cover it.
        */
        public static int getCoinPrice(Settlement settlement, Inventory builder, BuildingType type) {
            Inventory stockpile = settlement.getInventory();
            return getShortfall(builder, stockpile, type, ItemType.WOOD) * getCoinsPerUnit(settlement, ItemType.WOOD)
                + getShortfall(builder, stockpile, type, ItemType.STONE) * getCoinsPerUnit(settlement, ItemType.STONE);
        }

        /** What the building would cost in coins with no wood or stone at all. */
        public static int getFullCoinPrice(Settlement settlement, BuildingType type) {
            return BuildingCatalog.getWoodCost(type) * getCoinsPerUnit(settlement, ItemType.WOOD)
                + BuildingCatalog.getStoneCost(type) * getCoinsPerUnit(settlement, ItemType.STONE);
        }

        /** Whether the builder has the coins to buy what is missing (and something is missing). */
        public static bool canBuy(Settlement settlement, Inventory builder, BuildingType type) {
            int price = getCoinPrice(settlement, builder, type);
            return price > 0 && builder.getNumItems(ItemType.COIN) >= price;
        }

        /**
        * Why the builder cannot buy this building, or null when they can:
        * "You have the wood and stone: build it instead." or "Needs 1,980
        * coins; you have 1,200." Says nothing about room or era, which
        * the caller checks as it does for Build.
        */
        public static string describeWhyNot(Settlement settlement, Inventory builder, BuildingType type) {
            int price = getCoinPrice(settlement, builder, type);
            if (price == 0) {
                return "You have the wood and stone: build it instead.";
            }
            int coins = builder.getNumItems(ItemType.COIN);
            if (coins < price) {
                return "Needs " + formatCoins(price) + " coins; you have " + formatCoins(coins) + ".";
            }
            return null;
        }

        /**
        * What the coins pay for: "the 12 wood and 15 stone you are short of
        * (1,404 coins)".
        */
        public static string describeShortfall(Settlement settlement, Inventory builder, BuildingType type) {
            Inventory stockpile = settlement.getInventory();
            int wood = getShortfall(builder, stockpile, type, ItemType.WOOD);
            int stone = getShortfall(builder, stockpile, type, ItemType.STONE);
            string what = wood > 0 && stone > 0 ? wood + " wood and " + stone + " stone"
                : wood > 0 ? wood + " wood" : stone + " stone";
            return "the " + what + " you are short of (" + formatCoins(getCoinPrice(settlement, builder, type)) + " coins)";
        }

        /**
        * Pays for the building: every unit of wood and stone the builder and
        * the stockpile have that it needs (the builder's first), and the coins
        * for the rest. The caller has checked canBuy.
        */
        public static int pay(Settlement settlement, Inventory builder, BuildingType type) {
            int price = getCoinPrice(settlement, builder, type);
            Inventory stockpile = settlement.getInventory();
            take(builder, stockpile, ItemType.WOOD, BuildingCatalog.getWoodCost(type));
            take(builder, stockpile, ItemType.STONE, BuildingCatalog.getStoneCost(type));
            builder.removeItem(ItemType.COIN, price);
            return price;
        }

        /** 1980 as "1,980". */
        public static string formatCoins(int coins) {
            return coins.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void take(Inventory builder, Inventory stockpile, ItemType itemType, int wanted) {
            int fromBuilder = System.Math.Min(wanted, builder.getNumItems(itemType));
            if (fromBuilder > 0) {
                builder.removeItem(itemType, fromBuilder);
            }
            int fromStockpile = System.Math.Min(wanted - fromBuilder, stockpile == null ? 0 : stockpile.getNumItems(itemType));
            if (fromStockpile > 0) {
                stockpile.removeItem(itemType, fromStockpile);
            }
        }
    }
}
