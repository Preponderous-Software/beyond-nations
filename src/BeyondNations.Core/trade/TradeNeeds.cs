using System;

namespace beyondnations {

    /**
    * What a settlement can spare and what its nation is short of: the
    * questions caravans and living prices ask.
    *
    * A settlement's stock is its own stockpile; what its nation's land is
    * like comes from the regions it is settled in (NationResources, #318),
    * read through getStanding. A nation with a surplus of a commodity keeps
    * less of it back and wants less of it at market; one with a shortage
    * keeps more back and pays more for it. With no NationResources to ask
    * (a bare market in a test) every nation stands NORMAL.
    */
    public class TradeNeeds {
        /** Wood a settlement keeps back for its next stall and building before any goes on a caravan. */
        public const int WOOD_RESERVE = 40;
        /** Stone kept back the same way. */
        public const int STONE_RESERVE = 30;
        /** Food kept back per resident, and for the larder however small the town. */
        public const int FOOD_RESERVE_PER_RESIDENT = 2;
        public const int FOOD_RESERVE_BASE = 4;

        private readonly NationResources nationResources;
        private readonly NationRepository nationRepository;

        public TradeNeeds(NationResources nationResources, NationRepository nationRepository) {
            this.nationResources = nationResources;
            this.nationRepository = nationRepository;
        }

        /** The commodity an item counts as, or null for one the regions say nothing about (planks, tools...). */
        public static Commodity? commodityOf(ItemType itemType) {
            if (FoodItems.isFood(itemType)) {
                return Commodity.FOOD;
            }
            switch (itemType) {
                case ItemType.WOOD: return Commodity.WOOD;
                case ItemType.STONE: return Commodity.STONE;
                default: return null;
            }
        }

        /** How the settlement's nation's land stands for this item; NORMAL when unknown. */
        public static ResourceStanding getStanding(TradeNeeds needs, Settlement settlement, ItemType itemType) {
            Commodity? commodity = commodityOf(itemType);
            if (needs == null || needs.nationResources == null || commodity == null || settlement == null) {
                return ResourceStanding.NORMAL;
            }
            Nation nation = needs.nationRepository.getNation(settlement.getNationId());
            return nation == null ? ResourceStanding.NORMAL : needs.nationResources.getStanding(nation, commodity.Value);
        }

        /** Whether one nation's land is rich in what the other's lacks (NationResources.canSupply). */
        public static bool canSupply(TradeNeeds needs, Settlement supplier, Settlement buyer, ItemType itemType) {
            Commodity? commodity = commodityOf(itemType);
            if (needs == null || needs.nationResources == null || commodity == null) {
                return false;
            }
            Nation from = needs.nationRepository.getNation(supplier.getNationId());
            Nation to = needs.nationRepository.getNation(buyer.getNationId());
            return from != null && to != null && from != to && needs.nationResources.canSupply(from, to, commodity.Value);
        }

        /** Scales an amount by a standing: half again for a shortage, two thirds for a surplus. */
        public static int scaleByStanding(int amount, ResourceStanding standing, bool shortageWantsMore) {
            if (standing == ResourceStanding.NORMAL) {
                return amount;
            }
            bool more = (standing == ResourceStanding.SHORTAGE) == shortageWantsMore;
            return more ? amount * 3 / 2 : amount * 2 / 3;
        }

        /** What a settlement could load onto a caravan of this item without going short. */
        public static int getSurplus(TradeNeeds needs, Settlement settlement, ItemType itemType) {
            int held = settlement.getInventory().getNumItems(itemType);
            return Math.Max(0, held - getReserve(needs, settlement, itemType));
        }

        /** What a settlement keeps back: more of what its land lacks, less of what it has plenty of. */
        public static int getReserve(TradeNeeds needs, Settlement settlement, ItemType itemType) {
            int reserve;
            if (FoodItems.isFood(itemType)) {
                reserve = FOOD_RESERVE_BASE + FOOD_RESERVE_PER_RESIDENT * Math.Max(0, settlement.getResidentCount());
            }
            else {
                switch (itemType) {
                    case ItemType.WOOD: reserve = WOOD_RESERVE; break;
                    case ItemType.STONE: reserve = STONE_RESERVE; break;
                    case ItemType.COIN: return int.MaxValue / 2; // the treasury never travels as cargo
                    default: reserve = 0; break;
                }
            }
            return scaleByStanding(reserve, getStanding(needs, settlement, itemType), true);
        }
    }
}
