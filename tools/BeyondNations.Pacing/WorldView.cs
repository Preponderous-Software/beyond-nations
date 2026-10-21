namespace beyondnations.pacing {

    /**
    * What the bot and the curves read about the player's position in the
    * world. Kept here rather than borrowed from ObjectiveCatalog so a rework
    * of the objectives cannot break the harness that measures it.
    */
    public static class WorldView {

        public static int wood(Simulation simulation) {
            return simulation.getPlayer().getInventory().getNumItems(ItemType.WOOD);
        }

        public static int stone(Simulation simulation) {
            return simulation.getPlayer().getInventory().getNumItems(ItemType.STONE);
        }

        public static int coins(Simulation simulation) {
            return simulation.getPlayer().getInventory().getNumItems(ItemType.COIN);
        }

        public static Nation playerNation(Simulation simulation) {
            NationId nationId = simulation.getPlayer().getNationId();
            return nationId == null ? null : simulation.getNationRepository().getNation(nationId);
        }

        public static bool isLeader(Simulation simulation) {
            Nation nation = playerNation(simulation);
            return nation != null && nation.getLeaderId() == simulation.getPlayer().getId();
        }

        public static int members(Simulation simulation) {
            Nation nation = playerNation(simulation);
            return nation == null ? 0 : nation.getNumberOfMembers();
        }

        public static int settlements(Simulation simulation) {
            Nation nation = playerNation(simulation);
            return nation == null ? 0 : nation.getNumberOfSettlements();
        }

        public static int nationStalls(Simulation simulation) {
            Nation nation = playerNation(simulation);
            if (nation == null) {
                return 0;
            }
            int stalls = 0;
            foreach (EntityId settlementId in nation.getSettlements()) {
                if (simulation.getEntityRepository().getEntity(settlementId) is Settlement settlement) {
                    stalls += settlement.getMarket().getNumStalls();
                }
            }
            return stalls;
        }

        /** The player's coins plus the treasuries of the nation's settlements. */
        public static int funds(Simulation simulation) {
            int total = coins(simulation);
            Nation nation = playerNation(simulation);
            if (nation == null) {
                return total;
            }
            foreach (EntityId settlementId in nation.getSettlements()) {
                if (simulation.getEntityRepository().getEntity(settlementId) is Settlement settlement) {
                    total += settlement.getFunds();
                }
            }
            return total;
        }

        public static bool isInOwnSettlement(Simulation simulation) {
            Player player = simulation.getPlayer();
            EntityId settlementId = player.getCurrentSettlementId();
            if (settlementId == null || player.getNationId() == null) {
                return false;
            }
            return simulation.getEntityRepository().getEntity(settlementId) is Settlement settlement
                && settlement.getNationId() == player.getNationId();
        }
    }
}
