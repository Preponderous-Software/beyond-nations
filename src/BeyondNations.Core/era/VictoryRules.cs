using System.Collections.Generic;

namespace beyondnations {

    /** How a run ended, or NONE while it goes on. Saved by name. */
    public enum RunOutcome {
        NONE,
        DOMINION,
        PROSPERITY,
        CONCORD,
        DEFEAT
    }

    /**
    * The three ways to win and the one way to lose, judged on the player's
    * nation (NationStanding):
    *
    *   Dominion    from the Town era: hold more than half of every settlement
    *               in the world, and at least DominionMinSettlements of them
    *   Prosperity  in the Kingdom era: ProsperityFunds funds and
    *               ProsperityMembers members
    *   Concord     from the Town era: allied with every other nation that
    *               still stands, with at least ConcordMinNations of them
    *   Defeat      the nation the player built collapses -- no settlement and
    *               nobody but the player -- for CollapseGraceTicks in a row
    */
    public static class VictoryRules {
        public const Era DominionEra = Era.TOWN;
        public const int DominionMinSettlements = 6;

        public const Era ProsperityEra = Era.KINGDOM;
        public const int ProsperityFunds = 6000;
        public const int ProsperityMembers = 40;

        public const Era ConcordEra = Era.TOWN;
        public const int ConcordMinNations = 2;

        /** Five minutes at the default 50 ticks a second: half an in-game day. */
        public const int CollapseGraceTicks = 15000;

        public static bool isVictory(RunOutcome outcome) {
            return outcome == RunOutcome.DOMINION || outcome == RunOutcome.PROSPERITY || outcome == RunOutcome.CONCORD;
        }

        public static string getDisplayName(RunOutcome outcome) {
            switch (outcome) {
                case RunOutcome.DOMINION: return "Dominion";
                case RunOutcome.PROSPERITY: return "Prosperity";
                case RunOutcome.CONCORD: return "Concord";
                case RunOutcome.DEFEAT: return "Defeat";
            }
            return "";
        }

        /** One sentence on what the outcome means. */
        public static string describe(RunOutcome outcome, string nationName) {
            string nation = nationName ?? "Your nation";
            switch (outcome) {
                case RunOutcome.DOMINION: return nation + " holds most of the known world.";
                case RunOutcome.PROSPERITY: return nation + " is the richest kingdom in the land.";
                case RunOutcome.CONCORD: return nation + " stands allied with every nation that remains.";
                case RunOutcome.DEFEAT: return nation + " has collapsed: no settlement, and nobody left to follow you.";
            }
            return "";
        }

        /** The first victory the nation has earned, or NONE. */
        public static RunOutcome checkVictory(Simulation simulation, Era era, NationStanding standing) {
            if (standing == null) {
                return RunOutcome.NONE;
            }
            if (dominionProgress(simulation, era, standing).met) {
                return RunOutcome.DOMINION;
            }
            if (prosperityProgress(era, standing).met) {
                return RunOutcome.PROSPERITY;
            }
            if (concordProgress(simulation, era, standing).met) {
                return RunOutcome.CONCORD;
            }
            return RunOutcome.NONE;
        }

        /** How close a victory is, for the HUD: "Dominion: 3/11 settlements". */
        public readonly struct Progress {
            public readonly RunOutcome outcome;
            public readonly bool available;
            public readonly bool met;
            public readonly string text;

            public Progress(RunOutcome outcome, bool available, bool met, string text) {
                this.outcome = outcome;
                this.available = available;
                this.met = met;
                this.text = text;
            }
        }

        public static Progress dominionProgress(Simulation simulation, Era era, NationStanding standing) {
            int own = standing == null ? 0 : standing.settlements;
            int total = NationStanding.countAllSettlements(simulation);
            // more than half, and never fewer than the minimum
            int needed = System.Math.Max(DominionMinSettlements, total / 2 + 1);
            bool available = era >= DominionEra;
            string text = "Dominion: " + own + "/" + needed + " settlements (of " + total + ")";
            if (!available) {
                text += ", from the " + Eras.getDisplayName(DominionEra) + " era";
            }
            return new Progress(RunOutcome.DOMINION, available, available && own >= needed, text);
        }

        public static Progress prosperityProgress(Era era, NationStanding standing) {
            int funds = standing == null ? 0 : standing.funds;
            int members = standing == null ? 0 : standing.members;
            bool available = era >= ProsperityEra;
            string text = "Prosperity: funds " + System.Math.Min(funds, ProsperityFunds) + "/" + ProsperityFunds
                + ", members " + System.Math.Min(members, ProsperityMembers) + "/" + ProsperityMembers;
            if (!available) {
                text += ", in the " + Eras.getDisplayName(ProsperityEra) + " era";
            }
            return new Progress(RunOutcome.PROSPERITY, available, available && funds >= ProsperityFunds && members >= ProsperityMembers, text);
        }

        public static Progress concordProgress(Simulation simulation, Era era, NationStanding standing) {
            Nation own = standing == null ? null : standing.nation;
            List<Nation> others = own == null ? new List<Nation>() : NationStanding.otherNations(simulation, own);
            int allied = 0;
            foreach (Nation other in others) {
                if (simulation.getNationRepository().getRelations().isAllied(own, other)) {
                    allied++;
                }
            }
            bool available = era >= ConcordEra;
            bool met = available && others.Count >= ConcordMinNations && allied == others.Count;
            string text = "Concord: allied with " + allied + "/" + others.Count + " nations";
            if (others.Count < ConcordMinNations) {
                text += " (needs " + ConcordMinNations + " standing)";
            }
            if (!available) {
                text += ", from the " + Eras.getDisplayName(ConcordEra) + " era";
            }
            return new Progress(RunOutcome.CONCORD, available, met, text);
        }

        /**
        * Whether the nation the player built has fallen: the player has no
        * nation, or one with no settlement and nobody else in it.
        */
        public static bool isCollapsed(NationStanding standing) {
            return standing == null || (standing.settlements == 0 && standing.members <= 1);
        }
    }
}
