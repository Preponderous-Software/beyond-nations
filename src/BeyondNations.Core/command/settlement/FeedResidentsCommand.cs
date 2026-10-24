using System.Collections.Generic;

namespace beyondnations {

    /**
    * The player shares their food with the hungry pawns of their own nation
    * standing in the same settlement, the starving first, the most
    * nourishing first, until the hungry are fed or the food runs out (#353:
    * in winter there was no way to feed residents at all).
    *
    * It shares in rounds, one item to each pawn still short of
    * Pawn.HUNGRY_ENERGY_THRESHOLD each round, so a press leaves no one it
    * could feed still hungry. One item each used to leave most of them so
    * (an apple is 15 energy, and hungry is under 80): "You fed 7", and four
    * were hungry again on the next frame (playtest 0.6.3 s02). Scarce food
    * still goes round before anyone gets a second helping.
    */
    public class FeedResidentsCommand {
        private readonly EntityRepository entityRepository;

        public FeedResidentsCommand(EntityRepository entityRepository) {
            this.entityRepository = entityRepository;
        }

        /** The settlement of the player's own nation they stand in, or null. */
        public Settlement getOwnSettlement(Player player) {
            if (player.getNationId() == null || player.getCurrentSettlementId() == null) {
                return null;
            }
            Settlement settlement = entityRepository.getEntity(player.getCurrentSettlementId()) as Settlement;
            return settlement != null && player.getNationId().Equals(settlement.getNationId()) ? settlement : null;
        }

        /** How many hungry residents the player could feed here right now (0 outside their own settlement). */
        public int countHungryHere(Player player) {
            Settlement settlement = getOwnSettlement(player);
            return settlement == null ? 0 : ResidentHunger.getHungryPresent(entityRepository, settlement).Count;
        }

        /** Returns how many pawns were fed. */
        public int execute(Player player) {
            Settlement settlement = getOwnSettlement(player);
            if (settlement == null) {
                player.getStatus().update("Feed your own nation's residents from inside one of its settlements.");
                return 0;
            }
            List<Pawn> hungry = ResidentHunger.getHungryPresent(entityRepository, settlement);
            if (hungry.Count == 0) {
                player.getStatus().update("No one here is hungry.");
                return 0;
            }
            Inventory inventory = player.getInventory();
            Dictionary<Pawn, float> expected = new Dictionary<Pawn, float>();
            foreach (Pawn pawn in hungry) {
                expected[pawn] = pawn.getEnergy();
            }
            int fed = 0;
            bool gave = true;
            while (gave) {
                gave = false;
                foreach (Pawn pawn in hungry) {
                    if (expected[pawn] >= Pawn.HUNGRY_ENERGY_THRESHOLD) {
                        continue;
                    }
                    ItemType? food = FoodItems.getMostNourishingFood(inventory);
                    if (!food.HasValue) {
                        break;
                    }
                    inventory.removeItem(food.Value, 1);
                    // into the pawn's pack: it eats on its next energy steps, as with a gift
                    pawn.getInventory().addItem(food.Value, 1);
                    if (expected[pawn] == pawn.getEnergy()) {
                        fed++;
                    }
                    expected[pawn] += FoodItems.getEnergyRestored(food.Value);
                    gave = true;
                }
            }
            if (fed == 0) {
                player.getStatus().update(hungry.Count + (hungry.Count == 1 ? " resident here is" : " residents here are") + " hungry, and you have no food to share.");
                return 0;
            }
            int left = 0;
            foreach (Pawn pawn in hungry) {
                if (expected[pawn] < Pawn.HUNGRY_ENERGY_THRESHOLD) {
                    left++;
                }
            }
            player.getStatus().update("You fed " + fed + (fed == 1 ? " resident" : " residents") + " in the " + settlement.getDisplayName() + "."
                + (left > 0 ? " " + left + " still hungry: bring more food." : ""));
            return fed;
        }
    }
}
