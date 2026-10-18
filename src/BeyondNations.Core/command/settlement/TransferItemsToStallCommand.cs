
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

            stall.getInventory().transferContentsOfInventory(player.getInventory());
            player.getStatus().update("Transferred contents of inventory to stall.");
        }
    }
}