namespace beyondnations {

    /**
    * How grown a settlement is, read off how many buildings stand in it. Shown
    * on the settlement's nametag ("Village of ..."), and a town is ringed by a
    * palisade.
    */
    public enum SettlementTier {
        CAMP,
        VILLAGE,
        TOWN
    }

    public static class SettlementTiers {
        public const int VILLAGE_BUILDINGS = 2;
        public const int TOWN_BUILDINGS = 5;

        public static SettlementTier fromBuildingCount(int numBuildings) {
            if (numBuildings >= TOWN_BUILDINGS) {
                return SettlementTier.TOWN;
            }
            if (numBuildings >= VILLAGE_BUILDINGS) {
                return SettlementTier.VILLAGE;
            }
            return SettlementTier.CAMP;
        }

        public static string getDisplayName(SettlementTier tier) {
            switch (tier) {
                case SettlementTier.VILLAGE: return "Village";
                case SettlementTier.TOWN: return "Town";
                default: return "Camp";
            }
        }
    }
}
