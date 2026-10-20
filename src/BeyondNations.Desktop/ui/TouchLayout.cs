namespace beyondnations.desktop.ui {

    /** The player's choice of on-screen touch controls, in settings. */
    public enum TouchMode {
        /** On when the device is touch-first or has been touched. */
        Auto,
        On,
        Off
    }

    /**
    * Whether the screens lay themselves out for a finger rather than a
    * mouse, and how much of the bottom of the screen the on-screen controls
    * take.
    *
    * Only the browser build sets this (docs/WEB.md, "Touch controls"): the
    * page decides from the device and the player's setting and tells the
    * game. On the desktop it is never available and never active, so every
    * screen draws exactly as it always has; each touch adjustment is behind
    * isActive().
    *
    * What changes when it is active: panels (inventory, nations, crafting,
    * controls, chronicle) get a large Close button and scroll by dragging,
    * the HUD keeps the bottom band clear for the joystick and buttons, and
    * the controls list starts with the touch controls.
    */
    public static class TouchLayout {
        private static bool available;
        private static bool active;
        private static TouchMode mode = TouchMode.Auto;
        private static TouchMode lastPublished = TouchMode.Auto;
        private static float bottomReserve;

        /** Large enough for a fingertip (Apple's and Google's guidance is 44 to 48 points). */
        public const float MinTarget = 44f;

        /** True on a host that can show touch controls (the browser), so settings offer the switch. */
        public static bool isAvailable() {
            return available;
        }

        /** True when the screens should lay out for touch. */
        public static bool isActive() {
            return available && active;
        }

        public static TouchMode getMode() {
            return mode;
        }

        /**
        * CSS pixels at the bottom of the screen the on-screen controls cover
        * (0 when inactive); the HUD keeps its boxes and command bar above it.
        */
        public static float getBottomReserve() {
            return isActive() ? bottomReserve : 0f;
        }

        /** The host's report: what the page shows, under which setting. */
        public static void setState(TouchMode newMode, bool isActiveNow, float reserve) {
            available = true;
            mode = newMode;
            lastPublished = newMode;
            active = isActiveNow;
            bottomReserve = System.MathF.Max(0f, float.IsNaN(reserve) ? 0f : reserve);
        }

        /** The player changed the setting (config or pause screen); the host passes it on. */
        public static void setMode(TouchMode newMode) {
            mode = newMode;
        }

        /** The next setting in Auto, On, Off order, for a button that cycles. */
        public static TouchMode next(TouchMode current) {
            switch (current) {
                case TouchMode.Auto: return TouchMode.On;
                case TouchMode.On: return TouchMode.Off;
                default: return TouchMode.Auto;
            }
        }

        /**
        * Returns the setting once after the player changed it, so the host
        * can tell the page (which stores it); otherwise false.
        */
        public static bool consumeModeChange(out TouchMode changed) {
            changed = mode;
            if (mode == lastPublished) {
                return false;
            }
            lastPublished = mode;
            return true;
        }

        /** "auto", "on" or "off", as the page and the URL spell it. */
        public static string toName(TouchMode value) {
            switch (value) {
                case TouchMode.On: return "on";
                case TouchMode.Off: return "off";
                default: return "auto";
            }
        }

        /** The reverse of toName; anything else is Auto. */
        public static TouchMode parse(string name) {
            switch ((name ?? "").Trim().ToLowerInvariant()) {
                case "on": case "1": case "true": return TouchMode.On;
                case "off": case "0": case "false": return TouchMode.Off;
                default: return TouchMode.Auto;
            }
        }

        /** The settings button's caption: "Touch controls: Auto (on)". */
        public static string describe() {
            string text = "Touch controls: " + (mode == TouchMode.Auto ? "Auto" : mode == TouchMode.On ? "On" : "Off");
            if (mode == TouchMode.Auto && available) {
                text += active ? " (on)" : " (off)";
            }
            return text;
        }

        /** Back to the desktop's state; for tests. */
        public static void reset() {
            available = false;
            active = false;
            mode = TouchMode.Auto;
            lastPublished = TouchMode.Auto;
            bottomReserve = 0f;
        }
    }
}
