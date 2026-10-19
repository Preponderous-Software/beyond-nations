namespace beyondnations {

    /**
    * Giving the nearest pawn something from the player's inventory. What is
    * given is chosen for the pawn: food first when it is hungry (it is worth
    * the most to a pawn that needs it), otherwise coins, then any food, then
    * wood or stone. The relationship rises by how much the gift is worth to
    * that pawn right now.
    */
    public class GiftPawnCommand {
        public const int HUNGRY_FOOD_GIFT_GAIN = 6;
        public const int FOOD_GIFT_GAIN = 2;
        public const int COIN_GIFT_AMOUNT = 10;
        public const int COIN_GIFT_GAIN = 2;
        public const int RESOURCE_GIFT_AMOUNT = 5;
        public const int RESOURCE_GIFT_GAIN = 1;

        private readonly EventProducer eventProducer;

        public GiftPawnCommand(EventProducer eventProducer) {
            this.eventProducer = eventProducer;
        }

        /** Whether the player holds anything this command could give. */
        public static bool canGift(Player player) {
            Inventory inventory = player.getInventory();
            return FoodItems.hasFood(inventory)
                || inventory.getNumItems(ItemType.COIN) >= COIN_GIFT_AMOUNT
                || inventory.getNumItems(ItemType.WOOD) >= RESOURCE_GIFT_AMOUNT
                || inventory.getNumItems(ItemType.STONE) >= RESOURCE_GIFT_AMOUNT;
        }

        public void execute(Player player, Pawn pawn) {
            Inventory inventory = player.getInventory();
            bool hungry = pawn.needsFood();
            ItemType? food = FoodItems.getMostNourishingFood(inventory);

            ItemType itemType;
            int quantity;
            int gain;
            string reaction;
            if (hungry && food.HasValue) {
                itemType = food.Value;
                quantity = 1;
                gain = HUNGRY_FOOD_GIFT_GAIN;
                reaction = "Food! Thank you, I was starving!";
            }
            else if (inventory.getNumItems(ItemType.COIN) >= COIN_GIFT_AMOUNT) {
                itemType = ItemType.COIN;
                quantity = COIN_GIFT_AMOUNT;
                gain = COIN_GIFT_GAIN;
                reaction = "Coins? That's generous of you.";
            }
            else if (food.HasValue) {
                itemType = food.Value;
                quantity = 1;
                gain = FOOD_GIFT_GAIN;
                reaction = "I'll save this for later. Thanks.";
            }
            else if (inventory.getNumItems(ItemType.WOOD) >= RESOURCE_GIFT_AMOUNT) {
                itemType = ItemType.WOOD;
                quantity = RESOURCE_GIFT_AMOUNT;
                gain = RESOURCE_GIFT_GAIN;
                reaction = "Wood is always useful. Thanks.";
            }
            else if (inventory.getNumItems(ItemType.STONE) >= RESOURCE_GIFT_AMOUNT) {
                itemType = ItemType.STONE;
                quantity = RESOURCE_GIFT_AMOUNT;
                gain = RESOURCE_GIFT_GAIN;
                reaction = "Stone? Well, thank you.";
            }
            else {
                player.getStatus().update("You have nothing to give " + pawn.getName() + ". Food, " + COIN_GIFT_AMOUNT + " coins or " + RESOURCE_GIFT_AMOUNT + " wood or stone make a gift.");
                return;
            }

            inventory.removeItem(itemType, quantity);
            pawn.getInventory().addItem(itemType, quantity);
            bool tierUp = PawnRelations.raise(pawn, player, gain, eventProducer);

            player.getStatus().update("You gave " + pawn.getName() + " " + quantity + " " + itemType.ToString().ToLower() + ". "
                + PawnRelations.describe(pawn, player) + ": \"" + reaction + "\"" + PawnRelations.tierUpSuffix(tierUp, pawn, player));
        }
    }
}
