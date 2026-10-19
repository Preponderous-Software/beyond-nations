using System;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui.boxes {

    /**
    * The objective tracker on the world HUD: the current objective, its
    * counter as a bar ("Wood 12/40") and the hint with keys filled in, plus a
    * short "Objective complete!" toast when one is reached.
    *
    * It reads the tracker's cached progress, which the simulation refreshes
    * once a second, so drawing it costs nothing per frame beyond the text.
    * What it shows is decided by the static methods, which need no ImGui
    * frame and are what the tests check.
    */
    public class ObjectiveTrackerBox {
        public const float MinWidth = 180f;
        public const float MaxWidth = 260f;

        /** How long the toast stays up, in seconds of game time. */
        public const float ToastSeconds = 4f;

        private const ImGuiWindowFlags PanelFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoInputs;

        private static readonly Vector4 HeadingColour = new Vector4(1f, 0.84f, 0.35f, 1f);
        private static readonly Vector4 HintColour = new Vector4(0.78f, 0.80f, 0.85f, 1f);

        private readonly Simulation simulation;

        public ObjectiveTrackerBox(Simulation simulation) {
            this.simulation = simulation;
        }

        /** Panel width for a screen width: about a quarter, within bounds. */
        public static float widthFor(float screenWidth) {
            return Math.Clamp(screenWidth * 0.26f, MinWidth, MaxWidth);
        }

        /** "Objective 3" during the arc, "Ambition" afterwards. */
        public static string heading(ObjectiveTracker tracker) {
            Objective current = tracker.getCurrent();
            if (current == null) {
                return "Objectives";
            }
            if (current.isAmbition()) {
                return "Ambition";
            }
            return "Objective " + (tracker.getCompletedCount() + 1);
        }

        /** The hint for the current objective with its keys resolved. */
        public static string hint(Simulation simulation) {
            Objective current = simulation.getObjectiveTracker().getCurrent();
            return current == null ? "" : ObjectiveKeyNames.resolve(current.getHint(simulation));
        }

        /**
        * The toast's text while it is showing, or null once it has expired or
        * nothing has been completed yet.
        */
        public static string toastText(Simulation simulation) {
            ObjectiveTracker tracker = simulation.getObjectiveTracker();
            Objective completed = tracker.getLastCompleted();
            if (completed == null || !isToastShowing(simulation)) {
                return null;
            }
            string text = completed.getTitle();
            if (!completed.getReward().isEmpty()) {
                text += "\n" + completed.getReward().describe();
            }
            return text;
        }

        public static bool isToastShowing(Simulation simulation) {
            int completedTick = simulation.getObjectiveTracker().getLastCompletedTick();
            if (completedTick < 0) {
                return false;
            }
            TickCounter ticks = simulation.getTickCounter();
            int elapsed = ticks.getTotalTicks() - completedTick;
            return elapsed >= 0 && elapsed < ToastSeconds * ticks.getTicksPerSecond();
        }

        /**
        * Draws the panel and returns its bottom edge, as InfoBox.draw does,
        * so anything stacked under it can start there.
        */
        public float draw(float x, float y, float width) {
            ObjectiveTracker tracker = simulation.getObjectiveTracker();
            Objective current = tracker.getCurrent();
            if (current == null) {
                return y;
            }
            float bottom = y;
            ObjectiveProgress progress = tracker.getCurrentProgress();

            ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, 0), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.6f);
            if (ImGui.Begin("##objective-tracker", PanelFlags)) {
                ImGui.TextColored(HeadingColour, heading(tracker));
                ImGui.PushTextWrapPos(0f);
                ImGui.TextUnformatted(current.getTitle());
                ImGui.PopTextWrapPos();

                if (progress.hasCounter() && progress.getTarget() > 0) {
                    float fraction = Math.Clamp((float) progress.getCurrent() / progress.getTarget(), 0f, 1f);
                    ImGui.ProgressBar(fraction, new Vector2(-1f, 0f), progress.describe());
                }

                string hintText = hint(simulation);
                if (!string.IsNullOrEmpty(hintText)) {
                    ImGui.PushStyleColor(ImGuiCol.Text, HintColour);
                    ImGui.PushTextWrapPos(0f);
                    ImGui.TextUnformatted(hintText);
                    ImGui.PopTextWrapPos();
                    ImGui.PopStyleColor();
                }
                bottom = y + ImGui.GetWindowSize().Y;
            }
            ImGui.End();
            return bottom;
        }

        /**
        * The toast, centred horizontally and no higher than minY, which the
        * HUD sets below the tracker, the status line and the hunger warning
        * so none of them sit on top of each other on a narrow window.
        */
        public void drawToast(float screenWidth, float screenHeight, float minY) {
            string text = toastText(simulation);
            if (text == null) {
                return;
            }
            ImGui.SetNextWindowPos(new Vector2(screenWidth / 2f, Math.Max(minY, screenHeight * 0.16f)), ImGuiCond.Always, new Vector2(0.5f, 0f));
            ImGui.SetNextWindowBgAlpha(0.75f);
            if (ImGui.Begin("##objective-toast", PanelFlags | ImGuiWindowFlags.AlwaysAutoResize)) {
                ImGui.TextColored(HeadingColour, "Objective complete!");
                ImGui.TextUnformatted(text);
            }
            ImGui.End();
        }
    }
}
