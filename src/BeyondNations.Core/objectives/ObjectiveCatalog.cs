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

        /** Members the arc asks the player's nation to reach: the Village era's. */
        public const int NationGrowthTarget = EraRules.VillageMembers;

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

                // Once "Grow your nation to 5", which settlers joining on their
                // own made trivial; now the first era, which asks for a village.
                new Objective("grow-nation", "Reach the " + Eras.getDisplayName(Era.VILLAGE) + " era",
                    s => ObjectiveProgress.countersWith(s.getEraTracker().getEra() >= Era.VILLAGE,
                        "Members", members(s), EraRules.VillageMembers,
                        "Buildings", mostBuildings(s), SettlementTiers.VILLAGE_BUILDINGS),
                    s => eraHint(s),
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

        /** The n-th ambition as a camp would see it; see ambition(index, era). */
        public static Objective ambition(int index) {
            return ambition(index, Era.CAMP);
        }

        /**
        * The n-th ambition after the arc (0-based), made in the given era.
        * They cycle through nation size, funds, settlements and the next era
        * (in the kingdom, a victory), each lap a tier higher, so the list
        * never runs out; and each era sets its own floor under the targets,
        * so a nation that has grown is never handed a goal it has long
        * passed. The era is the one the ambition was made in (EraTracker
        * remembers it), so a loaded save rebuilds the same list.
        */
        public static Objective ambition(int index, Era era) {
            int category = index % AmbitionCategoryCount;
            int tier = index / AmbitionCategoryCount + 1;
            ObjectiveReward reward = ObjectiveReward.coins(mediumCoinReward() * (tier + (int) era));
            string id = "ambition-" + index;

            switch (category) {
                case 0: {
                    int target = eraMembers(era) + NationGrowthStep * (tier - 1);
                    return new Objective(id, "Grow your nation to " + target,
                        s => ObjectiveProgress.count("Members", members(s), target),
                        "Befriend settlers and press {InviteToNation}. Nationless settlers also join nearby settlements.",
                        reward, true);
                }
                case 1: {
                    int target = eraFunds(era) + fundsStep() * (tier - 1);
                    return new Objective(id, "Amass " + target + " coins",
                        s => ObjectiveProgress.count("Funds", funds(s), target),
                        "Counts your coins and your settlements' treasuries. Sell at market and build stalls.",
                        reward, true);
                }
                case 2: {
                    int target = ArcSettlementTarget + tier;
                    int limit = EraRules.settlementLimit(era);
                    if (limit > 0 && target > limit) {
                        // the era will not let the nation hold more: grow the ones it has
                        int buildings = 3 + 3 * tier;
                        return new Objective(id, "Put up " + buildings + " buildings",
                            s => ObjectiveProgress.count("Buildings", nationBuildings(s), buildings),
                            "Across all your settlements. Inside one, press {BuildBuilding} with wood and stone.",
                            reward.and(ItemType.STONE, BuildingCatalog.getStoneCost(BuildingType.HOUSE)), true);
                    }
                    return new Objective(id, "Hold " + target + " settlements",
                        s => ObjectiveProgress.count("Settlements", settlements(s), target),
                        "Press {FoundSettlement} with " + Settlement.WOOD_COST_TO_BUILD + " wood, far from other settlements.",
                        reward.and(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD / 4), true);
                }
                default: {
                    Era? next = Eras.next(era);
                    if (next == null) {
                        return new Objective(id, "Win the game",
                            s => ObjectiveProgress.flag(VictoryRules.isVictory(s.getEraTracker().getOutcome())),
                            "Dominion (most settlements), Prosperity (" + VictoryRules.ProsperityFunds + " funds, "
                                + VictoryRules.ProsperityMembers + " members) or Concord (allied with every nation).",
                            reward, true);
                    }
                    Era target = next.Value;
                    return new Objective(id, "Reach the " + Eras.getDisplayName(target) + " era",
                        s => eraProgress(s, target),
                        s => eraHint(s),
                        reward.and(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD / 2), true);
                }
            }
        }

        /** The members an era's nation-size ambitions start from. */
        public static int eraMembers(Era era) {
            switch (era) {
                case Era.VILLAGE: return EraRules.TownMembers - 2;
                case Era.TOWN: return EraRules.KingdomMembers - 5;
                case Era.KINGDOM: return VictoryRules.ProsperityMembers - 5;
                default: return EraRules.VillageMembers + 1;
            }
        }

        /** The funds an era's funds ambitions start from. */
        public static int eraFunds(Era era) {
            switch (era) {
                // the arc's rewards and treasuries already make 500 (docs/PACING.md)
                case Era.VILLAGE: return EraRules.TownFunds;
                case Era.TOWN: return EraRules.KingdomFunds;
                case Era.KINGDOM: return VictoryRules.ProsperityFunds * 2 / 3;
                default: return Stall.COIN_COST_TO_PURCHASE * 2;
            }
        }

        /**
        * Progress towards an era: met once the nation is in it, with the
        * requirements of the era it is leaving as the counter.
        */
        private static ObjectiveProgress eraProgress(Simulation simulation, Era target) {
            EraTracker tracker = simulation.getEraTracker();
            if (tracker.getEra() >= target) {
                return ObjectiveProgress.flag(true);
            }
            List<EraRequirement> requirements = tracker.getRequirements(simulation);
            return ObjectiveProgress.counterWith(false, "Requirements", EraRules.countMet(requirements), requirements.Count);
        }

        /** What to do next for the era: the first requirement still short, and how. */
        public static string eraHint(Simulation simulation) {
            EraRequirement? unmet = EraRules.firstUnmet(simulation.getEraTracker().getRequirements(simulation));
            if (unmet == null) {
                return "";
            }
            return unmet.Value.describe() + ". " + EraRules.hintFor(unmet.Value);
        }

        /** What each further lap of funds ambitions adds: five stalls' worth. */
        public static int fundsStep() {
            return Stall.COIN_COST_TO_PURCHASE * 5;
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

        /** Buildings across every settlement of the player's nation. */
        public static int nationBuildings(Simulation simulation) {
            NationStanding standing = NationStanding.of(simulation);
            return standing == null ? 0 : standing.buildings;
        }

        /** The most buildings any one settlement of the player's nation has. */
        public static int mostBuildings(Simulation simulation) {
            Nation nation = playerNation(simulation);
            int most = 0;
            if (nation == null) {
                return 0;
            }
            foreach (EntityId settlementId in nation.getSettlements()) {
                if (simulation.getEntityRepository().getEntity(settlementId) is Settlement settlement) {
                    most = System.Math.Max(most, settlement.getNumBuildings());
                }
            }
            return most;
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
