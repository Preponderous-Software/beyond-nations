using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using beyondnations.desktop.input;

namespace beyondnations.desktop.ui.screens {

    /**
    * Every key binding, listed in the game. Opened with ? from the world, or
    * with the Controls button on the HUD, the pause screen and the main menu,
    * so a player on a touch screen can reach it too.
    *
    * Before this the bindings were written down only in the README, so a
    * player who had not read it had no way to find out that N creates a
    * nation or that Page Up raises the render distance. The rows are built
    * from KeyBindings rather than typed out, so a rebinding shows up here
    * without anyone updating a second list, and a test checks that every
    * binding has a row.
    */
    public class ControlsScreen {
        public const string Title = "CONTROLS";
        public const string CloseHint = "(Press ? or ESCAPE to close)";
        public const string CloseLabel = "Close";

        public struct Row {
            public readonly string Section;
            public readonly string Keys;
            public readonly string Action;

            public Row(string section, string keys, string action) {
                Section = section;
                Keys = keys;
                Action = action;
            }
        }

        public static List<Row> getRows() {
            List<Row> rows = new List<Row>();
            const string move = "Moving";
            rows.Add(new Row(move, keys(KeyBindings.MoveForward, KeyBindings.MoveForwardAlt), "Walk forward"));
            rows.Add(new Row(move, keys(KeyBindings.MoveBackward, KeyBindings.MoveBackwardAlt), "Walk back"));
            rows.Add(new Row(move, keys(KeyBindings.MoveLeft, KeyBindings.MoveLeftAlt), "Turn left"));
            rows.Add(new Row(move, keys(KeyBindings.MoveRight, KeyBindings.MoveRightAlt), "Turn right"));
            rows.Add(new Row(move, keys(KeyBindings.Sprint), "Run (hold)"));
            rows.Add(new Row(move, keys(KeyBindings.Jump), "Jump"));
            rows.Add(new Row(move, keys(KeyBindings.ToggleAutoWalk), "Auto-walk on / off"));

            const string act = "Doing things";
            rows.Add(new Row(act, keys(KeyBindings.Interact), "Gather, talk, pick up, enter or leave"));
            rows.Add(new Row(act, keys(KeyBindings.GiftToPawn), "Give a gift to the pawn within reach"));
            rows.Add(new Row(act, keys(KeyBindings.InviteToNation), "Invite the pawn within reach to your nation"));
            rows.Add(new Row(act, keys(KeyBindings.PlantSapling), "Plant a sapling"));
            rows.Add(new Row(act, keys(KeyBindings.ToggleInventory), "Inventory"));

            const string nation = "Nations and settlements";
            rows.Add(new Row(nation, keys(KeyBindings.CreateNewNation), "Create a nation"));
            rows.Add(new Row(nation, keys(KeyBindings.JoinNation), "Join a nation"));
            rows.Add(new Row(nation, keys(KeyBindings.LeaveNation), "Leave your nation"));
            rows.Add(new Row(nation, keys(KeyBindings.FoundSettlement), "Found a settlement (leader)"));
            rows.Add(new Row(nation, keys(KeyBindings.BuildStall), "Build a market stall (leader)"));
            rows.Add(new Row(nation, keys(KeyBindings.TeleportToHomeSettlement), "Teleport home"));

            const string view = "View and game";
            rows.Add(new Row(view, "Right mouse (hold)", "Look around"));
            rows.Add(new Row(view, keys(KeyBindings.ToggleCameraView), "First / third person"));
            rows.Add(new Row(view, keys(KeyBindings.IncreaseRenderDistance, KeyBindings.DecreaseRenderDistance), "Render distance up / down"));
            rows.Add(new Row(view, keys(KeyBindings.TakeScreenshot), "Screenshot"));
            rows.Add(new Row(view, keys(KeyBindings.ShowControls), "This list"));
            rows.Add(new Row(view, keys(KeyBindings.Pause), "Pause menu / back"));

            const string debug = "Debug";
            rows.Add(new Row(debug, keys(KeyBindings.ToggleDebugMode), "Debug overlay on / off"));
            rows.Add(new Row(debug, keys(KeyBindings.GenerateNearbyLand), "Generate nearby land"));
            rows.Add(new Row(debug, keys(KeyBindings.SpawnNewPawn), "Spawn a pawn"));
            rows.Add(new Row(debug, keys(KeyBindings.SpawnMoney), "Spawn coins"));
            rows.Add(new Row(debug, keys(KeyBindings.SpawnWood), "Spawn wood"));
            rows.Add(new Row(debug, keys(KeyBindings.TeleportAllToPlayer), "Teleport every pawn here"));
            return rows;
        }

        private static string keys(params Key[] bound) {
            List<string> names = new List<string>();
            foreach (Key key in bound) {
                names.Add(KeyNames.describe(key));
            }
            return string.Join(" / ", names);
        }

        public UiAction draw(float width, float height) {
            UiAction action = UiAction.None;

            Overlay.beginFullScreen("##controls-screen", width, height);
            Overlay.dimBackground(width, height);

            float lineHeight = ImGui.GetTextLineHeightWithSpacing();
            float margin = UiScale.px(16f);
            float titleSize = Overlay.BaseFontSize * 2f;
            Overlay.textCentered(Title, titleSize, margin + titleSize / 2f, width);
            float top = margin * 1.5f + titleSize;

            // The key column is as wide as the widest key name, so long names
            // such as "Page Up / Page Down" never run into the action. Two
            // columns of sections when the window is wide enough, one (which
            // scrolls) when it is not.
            List<Row> rows = getRows();
            float keyColumn = 0f;
            float actionColumn = 0f;
            int sections = 0;
            string lastSection = null;
            foreach (Row row in rows) {
                keyColumn = System.MathF.Max(keyColumn, ImGui.CalcTextSize(row.Keys).X);
                actionColumn = System.MathF.Max(actionColumn, ImGui.CalcTextSize(row.Action).X);
                if (row.Section != lastSection) {
                    sections++;
                    lastSection = row.Section;
                }
            }
            float gutter = UiScale.px(14f);
            float columnWidth = keyColumn + gutter + actionColumn;
            int columns = width >= columnWidth * 2f + margin * 3f ? 2 : 1;
            int linesPerColumn = (rows.Count + sections * 2 + columns - 1) / columns;
            float totalWidth = columns * columnWidth + (columns - 1) * margin;
            float left = System.MathF.Max(margin, (width - totalWidth) / 2f);

            float buttonWidth = UiScale.px(120f);
            float buttonHeight = UiScale.px(32f);
            float buttonY = height - buttonHeight - margin;
            float listHeight = System.MathF.Max(lineHeight * 3f, buttonY - lineHeight * 1.5f - top);

            ImGui.SetCursorPos(new Vector2(left, top));
            ImGui.BeginChild("##controls-list", new Vector2(System.MathF.Max(columnWidth, width - left * 2f), listHeight),
                ImGuiChildFlags.None, ImGuiWindowFlags.NoBackground);
            Vector4 sectionColour = new Vector4(1f, 0.85f, 0.45f, 1f);
            Vector4 keyColour = new Vector4(0.65f, 0.85f, 1f, 1f);
            float columnX = 0f;
            float rowY = 0f;
            int linesInColumn = 0;
            lastSection = null;
            for (int i = 0; i < rows.Count; i++) {
                Row row = rows[i];
                if (row.Section != lastSection) {
                    // A section is never split across the two columns.
                    int sectionLines = 2;
                    for (int j = i; j < rows.Count && rows[j].Section == row.Section; j++) {
                        sectionLines++;
                    }
                    if (columns > 1 && columnX == 0f && linesInColumn > 0 && linesInColumn + sectionLines > linesPerColumn + 2) {
                        columnX = columnWidth + margin;
                        rowY = 0f;
                        linesInColumn = 0;
                    }
                    if (linesInColumn > 0) {
                        rowY += lineHeight;
                    }
                    ImGui.SetCursorPos(new Vector2(columnX, rowY));
                    ImGui.TextColored(sectionColour, row.Section);
                    rowY += lineHeight;
                    linesInColumn += 2;
                    lastSection = row.Section;
                }
                ImGui.SetCursorPos(new Vector2(columnX, rowY));
                ImGui.TextColored(keyColour, row.Keys);
                ImGui.SetCursorPos(new Vector2(columnX + keyColumn + gutter, rowY));
                ImGui.TextUnformatted(row.Action);
                rowY += lineHeight;
                linesInColumn++;
            }
            ImGui.EndChild();

            Overlay.textCentered(CloseHint, Overlay.BaseFontSize, buttonY - lineHeight * 0.75f, width);
            if (Overlay.buttonAt(CloseLabel, (width - buttonWidth) / 2f, buttonY, buttonWidth, buttonHeight)) {
                action = UiAction.CloseControls;
            }
            Overlay.end();
            return action;
        }
    }
}
