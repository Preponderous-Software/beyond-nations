
namespace beyondnations {

    public class PurchaseStallCommand {
        private NationRepository nationRepository;
        private EntityRepository entityRepository;

        public PurchaseStallCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            if (player.getNationId() == null) {
                player.getStatus().update("You are not in a nation.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());

            EntityId currentSettlementId = player.getCurrentSettlementId();
            if (currentSettlementId == null) {
                player.getStatus().update("You are not inside a settlement.");
                return;
            }

            Settlement settlement = (Settlement) entityRepository.getEntity(currentSettlementId);
            if (settlement.getNationId() != player.getNationId()) {
                player.getStatus().update("You can only purchase a stall in one of your own nation's settlements.");
                return;
            }
            Market market = settlement.getMarket();
            Stall stall = market.getStall(player.getId());
            if (stall != null) {
                player.getStatus().update("You can only own one stall per settlement.");
                return;
            }
            
            if (player.getInventory().getNumItems(ItemType.COIN) < Stall.COIN_COST_TO_PURCHASE) {
                player.getStatus().update("Not enough coin. Need " + (Stall.COIN_COST_TO_PURCHASE - player.getInventory().getNumItems(ItemType.COIN)) + " more.");
                return;
            }

            Stall stallForSale = market.getStallForSale();
            if (stallForSale == null) {
                player.getStatus().update("No stall for sale.");
                return;
            }

            stallForSale.setOwnerId(player.getId());
            player.getInventory().removeItem(ItemType.COIN, Stall.COIN_COST_TO_PURCHASE);
            nation.setRole(player.getId(), NationRole.MERCHANT);
            player.getStatus().update("Stall purchased. You are now a merchant.");
        }
    }
}