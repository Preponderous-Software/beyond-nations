using System.Collections.Generic;

namespace beyondnations.desktop.ui.boxes {

    /**
    * The player's inventory, ported from
    * Assets/Scripts/ui/boxes/InventoryInfoBox.cs. Same six item types, same
    * labels, same order. It used to be toggled with I; that key now opens the
    * full inventory screen (#181), and the key stays in the title as the hint
    * for reaching it. Crafted items (#47) join the list once the player
    * holds any, so the box does not grow by five zero rows for a new player.
    */
    public class InventoryInfoBox : InfoBox {
        private readonly Inventory inventory;

        public InventoryInfoBox(Inventory inventory) : base("Inventory Info (I)") {
            this.inventory = inventory;
        }

        /** Always listed, held or not. Crafted items are listed only once held. */
        private static readonly ItemType[] AlwaysShown = {
            ItemType.COIN, ItemType.WOOD, ItemType.STONE, ItemType.APPLE, ItemType.SAPLING, ItemType.CHICKEN_MEAT
        };

        private static readonly ItemType[] ShownWhenHeld = {
            ItemType.COOKED_MEAT, ItemType.PLANKS, ItemType.BRICKS, ItemType.AXE, ItemType.PICKAXE
        };

        public override List<string> getLines() {
            List<string> lines = new List<string>();
            foreach (ItemType itemType in AlwaysShown) {
                lines.Add(ItemNames.getDisplayName(itemType) + ": " + inventory.getNumItems(itemType));
            }
            foreach (ItemType itemType in ShownWhenHeld) {
                int count = inventory.getNumItems(itemType);
                if (count <= 0) {
                    continue;
                }
                string line = ItemNames.getDisplayName(itemType) + ": " + count;
                if (Tools.isTool(itemType)) {
                    line += " (" + Tools.getUsesLeft(inventory, itemType) + " uses)";
                }
                lines.Add(line);
            }
            return lines;
        }
    }
}
