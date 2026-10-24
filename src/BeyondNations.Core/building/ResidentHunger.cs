using System.Collections.Generic;

namespace beyondnations {

    /**
    * Which of a nation's pawns are going hungry, for the HUD's warning and
    * for Feed Residents (#353). In the 0.5.0 playtest fifty pawns starved
    * through one winter, most "within the walls", and the first sign the
    * player had was the death notice.
    */
    public static class ResidentHunger {

        /** The nation's living pawns that need food (Pawn.needsFood), the starving first. */
        public static List<Pawn> getHungryMembers(EntityRepository entityRepository, NationId nationId) {
            List<Pawn> starving = new List<Pawn>();
            List<Pawn> hungry = new List<Pawn>();
            if (nationId == null) {
                return hungry;
            }
            int entityCount = entityRepository.getEntityCount();
            for (int i = 0; i < entityCount; i++) {
                Pawn pawn = entityRepository.getEntityAt(i) as Pawn;
                if (pawn == null || pawn.isMarkedForDeletion() || !nationId.Equals(pawn.getNationId()) || !pawn.needsFood()) {
                    continue;
                }
                (pawn.isStarving() ? starving : hungry).Add(pawn);
            }
            starving.AddRange(hungry);
            return starving;
        }

        /** The hungry pawns standing in this settlement, the starving first: the ones Feed Residents can reach. */
        public static List<Pawn> getHungryPresent(EntityRepository entityRepository, Settlement settlement) {
            List<Pawn> present = new List<Pawn>();
            foreach (Pawn pawn in getHungryMembers(entityRepository, settlement.getNationId())) {
                if (settlement.getId().Equals(pawn.getCurrentSettlementId())) {
                    present.Add(pawn);
                }
            }
            return present;
        }

        /**
        * "Fortham: 3 members hungry, 1 starving." -- or null when there is
        * nothing to warn of. Starving pawns are always worth a line; merely
        * hungry ones only in winter, when no harvest will feed them.
        */
        public static string describeWarning(EntityRepository entityRepository, Nation nation, bool winter) {
            if (nation == null) {
                return null;
            }
            List<Pawn> hungry = getHungryMembers(entityRepository, nation.getId());
            int starving = hungry.FindAll(p => p.isStarving()).Count;
            if (starving == 0 && (!winter || hungry.Count == 0)) {
                return null;
            }
            return nation.getName() + ": " + hungry.Count + (hungry.Count == 1 ? " member" : " members") + " hungry"
                + (starving > 0 ? ", " + starving + " starving" : "") + ". Bring food home and Feed Residents.";
        }
    }
}
