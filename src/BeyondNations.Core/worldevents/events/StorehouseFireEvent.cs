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

        public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
            return new List<WorldEventChoice> {
                new WorldEventChoice("Form a bucket chain", "Only a quarter of the stockpile is lost, but every resident is worn out."),
                new WorldEventChoice("Save the stone, let the wood burn", "All the wood is lost; the stone is saved."),
                new WorldEventChoice("Let it burn out", "Nobody is hurt, but three quarters of the stockpile goes up in smoke."),
            };
        }

        public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) {
            return 2;
        }

        public int chooseForNation(WorldEventContext context, WorldEventInstance instance) {
            return context.residentsOf(context.getSettlement(instance.getSettlementId())).Count >= 3 ? 0 : 1;
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
