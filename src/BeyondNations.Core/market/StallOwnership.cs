using System.Collections.Generic;

namespace beyondnations {

    /**
    * Keeps who owns a stall and who is a merchant in step.
    *
    * A stall is kept by a member of the nation whose settlement it stands
    * in, and a merchant keeps a stall in one of its nation's settlements.
    * Before 0.6.0 a serf could buy a stall in a foreign market (one it was
    * only visiting, often the market of the nation it had just been invited
    * away from), and became a merchant whose stall was nowhere its nation
    * lives. Behaviour now copes with a merchant without a stall where it
    * stands (PawnBehaviorCalculator), and a pawn buys a stall only at home
    * among its own nation; this puts right a save written before that: the
    * foreign stall is given up, and a merchant left without a stall in its
    * own nation is a serf again, free to buy one at home.
    */
    public static class StallOwnership {

        /** Puts stall ownership and the merchant role back in step. Returns how many things it changed. */
        public static int reconcile(EntityRepository entityRepository, NationRepository nationRepository) {
            int repairs = 0;
            foreach (Entity entity in entityRepository.getEntities()) {
                Settlement settlement = entity as Settlement;
                if (settlement == null) {
                    continue;
                }
                Nation owner = settlement.getNationId() == null ? null : nationRepository.getNation(settlement.getNationId());
                foreach (Stall stall in settlement.getMarket().getStalls()) {
                    EntityId ownerId = stall.getOwnerId();
                    if (ownerId != null && (owner == null || !owner.isMember(ownerId))) {
                        Log.warning("save: released a stall in " + settlement.getName() + " kept by " + ownerId + ", who is not of its nation");
                        stall.setOwnerId(null);
                        repairs++;
                    }
                }
            }

            foreach (Nation nation in nationRepository.getNations()) {
                foreach (EntityId memberId in nation.getMemberIds()) {
                    if (nation.getRole(memberId) == NationRole.MERCHANT && !ownsStallIn(nation, memberId, entityRepository)) {
                        Log.warning("save: " + memberId + " of " + nation.getName() + " was a merchant without a stall; now a serf");
                        nation.setRole(memberId, NationRole.SERF);
                        repairs++;
                    }
                }
            }
            return repairs;
        }

        private static bool ownsStallIn(Nation nation, EntityId memberId, EntityRepository entityRepository) {
            foreach (EntityId settlementId in nation.getSettlements()) {
                Settlement settlement = entityRepository.getEntity(settlementId) as Settlement;
                if (settlement != null && settlement.getMarket().getStall(memberId) != null) {
                    return true;
                }
            }
            return false;
        }
    }
}
