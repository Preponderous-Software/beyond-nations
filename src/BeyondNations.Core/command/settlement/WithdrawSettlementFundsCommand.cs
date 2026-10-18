
namespace beyondnations {

    public class WithdrawSettlementFundsCommand {
        private NationRepository nationRepository;
        private EntityRepository entityRepository;

        public WithdrawSettlementFundsCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            if (player.getNationId() == null) {
                player.getStatus().update("You are not in a nation.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (nation.getRole(player.getId()) != NationRole.LEADER) {
                player.getStatus().update("You are not a leader.");
                return;
            }

            EntityId currentSettlementId = player.getCurrentSettlementId();
            if (currentSettlementId == null) {
                player.getStatus().update("You are not inside a settlement.");
                return;
            }
            Settlement settlement = (Settlement) entityRepository.getEntity(currentSettlementId);

            // if not owned by nation
            if (settlement.getNationId() != nation.getId()) {
                player.getStatus().update("You do not own this settlement.");
                return;
            }

            // if settlement has funds
            if (settlement.getFunds() == 0) {
                player.getStatus().update("Settlement has no funds.");
                return;
            }

            // transfer half of settlement funds to player
            int funds = settlement.getFunds();
            int fundsToWithdraw = funds / 2;
            if (fundsToWithdraw == 0) {
                fundsToWithdraw = 1;
            }
            settlement.removeFunds(fundsToWithdraw);
            player.getInventory().addItem(ItemType.COIN, fundsToWithdraw);

            player.getStatus().update("Withdrew " + fundsToWithdraw + " coins from settlement.");
        }
    }
}