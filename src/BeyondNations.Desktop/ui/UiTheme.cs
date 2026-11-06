using System.Numerics;
using ImGuiNET;

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

    /**
    * What buttons look like. They are drawn over the world, so a see-through
    * button takes its contrast from whatever the camera is looking at: every
    * colour here is opaque, and its text reaches 4.5:1 (TestUiTheme measures
    * it). ImGui's translucent default fell to about 1.8:1 over sunlit grass.
    */
    public static class UiTheme {
        private static readonly Vector4 White = new Vector4(1f, 1f, 1f, 1f);

        public static readonly ButtonColours Standard = new ButtonColours(
            new Vector4(0.11f, 0.25f, 0.38f, 1f),
            new Vector4(0.16f, 0.34f, 0.50f, 1f),
            new Vector4(0.08f, 0.19f, 0.30f, 1f),
            White);

        /** Hostile and irreversible buttons are drawn red; armed, brighter. */
        public static readonly ButtonColours Drastic = new ButtonColours(
            new Vector4(0.55f, 0.16f, 0.14f, 1f),
            new Vector4(0.66f, 0.20f, 0.17f, 1f),
            new Vector4(0.45f, 0.13f, 0.11f, 1f),
            White);

        public static readonly ButtonColours Armed = new ButtonColours(
            new Vector4(0.75f, 0.26f, 0.09f, 1f),
            new Vector4(0.75f, 0.26f, 0.09f, 1f),
            new Vector4(0.62f, 0.21f, 0.07f, 1f),
            White);

        /**
        * Grey on grey. Draw it with the style's DisabledAlpha pushed to 1:
        * ImGui's usual 0.6 would make it see-through again.
        */
        public static readonly ButtonColours Disabled = new ButtonColours(
            new Vector4(0.17f, 0.17f, 0.18f, 1f),
            new Vector4(0.17f, 0.17f, 0.18f, 1f),
            new Vector4(0.17f, 0.17f, 0.18f, 1f),
            new Vector4(0.66f, 0.66f, 0.66f, 1f));

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

        /**
        * Makes Standard every button's colour, in the current ImGui context:
        * the menus and crafting rows as well as the command bar. Both hosts
        * call this once, after creating the context.
        */
        public static void apply() {
            ImGuiStylePtr style = ImGui.GetStyle();
            style.Colors[(int) ImGuiCol.Button] = Standard.Fill;
            style.Colors[(int) ImGuiCol.ButtonHovered] = Standard.Hovered;
            style.Colors[(int) ImGuiCol.ButtonActive] = Standard.Active;
        }
    }
}
