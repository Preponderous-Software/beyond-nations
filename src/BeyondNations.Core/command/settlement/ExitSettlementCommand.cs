namespace beyondnations {

    public class ExitSettlementCommand {
        private EntityRepository entityRepository;

        public ExitSettlementCommand(EntityRepository entityRepository) {
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            player.getStatus().update("Exited settlement.");
            player.setCurrentSettlementId(null);
            player.setVisible(true);
        }
    }
}
