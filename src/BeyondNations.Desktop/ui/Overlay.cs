using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui {

    /**
    * The handful of drawing primitives the ported screens need.
    *
    * The Unity screens positioned everything themselves, in pixels, against
    * Screen.width and Screen.height, and sized their fonts as a fraction of the
    * screen height. Those positions are worth keeping -- they are the layout the
    * game has -- so rather than rewrite each screen around ImGui's automatic
    * layout, each one opens a borderless full-screen window and places its
    * widgets at the same coordinates. These helpers do the placing.
    */
    public static class Overlay {

        /**
        * The height in pixels of ImGui's built-in font at scale 1. Font sizes
        * in the Unity screens were fractions of the screen height, so they
        * become a scale factor against this.
        */
        public const float BaseFontSize = 13f;

        private const ImGuiWindowFlags FullScreenFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoBringToFrontOnFocus;

        /**
        * A window the size of the screen with no frame and no background, so
        * that widgets placed in it sit directly over the rendered world.
        */
        public static void beginFullScreen(string id, float width, float height) {
            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
            ImGui.Begin(id, FullScreenFlags);
        }

        public static void end() {
            ImGui.End();
        }

        /**
        * Converts a Unity font size in pixels into an ImGui font scale.
        */
        public static float fontScaleFor(float fontSizeInPixels) {
            return fontSizeInPixels / BaseFontSize;
        }

        /**
        * Draws text centred on the point (width / 2, centreY), which is what
        * a GUI.Label with TextAnchor.MiddleCenter did inside its rect.
        */
        public static void textCentered(string text, float fontSize, float centreY, float width) {
            float scale = fontScaleFor(fontSize);
            ImGui.SetWindowFontScale(scale);
            Vector2 size = ImGui.CalcTextSize(text);
            ImGui.SetCursorPos(new Vector2((width - size.X) * 0.5f, centreY - size.Y * 0.5f));
            ImGui.TextUnformatted(text);
            ImGui.SetWindowFontScale(1f);
        }

        public static void textAt(string text, float x, float y) {
            ImGui.SetCursorPos(new Vector2(x, y));
            ImGui.TextUnformatted(text);
        }

        public static bool buttonAt(string label, float x, float y, float width, float height) {
            ImGui.SetCursorPos(new Vector2(x, y));
            return ImGui.Button(label, new Vector2(width, height));
        }
    }
}
