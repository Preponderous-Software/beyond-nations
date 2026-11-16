namespace beyondnations {

    /**
    * A pawn's winter store is not for sale (playtest winter2 s03, after
    * #497): in autumn and winter a pawn keeps up to
    * SeasonalRules.WINTER_FOOD_STORE food in its pack whatever it does at
    * market -- selling a surplus, a trader selling its load, or a merchant
    * stocking its stall. The one member of a camp became the merchant of its
    * new stall the night before winter, put her 15 food on the stall with
    * her wood, and the Season box went from "15 of 14" to "1 of 14" while
    * the camp's till rose from 12 to 107 coins.
    *
    * The store is set aside from the pack before the deal and put back after
    * it, so every way of selling or stocking keeps it, the ones to come too.
    */
    public static class WinterStoreKeeping {

        /** The food a pawn holds back from market this season: WINTER_FOOD_STORE in autumn and winter, none otherwise. */
        public static int foodKept(Season season) {
            return season == Season.AUTUMN || season == Season.WINTER ? SeasonalRules.WINTER_FOOD_STORE : 0;
        }

        /**
        * Takes up to foodKept(season) food out of the pawn's pack, the most
        * nourishing first, and returns it to hand back with restore; null
        * when there is nothing to keep.
        */
        public static Inventory setAside(Pawn pawn, Season season) {
            int keep = foodKept(season);
            if (keep <= 0) {
                return null;
            }
            Inventory aside = new Inventory(0);
            Inventory pack = pawn.getInventory();
            foreach (ItemType food in FoodItems.getFoodInDescendingEnergyOrder()) {
                int take = System.Math.Min(keep, pack.getNumItems(food));
                if (take > 0) {
                    pack.removeItem(food, take);
                    aside.addItem(food, take);
                    keep -= take;
                }
            }
            return aside;
        }

        /** Puts back what setAside took. */
        public static void restore(Pawn pawn, Inventory aside) {
            if (aside == null) {
                return;
            }
            foreach (ItemType food in FoodItems.getFoodInDescendingEnergyOrder()) {
                int count = aside.getNumItems(food);
                if (count > 0) {
                    pawn.getInventory().addItem(food, count);
                }
            }
        }
    }
}
