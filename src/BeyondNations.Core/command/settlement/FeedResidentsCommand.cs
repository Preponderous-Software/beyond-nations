using System.Collections.Generic;

namespace beyondnations {

    /**
    * The player shares their food with the hungry pawns of their own nation
    * standing in the same settlement, the starving first, one item each,
    * the most nourishing first, until the hungry are fed or the food runs
    * out (#353: in winter there was no way to feed residents at all).
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
            int fed = 0;
            foreach (Pawn pawn in hungry) {
                ItemType? food = FoodItems.getMostNourishingFood(inventory);
                if (!food.HasValue) {
                    break;
                }
                inventory.removeItem(food.Value, 1);
                // into the pawn's pack: it eats on its next energy step, as with a gift
                pawn.getInventory().addItem(food.Value, 1);
                fed++;
            }
            if (fed == 0) {
                player.getStatus().update(hungry.Count + (hungry.Count == 1 ? " resident here is" : " residents here are") + " hungry, and you have no food to share.");
                return 0;
            }
            int left = hungry.Count - fed;
            player.getStatus().update("You fed " + fed + (fed == 1 ? " resident" : " residents") + " in the " + settlement.getDisplayName() + "."
                + (left > 0 ? " " + left + " still hungry: bring more food." : ""));
            return fed;
        }
    }
}
