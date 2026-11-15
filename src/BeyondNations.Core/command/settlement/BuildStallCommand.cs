
namespace beyondnations {

    public class BuildStallCommand {
        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private EventProducer eventProducer;

        // eventProducer is optional so that existing callers keep compiling;
        // without one the stall is built but the chronicle never hears of it.
        public BuildStallCommand(NationRepository nationRepository, EntityRepository entityRepository, EventProducer eventProducer = null) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
            this.eventProducer = eventProducer;
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

            Settlement currentSettlement = (Settlement) entityRepository.getEntity(player.getCurrentSettlementId());
            NationId settlementNationId = currentSettlement.getNationId();
            if (settlementNationId != player.getNationId()) {
                player.getStatus().update("You are not inside a settlement owned by your nation.");
                return;
            }
            // the leader, or the member it was given to (#176)
            string refusal = SettlementOwnership.refusal(player.getId(), nation, currentSettlement, entityRepository);
            if (refusal != null) {
                player.getStatus().update(refusal);
                return;
            }

            if (currentSettlement.getMarket().getNumStalls() >= currentSettlement.getMarket().getMaxNumStalls()) {
                player.getStatus().update("Market is full. Cannot build stall.");
                return;
            }

            if (player.getInventory().getNumItems(ItemType.WOOD) < Stall.WOOD_COST_TO_BUILD) {
                player.getStatus().update("Not enough wood. Need " + (Stall.WOOD_COST_TO_BUILD - player.getInventory().getNumItems(ItemType.WOOD)) + " more.");
                return;
            }

            if (player.getInventory().getNumItems(ItemType.STONE) < Stall.STONE_COST_TO_BUILD) {
                player.getStatus().update("Not enough stone. Need " + (Stall.STONE_COST_TO_BUILD - player.getInventory().getNumItems(ItemType.STONE)) + " more.");
                return;
            }

            currentSettlement.getMarket().createStall();
            Stall.payBuildCost(player.getInventory());
            player.getStatus().update("Stall built.");
            eventProducer?.produceStallBuiltEvent(currentSettlement, nation, player.getId());
        }
    }
}