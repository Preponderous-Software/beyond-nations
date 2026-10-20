using ImGuiNET;
using System.Numerics;

namespace beyondnations.desktop.ui.screens {

    /**
    * Between Start and the world (docs/SHARING.md): an optional seed, then
    * Begin. The field takes a number or any text -- "Ironvale" is hashed to a
    * seed (WorldSeed) -- and empty means a fresh world, as Start always gave.
    * Random fills the field with a fresh seed, so it can be read out or
    * shared before the world is built. A seed from the command line or a
    * share link (?seed=42) is already in the field.
    *
    * Begin sits where Start was on the main menu, so a player who taps
    * twice where Start was lands in a world, as before. On a phone a canvas
    * cannot open the keyboard, so the field is a button there that asks for
    * the seed with the browser's own prompt (ISharePlatform.promptsForText).
    */
    public class NewWorldScreen {
        public const string Title = "New World";
        public const string BeginLabel = "Begin";
        public const string RandomLabel = "Random seed";
        public const string BackLabel = "Back";
        public const string FieldLabel = "World seed (optional): a number or any word";
        public const string FieldHint = "empty for a fresh world";
        public const int MaxSeedLength = 64;

        private static readonly string[] ButtonLabels = { BeginLabel, RandomLabel, BackLabel };
        private static readonly UiAction[] ButtonActions = { UiAction.BeginWorld, UiAction.RollSeed, UiAction.Back };

        public UiAction draw(float width, float height, ShareSession share) {
            float centreY = height / 2f;
            float fieldWidth = System.MathF.Min(width - UiScale.px(32f), UiScale.px(360f));
            float fieldX = (width - fieldWidth) / 2f;
            float fieldHeight = UiScale.px(36f);
            // The field and the line under it sit just above the buttons,
            // which start where the main menu's Start does.
            float describeY = centreY - Overlay.lineHeight() * 0.25f;
            float fieldY = describeY - Overlay.lineHeight() * 0.75f - fieldHeight;

            Overlay.beginFullScreen("##new-world-screen", width, height);
            // no wider than a phone held upright
            Overlay.textCentered(Title, System.MathF.Min(height / 15f, width / 11f), centreY / 2f, width);

            Overlay.textCentered(FieldLabel, Overlay.BaseFontSize, fieldY - Overlay.lineHeight() * 0.6f, width);
            UiAction action = UiAction.None;
            if (share.getPlatform().promptsForText()) {
                string current = share.getSeedText();
                string label = current.Length > 0 ? "Seed: " + current : "Type a seed (optional)";
                if (Overlay.buttonAt(label + "##seed", fieldX, fieldY, fieldWidth, fieldHeight)) {
                    share.promptForSeed();
                }
            } else {
                string text = share.getSeedText();
                ImGui.SetCursorPos(new Vector2(fieldX, fieldY + (fieldHeight - Overlay.frameHeight()) / 2f));
                ImGui.SetNextItemWidth(fieldWidth);
                if (ImGui.InputTextWithHint("##seed", FieldHint, ref text, MaxSeedLength, ImGuiInputTextFlags.EnterReturnsTrue)) {
                    action = UiAction.BeginWorld;
                }
                share.setSeedText(text);
            }
            Overlay.textCentered(share.describeChosenSeed(), Overlay.BaseFontSize, describeY, width);

            int clicked = Overlay.menuButtons(ButtonLabels, width, centreY + UiScale.px(25f), height - UiScale.px(16f));
            Overlay.end();

            if (clicked >= 0) {
                action = ButtonActions[clicked];
            }
            return action;
        }
    }
}
