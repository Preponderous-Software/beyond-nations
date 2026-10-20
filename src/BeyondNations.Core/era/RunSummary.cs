using System.Collections.Generic;

namespace beyondnations {

    /**
    * What the end-of-run screen says, as plain text so it can be checked
    * without a window: the outcome, the era reached, how long the run
    * lasted, the nation's numbers, and the title of its history.
    */
    public static class RunSummary {

        /** "Victory: Dominion" or "Defeat". */
        public static string title(EraTracker eras) {
            RunOutcome outcome = eras.getOutcome();
            if (VictoryRules.isVictory(outcome)) {
                return "Victory: " + VictoryRules.getDisplayName(outcome);
            }
            return outcome == RunOutcome.DEFEAT ? "Defeat" : "The story so far";
        }

        public static string subtitle(EraTracker eras) {
            return VictoryRules.describe(eras.getOutcome(), eras.getNationName());
        }

        /** The subtitle, telling a line with no heir apart from a collapse. */
        public static string subtitle(Simulation simulation) {
            EraTracker eras = simulation.getEraTracker();
            SuccessionNotice notice = simulation.getRealm().getNotice();
            if (eras.getOutcome() == RunOutcome.DEFEAT && simulation.getRealm().isRunOver() && notice != null) {
                return notice.getPreviousName() + " died with no heir: " + notice.getReason() + " "
                    + notice.getNationName() + " passes out of your hands.";
            }
            return subtitle(eras);
        }

        /** "The History of Ironvale". */
        public static string historyTitle(EraTracker eras) {
            return "The History of " + (eras.getNationName() ?? "Your People");
        }

        /** Days survived: the in-game day the run ended on, or today's while it goes on. */
        public static int daysSurvived(Simulation simulation) {
            EraTracker eras = simulation.getEraTracker();
            int tick = eras.getOutcomeTick() >= 0 ? eras.getOutcomeTick() : simulation.getTickCounter().getTotalTicks();
            TimeOfDay time = simulation.getTimeOfDay();
            return TimeOfDay.dayAt(tick, time.getTicksPerDay(), time.getStartFraction()) + 1;
        }

        /** The key numbers, one "Label: value" line each. */
        public static List<string> statLines(Simulation simulation) {
            EraTracker eras = simulation.getEraTracker();
            List<string> lines = new List<string>();
            lines.Add("Era reached: " + Eras.getDisplayName(eras.getEra()) + " (" + Eras.number(eras.getEra()) + " of " + Eras.count() + ")");
            int days = daysSurvived(simulation);
            lines.Add("Days survived: " + days);
            List<string> reached = new List<string>();
            foreach (Era era in new[] { Era.VILLAGE, Era.TOWN, Era.KINGDOM }) {
                int tick = eras.getReachedTick(era);
                if (tick >= 0) {
                    TimeOfDay time = simulation.getTimeOfDay();
                    reached.Add(Eras.getDisplayName(era) + " on day " + (TimeOfDay.dayAt(tick, time.getTicksPerDay(), time.getStartFraction()) + 1));
                }
            }
            if (reached.Count > 0) {
                lines.Add("Eras: " + string.Join(", ", reached));
            }
            NationStanding standing = NationStanding.of(simulation);
            int members = standing == null ? 0 : standing.members;
            int settlements = standing == null ? 0 : standing.settlements;
            int funds = standing == null ? 0 : standing.funds;
            lines.Add("Members: " + members + " (most " + System.Math.Max(members, eras.getPeakMembers()) + ")");
            lines.Add("Settlements: " + settlements + " (most " + System.Math.Max(settlements, eras.getPeakSettlements()) + ")");
            lines.Add("Funds: " + funds + " (most " + System.Math.Max(funds, eras.getPeakFunds()) + ")");
            lines.Add("Objectives completed: " + simulation.getObjectiveTracker().getCompletedCount());
            lines.Add("Nations still standing: " + simulation.getNationRepository().getNumberOfNations());
            return lines;
        }
    }
}
