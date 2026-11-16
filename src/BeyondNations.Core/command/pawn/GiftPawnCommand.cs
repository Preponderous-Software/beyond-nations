namespace beyondnations {

    /**
    * Giving the nearest pawn something from the player's inventory. What is
    * given is chosen for the pawn: food first when it is hungry (it is worth
    * the most to a pawn that needs it), otherwise coins, then any food, then
    * wood or stone. The relationship rises by how much the gift is worth to
    * that pawn right now. A pawn that leads another nation will never join
    * the player's (InvitePawnToNationCommand), so a gift to one says so:
    * the gift still warms it, but it buys no member (#371).
    */
    public class GiftPawnCommand {
        public const int HUNGRY_FOOD_GIFT_GAIN = 6;
        public const int FOOD_GIFT_GAIN = 2;
        public const int COIN_GIFT_AMOUNT = 10;
        public const int COIN_GIFT_GAIN = 2;
        public const int RESOURCE_GIFT_AMOUNT = 5;
        public const int RESOURCE_GIFT_GAIN = 1;

        private readonly EventProducer eventProducer;
        private readonly NationRepository nationRepository;

        public GiftPawnCommand(EventProducer eventProducer) : this(eventProducer, null) {
        }

        /** With a nation repository, a gift to another nation's leader carries leaderWarning. */
        public GiftPawnCommand(EventProducer eventProducer, NationRepository nationRepository) {
            this.eventProducer = eventProducer;
            this.nationRepository = nationRepository;
        }

        /**
        * The nation this pawn leads, when it is not the player's own; null
        * otherwise. Such a pawn never accepts an invitation.
        */
        public static Nation getRivalNationLed(Player player, Pawn pawn, NationRepository nationRepository) {
            if (nationRepository == null || pawn.getNationId() == null || pawn.getNationId() == player.getNationId()) {
                return null;
            }
            Nation nation = nationRepository.getNation(pawn.getNationId());
            if (nation == null || !pawn.getId().Equals(nation.getLeaderId())) {
                return null;
            }
            return nation;
        }

        /**
        * " Alice leads Ashford and will never join your nation." for a pawn
        * that leads another nation, or "" -- appended to the gift's status
        * line and used as the Gift button's tooltip, so a player courting a
        * leader for a member learns it before spending more on it.
        */
        public static string leaderWarning(Player player, Pawn pawn, NationRepository nationRepository) {
            Nation nation = getRivalNationLed(player, pawn, nationRepository);
            if (nation == null) {
                return "";
            }
            return " " + pawn.getName() + " leads " + nation.getName() + " and will never join your nation.";
        }

        /** Whether the player holds anything this command could give. */
        public static bool canGift(Player player) {
            Inventory inventory = player.getInventory();
            return FoodItems.hasFood(inventory)
                || inventory.getNumItems(ItemType.COIN) >= COIN_GIFT_AMOUNT
                || inventory.getNumItems(ItemType.WOOD) >= RESOURCE_GIFT_AMOUNT
                || inventory.getNumItems(ItemType.STONE) >= RESOURCE_GIFT_AMOUNT;
        }

        /** What makes a gift, for the greyed Gift button and the refusal. */
        public static string describeWhatMakesAGift() {
            return "Food, " + COIN_GIFT_AMOUNT + " coins or " + RESOURCE_GIFT_AMOUNT + " wood or stone make a gift.";
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
                player.getStatus().update("You have nothing to give " + pawn.getName() + ". " + describeWhatMakesAGift());
                return;
            }

            inventory.removeItem(itemType, quantity);
            pawn.getInventory().addItem(itemType, quantity);
            bool tierUp = PawnRelations.raise(pawn, player, gain, eventProducer);

            player.getStatus().update("You gave " + pawn.getName() + " " + quantity + " " + itemType.ToString().ToLower() + ". "
                + PawnRelations.describe(pawn, player) + ": \"" + reaction + "\"" + PawnRelations.tierUpSuffix(tierUp, pawn, player)
                + leaderWarning(player, pawn, nationRepository));
        }
    }
}
