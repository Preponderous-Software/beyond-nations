using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui {

    /**
    * A titled panel of read-only lines, ported from Assets/Scripts/ui/InfoBox.cs.
    *
    * The Unity version carried an x, y, width, height and padding and each
    * subclass drew its own GUI.Box and then stepped a cursor down by twenty
    * pixels per GUI.Label. ImGui lays a window out itself, so the subclasses no
    * longer own any geometry: they produce the lines, and this base draws them.
    *
    * Splitting it that way is also what makes the boxes checkable. getLines()
    * needs no window and no GL context, so the exact text of every field --
    * which is what the acceptance criterion about the debug overlay is really
    * about -- is asserted in a unit test, and only the drawing needs a screen.
    */
    public abstract class InfoBox {
        private const ImGuiWindowFlags BoxFlags =
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoInputs;

        private readonly string title;
        private bool compact;

        protected InfoBox(string title) {
            this.title = title;
        }

        /** The box as one line, for a chip on the HUD; by default the title. */
        public virtual string getChipText() { return title; }

        /** What a chip of this box must not hide: a warning or a danger (HudBadge), or None. */
        public virtual HudBadge getBadge() { return HudBadge.None; }

        /**
        * Fewer lines, for a column with no room for every box in full (a
        * settlement's boxes at 960x540, playtest 0.5.0/0.6.0 #350). What a
        * box leaves out is its own choice; by default nothing.
        */
        public InfoBox setCompact(bool compact) {
            this.compact = compact;
            return this;
        }

        public bool isCompact() {
            return compact;
        }

        /**
        * What a box's height is estimated from, as plain numbers, so the
        * estimate can be checked without ImGui (current() reads the style).
        */
        public struct Metrics {
            /** A text row and the spacing under it. */
            public float LineHeight;
            /** A framed row, such as the energy bar, and the spacing under it. */
            public float FrameRowHeight;
            public float PaddingX;
            public float PaddingY;
            /** The width of a string as drawn. */
            public System.Func<string, float> Measure;

            public static Metrics current() {
                ImGuiStylePtr style = ImGui.GetStyle();
                return new Metrics {
                    LineHeight = Overlay.lineHeight(),
                    FrameRowHeight = Overlay.frameHeight() + style.ItemSpacing.Y,
                    PaddingX = style.WindowPadding.X,
                    PaddingY = style.WindowPadding.Y,
                    Measure = text => ImGui.CalcTextSize(text).X,
                };
            }
        }

        /**
        * The height the box takes at boxWidth, every line wrapped as
        * drawBody wraps it. Counting one row a line, as this used to, judged
        * a settlement's four boxes to fit at 1280x720 and 960x540 when the
        * wrapped lines made them too tall, so the season box was drawn over
        * the player box (#350, playtest 0.7.0 s09).
        */
        public float estimateHeight(float boxWidth, Metrics metrics) {
            float textWidth = getTextWidth(boxWidth, metrics);
            List<string> lines = getLines();
            int rows = 1; // the title bar
            for (int i = 0; i < lines.Count; i++) {
                rows += countRows(i, lines[i], textWidth, metrics.Measure);
            }
            return metrics.LineHeight * rows + metrics.PaddingY * 2f + getExtraHeight(metrics);
        }

        /** The width lines are wrapped to in a box boxWidth wide, as drawBody finds it. */
        public static float getTextWidth(float boxWidth, Metrics metrics) {
            return System.MathF.Max(40f, boxWidth - metrics.PaddingX * 2f);
        }

        /** The rows line index takes once wrapped; at least one. */
        protected virtual int countRows(int index, string line, float textWidth, System.Func<string, float> measure) {
            return System.Math.Max(1, TextWrap.wrap(line, textWidth, measure).Count);
        }

        /** Height drawBody adds beyond its text rows (a bar drawn in place of a line). */
        protected virtual float getExtraHeight(Metrics metrics) {
            return 0f;
        }

        private string heightKey() {
            return compact ? title + "#compact" : title;
        }

        public string getTitle() {
            return title;
        }

        /**
        * The contents of the box, one string per line, in the order the Unity
        * version drew them.
        */
        public abstract List<string> getLines();

        /**
        * Draws the box at the given position. A width of zero, or a height of
        * zero, tells ImGui to fit that axis to the contents. Returns the y of
        * the box's bottom edge, so a caller can stack the next box under it
        * rather than guessing its height.
        */
        public float draw(float x, float y, float width) {
            ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, 0), ImGuiCond.Always);
            float bottom = y;
            if (ImGui.Begin(title, BoxFlags)) {
                drawBody();
                float height = ImGui.GetWindowSize().Y;
                lastHeights[heightKey()] = height;
                bottom = ImGui.GetWindowPos().Y + height;
            }
            ImGui.End();
            return bottom;
        }

        /**
        * Draws the box so that its bottom edge sits at bottomY, unless that
        * would lift its top above minTopY, in which case it starts at minTopY
        * instead. Returns the top y it was drawn at. The height used is the
        * one this box had last frame (ImGui sizes a window to its contents a
        * frame late anyway), so a box anchored to the bottom of the screen
        * grows upwards as lines are added rather than down off the screen.
        */
        public float drawAbove(float x, float bottomY, float width, float minTopY) {
            float height = getExpectedHeight();
            float top = System.MathF.Max(minTopY, bottomY - height);
            draw(x, top, width);
            return top;
        }

        /**
        * How tall the box was when last drawn, or an estimate from its line
        * count before it has been drawn at all.
        */
        public float getExpectedHeight() {
            float height;
            if (!lastHeights.TryGetValue(heightKey(), out height)) {
                height = Overlay.lineHeight() * (getLines().Count + 1) + ImGui.GetStyle().WindowPadding.Y * 2f;
            }
            return height;
        }

        /**
        * The contents of the box. By default one text line per getLines()
        * entry; a box with something richer to show, such as a bar, overrides
        * this and still keeps getLines() for the text a test can check.
        */
        protected virtual void drawBody() {
            // wrapped to the box, so a long line ("Next (K): Workshop, 15
            // wood, 10 stone") is not cut off at the window's edge (#350)
            float width = System.MathF.Max(40f, ImGui.GetContentRegionAvail().X);
            foreach (string line in getLines()) {
                foreach (string part in TextWrap.wrap(line, width, text => ImGui.CalcTextSize(text).X)) {
                    ImGui.TextUnformatted(part);
                }
            }
        }

        private static readonly Dictionary<string, float> lastHeights = new Dictionary<string, float>();
    }
}
