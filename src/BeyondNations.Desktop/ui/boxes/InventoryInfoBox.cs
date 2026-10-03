using System.Collections.Generic;

namespace beyondnations.desktop.ui.boxes {

    /**
    * The player's inventory, ported from
    * Assets/Scripts/ui/boxes/InventoryInfoBox.cs. Same six item types, same
    * labels, same order. It used to be toggled with I; that key now opens the
    * full inventory screen (#181), and the key stays in the title as the hint
    * for reaching it.
    */
    public class InventoryInfoBox : InfoBox {
        private readonly Inventory inventory;

        public InventoryInfoBox(Inventory inventory) : base("Inventory Info (I)") {
            this.inventory = inventory;
        }

        public override List<string> getLines() {
            return new List<string> {
                "Coins: " + inventory.getNumItems(ItemType.COIN),
                "Wood: " + inventory.getNumItems(ItemType.WOOD),
                "Stone: " + inventory.getNumItems(ItemType.STONE),
                "Apples: " + inventory.getNumItems(ItemType.APPLE),
                "Saplings: " + inventory.getNumItems(ItemType.SAPLING),
                "Chicken Meat: " + inventory.getNumItems(ItemType.CHICKEN_MEAT)
            };
        }
    }
}
