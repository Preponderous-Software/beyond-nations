using System.Collections.Generic;

namespace beyondnations {

    /**
    * What a nation's land gives it plenty of, and what it lacks: the query
    * trade (caravans) and diplomacy should use to decide who needs what from
    * whom (docs/SYSTEMS.md, "Regions").
    *
    * It reads the regions of the land around the nation's settlements -- each
    * settlement's own chunk counted twice, and the eight chunks around it
    * once -- and averages RegionCatalog.abundance over them. 1.0 is the old,
    * region-less world; at SURPLUS_AT or more the nation has a surplus, at
    * SHORTAGE_AT or less a shortage. So a nation settled in the highlands has
    * a surplus of stone and a shortage of wood and food, and one in the forest
    * the other way about for wood and stone.
    *
    * This is the land's endowment, not a stock count: it changes only when the
    * nation founds, gains or loses a settlement, never because of what was
    * gathered this minute, which is what makes it a stable basis for trade
    * routes and alliances. It reads the region map and the settlements and
    * nothing else, and draws no randomness, so calling it never changes the
    * world. A nation with no settlements has no land, and so no surplus and no
    * shortage.
    */
    public class NationResources {
        public const float SURPLUS_AT = 1.35f;
        public const float SHORTAGE_AT = 0.75f;

        private static readonly Commodity[] AllCommodities = { Commodity.WOOD, Commodity.STONE, Commodity.FOOD };

        private readonly EntityRepository entityRepository;
        private readonly RegionMap regionMap;

        public NationResources(EntityRepository entityRepository, RegionMap regionMap) {
            this.entityRepository = entityRepository;
            this.regionMap = regionMap;
        }

        /** Every commodity, in a fixed order. */
        public static IReadOnlyList<Commodity> getCommodities() {
            return AllCommodities;
        }

        /**
        * How much of the commodity the nation's land yields, relative to the
        * old world (1.0); 1.0 for a nation with no settlements.
        */
        public float getAbundance(Nation nation, Commodity commodity) {
            float total = 0f;
            int weight = 0;
            foreach (EntityId settlementId in nation.getSettlements()) {
                Settlement settlement = entityRepository.getEntity(settlementId) as Settlement;
                if (settlement == null || settlement.isMarkedForDeletion()) {
                    continue;
                }
                int cx = regionMap.chunkCoordinate(settlement.getPosition().X);
                int cz = regionMap.chunkCoordinate(settlement.getPosition().Z);
                for (int dx = -1; dx <= 1; dx++) {
                    for (int dz = -1; dz <= 1; dz++) {
                        int w = dx == 0 && dz == 0 ? 2 : 1;
                        total += w * RegionCatalog.abundance(regionMap.regionOfChunk(cx + dx, cz + dz), commodity);
                        weight += w;
                    }
                }
            }
            return weight == 0 ? 1f : total / weight;
        }

        public ResourceStanding getStanding(Nation nation, Commodity commodity) {
            return standingOf(getAbundance(nation, commodity));
        }

        public static ResourceStanding standingOf(float abundance) {
            if (abundance >= SURPLUS_AT) {
                return ResourceStanding.SURPLUS;
            }
            if (abundance <= SHORTAGE_AT) {
                return ResourceStanding.SHORTAGE;
            }
            return ResourceStanding.NORMAL;
        }

        /** The commodities the nation has plenty of, in a fixed order (wood, stone, food). */
        public List<Commodity> getSurpluses(Nation nation) {
            return withStanding(nation, ResourceStanding.SURPLUS);
        }

        /** The commodities the nation is short of, in a fixed order (wood, stone, food). */
        public List<Commodity> getShortages(Nation nation) {
            return withStanding(nation, ResourceStanding.SHORTAGE);
        }

        /**
        * Whether the supplier has a surplus of something the buyer is short
        * of -- the simplest reason for one nation to trade with another.
        */
        public bool canSupply(Nation supplier, Nation buyer, Commodity commodity) {
            return getStanding(supplier, commodity) == ResourceStanding.SURPLUS && getStanding(buyer, commodity) == ResourceStanding.SHORTAGE;
        }

        private List<Commodity> withStanding(Nation nation, ResourceStanding standing) {
            List<Commodity> result = new List<Commodity>();
            foreach (Commodity commodity in AllCommodities) {
                if (getStanding(nation, commodity) == standing) {
                    result.Add(commodity);
                }
            }
            return result;
        }

        /** The item types a commodity covers, for code that moves goods. */
        public static IReadOnlyList<ItemType> itemTypesOf(Commodity commodity) {
            switch (commodity) {
                case Commodity.WOOD: return new[] { ItemType.WOOD };
                case Commodity.STONE: return new[] { ItemType.STONE };
                default: return FoodItems.getFoodInDescendingEnergyOrder();
            }
        }

        public static string name(Commodity commodity) {
            switch (commodity) {
                case Commodity.WOOD: return "wood";
                case Commodity.STONE: return "stone";
                default: return "food";
            }
        }
    }
}
