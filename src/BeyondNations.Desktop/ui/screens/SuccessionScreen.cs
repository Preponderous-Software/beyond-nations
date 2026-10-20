using System.Collections.Generic;

namespace beyondnations.desktop.ui.screens {

    /**
    * Shown when the player dies as a member of a nation (realm/Succession).
    *
    * With an heir it is a short pause in the story: who died, who takes
    * their place and why, and the line of rulers so far, with Continue to
    * play on as the heir. With no heir the line has ended and so has the
    * run, which the end-of-run screen tells (EndOfRunScreen, a defeat).
    *
    * The words come from getLines(), which needs no window, so they are
    * asserted in unit tests. Escape is read by the host: it continues.
    */
    public class SuccessionScreen {
        public const string SuccessionTitle = "THE RULE PASSES ON";
        public const string ContinueLabel = "Continue";
        /** How many past rulers the screen lists, most recent last. */
        public const int LineageShown = 6;

        private static readonly string[] SuccessionButtons = { ContinueLabel };
        private static readonly UiAction[] SuccessionActions = { UiAction.ResumeWorld };

        public static string getTitle(Simulation simulation) {
            return SuccessionTitle;
        }

        /** "2 days and 3 hours" style, from ticks, on the world's day length. */
        public static string describeReign(Simulation simulation, int ticks) {
            int ticksPerDay = System.Math.Max(1, simulation.getTimeOfDay().getTicksPerDay());
            int days = ticks / ticksPerDay;
            int hours = (ticks % ticksPerDay) * 24 / ticksPerDay;
            if (days == 0) {
                return hours <= 1 ? "less than an hour" : hours + " hours";
            }
            return days + (days == 1 ? " day" : " days") + (hours > 0 ? " and " + hours + (hours == 1 ? " hour" : " hours") : "");
        }

        public static List<string> getLines(Simulation simulation) {
            List<string> lines = new List<string>();
            Realm realm = simulation.getRealm();
            SuccessionNotice notice = realm.getNotice();
            if (notice == null) {
                return lines;
            }
            lines.Add(notice.getPreviousName() + " died of hunger after a reign of " + describeReign(simulation, notice.getReignTicks()) + ".");
            lines.Add("You go on as " + notice.getHeirName() + ", " + notice.getReason() + ".");
            lines.Add("The nation of " + notice.getNationName() + ", its settlements and its friends and enemies are yours still.");
            IReadOnlyList<Reign> lineage = realm.getLineage();
            if (lineage.Count > 0) {
                lines.Add("");
                lines.Add("Your line:");
                int start = System.Math.Max(0, lineage.Count - LineageShown);
                for (int i = start; i < lineage.Count; i++) {
                    Reign reign = lineage[i];
                    lines.Add((i + 1) + ". " + reign.getName() + " of " + reign.getNationName() + ", " + describeReign(simulation, reign.getEndTick() - reign.getStartTick()));
                }
                lines.Add((lineage.Count + 1) + ". " + notice.getHeirName() + ", reigning");
            }
            return lines;
        }

        public UiAction draw(float width, float height, Simulation simulation) {
            float margin = System.MathF.Max(width * 0.05f, UiScale.px(16f));
            float lineHeight = System.MathF.Max(UiScale.px(22f), height / 26f);

            Overlay.beginFullScreen("##succession-screen", width, height);
            Overlay.dimBackground(width, height);
            float y = height / 9f;
            Overlay.textCentered(getTitle(simulation), System.MathF.Min(height / 12f, width / 12f), y, width);
            y += lineHeight * 1.6f;
            foreach (string line in getLines(simulation)) {
                if (line.Length == 0) {
                    y += lineHeight * 0.4f;
                    continue;
                }
                y = NationScreen.drawWrappedCentred(line, height / 32f, y, width, margin, lineHeight * 0.85f, 4);
            }
            y += lineHeight * 0.6f;

            int clicked = Overlay.menuButtons(SuccessionButtons, width, y, height - lineHeight);
            Overlay.end();
            return clicked < 0 ? UiAction.None : SuccessionActions[clicked];
        }
    }
}
