
namespace beyondnations {

    public class TransferItemsToStallCommand {
        private NationRepository nationRepository;
        private EntityRepository entityRepository;

        public TransferItemsToStallCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            if (player.getNationId() == null) {
                player.getStatus().update("You are not in a nation.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (nation.getRole(player.getId()) != NationRole.MERCHANT) {
                player.getStatus().update("You are not a merchant.");
                return;
            }

            EntityId currentSettlementId = player.getCurrentSettlementId();
            if (currentSettlementId == null) {
                player.getStatus().update("You are not inside a settlement.");
                return;
            }

            Settlement settlement = (Settlement) entityRepository.getEntity(currentSettlementId);

            Stall stall = settlement.getMarket().getStall(player.getId());
            if (stall == null) {
                player.getStatus().update("You do not own a stall in this settlement.");
                return;
            }

            // goods only: the purse stays with the merchant (#418)
            if (stall.stockGoodsFrom(player) == 0) {
                player.getStatus().update("You have no goods to stock: your stall takes goods, not coins.");
                return;
            }
            player.getStatus().update("Moved your goods to your stall. Your coins stay in your purse.");
        }
    }
}