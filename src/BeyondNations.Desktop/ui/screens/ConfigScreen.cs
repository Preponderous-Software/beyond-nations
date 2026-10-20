using System.Numerics;
using ImGuiNET;
using beyondnations.desktop.ui.feel;

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

        public const string VolumeLabel = "Volume";
        public const string MuteLabel = "Mute Sound";
        public const string TipsLabel = "Show Tips";
        public const string NoSoundNote = "(sound plays in the browser version)";

        public UiAction draw(float width, float height, GameConfig gameConfig) {
            return draw(width, height, gameConfig, null);
        }

        /**
        * feel holds the player's sound and tips settings (ui/feel), drawn as
        * three more rows under the world's; null leaves them out.
        */
        public UiAction draw(float width, float height, GameConfig gameConfig, FeelSettings feel) {
            float centreX = width / 2f;
            float centreY = height / 2f;
            float rowHeight = UiScale.px(RowHeight);
            float widgetWidth = UiScale.px(WidgetWidth);
            float buttonWidth = UiScale.px(ButtonWidth);
            float buttonHeight = UiScale.px(ButtonHeight);
            float rowX = centreX - UiScale.px(150f);
            float firstRowY = centreY + buttonHeight / 2f;
            // With the sound rows there are eight; on a short window (a phone
            // held sideways) the rows close up so the last stays above Back.
            int rowCount = feel != null ? 8 : 5;
            float rowsBottom = height - buttonHeight * 2.5f;
            if (feel != null && firstRowY + rowHeight * (rowCount + 1) > rowsBottom) {
                firstRowY = System.MathF.Max(centreY / 2f + Overlay.lineHeight(), rowsBottom - rowHeight * (rowCount + 1));
                rowHeight = System.MathF.Max(Overlay.frameHeight() + UiScale.px(2f), (rowsBottom - firstRowY) / (rowCount + 1));
            }

            ConfigValues values = ConfigValues.readFrom(gameConfig);
            ConfigValues edited = ConfigValues.readFrom(gameConfig);

            UiAction action = UiAction.None;

            Overlay.beginFullScreen("##config-screen", width, height);
            Overlay.textCentered(Title, height / 15f, centreY / 2f, width);

            ImGui.SetCursorPos(new Vector2(rowX, firstRowY + rowHeight));
            ImGui.SetNextItemWidth(widgetWidth);
            ImGui.SliderInt("Chunk Size", ref edited.ChunkSize, ConfigValues.MinChunkSize, ConfigValues.MaxChunkSize);

            ImGui.SetCursorPos(new Vector2(rowX, firstRowY + rowHeight * 2f));
            ImGui.SetNextItemWidth(widgetWidth);
            ImGui.SliderInt("Location Scale", ref edited.LocationScale, ConfigValues.MinLocationScale, ConfigValues.MaxLocationScale);

            ImGui.SetCursorPos(new Vector2(rowX, firstRowY + rowHeight * 3f));
            ImGui.Checkbox("Respawn Pawns", ref edited.RespawnPawns);

            ImGui.SetCursorPos(new Vector2(rowX, firstRowY + rowHeight * 4f));
            ImGui.Checkbox("Keep Inventory", ref edited.KeepInventoryOnDeath);

            ImGui.SetCursorPos(new Vector2(rowX, firstRowY + rowHeight * 5f));
            ImGui.Checkbox("Lag Prevention", ref edited.LagPreventionEnabled);

            if (feel != null) {
                int volume = (int) System.MathF.Round(feel.getVolume() * 100f);
                ImGui.SetCursorPos(new Vector2(rowX, firstRowY + rowHeight * 6f));
                ImGui.SetNextItemWidth(widgetWidth);
                if (ImGui.SliderInt(VolumeLabel, ref volume, 0, 100)) {
                    feel.setVolume(volume / 100f);
                }
                if (!feel.isSoundAvailable()) {
                    ImGui.SameLine();
                    ImGui.TextUnformatted(NoSoundNote);
                }

                bool muted = feel.isMuted();
                ImGui.SetCursorPos(new Vector2(rowX, firstRowY + rowHeight * 7f));
                if (ImGui.Checkbox(MuteLabel, ref muted)) {
                    feel.setMuted(muted);
                }

                bool tips = feel.areHintsEnabled();
                ImGui.SetCursorPos(new Vector2(rowX, firstRowY + rowHeight * 8f));
                if (ImGui.Checkbox(TipsLabel, ref tips)) {
                    // turning tips back on starts them over in the next new world
                    if (tips) {
                        feel.resetHints();
                    } else {
                        feel.setHintsEnabled(false);
                    }
                }
            }

            if (Overlay.buttonAt("Back", centreX - buttonWidth / 2f, height - buttonHeight * 2f, buttonWidth, buttonHeight)) {
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
