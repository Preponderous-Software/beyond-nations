using System.Collections.Generic;

namespace beyondnations {

    /**
    * The numbers eras and victories are judged on, read off the player's
    * nation in one pass: members, settlements (and how many are villages or
    * towns), stalls, buildings and funds. Funds are the player's coins plus
    * every treasury of the nation's settlements, as the objectives count them.
    *
    * A snapshot: it reads the world and changes nothing.
    */
    public class NationStanding {
        public readonly Nation nation;
        public readonly int members;
        public readonly int settlements;
        public readonly int villages;
        public readonly int towns;
        public readonly int stalls;
        public readonly int buildings;
        public readonly int funds;
        public readonly bool playerLeads;

        private NationStanding(Nation nation, int members, int settlements, int villages, int towns, int stalls, int buildings, int funds, bool playerLeads) {
            this.nation = nation;
            this.members = members;
            this.settlements = settlements;
            this.villages = villages;
            this.towns = towns;
            this.stalls = stalls;
            this.buildings = buildings;
            this.funds = funds;
            this.playerLeads = playerLeads;
        }

        /** The player's nation as it stands, or null when the player has none. */
        public static NationStanding of(Simulation simulation) {
            Nation nation = ObjectiveCatalog.playerNation(simulation);
            if (nation == null) {
                return null;
            }
            Player player = simulation.getPlayer();
            int settlements = 0;
            int villages = 0;
            int towns = 0;
            int stalls = 0;
            int buildings = 0;
            int funds = player.getInventory().getNumItems(ItemType.COIN);
            foreach (EntityId settlementId in nation.getSettlements()) {
                Settlement settlement = simulation.getEntityRepository().getEntity(settlementId) as Settlement;
                if (settlement == null) {
                    continue;
                }
                settlements++;
                if (settlement.getTier() >= SettlementTier.VILLAGE) {
                    villages++;
                }
                if (settlement.getTier() >= SettlementTier.TOWN) {
                    towns++;
                }
                stalls += settlement.getMarket().getNumStalls();
                buildings += settlement.getNumBuildings();
                funds += settlement.getFunds();
            }
            return new NationStanding(nation, nation.getNumberOfMembers(), settlements, villages, towns, stalls, buildings, funds,
                nation.getLeaderId() == player.getId());
        }

        /** Every settlement standing in the world, whoever holds it. */
        public static int countAllSettlements(Simulation simulation) {
            int total = 0;
            foreach (Nation nation in simulation.getNationRepository().getNations()) {
                foreach (EntityId settlementId in nation.getSettlements()) {
                    if (simulation.getEntityRepository().getEntity(settlementId) is Settlement) {
                        total++;
                    }
                }
            }
            return total;
        }

        /** The nations other than the player's that still stand. */
        public static List<Nation> otherNations(Simulation simulation, Nation own) {
            List<Nation> others = new List<Nation>();
            foreach (Nation nation in simulation.getNationRepository().getNations()) {
                if (nation != own) {
                    others.Add(nation);
                }
            }
            return others;
        }
    }
}
