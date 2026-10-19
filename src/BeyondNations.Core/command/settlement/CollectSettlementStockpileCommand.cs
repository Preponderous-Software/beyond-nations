namespace beyondnations {

    /**
    * The leader takes the wood and stone that member pawns delivered under
    * orders out of the settlement's stockpile. Coins are left alone: those
    * are the settlement's funds, withdrawn with WithdrawSettlementFundsCommand.
    */
    public class CollectSettlementStockpileCommand {
        private static readonly ItemType[] StockpileItems = { ItemType.WOOD, ItemType.STONE };

        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;

        public CollectSettlementStockpileCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public static int getStockpileSize(Settlement settlement) {
            int total = 0;
            foreach (ItemType itemType in StockpileItems) {
                total += settlement.getInventory().getNumItems(itemType);
            }
            return total;
        }

        public void execute(Player player) {
            if (player.getNationId() == null) {
                player.getStatus().update("You are not in a nation.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (nation.getLeaderId() != player.getId()) {
                player.getStatus().update("You are not a leader.");
                return;
            }
            if (player.getCurrentSettlementId() == null) {
                player.getStatus().update("You are not inside a settlement.");
                return;
            }
            Settlement settlement = (Settlement) entityRepository.getEntity(player.getCurrentSettlementId());
            if (settlement.getNationId() != nation.getId()) {
                player.getStatus().update("You do not own this settlement.");
                return;
            }
            if (getStockpileSize(settlement) == 0) {
                player.getStatus().update("The stockpile is empty. Order members to gather for the nation to fill it.");
                return;
            }

            int wood = settlement.getInventory().getNumItems(ItemType.WOOD);
            int stone = settlement.getInventory().getNumItems(ItemType.STONE);
            foreach (ItemType itemType in StockpileItems) {
                int quantity = settlement.getInventory().getNumItems(itemType);
                if (quantity > 0) {
                    settlement.getInventory().removeItem(itemType, quantity);
                    player.getInventory().addItem(itemType, quantity);
                }
            }
            player.getStatus().update("Collected " + wood + " wood and " + stone + " stone from the stockpile.");
        }
    }
}
