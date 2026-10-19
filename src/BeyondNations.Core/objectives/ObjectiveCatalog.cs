using System.Collections.Generic;

namespace beyondnations {

    /**
    * The objectives themselves: a first arc that teaches the game one command
    * at a time and builds towards a nation of several settlements, and then
    * ambitions that scale without end.
    *
    * Every cost shown or asked for is read from the constant the command
    * checks (Settlement.WOOD_COST_TO_BUILD, Stall.WOOD_COST_TO_BUILD,
    * Stall.STONE_COST_TO_BUILD, Stall.COIN_COST_TO_PURCHASE), so retuning
    * a cost retunes its objective and its reward with it. Each step is reachable with the commands on
    * develop; hints name keys as "{HintKey}" tokens for the host to resolve.
    */
    public static class ObjectiveCatalog {

        /** Members the arc asks the player's nation to reach. */
        public const int NationGrowthTarget = 5;

        /** Members each further nation-size ambition adds. */
        public const int NationGrowthStep = 5;

        /** Settlements the arc ends by asking for. */
        public const int ArcSettlementTarget = 2;

        /** The categories ambitions cycle through, in order. */
        public const int AmbitionCategoryCount = 4;

        public static int firstWoodTarget() {
            // A fifth of a settlement: a handful of trees, enough to learn the key.
            return System.Math.Max(1, Settlement.WOOD_COST_TO_BUILD / 5);
        }

        private static int smallCoinReward() { return System.Math.Max(1, Stall.COIN_COST_TO_PURCHASE / 4); }
        private static int mediumCoinReward() { return System.Math.Max(1, Stall.COIN_COST_TO_PURCHASE / 2); }
        private static int largeCoinReward() { return Stall.COIN_COST_TO_PURCHASE; }

        public static List<Objective> firstArc() {
            return new List<Objective> {
                new Objective("gather-wood", "Gather wood",
                    s => ObjectiveProgress.count("Wood", wood(s), firstWoodTarget()),
                    "Walk up to a tree and press {Interact} to chop it.",
                    ObjectiveReward.of(ItemType.APPLE, 2)),

                new Objective("eat", "Eat something",
                    s => ObjectiveProgress.flag(s.getPlayer().getMealsEaten() > 0),
                    "Carry food and you eat it when your energy runs low. Trees drop apples; press {Interact} by a chicken for meat.",
                    ObjectiveReward.coins(smallCoinReward())),

                new Objective("join-nation", "Create or join a nation",
                    s => ObjectiveProgress.flag(s.getPlayer().getNationId() != null),
                    "Press {CreateNewNation} to found your own nation, or {JoinNation} to join one.",
                    ObjectiveReward.coins(smallCoinReward())),

                new Objective("found-settlement", "Found a settlement",
                    s => ObjectiveProgress.counterWith(settlements(s) >= 1, "Wood", wood(s), Settlement.WOOD_COST_TO_BUILD),
                    s => isLeader(s)
                        ? "Gather " + Settlement.WOOD_COST_TO_BUILD + " wood, then press {FoundSettlement} away from other settlements."
                        : "Only a leader can found one. Wait for yours, or press {LeaveNation} then {CreateNewNation} to lead.",
                    ObjectiveReward.coins(mediumCoinReward())),

                new Objective("enter-settlement", "Enter your settlement",
                    s => ObjectiveProgress.flag(isInOwnSettlement(s)),
                    "Walk up to it and press {Interact}. {TeleportToHomeSettlement} takes you home.",
                    ObjectiveReward.None),

                new Objective("build-stall", "Build a market stall",
                    s => ObjectiveProgress.countersWith(nationStalls(s) >= 1,
                        "Wood", wood(s), Stall.WOOD_COST_TO_BUILD, "Stone", stone(s), Stall.STONE_COST_TO_BUILD),
                    s => !isLeader(s)
                        ? "Your leader builds stalls; help by bringing wood and stone."
                        : Stall.canAffordToBuild(s.getPlayer().getInventory())
                            ? "Inside your settlement, press {BuildStall}."
                            : "Gather " + Stall.WOOD_COST_TO_BUILD + " wood and " + Stall.STONE_COST_TO_BUILD + " stone (press {Interact} at rocks), then press {BuildStall} inside your settlement.",
                    ObjectiveReward.coins(mediumCoinReward())),

                new Objective("grow-nation", "Grow your nation to " + NationGrowthTarget,
                    s => ObjectiveProgress.count("Members", members(s), NationGrowthTarget),
                    "Talk to settlers ({Interact}) and give gifts ({GiftToPawn}); once they know you, press {InviteToNation}.",
                    ObjectiveReward.coins(largeCoinReward())),

                new Objective("sell-resources", "Sell resources at market",
                    s => ObjectiveProgress.flag(s.getPlayer().getMarketSales() > 0),
                    "Inside a settlement, click Sell Resources. A merchant's stall with coins buys wood and stone.",
                    ObjectiveReward.coins(mediumCoinReward())),

                new Objective("second-settlement", "Found a second settlement",
                    s => ObjectiveProgress.count("Settlements", settlements(s), ArcSettlementTarget),
                    "Travel well away from your first, then press {FoundSettlement} with " + Settlement.WOOD_COST_TO_BUILD + " wood.",
                    ObjectiveReward.coins(largeCoinReward() * 2)),
            };
        }

        /**
        * The n-th ambition after the arc (0-based). They cycle through nation
        * size, funds, settlements and days survived, each lap a tier higher,
        * so the list never runs out.
        */
        public static Objective ambition(int index) {
            int category = index % AmbitionCategoryCount;
            int tier = index / AmbitionCategoryCount + 1;
            ObjectiveReward reward = ObjectiveReward.coins(mediumCoinReward() * tier);
            string id = "ambition-" + index;

            switch (category) {
                case 0: {
                    int target = NationGrowthTarget + NationGrowthStep * tier;
                    return new Objective(id, "Grow your nation to " + target,
                        s => ObjectiveProgress.count("Members", members(s), target),
                        "Befriend settlers and press {InviteToNation}. Nationless settlers also join nearby settlements.",
                        reward, true);
                }
                case 1: {
                    int target = fundsTarget(tier);
                    return new Objective(id, "Amass " + target + " coins",
                        s => ObjectiveProgress.count("Funds", funds(s), target),
                        "Counts your coins and your settlements' treasuries. Sell at market and build stalls.",
                        reward, true);
                }
                case 2: {
                    int target = ArcSettlementTarget + tier;
                    return new Objective(id, "Hold " + target + " settlements",
                        s => ObjectiveProgress.count("Settlements", settlements(s), target),
                        "Press {FoundSettlement} with " + Settlement.WOOD_COST_TO_BUILD + " wood, far from other settlements.",
                        reward.and(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD / 4), true);
                }
                default: {
                    int target = 2 * tier;
                    return new Objective(id, "Survive to day " + target,
                        s => ObjectiveProgress.count("Day", s.getTimeOfDay().getDay(), target),
                        "Keep food on hand; you eat automatically when hungry.",
                        reward, true);
                }
            }
        }

        public static int fundsTarget(int tier) {
            // Five stalls' worth, doubling each tier.
            return Stall.COIN_COST_TO_PURCHASE * 5 * (1 << System.Math.Min(tier - 1, 20));
        }

        // --- predicates over the simulation; all cheap, none walk every entity ---

        public static int wood(Simulation simulation) {
            return simulation.getPlayer().getInventory().getNumItems(ItemType.WOOD);
        }

        public static int stone(Simulation simulation) {
            return simulation.getPlayer().getInventory().getNumItems(ItemType.STONE);
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
            int total = simulation.getPlayer().getInventory().getNumItems(ItemType.COIN);
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
