using System.Numerics;

namespace beyondnations {

    public class FoundSettlementCommand {
        private RandomSource random;
        private NationRepository nationRepository;
        private EventProducer eventProducer;
        private EntityRepository entityRepository;
        private GameConfig gameConfig;

        public FoundSettlementCommand(NationRepository nationRepository, EventProducer eventProducer, EntityRepository entityRepository, GameConfig gameConfig, RandomSource random) {
            this.random = random;
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
            this.entityRepository = entityRepository;
            this.gameConfig = gameConfig;
        }

        public void execute(Player player) {
            // if not in a nation
            if (player.getNationId() == null) {
                player.getStatus().update("You are not in a nation.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            // if not the leader of a nation
            if (nation.getLeaderId() != player.getId()) {
                player.getStatus().update("You are not the leader of a nation.");
                return;
            }
            // if not enough wood
            if (player.getInventory().getNumItems(ItemType.WOOD) < Settlement.WOOD_COST_TO_BUILD) {
                player.getStatus().update("Not enough wood. Need " + (Settlement.WOOD_COST_TO_BUILD - player.getInventory().getNumItems(ItemType.WOOD)) + " more.");
                return;
            }

            // if another settlement within 200 units
            foreach (Entity entity in entityRepository.getEntities()) {
                if (entity.getType() == EntityType.SETTLEMENT) {
                    Settlement settlementToCheck = (Settlement)entity;
                    int distance = (int)Vector3.Distance(player.getPosition(), settlementToCheck.getPosition());
                    if (distance < gameConfig.getMinDistanceBetweenSettlements()) {
                        player.getStatus().update("Too close to another settlement.");
                        return;
                    }
                }
            }

            // remove wood
            player.getInventory().removeItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);

            // create settlement
            Settlement settlement = new Settlement(player.getPosition(), player.getNationId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nationRepository.getNation(player.getNationId()).addSettlement(settlement.getId());
            player.getStatus().update("Settlement founded.");
            player.setHomeSettlementId(settlement.getId());
        }
    }
}