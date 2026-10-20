using System.Collections.Generic;

namespace beyondnations.desktop.ui.screens {

    /**
    * The court: the Laws tab of the nations screen. One block per law --
    * its name and setting, any change still coming in, what the setting
    * costs and buys -- with a button for each setting underneath, wide
    * enough to tap. Only the leader's buttons do anything; anyone else
    * sees the laws they live under.
    *
    * The words come from getLines() and getRows(), which need no window, so
    * they are asserted in unit tests.
    */
    public class LawsPanel {
        public const string NoNation = "You belong to no nation, and live under no laws. Found one (N) to make them.";

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

        /** The lines above the laws: whose laws they are, and what they have done lately. */
        public static List<string> getLines(Simulation simulation) {
            List<string> lines = new List<string>();
            Player player = simulation.getPlayer();
            Nation own = simulation.getNationRepository().getNation(player.getNationId());
            if (own == null) {
                lines.Add(NoNation);
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
                float buttonGap, float buttonHeight) {
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
            List<Row> rows = getRows(simulation);
            for (int i = 0; i < rows.Count; i++) {
                Row row = rows[i];
                float effectSize = height / 40f;
                float effectScale = Overlay.fontScaleFor(effectSize);
                List<string> effect = TextWrap.wrap(row.effect, columnWidth, line => ImGuiNET.ImGui.CalcTextSize(line).X * effectScale);
                int effectLines = System.Math.Min(2, effect.Count);
                float effectStep = textStep * 0.8f;
                float blockHeight = textStep + effectStep * effectLines + (leads ? buttonHeight + buttonGap : 0f) + buttonGap * 2f;
                if (y + blockHeight > height - lineHeight * 3.2f) {
                    break;
                }
                Overlay.textAtScaled(row.title, height / 32f, left, y);
                y += textStep;
                for (int line = 0; line < effectLines; line++) {
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
