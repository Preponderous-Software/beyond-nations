namespace beyondnations {

    /**
    * What a merchant pawn makes from its haul before stocking its stall, so
    * markets offer more than raw wood and stone. Deterministic -- it reads
    * only the inventories and the settlement -- so it draws nothing from
    * the random stream.
    *
    *  - one axe and one pickaxe, but only while the stall has none of that
    *    tool, so a market keeps one of each on offer for the player to buy;
    *  - cooked meat from up to MAX_BATCHES chicken meat;
    *  - with a workshop in the settlement, up to MAX_BATCHES batches each of
    *    planks and bricks.
    *
    * Everything goes through Crafting with the same recipes the player uses.
    */
    public static class MerchantCrafting {
        public const int MAX_BATCHES = 2;

        /** Returns how many crafts were made. */
        public static int craftGoods(Inventory merchant, Settlement settlement, Inventory stall) {
            int crafted = 0;
            if (stall.getNumItems(ItemType.AXE) == 0 && Crafting.craft(RecipeBook.Axe, merchant, settlement)) {
                crafted++;
            }
            if (stall.getNumItems(ItemType.PICKAXE) == 0 && Crafting.craft(RecipeBook.Pickaxe, merchant, settlement)) {
                crafted++;
            }
            crafted += craftUpTo(RecipeBook.CookedMeat, merchant, settlement, MAX_BATCHES);
            crafted += craftUpTo(RecipeBook.Planks, merchant, settlement, MAX_BATCHES);
            crafted += craftUpTo(RecipeBook.Bricks, merchant, settlement, MAX_BATCHES);
            return crafted;
        }

        private static int craftUpTo(Recipe recipe, Inventory merchant, Settlement settlement, int batches) {
            int made = 0;
            while (made < batches && Crafting.craft(recipe, merchant, settlement)) {
                made++;
            }
            return made;
        }
    }
}
