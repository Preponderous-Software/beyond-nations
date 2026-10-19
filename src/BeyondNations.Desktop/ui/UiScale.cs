namespace beyondnations.desktop.ui {

    /**
    * How much larger than its 1x design size the UI is drawn: text, padding,
    * buttons and HUD boxes alike.
    *
    * The HUD was laid out in raw framebuffer pixels with ImGui's 13 px font,
    * so on a high-DPI display -- a laptop at 150 % or 200 % on Windows, where
    * GLFW makes the process DPI-aware and the framebuffer is the full panel
    * resolution -- every label came out a half or a third of its intended
    * size. The desktop host now asks the window for its content scale and
    * sets it here; the browser keeps 1, because it already lays the UI out in
    * CSS pixels and rasterises the font for devicePixelRatio itself.
    */
    public static class UiScale {
        public const float Min = 1f;
        public const float Max = 4f;

        private static float scale = 1f;

        public static float get() {
            return scale;
        }

        public static void set(float value) {
            scale = clamp(value);
        }

        /** A length in 1x design pixels, in the pixels the UI is drawn in. */
        public static float px(float designPixels) {
            return designPixels * scale;
        }

        /**
        * Chooses the scale. An explicit override (from --ui-scale) wins when
        * it is positive. Otherwise the larger of the monitor's content scale
        * and the framebuffer-to-window ratio, since macOS reports the latter
        * and Windows and X11 the former. Rounded to a quarter so the font is
        * rasterised at a sensible size, and kept within [Min, Max].
        */
        public static float resolve(float overrideScale, float contentScale, float framebufferToWindowRatio) {
            if (overrideScale > 0f && !float.IsNaN(overrideScale) && !float.IsInfinity(overrideScale)) {
                return clamp(overrideScale);
            }
            float detected = 1f;
            if (contentScale > detected && !float.IsInfinity(contentScale)) {
                detected = contentScale;
            }
            if (framebufferToWindowRatio > detected && !float.IsInfinity(framebufferToWindowRatio)) {
                detected = framebufferToWindowRatio;
            }
            return clamp(detected);
        }

        private static float clamp(float value) {
            if (float.IsNaN(value)) {
                return 1f;
            }
            float rounded = System.MathF.Round(value * 4f) / 4f;
            return System.Math.Clamp(rounded, Min, Max);
        }
    }
}
