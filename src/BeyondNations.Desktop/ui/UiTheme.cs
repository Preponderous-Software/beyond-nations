using System.Numerics;

namespace beyondnations.desktop.ui {

    /** The colours of one kind of button, plain data so a test can measure them. */
    public class ButtonColours {
        public readonly Vector4 Fill;
        public readonly Vector4 Hovered;
        public readonly Vector4 Active;
        public readonly Vector4 Text;

        public ButtonColours(Vector4 fill, Vector4 hovered, Vector4 active, Vector4 text) {
            Fill = fill;
            Hovered = hovered;
            Active = active;
            Text = text;
        }
    }

    /** ImGui's translucent defaults, as the buttons are drawn today. */
    public static class UiTheme {
        private static readonly Vector4 White = new Vector4(1f, 1f, 1f, 1f);

        public static readonly ButtonColours Standard = new ButtonColours(
            new Vector4(0.26f, 0.59f, 0.98f, 0.40f),
            new Vector4(0.26f, 0.59f, 0.98f, 1f),
            new Vector4(0.06f, 0.53f, 0.98f, 1f),
            White);

        public static readonly ButtonColours Drastic = new ButtonColours(
            new Vector4(0.55f, 0.16f, 0.14f, 0.90f),
            new Vector4(0.70f, 0.22f, 0.18f, 1f),
            new Vector4(0.06f, 0.53f, 0.98f, 1f),
            White);

        public static readonly ButtonColours Armed = new ButtonColours(
            new Vector4(0.85f, 0.30f, 0.10f, 1f),
            new Vector4(0.85f, 0.30f, 0.10f, 1f),
            new Vector4(0.06f, 0.53f, 0.98f, 1f),
            White);

        // BeginDisabled multiplies everything by the style's DisabledAlpha, 0.6
        public static readonly ButtonColours Disabled = new ButtonColours(
            new Vector4(0.26f, 0.59f, 0.98f, 0.24f),
            new Vector4(0.26f, 0.59f, 0.98f, 0.24f),
            new Vector4(0.26f, 0.59f, 0.98f, 0.24f),
            new Vector4(1f, 1f, 1f, 0.60f));

        /** How a command bar button is drawn: one that takes no click looks it, whatever else it is. */
        public static ButtonColours commandButton(bool usable, bool armed, bool drastic) {
            if (!usable) {
                return Disabled;
            }
            if (armed) {
                return Armed;
            }
            return drastic ? Drastic : Standard;
        }
    }
}
