using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui.screens {

    /**
    * Shown over the stopped world when a run is won or lost (EraTracker):
    * the outcome, the era reached, the days survived and the nation's key
    * numbers, then "The History of <Nation>" -- every chronicle line about
    * the player and their nation, with the era milestones -- in a window that
    * scrolls by dragging (a finger or a mouse), by the wheel or by its wide
    * scrollbar. Then the way on: Continue playing (after a victory only), New
    * world, Load (after a defeat: a line that ended with no heir is never
    * saved, so this is the way back) or Main menu, as big buttons in a row,
    * or stacked on a narrow screen. It is also where the realm's end of a
    * line lands (RealmScreens), so there is one end-of-run screen.
    *
    * The text comes from RunSummary in core; only the drawing needs a
    * window. The screen returns an intent and the host does the rest, as
    * every screen does; Continue playing marks the outcome as seen, so the
    * screen does not come back.
    */
    public class EndOfRunScreen {
        public const string ContinueLabel = "Continue playing";
        public const string NewWorldLabel = "New world";
        public const string LoadLabel = "Load";
        public const string MainMenuLabel = "Main menu";

        private static readonly Vector4 VictoryColour = new Vector4(1f, 0.84f, 0.35f, 1f);
        private static readonly Vector4 DefeatColour = new Vector4(1f, 0.45f, 0.4f, 1f);
        private static readonly Vector4 StatColour = new Vector4(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Vector4 StampColour = new Vector4(0.62f, 0.70f, 0.78f, 1f);
        private static readonly Vector4 MilestoneColour = new Vector4(1f, 0.84f, 0.35f, 1f);
        private static readonly Vector4 TextColour = new Vector4(0.93f, 0.93f, 0.93f, 1f);

        private const ImGuiWindowFlags HistoryFlags =
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav;

        /** The buttons offered for this outcome, in order. */
        public static string[] buttonLabels(RunOutcome outcome) {
            return VictoryRules.isVictory(outcome)
                ? new[] { ContinueLabel, NewWorldLabel, MainMenuLabel }
                : new[] { NewWorldLabel, LoadLabel, MainMenuLabel };
        }

        /** The history, oldest first, each as its time stamp and its text. */
        public static List<HistoryEntry> historyLines(Simulation simulation) {
            return new List<HistoryEntry>(simulation.getEraTracker().getHistory());
        }

        /**
        * How far a drag moves the history: the pointer's travel, inverted, so
        * the text follows the finger. Pure, for the tests.
        */
        public static float dragScroll(float scrollY, float maxScrollY, float pointerDeltaY) {
            return System.Math.Clamp(scrollY - pointerDeltaY, 0f, System.Math.Max(0f, maxScrollY));
        }

        public UiAction draw(float width, float height, Simulation simulation) {
            EraTracker eras = simulation.getEraTracker();
            RunOutcome outcome = eras.getOutcome();
            bool victory = VictoryRules.isVictory(outcome);
            float margin = UiScale.px(16f);
            float line = Overlay.lineHeight();

            Overlay.beginFullScreen("##end-of-run-screen", width, height);
            // darker than the pause screen: the run is over, and the world's
            // nametags should not compete with the title
            Overlay.dimBackground(width, height);
            Overlay.dimBackground(width, height);
            float y = margin + UiScale.px(18f);
            Overlay.textCenteredColoured(victory ? VictoryColour : DefeatColour, RunSummary.title(eras),
                System.MathF.Min(height / 14f, UiScale.px(34f)), y, width);
            y += UiScale.px(28f);
            foreach (string wrapped in TextWrap.wrap(RunSummary.subtitle(simulation), width - margin * 2f, s => ImGui.CalcTextSize(s).X)) {
                Overlay.textCentered(wrapped, Overlay.BaseFontSize, y, width);
                y += line;
            }
            y += line * 0.5f;

            // the stats, in two columns when there is room
            List<string> stats = RunSummary.statLines(simulation);
            bool twoColumns = width >= UiScale.px(560f);
            float columnWidth = twoColumns ? (width - margin * 3f) / 2f : width - margin * 2f;
            int rows = twoColumns ? (stats.Count + 1) / 2 : stats.Count;
            for (int i = 0; i < stats.Count; i++) {
                int column = twoColumns && i >= rows ? 1 : 0;
                int row = twoColumns && i >= rows ? i - rows : i;
                ImGui.SetCursorPos(new Vector2(margin + column * (columnWidth + margin), y + row * line));
                Overlay.textColoured(StatColour, stats[i]);
            }
            y += rows * line + line * 0.5f;

            // the buttons take the bottom; the history takes what is left
            string[] labels = buttonLabels(outcome);
            float buttonHeight = System.MathF.Max(UiScale.px(44f), Overlay.frameHeight());
            float gap = UiScale.px(10f);
            float padding = ImGui.GetStyle().FramePadding.X * 2f;
            float rowWidth = -gap;
            foreach (string label in labels) {
                rowWidth += System.MathF.Max(UiScale.px(150f), ImGui.CalcTextSize(label).X + padding * 2f) + gap;
            }
            bool stacked = rowWidth > width - margin * 2f;
            float buttonsHeight = stacked ? labels.Length * (buttonHeight + gap) - gap : buttonHeight;
            float buttonsTop = height - margin - buttonsHeight;

            int clicked = -1;
            if (stacked) {
                float buttonWidth = width - margin * 2f;
                for (int i = 0; i < labels.Length; i++) {
                    if (Overlay.buttonAt(labels[i], margin, buttonsTop + i * (buttonHeight + gap), buttonWidth, buttonHeight)) {
                        clicked = i;
                    }
                }
            } else {
                float x = (width - rowWidth) / 2f;
                for (int i = 0; i < labels.Length; i++) {
                    float buttonWidth = System.MathF.Max(UiScale.px(150f), ImGui.CalcTextSize(labels[i]).X + padding * 2f);
                    if (Overlay.buttonAt(labels[i], x, buttonsTop, buttonWidth, buttonHeight)) {
                        clicked = i;
                    }
                    x += buttonWidth + gap;
                }
            }
            Overlay.end();

            float historyHeight = buttonsTop - gap - y;
            if (historyHeight > line * 3f) {
                drawHistory(simulation, margin, y, width - margin * 2f, historyHeight);
            }

            if (clicked < 0) {
                return UiAction.None;
            }
            switch (labels[clicked]) {
                case ContinueLabel:
                    eras.acknowledgeOutcome();
                    return UiAction.ResumeWorld;
                case NewWorldLabel:
                    return UiAction.NewWorld;
                case LoadLabel:
                    // a line that has ended is never saved: Load brings back the last save
                    return UiAction.OpenLoad;
                default:
                    return UiAction.MainMenu;
            }
        }

        private static void drawHistory(Simulation simulation, float x, float y, float width, float height) {
            EraTracker eras = simulation.getEraTracker();
            ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.85f);
            if (ImGui.Begin(RunSummary.historyTitle(eras) + "##end-of-run-history", HistoryFlags | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse)) {
                // a scrollbar wide enough for a thumb
                ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, UiScale.px(18f));
                if (ImGui.BeginChild("##end-of-run-history-scroll", Vector2.Zero)) {
                    List<HistoryEntry> entries = historyLines(simulation);
                    if (entries.Count == 0) {
                        Overlay.textColoured(StampColour, "Nothing was written down.");
                    }
                    ImGui.PushTextWrapPos(0f);
                    foreach (HistoryEntry entry in entries) {
                        Overlay.textColoured(StampColour, entry.getTimestamp());
                        ImGui.SameLine();
                        Overlay.textColoured(entry.isMilestone() ? MilestoneColour : TextColour, entry.getText());
                    }
                    ImGui.PopTextWrapPos();
                    // drag anywhere in the list to scroll it: the touch path
                    if (ImGui.IsWindowHovered() && ImGui.IsMouseDragging(ImGuiMouseButton.Left)) {
                        ImGui.SetScrollY(dragScroll(ImGui.GetScrollY(), ImGui.GetScrollMaxY(), ImGui.GetIO().MouseDelta.Y));
                    }
                }
                ImGui.EndChild();
                ImGui.PopStyleVar();
            }
            ImGui.End();
        }
    }
}
