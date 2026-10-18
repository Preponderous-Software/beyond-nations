using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui.screens {

    /**
    * The config screen, ported from Assets/Scripts/screens/ConfigScreen.cs.
    *
    * The five settings are the same five, with the same labels. What changes is
    * how they are edited: the Unity screen drew a button whose caption was the
    * current value and cycled it on every click, which is all OnGUI made
    * practical. Sliders and checkboxes say the same thing in a form that can be
    * read at a glance and moved in either direction.
    *
    * The values are read out of the live GameConfig every frame and written
    * back the moment a widget changes one, so the running simulation sees the
    * change exactly as it did before.
    */
    public class ConfigScreen {
        public const string Title = "Config";

        private const float RowHeight = 28f;
        private const float WidgetWidth = 160f;
        private const float ButtonWidth = 100f;
        private const float ButtonHeight = 20f;

        public UiAction draw(float width, float height, GameConfig gameConfig) {
            float centreX = width / 2f;
            float centreY = height / 2f;
            float rowX = centreX - 150f;
            float firstRowY = centreY + ButtonHeight / 2f;

            ConfigValues values = ConfigValues.readFrom(gameConfig);
            ConfigValues edited = ConfigValues.readFrom(gameConfig);

            UiAction action = UiAction.None;

            Overlay.beginFullScreen("##config-screen", width, height);
            Overlay.textCentered(Title, height / 15f, centreY / 2f, width);

            ImGui.SetCursorPos(new Vector2(rowX, firstRowY + RowHeight));
            ImGui.SetNextItemWidth(WidgetWidth);
            ImGui.SliderInt("Chunk Size", ref edited.ChunkSize, ConfigValues.MinChunkSize, ConfigValues.MaxChunkSize);

            ImGui.SetCursorPos(new Vector2(rowX, firstRowY + RowHeight * 2f));
            ImGui.SetNextItemWidth(WidgetWidth);
            ImGui.SliderInt("Location Scale", ref edited.LocationScale, ConfigValues.MinLocationScale, ConfigValues.MaxLocationScale);

            ImGui.SetCursorPos(new Vector2(rowX, firstRowY + RowHeight * 3f));
            ImGui.Checkbox("Respawn Pawns", ref edited.RespawnPawns);

            ImGui.SetCursorPos(new Vector2(rowX, firstRowY + RowHeight * 4f));
            ImGui.Checkbox("Keep Inventory", ref edited.KeepInventoryOnDeath);

            ImGui.SetCursorPos(new Vector2(rowX, firstRowY + RowHeight * 5f));
            ImGui.Checkbox("Lag Prevention", ref edited.LagPreventionEnabled);

            if (Overlay.buttonAt("Back", centreX - ButtonWidth / 2f, height - ButtonHeight * 2f, ButtonWidth, ButtonHeight)) {
                action = UiAction.Back;
            }
            Overlay.end();

            if (edited.differsFrom(values)) {
                edited.applyTo(gameConfig);
            }

            return action;
        }
    }
}
