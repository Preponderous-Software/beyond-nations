namespace beyondnations {

    /**
    * What a tool does and how long it lasts.
    *
    *   Axe      +AXE_BONUS_WOOD wood from every tree,    worn out after AXE_DURABILITY trees
    *   Pickaxe  +PICKAXE_BONUS_STONE stone from every rock, worn out after PICKAXE_DURABILITY rocks
    *
    * A tool is an ordinary item in the inventory; the uses already spent on
    * the one in hand are kept by the inventory (Inventory.getToolWear). When
    * it wears out, one is removed and the next, if any, starts fresh -- so
    * wood and stone stay in demand for replacements.
    *
    * Both the player's interact command and pawn gathering go through
    * applyGatherBonus, so a pawn that somehow holds an axe benefits the same.
    */
    public static class Tools {
        public const int AXE_BONUS_WOOD = 3;
        public const int AXE_DURABILITY = 20;
        public const int PICKAXE_BONUS_STONE = 2;
        public const int PICKAXE_DURABILITY = 15;

        public static bool isTool(ItemType itemType) {
            return itemType == ItemType.AXE || itemType == ItemType.PICKAXE;
        }

        /** Gathers a fresh tool of this type lasts; 0 for anything that is not a tool. */
        public static int getDurability(ItemType toolType) {
            switch (toolType) {
                case ItemType.AXE: return AXE_DURABILITY;
                case ItemType.PICKAXE: return PICKAXE_DURABILITY;
            }
            return 0;
        }

        /** Gathers left in the tool in hand, or 0 when none is carried. */
        public static int getUsesLeft(Inventory inventory, ItemType toolType) {
            if (inventory.getNumItems(toolType) <= 0) {
                return 0;
            }
            return getDurability(toolType) - inventory.getToolWear(toolType);
        }

        /** The tool that helps with this kind of node, or null. */
        public static ItemType? getToolFor(EntityType nodeType) {
            switch (nodeType) {
                case EntityType.TREE: return ItemType.AXE;
                case EntityType.ROCK: return ItemType.PICKAXE;
            }
            return null;
        }

        /**
        * Adds the tool's extra yield to the gatherer and spends one use of the
        * tool, if the gatherer carries the right one for this node. Call once
        * per node gathered.
        */
        public static GatherBonus applyGatherBonus(Inventory gatherer, EntityType nodeType) {
            ItemType? tool = getToolFor(nodeType);
            if (tool == null || gatherer.getNumItems(tool.Value) <= 0) {
                return GatherBonus.None;
            }
            ItemType toolType = tool.Value;
            ItemType resource = toolType == ItemType.AXE ? ItemType.WOOD : ItemType.STONE;
            int bonus = toolType == ItemType.AXE ? AXE_BONUS_WOOD : PICKAXE_BONUS_STONE;
            gatherer.addItem(resource, bonus);

            int wear = gatherer.getToolWear(toolType) + 1;
            bool broke = wear >= getDurability(toolType);
            if (broke) {
                // removing the last one forgets the wear by itself; with more
                // left, the next one starts fresh
                gatherer.removeItem(toolType, 1);
                gatherer.setToolWear(toolType, 0);
            } else {
                gatherer.setToolWear(toolType, wear);
            }
            return new GatherBonus(toolType, resource, bonus, broke);
        }
    }

    /** What a tool added to one gather. */
    public class GatherBonus {
        public static readonly GatherBonus None = new GatherBonus(null, ItemType.COIN, 0, false);

        private readonly ItemType? tool;
        private readonly ItemType resource;
        private readonly int amount;
        private readonly bool broke;

        public GatherBonus(ItemType? tool, ItemType resource, int amount, bool broke) {
            this.tool = tool;
            this.resource = resource;
            this.amount = amount;
            this.broke = broke;
        }

        public ItemType? getTool() {
            return tool;
        }

        public ItemType getResource() {
            return resource;
        }

        /** Extra items the tool produced; 0 when no tool helped. */
        public int getAmount() {
            return amount;
        }

        /** Whether the tool wore out on this gather. */
        public bool didBreak() {
            return broke;
        }
    }
}
