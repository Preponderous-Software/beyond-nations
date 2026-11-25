using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui {

    /** How a chip sits in the room it is given: against its left or right edge, or centred. */
    public enum HudAlign { Left, Right, Centre }

    /**
    * The one path every contextual piece of the world HUD is drawn through:
    * the one-line chips (the carrying chip, the season chip with the winter
    * countdown) and the boxes shown only while they matter (settlement,
    * market, season detail, debug). Each is placed where WorldHud asks and
    * then moved the least distance off the no-go rects (the command bar and
    * the touch controls, HudLayout.avoid) and off what this frame has
    * already placed, so nothing contextual ever covers a button or the
    * joystick at any window size.
    *
    * A chip is a button: a tap on it (once it has settled, ButtonSettle, so a
    * chip appearing under a finger does not take its tap) is reported to the
    * caller, which opens the Journal on the matching tab. While something is
    * drawn over the HUD (the More sheet, the caravan or market panel) chips
    * take no tap (setBlocked). A chip carries a coloured dot and a text mark
    * for its badge (! for a warning or danger, * for news), so the colour is
    * never the only sign.
    *
    * There is no collapsing, dragging or docking: a piece of the HUD is shown
    * because it is relevant now (HudRelevance), not because the player
    * arranged it.
    */
    public class HudPanels {
        private const ImGuiWindowFlags ChipFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoBackground;

        private static readonly Vector4 ChipFill = new Vector4(0.08f, 0.09f, 0.11f, 0.78f);
        private static readonly Vector4 ChipHover = new Vector4(0.16f, 0.20f, 0.26f, 0.90f);
        private static readonly Vector4 ChipActive = new Vector4(0.11f, 0.25f, 0.38f, 0.95f);
        public static readonly Vector4 NewColour = new Vector4(0.45f, 0.75f, 1f, 1f);
        public static readonly Vector4 WarningColour = new Vector4(1f, 0.75f, 0.3f, 1f);
        public static readonly Vector4 DangerColour = new Vector4(1f, 0.35f, 0.3f, 1f);

        private static HudPanels current;

        private readonly ButtonSettle settle = new ButtonSettle();
        private readonly List<HudRect> obstacles = new List<HudRect>();
        // something drawn over the HUD (the More sheet) covers it: no chip takes a tap
        private bool blocked;
        private float screenWidth;
        private float screenHeight;
        private float gap;
        private double now;

        /** The HudPanels drawing this frame's HUD, or null outside it. */
        public static HudPanels getCurrent() {
            return current;
        }

        /**
        * Starts a frame of the HUD on a width by height screen. noGo is what
        * no chip or box may cover: the command bar and the touch controls.
        */
        public void begin(float width, float height, float gap, double now, IEnumerable<HudRect> noGo) {
            current = this;
            screenWidth = width;
            screenHeight = height;
            this.gap = gap;
            this.now = now;
            obstacles.Clear();
            if (noGo != null) {
                foreach (HudRect rect in noGo) {
                    if (!rect.isEmpty()) {
                        obstacles.Add(rect);
                    }
                }
            }
        }

        /** While something covers the HUD (a phone's More sheet, the caravan or market panel), no chip takes a tap. Call after begin. */
        public void setBlocked(bool covered) {
            blocked = covered;
        }

        public bool isBlocked() {
            return blocked;
        }

        public void end() {
            settle.endFrame();
            if (current == this) {
                current = null;
            }
        }

        /** A chip's height: a fingertip on a touch screen. */
        public static float stripHeight() {
            return TouchLayout.isActive()
                ? System.MathF.Max(UiScale.px(TouchLayout.MinTarget), Overlay.frameHeight())
                : Overlay.frameHeight() + UiScale.px(4f);
        }

        /** "Autumn 2/2 - Winter in 1 day! !": the chip's text with a mark for its badge, so colour is not the only sign. */
        public static string chipLabel(string text, HudBadge badge) {
            string mark = badge == HudBadge.Warning || badge == HudBadge.Danger ? " !" : badge == HudBadge.New ? " *" : "";
            // a line that already ends in "!" says so itself
            if (mark == " !" && text.EndsWith("!")) {
                mark = "";
            }
            return text + mark;
        }

        /** The most lines a chip wraps to; past that its last line is cut short with "...". */
        public const int ChipMaxLines = 3;

        /**
        * A chip's text as lines no wider than maxWidth (measure gives a
        * line's width): one line when it fits, else wrapped at spaces, at
        * most maxLines, the last cut short ("...") when there is more.
        */
        public static List<string> chipLines(string text, float maxWidth, System.Func<string, float> measure, int maxLines) {
            List<string> lines = measure(text) <= maxWidth ? new List<string> { text } : TextWrap.wrap(text, maxWidth, measure);
            if (lines.Count == 0) {
                lines.Add(text);
            }
            if (lines.Count > maxLines) {
                string rest = string.Join(" ", lines.GetRange(maxLines - 1, lines.Count - maxLines + 1));
                lines = lines.GetRange(0, maxLines - 1);
                lines.Add(rest);
            }
            for (int i = 0; i < lines.Count; i++) {
                if (measure(lines[i]) > maxWidth) {
                    lines[i] = ChroniclePanel.fitToWidth(lines[i], maxWidth, measure);
                }
            }
            return lines;
        }

        /**
        * Where a piece wanted at `wanted` goes: moved the least distance off
        * the no-go rects and whatever was placed before it this frame, then
        * itself kept clear of by what comes after.
        */
        public HudRect place(HudRect wanted) {
            HudRect placed = HudLayout.avoid(wanted, screenWidth, screenHeight, obstacles);
            obstacles.Add(placed);
            return placed;
        }

        /**
        * A one-line chip, at most maxWidth wide, in the room from x to
        * x + maxWidth at y, aligned as asked. Returns true when it was tapped.
        * bottom is set to the bottom of where it went.
        */
        public bool chip(string id, string text, HudBadge badge, float x, float y, float maxWidth, HudAlign align, out float bottom) {
            ImGuiStylePtr style = ImGui.GetStyle();
            float h = stripHeight();
            float dotRoom = badge == HudBadge.None ? 0f : h * 0.5f;
            float maxText = System.MathF.Max(UiScale.px(40f), maxWidth - style.FramePadding.X * 2f - dotRoom);
            // a line too long for the room wraps (up to ChipMaxLines) rather than lose its end
            List<string> lines = chipLines(text, maxText - ImGui.CalcTextSize(" !").X, s => ImGui.CalcTextSize(s).X, ChipMaxLines);
            lines[lines.Count - 1] = chipLabel(lines[lines.Count - 1], badge);
            string label = string.Join("\n", lines);
            Vector2 labelSize = ImGui.CalcTextSize(label);
            h = System.MathF.Max(h, labelSize.Y + style.FramePadding.Y * 2f);
            float w = System.MathF.Min(maxWidth, labelSize.X + style.FramePadding.X * 2f + dotRoom);
            float px = align == HudAlign.Right ? x + maxWidth - w : align == HudAlign.Centre ? x + (maxWidth - w) * 0.5f : x;
            HudRect rect = place(new HudRect(px, y, w, h));
            bottom = rect.Bottom;

            bool tapped = false;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.SetNextWindowPos(new Vector2(rect.X, rect.Y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(rect.W, rect.H), ImGuiCond.Always);
            bool open = ImGui.Begin("##hud-chip-" + id, ChipFlags);
            ImGui.PopStyleVar();
            if (open) {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ChipFill));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetColorU32(ChipHover));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetColorU32(ChipActive));
                ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0f, 0.5f));
                Vector2 min = ImGui.GetCursorScreenPos();
                bool pressed = ImGui.Button(label + "###hud-chip-button-" + id, new Vector2(rect.W, rect.H));
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(3);
                bool settled = settle.isSettled("hud-chip-" + id, System.MathF.Round(rect.X), System.MathF.Round(rect.Y), now);
                tapped = pressed && settled && !blocked;
                if (badge != HudBadge.None) {
                    Vector4 colour = badge == HudBadge.Danger ? DangerColour : badge == HudBadge.Warning ? WarningColour : NewColour;
                    float line = stripHeight();
                    float radius = System.MathF.Max(3f, line * 0.16f);
                    ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(min.X + rect.W - radius - line * 0.18f, min.Y + rect.H * 0.5f), radius, ImGui.GetColorU32(colour));
                }
            }
            ImGui.End();
            return tapped;
        }

        /** A box from the top down at (x, y), kept off the no-go rects; returns its bottom plus nothing. */
        public float box(InfoBox box, float x, float y, float width) {
            HudRect rect = place(new HudRect(x, y, width, box.getExpectedHeight()));
            return box.draw(rect.X, rect.Y, width);
        }

        /** A box whose bottom sits at bottomY (it grows upward), no higher than minTop, kept off the no-go rects; returns its top. */
        public float boxAbove(InfoBox box, float x, float bottomY, float width, float minTop) {
            float height = box.getExpectedHeight();
            float top = System.MathF.Max(minTop, bottomY - height);
            HudRect rect = place(new HudRect(x, top, width, height));
            box.draw(rect.X, rect.Y, width);
            return rect.Y;
        }

        public float getGap() {
            return gap;
        }
    }
}
