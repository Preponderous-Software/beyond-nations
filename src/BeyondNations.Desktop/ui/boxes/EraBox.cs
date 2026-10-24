using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui.boxes {

    /**
    * The era on the world HUD, kept to one button and one bar: "Era: Village
    * (2/4)" and how many of the next era's requirements are met. Tapping or
    * clicking the button opens the detail under it -- each requirement, what
    * the next era unlocks and, once they are within reach, how close each
    * victory is -- and tapping again closes it, so nothing needs a hover.
    * While the player's nation is collapsing it says so, in red, with the
    * time left. While its shelter from sieges lasts the detail says so, and
    * once the Town era ends it the box counts down, in amber and without
    * being opened, to when sieges can begin -- or, while one of the player's
    * settlements is already besieged, says so in red instead.
    *
    * What it says is decided by the static methods, which need no ImGui
    * frame and are what the tests check.
    */
    public class EraBox {
        private static readonly Vector4 HeadingColour = new Vector4(0.55f, 0.85f, 1f, 1f);
        private static readonly Vector4 MetColour = new Vector4(0.6f, 1f, 0.6f, 1f);
        private static readonly Vector4 UnmetColour = new Vector4(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Vector4 DangerColour = new Vector4(1f, 0.4f, 0.35f, 1f);
        private static readonly Vector4 WarningColour = new Vector4(1f, 0.75f, 0.3f, 1f);

        public const string ShelteredLine = EraRules.ShelterDescription;

        private const ImGuiWindowFlags PanelFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav;

        private bool expanded;

        public bool isExpanded() { return expanded; }
        public void setExpanded(bool value) { expanded = value; }

        /** "Era: Village (2/4)". */
        public static string headline(Simulation simulation) {
            Era era = simulation.getEraTracker().getEra();
            return "Era: " + Eras.getDisplayName(era) + " (" + Eras.number(era) + "/" + Eras.count() + ")";
        }

        /** "Town: 2/4", or the victory goal in the last era. */
        public static string progressLabel(Simulation simulation) {
            EraTracker eras = simulation.getEraTracker();
            Era? next = Eras.next(eras.getEra());
            if (next == null) {
                return VictoryRules.isVictory(eras.getOutcome()) ? "Won: " + VictoryRules.getDisplayName(eras.getOutcome()) : "Next: a victory";
            }
            List<EraRequirement> requirements = eras.getRequirements(simulation);
            return Eras.getDisplayName(next.Value) + ": " + EraRules.countMet(requirements) + "/" + requirements.Count;
        }

        public static float progressFraction(Simulation simulation) {
            List<EraRequirement> requirements = simulation.getEraTracker().getRequirements(simulation);
            if (requirements.Count == 0) {
                return VictoryRules.isVictory(simulation.getEraTracker().getOutcome()) ? 1f : 0f;
            }
            // each requirement counts for how far along it is, so the bar moves between steps
            float total = 0f;
            foreach (EraRequirement requirement in requirements) {
                total += requirement.target <= 0 ? 1f : System.Math.Min(1f, (float) System.Math.Max(0, requirement.current) / requirement.target);
            }
            return total / requirements.Count;
        }

        /** "Collapsing! 4:12 left", or null when the nation stands. */
        public static string collapseWarning(Simulation simulation) {
            int left = simulation.getEraTracker().getCollapseTicksLeft(simulation);
            if (left < 0) {
                return null;
            }
            int seconds = left / simulation.getTickCounter().getTicksPerSecond();
            return "Collapsing! " + (seconds / 60) + ":" + (seconds % 60).ToString("00") + " left";
        }

        /** "Sieges can begin in 4:12", or null when the player's nation is sheltered or the warning is over. */
        public static string siegeWarning(Simulation simulation) {
            int left = simulation.getEraTracker().getSiegeWarningTicksLeft(simulation);
            if (left < 0 || ObjectiveCatalog.playerNation(simulation) == null) {
                return null;
            }
            int seconds = left / simulation.getTickCounter().getTicksPerSecond();
            return "Sieges can begin in " + (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }

        /**
        * "Your village: Besieged by Greenshire (2/6)!" for the first of the
        * player's settlements under siege, or null. Shown in place of the
        * countdown, which only holds off new sieges: with a siege already
        * under way it read "Sieges can begin in 4:58" while the village fell
        * (playtest 0.6.1 s01 and s04, #396).
        */
        public static string siegeUnderWay(Simulation simulation) {
            Nation own = ObjectiveCatalog.playerNation(simulation);
            if (own == null) {
                return null;
            }
            foreach (Settlement settlement in simulation.getDiplomacy().getSettlements(own)) {
                if (settlement.getSiegeText() != null) {
                    return "Your " + SettlementTiers.getDisplayName(settlement.getTier()).ToLowerInvariant() + ": " + settlement.getSiegeText() + "!";
                }
            }
            return null;
        }

        /** The opened detail, one line each, with whether each requirement is met. */
        public static List<KeyValuePair<string, bool>> detailLines(Simulation simulation) {
            List<KeyValuePair<string, bool>> lines = new List<KeyValuePair<string, bool>>();
            EraTracker eras = simulation.getEraTracker();
            Era? next = Eras.next(eras.getEra());
            if (next != null) {
                lines.Add(new KeyValuePair<string, bool>("To reach the " + Eras.getDisplayName(next.Value) + " era:", false));
                foreach (EraRequirement requirement in eras.getRequirements(simulation)) {
                    lines.Add(new KeyValuePair<string, bool>((requirement.isMet() ? "[x] " : "[ ] ") + requirement.describe(), requirement.isMet()));
                }
                List<string> unlocks = EraRules.describeUnlocks(next.Value);
                if (unlocks.Count > 0) {
                    lines.Add(new KeyValuePair<string, bool>("Unlocks: " + string.Join(", ", unlocks), false));
                }
            }
            Nation own = ObjectiveCatalog.playerNation(simulation);
            if (own != null && own.isSheltered()) {
                lines.Add(new KeyValuePair<string, bool>(ShelteredLine, true));
            }
            NationStanding standing = NationStanding.of(simulation);
            Era era = eras.getEra();
            if (era >= VictoryRules.ConcordEra || era >= VictoryRules.DominionEra) {
                lines.Add(new KeyValuePair<string, bool>("Victories:", false));
                foreach (VictoryRules.Progress progress in new[] {
                        VictoryRules.dominionProgress(simulation, era, standing),
                        VictoryRules.prosperityProgress(era, standing),
                        VictoryRules.concordProgress(simulation, era, standing) }) {
                    lines.Add(new KeyValuePair<string, bool>(progress.text, progress.met));
                }
            }
            return lines;
        }

        /**
        * Draws the box at (x, y) and returns its bottom edge, as the other
        * boxes do, so whatever is stacked under it starts there. Open, the
        * detail scrolls rather than taking the box below maxBottom: at
        * 960x540 inside a settlement it pushed the objective tracker over
        * the command bar (playtest 0.6.1 s04, #397).
        */
        public float draw(Simulation simulation, float x, float y, float width, float maxBottom = float.MaxValue) {
            float bottom = y;
            ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, 0), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.6f);
            if (ImGui.Begin("##era-box", PanelFlags)) {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(HeadingColour));
                // a button, not a hover: the touch path to the detail
                float buttonHeight = TouchLayout.isActive()
                    ? System.MathF.Max(TouchLayout.MinTarget, Overlay.frameHeight())
                    : Overlay.frameHeight() + UiScale.px(6f);
                bool toggled = ImGui.Button(headline(simulation) + (expanded ? "  -" : "  +") + "##era-toggle",
                    new Vector2(-1f, buttonHeight));
                ImGui.PopStyleColor();
                if (toggled) {
                    expanded = !expanded;
                }
                ImGui.ProgressBar(progressFraction(simulation), new Vector2(-1f, 0f), progressLabel(simulation));
                string warning = collapseWarning(simulation);
                if (warning != null) {
                    Overlay.textColoured(DangerColour, warning);
                }
                string underWay = siegeUnderWay(simulation);
                string siege = underWay == null ? siegeWarning(simulation) : null;
                if (underWay != null) {
                    ImGui.PushTextWrapPos(0f);
                    Overlay.textColoured(DangerColour, underWay);
                    ImGui.PopTextWrapPos();
                }
                else if (siege != null) {
                    Overlay.textColoured(WarningColour, siege);
                }
                if (expanded) {
                    List<KeyValuePair<string, bool>> lines = detailLines(simulation);
                    float available = maxBottom - ImGui.GetCursorScreenPos().Y - ImGui.GetStyle().WindowPadding.Y;
                    bool scrolls = detailHeight(lines, ImGui.GetContentRegionAvail().X) > available
                        && available >= Overlay.lineHeight() * 3f;
                    if (scrolls) {
                        ImGui.BeginChild("##era-detail", new Vector2(-1f, available));
                    }
                    ImGui.PushTextWrapPos(0f);
                    foreach (KeyValuePair<string, bool> line in lines) {
                        Overlay.textColoured(line.Value ? MetColour : UnmetColour, line.Key);
                    }
                    ImGui.PopTextWrapPos();
                    if (scrolls) {
                        ImGui.EndChild();
                    }
                }
                bottom = y + ImGui.GetWindowSize().Y;
            }
            ImGui.End();
            return bottom;
        }

        /** The height the detail lines take wrapped to this width, as ImGui lays them out. */
        private static float detailHeight(List<KeyValuePair<string, bool>> lines, float wrapWidth) {
            float height = 0f;
            foreach (KeyValuePair<string, bool> line in lines) {
                height += ImGui.CalcTextSize(line.Key, false, wrapWidth).Y + ImGui.GetStyle().ItemSpacing.Y;
            }
            return height;
        }
    }
}
