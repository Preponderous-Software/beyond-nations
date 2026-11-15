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

        /**
        * Whether Feed Residents also lays in winter stores this season: in
        * autumn and winter, when the pawns themselves want WINTER_FOOD_STORE
        * food in their packs (SeasonalRules.wantsStores).
        */
        public static bool stocksForWinter(Season season) {
            return season == Season.AUTUMN || season == Season.WINTER;
        }

        /**
        * The nation's members standing here who are not hungry but carry less
        * than SeasonalRules.WINTER_FOOD_STORE food, in autumn and winter (none
        * otherwise): the ones a press stocks for winter once the hungry are fed.
        */
        public List<Pawn> getLowOnStoresHere(Player player, Season season) {
            List<Pawn> low = new List<Pawn>();
            Settlement settlement = getOwnSettlement(player);
            if (settlement == null || !stocksForWinter(season)) {
                return low;
            }
            int entityCount = entityRepository.getEntityCount();
            for (int i = 0; i < entityCount; i++) {
                Pawn pawn = entityRepository.getEntityAt(i) as Pawn;
                if (pawn == null || pawn.isMarkedForDeletion() || !settlement.getNationId().Equals(pawn.getNationId())
                        || !settlement.getId().Equals(pawn.getCurrentSettlementId()) || pawn.needsFood()
                        || FoodItems.countFood(pawn.getInventory()) >= SeasonalRules.WINTER_FOOD_STORE) {
                    continue;
                }
                low.Add(pawn);
            }
            return low;
        }

        /** How many residents here a press would stock for winter (0 outside autumn and winter). */
        public int countLowOnStoresHere(Player player, Season season) {
            return getLowOnStoresHere(player, season).Count;
        }

        /** Feeds the hungry here, with no winter stocking. Returns how many pawns were fed. */
        public int execute(Player player) {
            return execute(player, Season.SPRING);
        }

        /**
        * Feeds the hungry here, then, in autumn and winter, gives the other
        * members here food up to WINTER_FOOD_STORE each, one item a round so
        * scarce food goes round (#353: the warning a day before winter needs
        * something the player can do about it). Returns how many pawns got
        * anything.
        */
        public int execute(Player player, Season season) {
            Settlement settlement = getOwnSettlement(player);
            if (settlement == null) {
                player.getStatus().update("Feed your own nation's residents from inside one of its settlements.");
                return 0;
            }
            List<Pawn> hungry = ResidentHunger.getHungryPresent(entityRepository, settlement);
            List<Pawn> low = getLowOnStoresHere(player, season);
            if (hungry.Count == 0 && low.Count == 0) {
                player.getStatus().update(stocksForWinter(season) ? "No one here is hungry or short of winter stores." : "No one here is hungry.");
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
            int stocked = stock(inventory, low);
            if (fed == 0 && stocked == 0) {
                player.getStatus().update(hungry.Count > 0
                    ? hungry.Count + (hungry.Count == 1 ? " resident here is" : " residents here are") + " hungry, and you have no food to share."
                    : low.Count + (low.Count == 1 ? " resident here is" : " residents here are") + " short of winter stores, and you have no food to share.");
                return 0;
            }
            int left = 0;
            foreach (Pawn pawn in hungry) {
                if (expected[pawn] < Pawn.HUNGRY_ENERGY_THRESHOLD) {
                    left++;
                }
            }
            int stillLow = 0;
            foreach (Pawn pawn in low) {
                if (FoodItems.countFood(pawn.getInventory()) < SeasonalRules.WINTER_FOOD_STORE) {
                    stillLow++;
                }
            }
            string done = fed > 0 && stocked > 0
                ? "You fed " + fed + " and stocked " + stocked + (stocked == 1 ? " resident" : " residents") + " for winter"
                : fed > 0 ? "You fed " + fed + (fed == 1 ? " resident" : " residents")
                : "You stocked " + stocked + (stocked == 1 ? " resident" : " residents") + " for winter";
            player.getStatus().update(done + " in the " + settlement.getDisplayName() + "."
                + (left > 0 ? " " + left + " still hungry: bring more food." : "")
                + (left == 0 && stillLow > 0 ? " " + stillLow + " still short of winter stores: bring more food." : ""));
            return fed + stocked;
        }

        /** One food a round to each pawn short of WINTER_FOOD_STORE until all have it or the food runs out; how many got any. */
        private static int stock(Inventory inventory, List<Pawn> low) {
            HashSet<Pawn> given = new HashSet<Pawn>();
            bool gave = true;
            while (gave) {
                gave = false;
                foreach (Pawn pawn in low) {
                    if (FoodItems.countFood(pawn.getInventory()) >= SeasonalRules.WINTER_FOOD_STORE) {
                        continue;
                    }
                    ItemType? food = FoodItems.getMostNourishingFood(inventory);
                    if (!food.HasValue) {
                        return given.Count;
                    }
                    inventory.removeItem(food.Value, 1);
                    pawn.getInventory().addItem(food.Value, 1);
                    given.Add(pawn);
                    gave = true;
                }
            }
            return given.Count;
        }
    }
}
