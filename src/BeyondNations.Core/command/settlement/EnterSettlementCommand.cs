namespace beyondnations {

    public class EnterSettlementCommand {
        private EntityRepository entityRepository;

        public EnterSettlementCommand(EntityRepository entityRepository) {
            this.entityRepository = entityRepository;
        }

        public void execute(Player player, Settlement settlement) {
            // enter settlement
            EntityId settlementId = settlement.getId();
            player.setCurrentSettlementId(settlementId);
            player.getStatus().update("Entered settlement.");

            // The player stays drawn while inside, standing in the town among
            // the pawns milling about it, rather than vanishing (which made a
            // settlement look deserted from the trailing camera).
            player.setVisible(true);
        }
    }
}
