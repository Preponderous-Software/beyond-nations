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
    * energy at 0.4 of the rate (shelterMultiplier, #353, #504).
    *
    * Two rules keep the good seasons from storing up a mild winter. Apples
    * spoil all year: a pawn's or a market stall's apples beyond a modest store
    * rot a sixth at a time (applesSpoiling), so nobody carries a hoard into
    * winter. And in winter the trees a pawn fells are bare (treesBearFruit),
    * so wood no longer brings food with it, for the player as for pawns. The
    * player used to be spared, which left stored food and the winter warning
    * (#456) with little to do. The player's pack and own stalls still never
    * rot. A world started in winter still teaches eating: "Gather wood"
    * rewards two apples, and the "Eat something" hint points at chickens.
    */
    public static class SeasonalRules {

        /** How much faster energy burns outside a settlement in winter. Indoors is as warm as ever. */
        public const float WINTER_COLD_MULTIPLIER = 1.3f;

        /**
        * Food a pawn wants to carry into winter: what a member of a nation that
        * shelters at home eats in a winter. From autumn on, a pawn holding
        * less leans towards food when it gathers (PawnBehaviorExecutor), and
        * the winter warning, the Season box and Feed Residents all aim at it
        * for each member.
        *
        * A pawn burns 0.0035 energy a tick on average (Pawn's 0.002-0.005), at
        * 0.8 of that indoors and 1.3 out in the cold. Members of a sheltering
        * nation still spend a fifth to half of a winter outdoors (measured
        * 15-55% over seeds 101-108, a player's camp of six to eleven), so at
        * 30% outdoors a winter of DAYS_PER_SEASON days of 30,000 ticks burns
        * about 200 energy: 13 apples at 15 each. The measured burn was
        * 166-231 a member (gluttons burn half again).
        *
        * It was 4 (#488), then 12 (#495) with the sum done indoors only; and
        * pawns ate below 90 energy, so the full bar took up to 5 of an apple's
        * 15 and a third of the stores went nowhere (playtest s03: a camp
        * stocked to the target was empty early on the second winter day).
        * Pawns now eat what fits (Pawn.eatWhatFits). TestSeasonalRules
        * .testWinterFoodStore_IsWhatAShelteringWinterEats checks the sum.
        */
        public const int WINTER_FOOD_STORE = 14;

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
        * Apples a pawn keeps without any spoiling: the WINTER_FOOD_STORE, so
        * the store laid in for winter never rots, only a hoard does, and Feed
        * Residents, which stocks a member up to the store, never hands over an
        * apple that will rot. The Season box counts only what keeps and says
        * how much is rotting (WinterStores.Count.Spoiling).
        */
        public const int PAWN_APPLES_KEPT = WINTER_FOOD_STORE;

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
        * Whether a tree felled this season, by a pawn or the player, carries
        * apples. Not in winter: the fruit has fallen, so felling trees for
        * wood gets wood and saplings and no food. (The apples stay with the tree, so a
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
        * the walls" in one winter (#353), so it was set to a quarter.
        *
        * It is the one knob for how harsh winter is (#504). After the 0.8.0
        * food fixes (#495, #497, #499) a quarter left winter almost harmless:
        * 2.3 deaths a winter over seeds 41-56, against about 10 before. At 0.4
        * a starving pawn that gets home on its last 40 energy lasts about a
        * day of the two-day winter (a quarter lasted about two), so a nation
        * that goes into winter short starves late in winter unless someone
        * feeds it: 5.4 deaths a winter over seeds 41-56 (2 to 12 a seed). A
        * nation stocked to WINTER_FOOD_STORE never starves, so it is untouched:
        * a player's camp stocked to it loses 1 member over seeds 101-108, as
        * before, and an unstocked one loses 37 of 56, against 23.
        */
        public const float WINTER_SHELTER_MULTIPLIER = 0.4f;

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
                default: return "No harvests, bare trees; the cold bites outdoors.";
            }
        }
    }
}
