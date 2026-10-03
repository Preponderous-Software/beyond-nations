using System.Collections.Generic;

namespace beyondnations.desktop.ui.screens {

    /**
    * The inventory screen opened from the world with I (#181).
    *
    * The HUD box in the corner names six item types whether or not the player
    * holds any of them. This screen lists the slots themselves instead, in
    * slot order, so it shows whatever the inventory actually contains --
    * including a slot added for an item type the HUD box does not know about.
    *
    * The lines are produced by getLines(), which needs no window, so the text
    * is asserted in a unit test and only the drawing needs a screen. I and
    * Escape are read by the host, since leaving this screen is a transition.
    */
    public class InventoryScreen {
        public const string Title = "INVENTORY";
        public const string CloseHint = "(Press I or ESCAPE to close)";
        public const string EmptyLine = "Your inventory is empty.";

        public static List<string> getLines(Inventory inventory) {
            List<string> lines = new List<string>();
            foreach (ItemSlot slot in inventory.getSlots()) {
                if (slot.isEmpty()) {
                    continue;
                }
                lines.Add(getItemName(slot.getItemStack().getItemType()) + ": " + slot.getQuantity());
            }
            if (lines.Count == 0) {
                lines.Add(EmptyLine);
            }
            return lines;
        }

        /**
        * The same labels the HUD box uses, so an item reads the same in both.
        */
        public static string getItemName(ItemType itemType) {
            switch (itemType) {
                case ItemType.COIN: return "Coins";
                case ItemType.WOOD: return "Wood";
                case ItemType.STONE: return "Stone";
                case ItemType.APPLE: return "Apples";
                case ItemType.SAPLING: return "Saplings";
                case ItemType.CHICKEN_MEAT: return "Chicken Meat";
                default: return itemType.ToString();
            }
        }

        public UiAction draw(float width, float height, Inventory inventory) {
            float lineHeight = height / 20f;
            float y = height / 4f;

            Overlay.beginFullScreen("##inventory-screen", width, height);
            Overlay.textCentered(Title, height / 10f, y, width);
            y += lineHeight * 2f;
            foreach (string line in getLines(inventory)) {
                Overlay.textCentered(line, height / 30f, y, width);
                y += lineHeight;
            }
            Overlay.textCentered(CloseHint, height / 30f, y + lineHeight, width);
            Overlay.end();

            return UiAction.None;
        }
    }
}
