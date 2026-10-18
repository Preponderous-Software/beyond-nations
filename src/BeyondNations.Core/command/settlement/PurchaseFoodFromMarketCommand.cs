
namespace beyondnations {

    public class PurchaseFoodFromMarketCommand {
        private NationRepository nationRepository;
        private EntityRepository entityRepository;

        public PurchaseFoodFromMarketCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            EntityId currentSettlementId = player.getCurrentSettlementId();
            if (currentSettlementId == null) {
                player.getStatus().update("You are not inside a settlement.");
                return;
            }

            Settlement settlement = (Settlement) entityRepository.getEntity(currentSettlementId);
            Market market = settlement.getMarket();
            if (market == null) {
                player.getStatus().update("This settlement does not have a market.");
                return;
            }
            if (market.getNumStalls() == 0) {
                player.getStatus().update("This settlement does not have any stalls.");
                return;
            }

            EntityId stallOwnerId = market.purchaseFood(player);
            if (stallOwnerId == null) {
                player.getStatus().update("No one is selling food at this market or you do not have enough money to purchase food.");
                return;
            }

            // increase relationship with owner of stall
            Entity stallOwner = entityRepository.getEntity(stallOwnerId);
            if (stallOwner == null) {
                Log.error("stall owner is null");
                return;
            }
            player.increaseRelationship(stallOwner, 1);
            if (stallOwner is Player) {
                Player stallOwnerPlayer = (Player) stallOwner;
                stallOwnerPlayer.increaseRelationship(player, 1);
            }
            else if (stallOwner is Pawn) {
                Pawn stallOwnerPawn = (Pawn) stallOwner;
                stallOwnerPawn.increaseRelationship(player, 1);
            }
            else {
                Log.error("stall owner is not a player or pawn");
                return;
            }
            
            player.getStatus().update("You purchased food from " + stallOwner.getName() + ". Relationship increased to " + player.getRelationships()[stallOwnerId] + ".");
        }
    }
}