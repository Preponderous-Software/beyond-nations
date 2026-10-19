namespace beyondnations {

    /**
    * The player buys one of a particular item -- a tool a merchant made, say
    * -- from whichever stall in this settlement's market has one. Food has
    * its own command, which picks the best value food for the player.
    */
    public class BuyItemFromMarketCommand {
        private readonly EntityRepository entityRepository;

        public BuyItemFromMarketCommand(EntityRepository entityRepository) {
            this.entityRepository = entityRepository;
        }

        public bool execute(Player player, ItemType itemType) {
            Settlement settlement = CraftItemCommand.getCurrentSettlement(player, entityRepository);
            if (settlement == null) {
                player.getStatus().update("You are not inside a settlement.");
                return false;
            }
            Market market = settlement.getMarket();
            int price = market.getPriceToBuy(player, itemType);
            if (price < 0) {
                player.getStatus().update("No one here is selling " + ItemNames.getSingularName(itemType) + ".");
                return false;
            }
            EntityId stallOwnerId = market.buyItem(player, itemType, 1);
            if (stallOwnerId == null) {
                player.getStatus().update("You need " + price + " coins to buy " + ItemNames.describeQuantity(itemType, 1) + ".");
                return false;
            }
            Entity stallOwner = entityRepository.getEntity(stallOwnerId);
            if (stallOwner != null) {
                player.increaseRelationship(stallOwner, 1);
                if (stallOwner is Pawn) {
                    ((Pawn) stallOwner).increaseRelationship(player, 1);
                }
            }
            player.getStatus().update("Bought " + ItemNames.describeQuantity(itemType, 1) + " for " + price + " coins"
                + (stallOwner != null ? " from " + stallOwner.getName() : "") + ".");
            return true;
        }
    }
}
