using System.Numerics;

namespace beyondnations {

    public class TeleportAllPawnsCommand {
        private RandomSource random;
        private EntityRepository entityRepository;

        public TeleportAllPawnsCommand(EntityRepository entityRepository, RandomSource random) {
            this.random = random;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            foreach (Entity entity in entityRepository.getEntities()) {
                if (entity.getType() == EntityType.PAWN) {
                    Pawn pawn = (Pawn)entity;

                    if (pawn.isCurrentlyInSettlement()) {
                            Settlement currentSettlement = entityRepository.getEntity(pawn.getCurrentSettlementId()) as Settlement;
                            currentSettlement.removeCurrentlyPresentEntity(pawn.getId());
                            pawn.clearCurrentSettlementId();
                            pawn.setPosition(player.getPosition() + new Vector3(random.range(-20, 20), 0, random.range(-20, 20)));
                            pawn.setColor(currentSettlement.getColor());
                    }
                    else {
                        pawn.setPosition(player.getPosition() + new Vector3(random.range(-20, 20), 0, random.range(-20, 20)));
                    }

                    pawn.setTargetEntity(null);
                }
            }
        }
    }
}