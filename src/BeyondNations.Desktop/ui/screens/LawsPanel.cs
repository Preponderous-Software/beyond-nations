using System.Collections.Generic;

namespace beyondnations.desktop.ui.screens {

    /**
    * The court: the Laws tab of the nations screen. One block per law --
    * its name and setting, any change still coming in, what the setting
    * costs and buys -- with a button for each setting underneath, wide
    * enough to tap. Only the leader's buttons do anything; anyone else
    * sees the laws they live under.
    *
    * The blocks scroll when they do not all fit, which on a short window
    * they do not (0.6.4 playtest s06: at 960x540 Rationing was never drawn,
    * and nothing said it was there).
    *
    * The words come from getLines() and getRows(), and what fits from
    * fit(), which need no window, so they are asserted in unit tests.
    */
    public class LawsPanel {
        public const string NoNation = "You belong to no nation, and live under no laws. Found one ({CreateNewNation}) to make them.";

        /** Rows the list should move by this frame; NationScreen.readScroll. */
        public delegate int ScrollReader(float listTop, float listBottom, float rowHeight);

        /** The first law on screen; the list scrolls when the laws outnumber the screen. */
        private int firstRow;

        /** One law, as the panel shows it. */
        public class Row {
            public readonly LawType law;
            /** "Taxes: Low", with "(High in 25 s)" when a change is coming. */
            public readonly string title;
            /** What the setting in force costs and buys. */
            public readonly string effect;
            public readonly int level;
            /** The setting a proclaimed change will bring in, or -1. */
            public readonly int pending;

            public Row(LawType law, string title, string effect, int level, int pending) {
                this.law = law;
                this.title = title;
                this.effect = effect;
                this.level = level;
                this.pending = pending;
            }
        }

        /** Which laws are on screen, and where the first of them starts. */
        public class Window {
            public readonly int firstRow;
            public readonly int visible;
            /** Some law is out of sight, so the count line and Up/Down are shown. */
            public readonly bool overflow;
            public readonly float listTop;

            public Window(int firstRow, int visible, bool overflow, float listTop) {
                this.firstRow = firstRow;
                this.visible = visible;
                this.overflow = overflow;
                this.listTop = listTop;
            }
        }

        /**
        * Fits laws of the given heights between top and bottom, as near to
        * firstRow as the list allows. The strip for the count line and
        * Up/Down takes room only when some law would otherwise be lost.
        */
        public static Window fit(float[] steps, float top, float bottom, float stripHeight, int firstRow) {
            bool overflow = NationScreen.countFitting(steps, 0, bottom - top) < steps.Length;
            float listTop = overflow ? top + stripHeight : top;
            float space = bottom - listTop;
            int first = NationScreen.clampFirstRow(firstRow, steps.Length, NationScreen.countFittingFromEnd(steps, space));
            return new Window(first, NationScreen.countFitting(steps, first, space), overflow, listTop);
        }

        /** "Laws 1-2 of 4. Scroll for more.", for the line over a list that does not all fit. */
        public static string describeShown(int firstRow, int shown, int total, bool touch) {
            string laws = shown == 1 ? "Law " + (firstRow + 1) : "Laws " + (firstRow + 1) + "-" + (firstRow + shown);
            return laws + " of " + total + "." + (touch ? " Drag or tap Up/Down for more." : " Scroll for more.");
        }

        /** The lines above the laws: whose laws they are, and what they have done lately. */
        public static List<string> getLines(Simulation simulation) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            if (own == null) {
                lines.Add(ObjectiveKeyNames.resolve(NoNation));
                return lines;
            }
            Realm realm = simulation.getRealm();
            bool leads = own.getLeaderId().Equals(player.getId());
            lines.Add("The laws of " + own.getName() + ". " + (leads
                ? "Changes take " + (Realm.LAW_DELAY_TICKS / TickCounter.DefaultTicksPerSecond) + " s to come in."
                : "Only its leader can change them."));
            NationLaws laws = realm.getLaws(own);
            int conscripts = realm.getConscripts(own);
            string record = "Defence " + simulation.getDiplomacy().getDefence(own) + (conscripts > 0 ? " (" + conscripts + " conscripts)" : "");
            if (laws != null) {
                record += ". Taxes: " + laws.getLastTaxCollected() + " coins last collection, " + laws.getTotalTaxCollected() + " in all";
                if (laws.getDeserters() > 0) {
                    record += ". Deserters: " + laws.getDeserters();
                }
            }
            lines.Add(record + ".");
            return lines;
        }

        public static List<Row> getRows(Simulation simulation) {
            List<Row> rows = new List<Row>();
            Nation own = simulation.getNationRepository().getNation(simulation.getPlayer().getNationId());
            if (own == null) {
                return rows;
            }
            Realm realm = simulation.getRealm();
            NationLaws laws = realm.getLaws(own);
            foreach (LawType law in Laws.All) {
                int level = laws == null ? 0 : laws.getLevel(law);
                int pending = laws == null ? -1 : laws.getPending(law);
                string title = Laws.getName(law) + ": " + Laws.getLevelName(law, level);
                if (pending >= 0) {
                    int ticks = realm.getTicksUntilEnacted(own, law);
                    title += " (" + Laws.getLevelName(law, pending) + " in " + (ticks + TickCounter.DefaultTicksPerSecond - 1) / TickCounter.DefaultTicksPerSecond + " s)";
                }
                rows.Add(new Row(law, title, Laws.describe(law, level), level, pending));
            }
            return rows;
        }

        /** Draws the panel from y down. Returns the command a tapped button asks for, to run after drawing. */
        public System.Action draw(float width, float height, Simulation simulation, float y, float left, float lineHeight,
                float buttonGap, float buttonHeight, ScrollReader readScroll) {
            foreach (string line in getLines(simulation)) {
                y = NationScreen.drawWrappedCentred(line, height / 36f, y, width, left, lineHeight * 0.7f, 3);
            }
            y += lineHeight * 0.3f;

            Player player = simulation.getPlayer();
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            bool leads = own != null && own.getLeaderId().Equals(player.getId());
            System.Action pending = null;
            float textStep = lineHeight * 0.7f;
            float columnWidth = width - left * 2f;
            float effectSize = height / 40f;
            float effectScale = Overlay.fontScaleFor(effectSize);
            float effectStep = textStep * 0.8f;
            List<Row> rows = getRows(simulation);
            List<List<string>> effects = new List<List<string>>(rows.Count);
            float[] steps = new float[rows.Count];
            for (int i = 0; i < rows.Count; i++) {
                List<string> effect = TextWrap.wrap(rows[i].effect, columnWidth, line => ImGuiNET.ImGui.CalcTextSize(line).X * effectScale);
                effects.Add(effect);
                steps[i] = textStep + effectStep * System.Math.Min(2, effect.Count) + (leads ? buttonHeight + buttonGap : 0f) + buttonGap * 2f;
            }

            // scrolling, as on the nations list: the wheel, the keys, a drag, or Up/Down
            float listBottom = height - lineHeight * 3.2f;
            float stripHeight = buttonHeight + buttonGap;
            Window window = fit(steps, y, listBottom, stripHeight, firstRow);
            int move = readScroll(window.listTop, listBottom, rows.Count > 0 ? steps[0] : lineHeight);
            window = fit(steps, y, listBottom, stripHeight, firstRow + move);
            firstRow = window.firstRow;
            if (window.overflow) {
                float arrowWidth = System.MathF.Min(UiScale.px(80f), columnWidth / 5f);
                float lineWidth = width - (arrowWidth + buttonGap) * 2f;
                NationScreen.drawWrappedCentred(describeShown(firstRow, window.visible, rows.Count, TouchLayout.isActive()), height / 38f,
                    y + (buttonHeight - lineHeight * 0.7f) * 0.5f, lineWidth, left, lineHeight * 0.7f, 1);
                float ax = width - left - arrowWidth * 2f - buttonGap;
                int page = System.Math.Max(1, window.visible - 1);
                if (Overlay.buttonAt("Up##laws-up", ax, y, arrowWidth, buttonHeight)) {
                    window = fit(steps, y, listBottom, stripHeight, firstRow - page);
                }
                if (Overlay.buttonAt("Down##laws-down", ax + arrowWidth + buttonGap, y, arrowWidth, buttonHeight)) {
                    window = fit(steps, y, listBottom, stripHeight, firstRow + page);
                }
                firstRow = window.firstRow;
            }
            y = window.listTop;

            for (int i = firstRow; i < firstRow + window.visible; i++) {
                Row row = rows[i];
                Overlay.textAtScaled(row.title, height / 32f, left, y);
                y += textStep;
                List<string> effect = effects[i];
                for (int line = 0; line < effect.Count && line < 2; line++) {
                    Overlay.textAtScaled(effect[line], effectSize, left, y);
                    y += effectStep;
                }
                if (leads) {
                    int levels = Laws.getNumLevels(row.law);
                    float buttonWidth = System.MathF.Min(UiScale.px(120f), (columnWidth - buttonGap * (levels - 1)) / levels);
                    float x = left;
                    for (int level = 0; level < levels; level++) {
                        string name = Laws.getLevelName(row.law, level);
                        bool chosen = row.pending >= 0 ? level == row.pending : level == row.level;
                        string label = (chosen ? "> " + name + " <" : name) + "##law" + i + "-" + level;
                        if (Overlay.buttonAt(label, x, y, buttonWidth, buttonHeight)) {
                            LawType law = row.law;
                            int toLevel = level;
                            pending = () => new SetLawCommand(simulation.getNationRepository(), simulation.getRealm()).execute(player, law, toLevel);
                        }
                        x += buttonWidth + buttonGap;
                    }
                    y += buttonHeight + buttonGap;
                }
                y += buttonGap * 2f;
            }
            return pending;
        }
    }
}
