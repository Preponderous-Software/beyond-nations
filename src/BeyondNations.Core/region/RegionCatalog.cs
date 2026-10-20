namespace beyondnations {

    /**
    * What each region holds, in one table, read by world generation, rock
    * regrowth, chicken hatching, farms and NationResources alike, so "highlands
    * are rich in stone" is decided here and nowhere else.
    *
    * Every factor is relative to the old, region-less world (1.0): a factor of
    * 2 means twice as many rocks (or trees, or chickens) as a chunk used to
    * get. No region has none of anything -- WorldGenerator places at least
    * MIN_ROCK_ATTEMPTS_PER_CHUNK rocks everywhere, and regrowth surfaces rocks
    * in every region -- so a player who starts outside the highlands is short
    * of stone, never stuck for it.
    */
    public static class RegionCatalog {

        /** Rock placement attempts every chunk gets, whatever its region. */
        public const int MIN_ROCK_ATTEMPTS_PER_CHUNK = 2;

        public static string name(Region region) {
            switch (region) {
                case Region.FOREST: return "Forest";
                case Region.HIGHLANDS: return "Highlands";
                case Region.MOOR: return "Moor";
                default: return "Plains";
            }
        }

        /** A few words on what the region is good for, for the HUD. */
        public static string describe(Region region) {
            switch (region) {
                case Region.FOREST: return "plenty of wood, little stone";
                case Region.HIGHLANDS: return "rich in stone, poor in food";
                case Region.MOOR: return "scarce in everything";
                default: return "good farming and game";
            }
        }

        /** How much of a commodity the region yields, relative to the old world (1.0). */
        public static float abundance(Region region, Commodity commodity) {
            switch (commodity) {
                case Commodity.WOOD: return treeFactor(region);
                case Commodity.STONE: return rockFactor(region);
                default: return foodFactor(region);
            }
        }

        /** Trees a new chunk gets. */
        public static float treeFactor(Region region) {
            switch (region) {
                case Region.FOREST: return 1.8f;
                case Region.HIGHLANDS: return 0.6f;
                case Region.MOOR: return 0.5f;
                default: return 0.9f;
            }
        }

        /** Rocks a new chunk gets, and how many rocks regrowth surfaces there at once. */
        public static float rockFactor(Region region) {
            switch (region) {
                case Region.FOREST: return 0.6f;
                case Region.HIGHLANDS: return 2.2f;
                case Region.MOOR: return 0.9f;
                default: return 0.8f;
            }
        }

        /** Chickens: the chance a new chunk has any, and the chance one hatches there. */
        public static float chickenFactor(Region region) {
            switch (region) {
                case Region.FOREST: return 0.8f;
                case Region.HIGHLANDS: return 0.5f;
                case Region.MOOR: return 0.4f;
                default: return 2.0f;
            }
        }

        /**
        * Food overall, as NationResources reads it: trees carry apples and
        * chickens are meat, and plains farms harvest more (farmBonus).
        */
        public static float foodFactor(Region region) {
            switch (region) {
                case Region.FOREST: return 1.2f;
                case Region.HIGHLANDS: return 0.6f;
                case Region.MOOR: return 0.5f;
                default: return 1.6f;
            }
        }

        /** Extra apples a farm harvests per round in this region (plains farm well). */
        public static int farmBonus(Region region) {
            return region == Region.PLAINS ? 1 : 0;
        }

        /**
        * The colour the region's ground is tinted toward, before the season
        * has its say (WorldPalette).
        */
        public static Rgba groundColour(Region region) {
            switch (region) {
                case Region.FOREST: return new Rgba(0.10f, 0.42f, 0.14f);
                case Region.HIGHLANDS: return new Rgba(0.48f, 0.50f, 0.44f);
                case Region.MOOR: return new Rgba(0.46f, 0.40f, 0.26f);
                default: return new Rgba(0.40f, 0.68f, 0.22f);
            }
        }
    }
}
