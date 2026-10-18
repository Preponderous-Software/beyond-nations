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

            // The player is inside, so it is no longer drawn. Where the camera
            // sits while inside is the host's decision, taken from the player's
            // current settlement; that arrives with #219.
            player.setVisible(false);
        }
    }
}
