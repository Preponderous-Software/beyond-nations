
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
            EntityId currentSettlementId = player.getCurrentSettlementId();
            if (currentSettlementId == null) {
                player.getStatus().update("You are not inside a settlement.");
                return;
            }
            Settlement settlement = (Settlement) entityRepository.getEntity(currentSettlementId);

            // the leader, or the member it was given to (#176)
            string refusal = SettlementOwnership.refusal(player.getId(), nation, settlement, entityRepository);
            if (refusal != null) {
                player.getStatus().update(refusal);
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