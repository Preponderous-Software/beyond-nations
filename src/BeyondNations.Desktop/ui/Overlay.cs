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
        * The height in pixels of ImGui's built-in font at a UI scale of 1.
        */
        public const float DesignFontSize = 13f;

        /**
        * The height in pixels of the UI font as it is rasterised: the design
        * size times UiScale, since the desktop host builds the font atlas at
        * that size on a high-DPI display. Font sizes in the Unity screens
        * were fractions of the screen height, so they become a scale factor
        * against this.
        */
        public static float BaseFontSize {
            get { return DesignFontSize * UiScale.get(); }
        }

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

        /**
        * Darkens the whole screen behind the widgets that follow, so a screen
        * laid over the world reads as a panel in front of it rather than as
        * text floating on the terrain. Call straight after beginFullScreen.
        */
        public static void dimBackground(float width, float height) {
            ImGui.GetWindowDrawList().AddRectFilled(
                Vector2.Zero, new Vector2(width, height), ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.6f)));
        }

        /**
        * The height of one line of text plus the spacing after it, and of a
        * framed widget such as a button, worked out from the style rather
        * than asked of ImGui: the browser build links cimgui statically and
        * the shared UI keeps to the ImGui calls it is known to run there.
        */
        public static float lineHeight() {
            return BaseFontSize + ImGui.GetStyle().ItemSpacing.Y;
        }

        public static float frameHeight() {
            return BaseFontSize + ImGui.GetStyle().FramePadding.Y * 2f;
        }

        /**
        * Fills a rectangle given in window-local coordinates (the ones
        * SetCursorPos takes) in the current window.
        */
        public static void fillLocalRect(Vector2 localMin, Vector2 size, Vector4 colour) {
            Vector2 origin = ImGui.GetWindowPos();
            ImGui.GetWindowDrawList().AddRectFilled(origin + localMin, origin + localMin + size, ImGui.GetColorU32(colour));
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

        /**
        * A centred column of menu buttons starting at topY, all as wide as the
        * widest label (and at least 160 design pixels), each 40 design pixels
        * tall unless that would run past maxBottom, in which case the buttons
        * and the gaps shrink together so the last one still fits. The main
        * menu used fixed 50 px buttons two heights apart, so on a window under
        * about 520 px high Exit was cut off at the bottom. Returns the index
        * of the button clicked this frame, or -1.
        */
        public static int menuButtons(string[] labels, float width, float topY, float maxBottom) {
            float ignored;
            return menuButtons(labels, width, topY, maxBottom, out ignored);
        }

        public static int menuButtons(string[] labels, float width, float topY, float maxBottom, out float bottom) {
            float padding = ImGui.GetStyle().FramePadding.X * 2f;
            float buttonWidth = UiScale.px(160f);
            foreach (string label in labels) {
                buttonWidth = System.MathF.Max(buttonWidth, ImGui.CalcTextSize(label).X + padding * 2f);
            }
            float buttonHeight = UiScale.px(40f);
            float gap = UiScale.px(12f);
            float needed = labels.Length * buttonHeight + (labels.Length - 1) * gap;
            float available = maxBottom - topY;
            if (needed > available && available > 0f) {
                float shrink = available / needed;
                buttonHeight = System.MathF.Max(frameHeight(), buttonHeight * shrink);
                gap = System.MathF.Max(UiScale.px(2f), gap * shrink);
            }
            int clicked = -1;
            float y = topY;
            for (int i = 0; i < labels.Length; i++) {
                if (buttonAt(labels[i], (width - buttonWidth) / 2f, y, buttonWidth, buttonHeight)) {
                    clicked = i;
                }
                y += buttonHeight + gap;
            }
            bottom = y - gap;
            return clicked;
        }

        /**
        * Left-aligned text at (x, y) at a Unity-style font size, using the
        * same calls textCentered does.
        */
        public static void textAtScaled(string text, float fontSize, float x, float y) {
            ImGui.SetWindowFontScale(fontScaleFor(fontSize));
            ImGui.SetCursorPos(new Vector2(x, y));
            ImGui.TextUnformatted(text);
            ImGui.SetWindowFontScale(1f);
        }

        public static bool buttonAt(string label, float x, float y, float width, float height) {
            ImGui.SetCursorPos(new Vector2(x, y));
            return ImGui.Button(label, new Vector2(width, height));
        }
    }
}
