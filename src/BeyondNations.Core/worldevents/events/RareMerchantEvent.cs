using System.Collections.Generic;

namespace beyondnations {

    /**
    * A travelling merchant with a rare deal: a pair of tools for far below
    * their market price, or a straight trade of wood for stone. Personal:
    * it happens to the player, nation or not.
    */
    public class RareMerchantEvent : IWorldEvent {
        public const string Id = "merchant";
        public const int TOOLS_COINS = 50;
        public const int TRADE_WOOD = 20;
        public const int TRADE_STONE = 15;

        public string getId() { return Id; }
        public string getTitle() { return "A travelling merchant"; }
        public bool isPersonal() { return true; }

        public int getWeight(WorldEventContext context, Nation nation) {
            Inventory inventory = context.getPlayer().getInventory();
            if (inventory.getNumItems(ItemType.COIN) < TOOLS_COINS && inventory.getNumItems(ItemType.WOOD) < TRADE_WOOD) {
                return 0;
            }
            return 4;
        }

        public bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance) {
            return true;
        }

        public string describe(WorldEventContext context, WorldEventInstance instance) {
            return "A merchant with a mule train hails you on the road. They are heading for the coast and want to travel light: "
                + "an axe and a pickaxe for " + TOOLS_COINS + " coins, worth hundreds at any market, or stone for your wood.";
        }

        public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
            Inventory inventory = context.getPlayer().getInventory();
            return new List<WorldEventChoice> {
                WorldEventChoice.requiring(inventory.getNumItems(ItemType.COIN) >= TOOLS_COINS, "Buy an axe and a pickaxe (" + TOOLS_COINS + " coins)",
                    "Two tools at a fraction of their price.", "Needs " + TOOLS_COINS + " coins"),
                WorldEventChoice.requiring(inventory.getNumItems(ItemType.WOOD) >= TRADE_WOOD, "Trade " + TRADE_WOOD + " wood for " + TRADE_STONE + " stone",
                    "Stone without a trip to the quarry.", "Needs " + TRADE_WOOD + " wood"),
                new WorldEventChoice("Decline", "Keep what you have."),
            };
        }

        public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) {
            return 2;
        }

        public int chooseForNation(WorldEventContext context, WorldEventInstance instance) {
            return 2;
        }

        public string resolve(WorldEventContext context, WorldEventInstance instance, int choice) {
            Inventory inventory = context.getPlayer().getInventory();
            if (choice == 0) {
                inventory.removeItem(ItemType.COIN, TOOLS_COINS);
                inventory.addItem(ItemType.AXE, 1);
                inventory.addItem(ItemType.PICKAXE, 1);
                return "you bought an axe and a pickaxe from a travelling merchant for " + TOOLS_COINS + " coins.";
            }
            if (choice == 1) {
                inventory.removeItem(ItemType.WOOD, TRADE_WOOD);
                inventory.addItem(ItemType.STONE, TRADE_STONE);
                return "you traded " + TRADE_WOOD + " wood for " + TRADE_STONE + " stone with a travelling merchant.";
            }
            return "a travelling merchant offered you a rare deal, and you let it pass.";
        }
    }
}
