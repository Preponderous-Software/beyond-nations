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
    *   Camp -> Village   a village (2 buildings), 10 members, 2 market stalls; day 2
    *   Village -> Town   a town (5 buildings), 18 members, 2 settlements, 1,000 funds; day 4
    *   Town -> Kingdom   3 towns, 40 members, 5 settlements, 6,000 funds; day 6
    *
    * Unlocks, for a nation the player leads:
    *
    *   Camp      houses and farms; one settlement; no sieges (nor in the Village)
    *   Village   the workshop (planks and bricks) and the storehouse (more
    *             stalls, and caravans); up to 3 settlements
    *   Town      up to 6 settlements
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

        // Raised with the Kingdom's day brought from 19 to 6 (docs/PACING.md,
        // "Calendar waits"): at 2 towns, 25 members, 4 settlements and 3,000
        // funds the courting bot had them all 27 to 37 minutes in, and then
        // waited 140 to 150 minutes on the calendar alone.
        public const int KingdomTowns = 3;
        public const int KingdomMembers = 40;
        public const int KingdomSettlements = 5;
        public const int KingdomFunds = 6000;

        // Each era also waits on the calendar (a day is ten minutes at the
        // default rates): a nation that courts every settler is a village in
        // two minutes on most seeds, so a night has to pass before it is one
        // in name, and so on up. These set the floor of the pacing (Village
        // from 7 minutes, Town from 27, Kingdom from 47); the
        // other requirements set how long a nation takes above it.
        //
        // The Village waits for day 2, not day 3 (#352): at day 3 a nation
        // that had everything else by minute 3 idled for 14 minutes with
        // nothing that could bring the era sooner. Now a nation that grows on
        // its own (no courting) reaches 10 members between minutes 3 and 13,
        // so on most seeds the members, which courting and housing speed up,
        // decide when the era comes, and the calendar only stops the
        // two-minute village (docs/PACING.md, "The eras").
        //
        // The Town waits for day 4, not day 7, for the same reason: at day 7
        // (57 minutes in) the pacing bot had every other requirement by
        // minute 8 to 10 and then waited 35 to 49 minutes on the calendar
        // alone, at its settlement cap, with the world's strangers all taken
        // and its objective list stuck on "Reach the Town era", so a 45 to
        // 60 minute session never saw the Town era, its sixth settlement or
        // a siege. A nation that does not court still had them by minute 10
        // to 18 (docs/PACING.md, "Calendar waits").
        //
        // The Kingdom waits for day 6, not day 19, and asks for more: at day
        // 19 (177 minutes in) the Kingdom came at 177m 00s on every seed, and
        // a Prosperity victory within a minute of it, so the calendar, not
        // play, decided the end of a run. Now the funds and towns decide it,
        // and the calendar only stops a nation that has them before minute 47.
        public const int VillageDay = 2;
        public const int TownDay = 4;
        public const int KingdomDay = 6;

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
                    return ObjectiveCatalog.InviteMembersHint;
                case "Stalls":
                    return "Inside your settlement, press {BuildStall} with " + Stall.WOOD_COST_TO_BUILD + " wood and " + Stall.STONE_COST_TO_BUILD + " stone.";
                case "Settlements":
                    return "Far from other settlements, press {FoundSettlement} with " + FoundingCost.wood(requirement.current)
                        + " wood" + (FoundingCost.stone(requirement.current) > 0 ? " and " + FoundingCost.stone(requirement.current) + " stone" : "") + ".";
                case "Funds":
                    return "Your coins and your settlements' treasuries. Stalls earn coin; sell at market, or send a caravan from a Storehouse.";
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
                // the Village, not the Town: caravans need a storehouse, and the
                // Town era's day (day 7 then) put the first caravan an hour into a run (#373)
                case BuildingType.STOREHOUSE: return Era.VILLAGE;
                default: return Era.CAMP;
            }
        }

        public static bool unlocksBuilding(Era era, BuildingType type) {
            return era >= unlockedIn(type);
        }

        // --- the world's calendar, for nations led by pawns ---

        /**
        * The era the calendar alone allows on this day: the era a nation
        * that met every other requirement at once would be in. A settlement
        * of a nation led by a pawn is no town before this reaches the Town
        * era (pawnLedTierCap), so a neighbour is not a town on day 1 while
        * the player is a camp (0.6.0 playtest s05, #373).
        */
        public static Era calendarEra(int day) {
            if (day >= KingdomDay) {
                return Era.KINGDOM;
            }
            if (day >= TownDay) {
                return Era.TOWN;
            }
            if (day >= VillageDay) {
                return Era.VILLAGE;
            }
            return Era.CAMP;
        }

        /**
        * The highest tier a settlement of a pawn-led nation counts as in this
        * calendar era: a village until the Town era's day, as the player's
        * nation cannot be a Town-era nation before it, however many
        * buildings it has. What it builds is not held back: capping its
        * buildings left pawns homeless, and holding its workshop and
        * storehouse to the calendar too, and either way seed 42 lost over
        * half its people in its first winter (TestSeasonalRules).
        */
        public static SettlementTier pawnLedTierCap(Era calendar) {
            return calendar < Era.TOWN ? SettlementTier.VILLAGE : SettlementTier.TOWN;
        }

        /**
        * Whether the player's nation (led or joined) is safe from sieges in
        * this era. A new nation used to be besieged and taken within minutes
        * by neighbours twice its size (docs/PACING.md), and a nation the
        * player had joined still was; until the Town era, when its towns
        * have palisades of their own, it is not. Raids still come, but
        * rationed, and border friction alone cannot make a neighbour hostile
        * (Diplomacy.SHELTERED_FRICTION_FLOOR, SHELTERED_RAID_EVERY_ROUNDS).
        */
        public static bool isSheltered(Era era) {
            return era < Era.TOWN;
        }

        /**
        * What the shelter means, for the era box: sieges are off, but raids
        * are not, so it must not read as if the nation were safe.
        */
        public const string ShelterDescription =
            "Sheltered until the Town era: no sieges, border quarrels stop at Rival, and raids come at most once every 2 minutes.";

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

        private static readonly string[] Ordinals = {
            "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth"
        };

        /**
        * Why a nation held to limit settlements by its era cannot found
        * another, and what to do about it: "Reach the Village era for a
        * second settlement." It is the reason on the greyed Found Settlement
        * button and what pressing it says (0.8.0 playtest s07: the button
        * vanished at the cap, and only the key told why).
        */
        public static string describeSettlementCap(int limit) {
            string next = limit >= 0 && limit < Ordinals.Length ? "a " + Ordinals[limit] + " settlement" : "more settlements";
            foreach (Era era in (Era[]) System.Enum.GetValues(typeof(Era))) {
                int allowed = settlementLimit(era);
                if (allowed == 0 || allowed > limit) {
                    return "Reach the " + Eras.getDisplayName(era) + " era for " + next + ".";
                }
            }
            return "Reach the next era for " + next + ".";
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
                case BuildingType.STOREHOUSE: return " (more market stalls, and caravans)";
            }
            return "";
        }

        /** "Workshop is unlocked in the Village era." */
        public static string describeLocked(BuildingType type) {
            return "The " + BuildingCatalog.getDisplayName(type) + " is unlocked in the " + Eras.getDisplayName(unlockedIn(type)) + " era.";
        }
    }
}
