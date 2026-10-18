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
        * zero, tells ImGui to fit that axis to the contents.
        */
        public void draw(float x, float y, float width) {
            ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, 0), ImGuiCond.Always);
            if (ImGui.Begin(title, BoxFlags)) {
                foreach (string line in getLines()) {
                    ImGui.TextUnformatted(line);
                }
            }
            ImGui.End();
        }
    }
}
