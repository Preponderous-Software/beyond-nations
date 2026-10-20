namespace beyondnations {

    /**
    * What each season does to the world, in one place (docs/SYSTEMS.md,
    * "Seasons"). Spring and summer are the world as it always was, except that
    * chickens hatch; autumn brings a double harvest; winter is lean.
    *
    * | Season | Farms        | Saplings | Chickens hatch | Hunger outside |
    * | ------ | ------------ | -------- | -------------- | -------------- |
    * | Spring | 1 apple      | grow     | yes            | normal         |
    * | Summer | 1 apple      | grow     | yes            | normal         |
    * | Autumn | 2 apples     | grow     | no             | normal         |
    * | Winter | nothing      | 3x slower| no             | x1.3 (cold)    |
    *
    * Plains farms harvest one apple more whenever farms harvest at all
    * (RegionCatalog.farmBonus).
    */
    public static class SeasonalRules {

        /** How much faster energy burns outside a settlement in winter. Indoors is as warm as ever. */
        public const float WINTER_COLD_MULTIPLIER = 1.3f;

        /**
        * Food a pawn wants to carry into winter. From autumn on, a pawn holding
        * less leans towards food when it gathers (PawnBehaviorExecutor).
        */
        public const int WINTER_FOOD_STORE = 4;

        /** Ticks between chicken hatchings in spring and summer: 30 seconds. */
        public const int CHICKEN_HATCH_INTERVAL_TICKS = 1500;

        /** How far a pawn laying in stores will walk for a chicken rather than fell the nearest tree. */
        public const float STORES_HUNT_RANGE = 120f;

        /** Apples each farm adds per harvest round in this season, before the region's bonus. */
        public static int farmYield(Season season) {
            switch (season) {
                case Season.AUTUMN: return 2;
                case Season.WINTER: return 0;
                default: return 1;
            }
        }

        /** How many times longer than usual a sapling takes to grow in winter. */
        public const int WINTER_SAPLING_GROWTH_FACTOR = 3;

        /**
        * How many times its usual growing time a sapling needs before it is a
        * tree: WINTER_SAPLING_GROWTH_FACTOR in winter, 1 otherwise. Stopping
        * growth outright left a world of saplings and almost no trees by
        * midwinter (pawns fell about 1,500 trees a season); slowed, wood is
        * scarce in winter but never gone.
        */
        public static int saplingGrowthFactor(Season season) {
            return season == Season.WINTER ? WINTER_SAPLING_GROWTH_FACTOR : 1;
        }

        public static bool chickensHatch(Season season) {
            return season == Season.SPRING || season == Season.SUMMER;
        }

        /** The factor on energy burned outside a settlement. */
        public static float coldMultiplier(Season season) {
            return season == Season.WINTER ? WINTER_COLD_MULTIPLIER : 1f;
        }

        /**
        * Whether a pawn should lay in food: in autumn and winter, while it
        * carries less than WINTER_FOOD_STORE.
        */
        public static bool wantsStores(Pawn pawn, Season season) {
            if (season != Season.AUTUMN && season != Season.WINTER) {
                return false;
            }
            return countFood(pawn.getInventory()) < WINTER_FOOD_STORE;
        }

        public static int countFood(Inventory inventory) {
            int total = 0;
            foreach (ItemType food in FoodItems.getFoodInDescendingEnergyOrder()) {
                total += inventory.getNumItems(food);
            }
            return total;
        }

        /** One line on what the season means for the player, for the HUD. */
        public static string describe(Season season) {
            switch (season) {
                case Season.SPRING: return "Saplings grow, chickens hatch.";
                case Season.SUMMER: return "Long days; chickens hatch.";
                case Season.AUTUMN: return "Farms yield double. Lay in food.";
                default: return "No harvests, slow regrowth; the cold bites outdoors.";
            }
        }
    }
}
