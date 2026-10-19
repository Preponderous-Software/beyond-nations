using System.Collections.Generic;

namespace beyondnations {

    /**
    * Every recipe in the game. A handful, each with a reason to exist:
    *
    *   Axe          4 wood, 2 stone                 anywhere   +3 wood per tree for 20 trees
    *   Pickaxe      3 wood, 3 stone                 anywhere   +2 stone per rock for 15 rocks
    *   Cooked Meat  1 chicken meat, 1 wood          anywhere   45 energy instead of raw meat's 25
    *   Planks x2    3 wood                          Workshop   sell for more than the wood
    *   Bricks x2    3 stone, 1 wood                 Workshop   sell for more than the stone
    *
    * Tools wear out (Tools), so materials stay in demand. Prices are in
    * ItemCostCalculator, food values in FoodItems.
    */
    public static class RecipeBook {
        public static readonly Recipe Axe = new Recipe("Axe", ItemType.AXE, 1,
            stacks(ItemType.WOOD, 4, ItemType.STONE, 2), null,
            "+" + Tools.AXE_BONUS_WOOD + " wood per tree, lasts " + Tools.AXE_DURABILITY + " trees");

        public static readonly Recipe Pickaxe = new Recipe("Pickaxe", ItemType.PICKAXE, 1,
            stacks(ItemType.WOOD, 3, ItemType.STONE, 3), null,
            "+" + Tools.PICKAXE_BONUS_STONE + " stone per rock, lasts " + Tools.PICKAXE_DURABILITY + " rocks");

        public static readonly Recipe CookedMeat = new Recipe("Cooked Meat", ItemType.COOKED_MEAT, 1,
            stacks(ItemType.CHICKEN_MEAT, 1, ItemType.WOOD, 1), null,
            FoodItems.getEnergyRestored(ItemType.COOKED_MEAT) + " energy (raw: " + FoodItems.getEnergyRestored(ItemType.CHICKEN_MEAT) + ")");

        public static readonly Recipe Planks = new Recipe("Planks", ItemType.PLANKS, 2,
            stacks(ItemType.WOOD, 3), BuildingType.WORKSHOP,
            "trade good: sells for more than wood");

        public static readonly Recipe Bricks = new Recipe("Bricks", ItemType.BRICKS, 2,
            stacks(ItemType.STONE, 3, ItemType.WOOD, 1), BuildingType.WORKSHOP,
            "trade good: sells for more than stone");

        private static readonly List<Recipe> all = new List<Recipe> { Axe, Pickaxe, CookedMeat, Planks, Bricks };

        /** In the order the crafting screen lists them. */
        public static IReadOnlyList<Recipe> getAll() {
            return all;
        }

        public static Recipe forOutput(ItemType output) {
            foreach (Recipe recipe in all) {
                if (recipe.getOutput() == output) {
                    return recipe;
                }
            }
            return null;
        }

        private static List<ItemStack> stacks(params object[] typeQuantityPairs) {
            List<ItemStack> list = new List<ItemStack>();
            for (int i = 0; i + 1 < typeQuantityPairs.Length; i += 2) {
                list.Add(new ItemStack((ItemType) typeQuantityPairs[i], (int) typeQuantityPairs[i + 1]));
            }
            return list;
        }
    }
}
