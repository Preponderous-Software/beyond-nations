namespace beyondnations {

    /**
    * What the player pays to found a settlement (#145). The first is
    * Settlement.WOOD_COST_TO_BUILD wood, as it always was. Each one after
    * it costs more wood and some stone, rising with the settlements the
    * nation already holds, so spreading out is a mid-game goal rather
    * than a minute's walk: the second is 75 wood and 10 stone, the third
    * 100 wood and 20 stone. The era still decides how many a nation the
    * player leads may hold (EraRules.settlementLimit).
    *
    * Settlers the nation sends out on its own (PopulationGrowth) and
    * pawn-led nations still pay the base wood: their settlements are
    * paid for from a settlement's stockpile, not the player's pack.
    */
    public static class FoundingCost {
        public const int FurtherWoodStep = 25;
        public const int FurtherStoneStep = 10;

        /** Wood to found the next settlement while the nation holds held. */
        public static int wood(int held) {
            return Settlement.WOOD_COST_TO_BUILD + FurtherWoodStep * System.Math.Max(0, held);
        }

        /** Stone to found the next settlement while the nation holds held; none for the first. */
        public static int stone(int held) {
            return FurtherStoneStep * System.Math.Max(0, held);
        }

        public static int wood(Nation nation) {
            return wood(nation == null ? 0 : nation.getNumberOfSettlements());
        }

        public static int stone(Nation nation) {
            return stone(nation == null ? 0 : nation.getNumberOfSettlements());
        }

        public static bool canAfford(Nation nation, Inventory inventory) {
            return inventory.getNumItems(ItemType.WOOD) >= wood(nation) && inventory.getNumItems(ItemType.STONE) >= stone(nation);
        }

        /** "75 wood and 10 stone", or "50 wood" for the first. */
        public static string describe(Nation nation) {
            int stoneCost = stone(nation);
            return wood(nation) + " wood" + (stoneCost > 0 ? " and " + stoneCost + " stone" : "");
        }
    }
}
