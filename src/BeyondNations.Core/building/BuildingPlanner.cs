using System.Collections.Generic;

namespace beyondnations {

    /**
    * Decides what a settlement should build next. Used by leader pawns, and by
    * the player's build key, which builds whatever this suggests.
    *
    * The order is by need: a house when the settlement is nearly out of room
    * for residents, a farm when farms lag behind houses, a storehouse when the
    * market is full; then, once it is a village (two buildings), its one
    * workshop for crafting planks and bricks; after that houses, farms and
    * storehouses in turn. The
    * first that the settlement still has room for and the builder can pay for
    * wins. Deterministic: no randomness, only the settlement's state.
    */
    public static class BuildingPlanner {

        public static List<BuildingType> getPriorityOrder(Settlement settlement) {
            List<BuildingType> order = new List<BuildingType>(6);
            int houses = settlement.getNumBuildings(BuildingType.HOUSE);
            int farms = settlement.getNumBuildings(BuildingType.FARM);
            Market market = settlement.getMarket();

            if (settlement.getResidentCount() >= settlement.getResidentCap() - 2) {
                order.Add(BuildingType.HOUSE);
            }
            if (farms < 1 + houses / 2) {
                order.Add(BuildingType.FARM);
            }
            if (market.getNumStalls() >= market.getMaxNumStalls()) {
                order.Add(BuildingType.STOREHOUSE);
            }
            // a village gets its workshop before more of the same: put last
            // only, it would never be built, since the other limits add up
            // to all twelve building slots
            if (settlement.getNumBuildings() >= 2) {
                order.Add(BuildingType.WORKSHOP);
            }
            addIfAbsent(order, BuildingType.HOUSE);
            addIfAbsent(order, BuildingType.FARM);
            addIfAbsent(order, BuildingType.STOREHOUSE);
            addIfAbsent(order, BuildingType.WORKSHOP);
            return order;
        }

        /**
        * What the settlement would build next if materials were no object, or
        * null when it has no room for anything more.
        */
        public static BuildingType? suggestNext(Settlement settlement) {
            foreach (BuildingType type in getPriorityOrder(settlement)) {
                if (settlement.canAddBuilding(type)) {
                    return type;
                }
            }
            return null;
        }

        /**
        * What the holder of this inventory can build here next, or null when
        * nothing the settlement has room for is affordable. The settlement's
        * stockpile counts toward the cost.
        */
        public static BuildingType? chooseNext(Settlement settlement, Inventory inventory) {
            foreach (BuildingType type in getPriorityOrder(settlement)) {
                if (settlement.canAddBuilding(type) && BuildingCatalog.canAfford(inventory, settlement.getInventory(), type)) {
                    return type;
                }
            }
            return null;
        }

        private static void addIfAbsent(List<BuildingType> order, BuildingType type) {
            if (!order.Contains(type)) {
                order.Add(type);
            }
        }
    }
}
