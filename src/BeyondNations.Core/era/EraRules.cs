using System.Collections.Generic;

namespace beyondnations {

    /**
    * One thing the player's nation needs before it can leave its era:
    * "Members 3/5". Met when current reaches target.
    */
    public readonly struct EraRequirement {
        public readonly string label;
        public readonly int current;
        public readonly int target;

        public EraRequirement(string label, int current, int target) {
            this.label = label;
            this.current = current;
            this.target = target;
        }

        public bool isMet() {
            return current >= target;
        }

        /** "Members 3/5", the current value capped at the target. */
        public string describe() {
            return label + " " + System.Math.Min(current, target) + "/" + target;
        }
    }

    /**
    * What each era asks and what it gives, in one table so the tracker, the
    * HUD, the objectives and the docs never disagree about a number.
    *
    *   Camp -> Village   a village (2 buildings), 10 members, 2 market stalls; day 3
    *   Village -> Town   a town (5 buildings), 18 members, 2 settlements, 1,000 funds; day 7
    *   Town -> Kingdom   2 towns, 25 members, 4 settlements, 3,000 funds; day 19
    *
    * Unlocks, for a nation the player leads (pawn-led nations are never held
    * back -- the era is the player's run, not the world's):
    *
    *   Camp      houses and farms; one settlement; no sieges (nor in the Village)
    *   Village   the workshop (planks and bricks); up to 3 settlements
    *   Town      the storehouse (more stalls); up to 6 settlements
    *   Kingdom   as many settlements as the land holds; Prosperity victory
    */
    public static class EraRules {

        // Tuned with the pacing bot (docs/PACING.md): settlers join a new
        // nation on their own for its first couple of minutes, so a handful
        // of members is no measure of a village.
        public const int VillageMembers = 10;
        public const int VillageStalls = 2;

        public const int TownMembers = 18;
        public const int TownSettlements = 2;
        public const int TownFunds = 1000;

        public const int KingdomTowns = 2;
        public const int KingdomMembers = 25;
        public const int KingdomSettlements = 4;
        public const int KingdomFunds = 3000;

        // Each era also waits on the calendar (a day is ten minutes at the
        // default rates): a nation that settlers flock to is a village in two
        // minutes on most seeds, so two nights have to pass before it is
        // one in name, and so on up. These set the floor of the pacing
        // (Village from about 17 minutes, Town about an hour, Kingdom about
        // three hours); the other requirements set how long a nation takes
        // above it.
        public const int VillageDay = 3;
        public const int TownDay = 7;
        public const int KingdomDay = 19;

        /**
        * What the player's nation must have to leave this era; empty in the
        * last one. day is the in-game day, counted from 1.
        */
        public static List<EraRequirement> requirementsToLeave(Era era, NationStanding standing, int day) {
            int members = standing == null ? 0 : standing.members;
            int settlements = standing == null ? 0 : standing.settlements;
            int villages = standing == null ? 0 : standing.villages;
            int towns = standing == null ? 0 : standing.towns;
            int stalls = standing == null ? 0 : standing.stalls;
            int funds = standing == null ? 0 : standing.funds;
            switch (era) {
                case Era.CAMP:
                    return new List<EraRequirement> {
                        new EraRequirement("Village", villages, 1),
                        new EraRequirement("Members", members, VillageMembers),
                        new EraRequirement("Stalls", stalls, VillageStalls),
                        new EraRequirement("Day", day, VillageDay),
                    };
                case Era.VILLAGE:
                    return new List<EraRequirement> {
                        new EraRequirement("Town", towns, 1),
                        new EraRequirement("Members", members, TownMembers),
                        new EraRequirement("Settlements", settlements, TownSettlements),
                        new EraRequirement("Funds", funds, TownFunds),
                        new EraRequirement("Day", day, TownDay),
                    };
                case Era.TOWN:
                    return new List<EraRequirement> {
                        new EraRequirement("Towns", towns, KingdomTowns),
                        new EraRequirement("Members", members, KingdomMembers),
                        new EraRequirement("Settlements", settlements, KingdomSettlements),
                        new EraRequirement("Funds", funds, KingdomFunds),
                        new EraRequirement("Day", day, KingdomDay),
                    };
                default:
                    return new List<EraRequirement>();
            }
        }

        public static bool allMet(List<EraRequirement> requirements) {
            if (requirements.Count == 0) {
                return false;
            }
            foreach (EraRequirement requirement in requirements) {
                if (!requirement.isMet()) {
                    return false;
                }
            }
            return true;
        }

        public static int countMet(List<EraRequirement> requirements) {
            int met = 0;
            foreach (EraRequirement requirement in requirements) {
                if (requirement.isMet()) {
                    met++;
                }
            }
            return met;
        }

        /** The first requirement not yet met, or null. */
        public static EraRequirement? firstUnmet(List<EraRequirement> requirements) {
            foreach (EraRequirement requirement in requirements) {
                if (!requirement.isMet()) {
                    return requirement;
                }
            }
            return null;
        }

        /** How to make progress on one requirement, with "{HintKey}" tokens for the host. */
        public static string hintFor(EraRequirement requirement) {
            switch (requirement.label) {
                case "Village":
                case "Town":
                case "Towns": {
                    int buildings = requirement.label == "Village" ? SettlementTiers.VILLAGE_BUILDINGS : SettlementTiers.TOWN_BUILDINGS;
                    return "A settlement with " + buildings + " buildings is a " + (requirement.label == "Village" ? "village" : "town")
                        + ". Inside yours, press {BuildBuilding} with wood and stone.";
                }
                case "Members":
                    return "Befriend settlers and press {InviteToNation}; homes with room draw newcomers and families.";
                case "Stalls":
                    return "Inside your settlement, press {BuildStall} with " + Stall.WOOD_COST_TO_BUILD + " wood and " + Stall.STONE_COST_TO_BUILD + " stone.";
                case "Settlements":
                    return "Far from other settlements, press {FoundSettlement} with " + Settlement.WOOD_COST_TO_BUILD + " wood.";
                case "Funds":
                    return "Your coins and your settlements' treasuries. Stalls earn coin; sell at market.";
                case "Day":
                    return "An era is not built in a day: hold on, and keep growing, until day " + requirement.target + ".";
            }
            return "";
        }

        // --- unlocks ---

        /** The era in which a nation the player leads may first put up this building. */
        public static Era unlockedIn(BuildingType type) {
            switch (type) {
                case BuildingType.WORKSHOP: return Era.VILLAGE;
                case BuildingType.STOREHOUSE: return Era.TOWN;
                default: return Era.CAMP;
            }
        }

        public static bool unlocksBuilding(Era era, BuildingType type) {
            return era >= unlockedIn(type);
        }

        /**
        * Whether the player's nation (led or joined) is safe from sieges in
        * this era. A new nation used to be besieged and taken within minutes
        * by neighbours twice its size (docs/PACING.md), and a nation the
        * player had joined still was; until the Town era, when its towns
        * have palisades of their own, it is not. Raids still come.
        */
        public static bool isSheltered(Era era) {
            return era < Era.TOWN;
        }

        /**
        * When the shelter ends at the Town era, how long (in ticks: five
        * minutes at the default rate) the player is given warning before
        * any siege of their nation may begin. The era box counts it down
        * and the status line says when it starts, a minute before it ends
        * and when it ends.
        */
        public const int SiegeWarningTicks = 15000;

        /** How many settlements a nation the player leads may hold in this era; 0 for no limit. */
        public static int settlementLimit(Era era) {
            switch (era) {
                case Era.CAMP: return 1;
                case Era.VILLAGE: return 3;
                case Era.TOWN: return 6;
                default: return 0;
            }
        }

        /** What reaching this era unlocks, one short phrase each. */
        public static List<string> describeUnlocks(Era era) {
            List<string> unlocks = new List<string>();
            foreach (BuildingType type in (BuildingType[]) System.Enum.GetValues(typeof(BuildingType))) {
                if (unlockedIn(type) == era && era != Era.CAMP) {
                    unlocks.Add(BuildingCatalog.getDisplayName(type) + describeBuildingBenefit(type));
                }
            }
            int limit = settlementLimit(era);
            if (era != Era.CAMP) {
                unlocks.Add(limit == 0 ? "any number of settlements" : "up to " + limit + " settlements");
            }
            if (era == Era.KINGDOM) {
                unlocks.Add("a Prosperity victory");
            }
            return unlocks;
        }

        private static string describeBuildingBenefit(BuildingType type) {
            switch (type) {
                case BuildingType.WORKSHOP: return " (craft planks and bricks)";
                case BuildingType.STOREHOUSE: return " (more market stalls)";
            }
            return "";
        }

        /** "Workshop is unlocked in the Village era." */
        public static string describeLocked(BuildingType type) {
            return "The " + BuildingCatalog.getDisplayName(type) + " is unlocked in the " + Eras.getDisplayName(unlockedIn(type)) + " era.";
        }
    }
}
