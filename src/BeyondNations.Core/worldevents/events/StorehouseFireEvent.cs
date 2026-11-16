using System.Collections.Generic;

namespace beyondnations {

    /**
    * Fire at a settlement's storehouse. A bucket chain saves most of the
    * stockpile but exhausts the residents; saving the stone first loses all
    * the wood; letting it burn loses most of everything.
    */
    public class StorehouseFireEvent : IWorldEvent {
        public const string Id = "fire";
        public const int MIN_STOCKPILE = 10;
        public const float BUCKET_CHAIN_ENERGY = 25f;

        public string getId() { return Id; }
        public string getTitle() { return "Fire at the storehouse"; }
        public bool isPersonal() { return false; }

        public int getWeight(WorldEventContext context, Nation nation) {
            Settlement settlement = mostStocked(context, nation);
            return settlement == null ? 0 : 3 + System.Math.Min(7, stockpile(settlement) / 20);
        }

        private static int stockpile(Settlement settlement) {
            return settlement.getInventory().getNumItems(ItemType.WOOD) + settlement.getInventory().getNumItems(ItemType.STONE);
        }

        private static Settlement mostStocked(WorldEventContext context, Nation nation) {
            Settlement most = null;
            foreach (Settlement settlement in context.settlementsOf(nation)) {
                if (settlement.getNumBuildings(BuildingType.STOREHOUSE) == 0 || stockpile(settlement) < MIN_STOCKPILE) {
                    continue;
                }
                if (most == null || stockpile(settlement) > stockpile(most)) {
                    most = settlement;
                }
            }
            return most;
        }

        public bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance) {
            Settlement settlement = mostStocked(context, nation);
            if (settlement == null) {
                return false;
            }
            instance.setSettlementId(settlement.getId());
            return true;
        }

        public string describe(WorldEventContext context, WorldEventInstance instance) {
            Settlement settlement = context.getSettlement(instance.getSettlementId());
            if (settlement == null) {
                return "Fire has broken out at a storehouse.";
            }
            return "Fire has broken out at the storehouse in the " + settlement.getDisplayName() + ", where "
                + settlement.getInventory().getNumItems(ItemType.WOOD) + " wood and " + settlement.getInventory().getNumItems(ItemType.STONE)
                + " stone are piled. The flames are spreading fast.";
        }

        /**
        * How many residents a bucket chain would leave starving: hungry, with
        * nothing to eat, and under Pawn.STARVING_ENERGY_THRESHOLD once it has
        * worn them out.
        */
        public static int countLeftStarving(WorldEventContext context, Settlement settlement) {
            int starving = 0;
            if (settlement == null) {
                return 0;
            }
            foreach (Pawn resident in context.residentsOf(settlement)) {
                if (resident.needsFood() && resident.getEnergy() - BUCKET_CHAIN_ENERGY < Pawn.STARVING_ENERGY_THRESHOLD) {
                    starving++;
                }
            }
            return starving;
        }

        public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
            int leftStarving = countLeftStarving(context, context.getSettlement(instance.getSettlementId()));
            return new List<WorldEventChoice> {
                new WorldEventChoice("Form a bucket chain", "Only a quarter of the stockpile is lost, but every resident is worn out."
                    + (leftStarving == 0 ? "" : " " + leftStarving + (leftStarving == 1 ? " is" : " are") + " already hungry and would be left starving.")),
                new WorldEventChoice("Save the stone, let the wood burn", "All the wood is lost; the stone is saved."),
                new WorldEventChoice("Let it burn out", "Nobody is hurt, but three quarters of the stockpile goes up in smoke."),
            };
        }

        public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) {
            return 2;
        }

        /**
        * A nation's own choice: a bucket chain with hands enough for one,
        * unless it would leave residents starving. In a hungry winter the
        * chain took residents sheltering at 10-30 energy down to 1, and a
        * town lost twenty in a few seconds (seed 42, #484).
        */
        public int chooseForNation(WorldEventContext context, WorldEventInstance instance) {
            Settlement settlement = context.getSettlement(instance.getSettlementId());
            return context.residentsOf(settlement).Count >= 3 && countLeftStarving(context, settlement) == 0 ? 0 : 1;
        }

        public string resolve(WorldEventContext context, WorldEventInstance instance, int choice) {
            Settlement settlement = context.getSettlement(instance.getSettlementId());
            if (settlement == null) {
                return null;
            }
            Inventory stores = settlement.getInventory();
            int wood = stores.getNumItems(ItemType.WOOD);
            int stone = stores.getNumItems(ItemType.STONE);
            int woodLost;
            int stoneLost;
            string how;
            string who = context.chooser(instance);
            if (choice == 0) {
                woodLost = wood / 4;
                stoneLost = stone / 4;
                foreach (Pawn resident in context.residentsOf(settlement)) {
                    resident.setEnergy(System.Math.Max(1f, resident.getEnergy() - BUCKET_CHAIN_ENERGY));
                }
                how = who + " formed a bucket chain";
            }
            else if (choice == 1) {
                woodLost = wood;
                stoneLost = 0;
                how = who + " saved the stone and let the wood burn";
            }
            else {
                woodLost = wood * 3 / 4;
                stoneLost = stone * 3 / 4;
                how = who + " let it burn out";
            }
            WorldEventContext.takeUpTo(stores, ItemType.WOOD, woodLost);
            WorldEventContext.takeUpTo(stores, ItemType.STONE, stoneLost);
            return "fire broke out at the storehouse in the " + settlement.getDisplayName() + "; " + how + ", and "
                + WorldEvents.describeLoot(0, woodLost, stoneLost) + " was lost.";
        }
    }
}
