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

        protected InfoBox(string title) {
            this.title = title;
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
                lastHeights[title] = height;
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
            float height;
            if (!lastHeights.TryGetValue(title, out height)) {
                height = ImGui.GetFrameHeightWithSpacing() * (getLines().Count + 1);
            }
            float top = System.MathF.Max(minTopY, bottomY - height);
            draw(x, top, width);
            return top;
        }

        /**
        * The contents of the box. By default one text line per getLines()
        * entry; a box with something richer to show, such as a bar, overrides
        * this and still keeps getLines() for the text a test can check.
        */
        protected virtual void drawBody() {
            foreach (string line in getLines()) {
                ImGui.TextUnformatted(line);
            }
        }

        private static readonly Dictionary<string, float> lastHeights = new Dictionary<string, float>();
    }
}
