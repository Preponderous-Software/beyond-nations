namespace beyondnations {

    /**
    * The kind of land a chunk is (RegionMap). Each region is rich in some
    * things and short of others (RegionCatalog), which is what makes one
    * nation's land different from the next.
    */
    public enum Region {
        /** Open grassland: good farming and game, a little of everything else. */
        PLAINS,
        /** Dense woods: plenty of wood and apples, little stone. */
        FOREST,
        /** Rocky uplands: rich in stone, poor in wood and food. */
        HIGHLANDS,
        /** Bare, boggy moor: scarce in nearly everything. */
        MOOR,
    }

    /** The things a region can be rich or poor in, as nations trade and treat them. */
    public enum Commodity {
        WOOD,
        STONE,
        /** Anything edible (FoodItems): apples, chicken meat, cooked meat. */
        FOOD,
    }

    /** Whether a nation's land gives it more or less of a commodity than most. */
    public enum ResourceStanding {
        SHORTAGE,
        NORMAL,
        SURPLUS,
    }
}
