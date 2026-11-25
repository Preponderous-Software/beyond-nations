namespace beyondnations.pacing {

    /**
    * Late content a session should reach, watched for alongside the
    * objectives: things the objective list does not ask for but a normal
    * 45 to 60 minute session should see (#372, #373). Each is checked once
    * a second of game time; the budget gives each a latest time, counted
    * from the start of the world (docs/PACING.md, "Milestones").
    */
    public static class PacingMilestones {
        /** One of the player's nation's settlements has a Storehouse. */
        public const string Storehouse = "storehouse";
        /** The player has a caravan of their own on the road. */
        public const string Caravan = "caravan";
        /** The rule has passed to an heir at least once (old age, hunger or abdication). */
        public const string Succession = "succession";
        /**
        * The player's nation is in the Town era: its shelter from sieges
        * ends and it may hold six settlements. At day 7 it came 57 minutes
        * in, after 35 to 49 minutes of waiting on the calendar alone, and so after
        * a session (docs/PACING.md, "Calendar waits").
        */
        public const string TownEra = "town-era";

        public static readonly string[] All = { Storehouse, Caravan, Succession, TownEra };

        public static bool isReached(Simulation simulation, string id) {
            switch (id) {
                case Storehouse: {
                    Nation nation = WorldView.playerNation(simulation);
                    if (nation == null) {
                        return false;
                    }
                    foreach (EntityId settlementId in nation.getSettlements()) {
                        if (simulation.getEntityRepository().getEntity(settlementId) is Settlement settlement
                                && settlement.getNumBuildings(BuildingType.STOREHOUSE) > 0) {
                            return true;
                        }
                    }
                    return false;
                }
                case Caravan:
                    return simulation.getTradeCaravans().getNumPlayerCaravans() > 0;
                case Succession:
                    return simulation.getRealm().getLineage().Count > 0;
                case TownEra:
                    return WorldView.playerNation(simulation) != null && simulation.getEraTracker().getEra() >= Era.TOWN;
            }
            throw new System.ArgumentException("unknown milestone " + id);
        }
    }
}
