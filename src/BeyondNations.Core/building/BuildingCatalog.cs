namespace beyondnations {

    /**
    * What every building costs, how many of each a settlement may have, and
    * what each one does. One table, so the command, the pawn behaviour, the
    * HUD and the docs never disagree about a number.
    *
    *   House       wood 20, stone 15  -- room for HOUSE_RESIDENTS more pawns to call the settlement home
    *   Storehouse  wood 10, stone 30  -- STOREHOUSE_STALLS more market stalls
    *   Farm        wood 25, stone 10  -- one apple every FARM_TICKS_PER_HARVEST ticks, stocked into a merchant's stall
    *
    * Every building needs far more stone than a stall does: a settlement
    * cannot grow past a camp without someone quarrying.
    */
    public static class BuildingCatalog {

        /** A settlement holds at most this many buildings, one per layout slot. */
        public const int MAX_BUILDINGS = 12;

        /** How many pawns may call a settlement with no houses home. */
        public const int BASE_RESIDENTS = 8;
        public const int HOUSE_RESIDENTS = 4;
        public const int STOREHOUSE_STALLS = 2;
        public const int FARM_TICKS_PER_HARVEST = 500;

        public static int getWoodCost(BuildingType type) {
            switch (type) {
                case BuildingType.HOUSE: return 20;
                case BuildingType.STOREHOUSE: return 10;
                case BuildingType.FARM: return 25;
            }
            return 0;
        }

        public static int getStoneCost(BuildingType type) {
            switch (type) {
                case BuildingType.HOUSE: return 15;
                case BuildingType.STOREHOUSE: return 30;
                case BuildingType.FARM: return 10;
            }
            return 0;
        }

        /** The most of this type one settlement may have. */
        public static int getLimit(BuildingType type) {
            switch (type) {
                case BuildingType.HOUSE: return 6;
                case BuildingType.STOREHOUSE: return 2;
                case BuildingType.FARM: return 4;
            }
            return 0;
        }

        public static string getDisplayName(BuildingType type) {
            switch (type) {
                case BuildingType.HOUSE: return "House";
                case BuildingType.STOREHOUSE: return "Storehouse";
                case BuildingType.FARM: return "Farm";
            }
            return type.ToString();
        }

        /** e.g. "20 wood, 15 stone" */
        public static string describeCost(BuildingType type) {
            return getWoodCost(type) + " wood, " + getStoneCost(type) + " stone";
        }

        public static bool canAfford(Inventory inventory, BuildingType type) {
            return inventory.getNumItems(ItemType.WOOD) >= getWoodCost(type)
                && inventory.getNumItems(ItemType.STONE) >= getStoneCost(type);
        }

        /**
        * Whether the builder can pay, counting the settlement's stockpile (the
        * wood and stone member pawns deliver under orders) as well as what the
        * builder carries. A null stockpile counts as empty.
        */
        public static bool canAfford(Inventory builder, Inventory stockpile, BuildingType type) {
            return available(builder, stockpile, ItemType.WOOD) >= getWoodCost(type)
                && available(builder, stockpile, ItemType.STONE) >= getStoneCost(type);
        }

        public static void deductCost(Inventory inventory, BuildingType type) {
            inventory.removeItem(ItemType.WOOD, getWoodCost(type));
            inventory.removeItem(ItemType.STONE, getStoneCost(type));
        }

        /**
        * Takes the cost from what the builder carries first and the rest from
        * the stockpile. The caller has checked canAfford.
        */
        public static void deductCost(Inventory builder, Inventory stockpile, BuildingType type) {
            take(builder, stockpile, ItemType.WOOD, getWoodCost(type));
            take(builder, stockpile, ItemType.STONE, getStoneCost(type));
        }

        private static int available(Inventory builder, Inventory stockpile, ItemType itemType) {
            return builder.getNumItems(itemType) + (stockpile == null ? 0 : stockpile.getNumItems(itemType));
        }

        private static void take(Inventory builder, Inventory stockpile, ItemType itemType, int amount) {
            int fromBuilder = System.Math.Min(amount, builder.getNumItems(itemType));
            if (fromBuilder > 0) {
                builder.removeItem(itemType, fromBuilder);
            }
            int rest = amount - fromBuilder;
            if (rest > 0 && stockpile != null) {
                stockpile.removeItem(itemType, rest);
            }
        }
    }
}
