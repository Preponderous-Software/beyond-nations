namespace beyondnations {

    /**
    * What each item is called on screen. The inventory screen, the HUD box,
    * the crafting screen and status messages all read this, so an item reads
    * the same everywhere.
    */
    public static class ItemNames {

        /** The list-style label: "Coins", "Wood", "Axes". */
        public static string getDisplayName(ItemType itemType) {
            switch (itemType) {
                case ItemType.COIN: return "Coins";
                case ItemType.WOOD: return "Wood";
                case ItemType.STONE: return "Stone";
                case ItemType.APPLE: return "Apples";
                case ItemType.SAPLING: return "Saplings";
                case ItemType.CHICKEN_MEAT: return "Chicken Meat";
                case ItemType.AXE: return "Axes";
                case ItemType.PICKAXE: return "Pickaxes";
                case ItemType.COOKED_MEAT: return "Cooked Meat";
                case ItemType.PLANKS: return "Planks";
                case ItemType.BRICKS: return "Bricks";
                default: return itemType.ToString();
            }
        }

        /** The name of one of these, lower case, for sentences: "axe", "plank". */
        public static string getSingularName(ItemType itemType) {
            switch (itemType) {
                case ItemType.COIN: return "coin";
                case ItemType.WOOD: return "wood";
                case ItemType.STONE: return "stone";
                case ItemType.APPLE: return "apple";
                case ItemType.SAPLING: return "sapling";
                case ItemType.CHICKEN_MEAT: return "chicken meat";
                case ItemType.AXE: return "axe";
                case ItemType.PICKAXE: return "pickaxe";
                case ItemType.COOKED_MEAT: return "cooked meat";
                case ItemType.PLANKS: return "plank";
                case ItemType.BRICKS: return "brick";
                default: return itemType.ToString().ToLowerInvariant();
            }
        }

        /** "3 wood", "1 axe", "2 planks": wood, stone and meat take no s. */
        public static string describeQuantity(ItemType itemType, int quantity) {
            string name = getSingularName(itemType);
            if (quantity == 1 || isUncountable(itemType)) {
                return quantity + " " + name;
            }
            return quantity + " " + name + "s";
        }

        /** Wood, stone and meat are counted without a plural s. */
        public static bool isUncountable(ItemType itemType) {
            return itemType == ItemType.WOOD || itemType == ItemType.STONE
                || itemType == ItemType.CHICKEN_MEAT || itemType == ItemType.COOKED_MEAT;
        }
    }
}
