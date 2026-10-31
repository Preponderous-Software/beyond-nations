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
    * (RegionCatalog.farmBonus). Indoors in winter, a starving pawn burns
    * energy at a quarter of the rate (shelterMultiplier, #353).
    *
    * Two rules keep the good seasons from storing up a mild winter. Apples
    * spoil all year: a pawn's or a market stall's apples beyond a modest store
    * rot a sixth at a time (applesSpoiling), so nobody carries a hoard into
    * winter. And in winter the trees a pawn fells are bare (treesBearFruit),
    * so wood no longer brings food with it. The player is spared both: their
    * pack and their own stalls never rot, and a tree still gives them its
    * apples, so a world started in winter can still teach eating.
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

        /** How far a pawn after food (hungry, or laying in stores) will walk for a chicken rather than take a fruit tree. */
        public const float STORES_HUNT_RANGE = 120f;

        /** Apples each farm adds per harvest round in this season, before the region's bonus. */
        public static int farmYield(Season season) {
            switch (season) {
                case Season.AUTUMN: return 2;
                case Season.WINTER: return 0;
                default: return 1;
            }
        }

        /** Ticks between spoilage rounds: 30 seconds, the same as a chicken hatching. */
        public const int APPLE_SPOIL_INTERVAL_TICKS = 1500;

        /**
        * Apples a pawn keeps without any spoiling: three times WINTER_FOOD_STORE, so
        * the store laid in for winter never rots, only a hoard does.
        */
        public const int PAWN_APPLES_KEPT = 3 * WINTER_FOOD_STORE;

        /** Apples a market stall keeps without any spoiling: a small stock that still sells through winter. */
        public const int STALL_APPLES_KEPT = 10;

        /** The share of the apples beyond the kept store that rots each round: one in SPOIL_DIVISOR, rounded up. */
        public const int SPOIL_DIVISOR = 6;

        /**
        * How many of these apples rot this round, given how many keep: none of
        * the kept store, and a sixth (rounded up) of the rest. Halves a hoard
        * in about two minutes, so a pawn that fells trees all summer
        * carries a couple of dozen apples into winter rather than two hundred.
        */
        public static int applesSpoiling(int apples, int kept) {
            if (apples <= kept) {
                return 0;
            }
            return (apples - kept + SPOIL_DIVISOR - 1) / SPOIL_DIVISOR;
        }

        /**
        * Whether a tree a pawn fells this season carries apples. Not in
        * winter: the fruit has fallen, so a pawn felling trees for wood gets
        * wood and saplings and no food. (The apples stay with the tree, so a
        * tree standing at the end of winter bears again in spring.)
        */
        public static bool treesBearFruit(Season season) {
            return season != Season.WINTER;
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
        * How much slower a starving pawn burns energy while it shelters in a
        * settlement in winter: it huddles by the fire and rations what is left.
        * In the 0.5.0 and 0.6.0 playtests pawns starved by the dozen "within
        * the walls" in one winter (#353); at a quarter of the rate a pawn that
        * gets home with its last reserves lasts out most of a winter, long
        * enough for a farm in spring, a market apple or Feed Residents.
        */
        public const float WINTER_SHELTER_MULTIPLIER = 0.25f;

        /** The factor on energy a pawn burns inside a settlement, after GameConfig's settlement multiplier. */
        public static float shelterMultiplier(Season season, bool starving) {
            return season == Season.WINTER && starving ? WINTER_SHELTER_MULTIPLIER : 1f;
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
            return FoodItems.countFood(inventory);
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
