using System.Collections.Generic;

namespace beyondnations {

    /**
    * A fever in a crowded settlement. A healer costs coin and nobody suffers;
    * quarantine weakens the sick; left alone it takes the weakest of them.
    */
    public class SicknessEvent : IWorldEvent {
        public const string Id = "sickness";
        public const int MIN_RESIDENTS = 3;
        public const int MAX_SICK = 3;
        public const int HEALER_COINS = 20;
        public const int HEALER_RELATIONSHIP = 2;
        public const float QUARANTINE_ENERGY = 40f;
        public const float UNTREATED_ENERGY = 70f;

        public string getId() { return Id; }
        public string getTitle() { return "Sickness"; }
        public bool isPersonal() { return false; }

        public int getWeight(WorldEventContext context, Nation nation) {
            Settlement settlement = mostCrowded(context, nation);
            if (settlement == null) {
                return 0;
            }
            return 2 + System.Math.Min(10, context.residentsOf(settlement).Count);
        }

        private static Settlement mostCrowded(WorldEventContext context, Nation nation) {
            Settlement most = null;
            int mostResidents = MIN_RESIDENTS - 1;
            foreach (Settlement settlement in context.settlementsOf(nation)) {
                int residents = context.residentsOf(settlement).Count;
                if (residents > mostResidents) {
                    most = settlement;
                    mostResidents = residents;
                }
            }
            return most;
        }

        public bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance) {
            Settlement settlement = mostCrowded(context, nation);
            if (settlement == null) {
                return false;
            }
            instance.setSettlementId(settlement.getId());
            instance.setMagnitude(System.Math.Min(MAX_SICK, context.residentsOf(settlement).Count));
            return true;
        }

        /** The sick: the residents with the least energy, as many as the instance says. */
        public static List<Pawn> getSick(WorldEventContext context, WorldEventInstance instance) {
            List<Pawn> residents = context.residentsOf(context.getSettlement(instance.getSettlementId()));
            // stable: ties keep repository order
            List<Pawn> sorted = new List<Pawn>(residents);
            for (int i = 1; i < sorted.Count; i++) {
                Pawn pawn = sorted[i];
                int j = i - 1;
                while (j >= 0 && sorted[j].getEnergy() > pawn.getEnergy()) {
                    sorted[j + 1] = sorted[j];
                    j--;
                }
                sorted[j + 1] = pawn;
            }
            return sorted.GetRange(0, System.Math.Min(instance.getMagnitude(), sorted.Count));
        }

        public string describe(WorldEventContext context, WorldEventInstance instance) {
            Settlement settlement = context.getSettlement(instance.getSettlementId());
            string where = settlement == null ? "a settlement" : "the " + settlement.getDisplayName();
            return "A fever is spreading through " + where + ". " + instance.getMagnitude() + " of its people have taken to their beds, and more may follow.";
        }

        public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
            return new List<WorldEventChoice> {
                WorldEventChoice.requiring(context.availableCoins(instance) >= HEALER_COINS, "Pay a healer (" + HEALER_COINS + " coins)",
                    "Nobody suffers, and the townsfolk think better of you.", "Needs " + HEALER_COINS + " coins"),
                new WorldEventChoice("Quarantine the sick", "Free. The sick are badly weakened, but the fever goes no further."),
                new WorldEventChoice("Let it run its course", "Free. The sick are laid low, and the weakest of them may not get up again."),
            };
        }

        public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) {
            return 1;
        }

        public int chooseForNation(WorldEventContext context, WorldEventInstance instance) {
            return context.availableCoins(instance) >= HEALER_COINS * 2 ? 0 : 1;
        }

        public string resolve(WorldEventContext context, WorldEventInstance instance, int choice) {
            Settlement settlement = context.getSettlement(instance.getSettlementId());
            if (settlement == null) {
                return null;
            }
            string where = "the " + settlement.getDisplayName();
            string who = context.chooser(instance);
            List<Pawn> sick = getSick(context, instance);
            if (choice == 0) {
                context.payCoins(instance, HEALER_COINS);
                if (instance.isForPlayer()) {
                    foreach (Pawn resident in context.residentsOf(settlement)) {
                        context.warmToPlayer(resident, HEALER_RELATIONSHIP);
                    }
                }
                return "a fever broke out in " + where + "; " + who + " paid a healer, and every patient recovered.";
            }
            if (choice == 1) {
                foreach (Pawn pawn in sick) {
                    pawn.setEnergy(System.Math.Max(1f, pawn.getEnergy() - QUARANTINE_ENERGY));
                }
                return "a fever broke out in " + where + "; " + who + " shut the sick away until it passed.";
            }
            string lost = null;
            for (int i = 0; i < sick.Count; i++) {
                Pawn pawn = sick[i];
                if (i == 0 && pawn.getEnergy() <= UNTREATED_ENERGY) {
                    // the death itself is the simulation's ordinary one, next tick; below
                    // zero by more than any one meal, so eating first cannot undo it
                    pawn.setEnergy(-100f);
                    lost = pawn.getName();
                } else {
                    pawn.setEnergy(System.Math.Max(1f, pawn.getEnergy() - UNTREATED_ENERGY));
                }
            }
            return lost != null
                ? "a fever ran through " + where + " untreated, and " + lost + " did not survive it."
                : "a fever ran through " + where + " untreated; the sick were laid low, but all of them lived.";
        }
    }
}
