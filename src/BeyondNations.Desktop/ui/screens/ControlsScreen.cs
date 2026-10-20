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

        public const string TouchSection = "Touch screen";

        public static List<Row> getRows() {
            return getRows(false);
        }

        /**
        * With touch, the on-screen controls come first (TouchLayout,
        * docs/WEB.md): a player on a phone reads what is under their thumbs
        * before the keyboard list, which still follows for a keyboard
        * attached later.
        */
        public static List<Row> getRows(bool touch) {
            List<Row> rows = new List<Row>();
            if (touch) {
                rows.Add(new Row(TouchSection, "Left stick", "Walk; push sideways to turn"));
                rows.Add(new Row(TouchSection, "Drag the world", "Steer and tilt the view"));
                rows.Add(new Row(TouchSection, "Tap the world", "Use whatever is within reach"));
                rows.Add(new Row(TouchSection, "Use", "Gather, talk, pick up, enter"));
                rows.Add(new Row(TouchSection, "Jump / Run", "Jump; run on or off"));
                rows.Add(new Row(TouchSection, "Button row", "Bag, Craft, Realm (nations), News, View, ?, Menu"));
                rows.Add(new Row(TouchSection, "Drag a panel", "Scroll it; Close (top right) shuts it"));
            }
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
            rows.Add(new Row(act, keys(KeyBindings.ToggleCrafting), "Crafting: tools, cooked meat, planks, bricks"));

            const string nation = "Nations and settlements";
            rows.Add(new Row(nation, keys(KeyBindings.CreateNewNation), "Create a nation"));
            rows.Add(new Row(nation, keys(KeyBindings.JoinNation), "Join a nation"));
            rows.Add(new Row(nation, keys(KeyBindings.LeaveNation), "Leave your nation"));
            rows.Add(new Row(nation, keys(KeyBindings.FoundSettlement), "Found a settlement (leader)"));
            rows.Add(new Row(nation, keys(KeyBindings.BuildStall), "Build a market stall (leader)"));
            rows.Add(new Row(nation, keys(KeyBindings.BuildBuilding), "Build the settlement's next building (leader)"));
            rows.Add(new Row(nation, keys(KeyBindings.TeleportToHomeSettlement), "Teleport home"));
            rows.Add(new Row(nation, keys(KeyBindings.ToggleNations), "Nations and laws: tribute, alliances, war and peace"));

            const string view = "View and game";
            rows.Add(new Row(view, "Right mouse (hold)", "Steer and tilt the view"));
            rows.Add(new Row(view, keys(KeyBindings.ToggleCameraView), "First / third person"));
            rows.Add(new Row(view, keys(KeyBindings.ToggleChronicle), "Chronicle: recent, full history, hidden"));
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

        public const string PreviousLabel = "< Prev";
        public const string NextLabel = "Next >";

        private int page;

        /**
        * Fills columns of at most linesPerColumn lines (a section heading,
        * and the blank line before every heading but a column's first, count
        * as lines), starting a new column rather than splitting a section
        * unless the section is taller than a whole column. Returns the column
        * each row goes in. Kept apart from drawing so it can be tested.
        */
        public static int[] assignColumns(List<Row> rows, int linesPerColumn) {
            linesPerColumn = System.Math.Max(2, linesPerColumn);
            int[] column = new int[rows.Count];
            int current = 0;
            int lines = 0;
            string last = null;
            for (int i = 0; i < rows.Count; i++) {
                if (rows[i].Section != last) {
                    int sectionLines = 1;
                    for (int j = i; j < rows.Count && rows[j].Section == rows[i].Section; j++) {
                        sectionLines++;
                    }
                    int gap = lines > 0 ? 1 : 0;
                    if (lines > 0 && lines + gap + sectionLines > linesPerColumn) {
                        current++;
                        lines = 0;
                        gap = 0;
                    }
                    lines += gap + 1;
                    last = rows[i].Section;
                } else if (lines >= linesPerColumn) {
                    current++;
                    lines = 1;
                }
                column[i] = current;
                lines++;
            }
            return column;
        }

        public UiAction draw(float width, float height) {
            UiAction action = UiAction.None;

            Overlay.beginFullScreen("##controls-screen", width, height);
            Overlay.dimBackground(width, height);

            float margin = UiScale.px(16f);
            float titleSize = Overlay.BaseFontSize * 2f;
            Overlay.textCentered(Title, titleSize, margin + titleSize / 2f, width);
            float top = margin * 1.5f + titleSize;

            float buttonWidth = UiScale.px(120f);
            float buttonHeight = UiScale.px(TouchLayout.isActive() ? TouchLayout.MinTarget : 32f);
            float buttonY = height - buttonHeight - margin;
            float listBottom = buttonY - Overlay.lineHeight() * 1.5f;
            float gutter = UiScale.px(14f);
            float lineHeight = Overlay.lineHeight();

            // As many columns as fit across (up to three), each as tall as
            // the space above the Close button. What does not fit goes on
            // further pages behind Prev and Next, so a short window or a
            // phone shows every binding at a readable size rather than
            // cutting the list off.
            List<Row> rows = getRows(TouchLayout.isActive());
            float keyColumn = 0f;
            float actionColumn = 0f;
            foreach (Row row in rows) {
                keyColumn = System.MathF.Max(keyColumn, ImGui.CalcTextSize(row.Keys).X);
                actionColumn = System.MathF.Max(actionColumn, ImGui.CalcTextSize(row.Action).X);
            }
            float columnWidth = keyColumn + gutter + actionColumn;
            int across = System.Math.Clamp((int) ((width - margin) / (columnWidth + margin)), 1, 3);
            int linesPerColumn = (int) ((listBottom - top) / lineHeight);
            int[] column = assignColumns(rows, linesPerColumn);
            int columnCount = column.Length == 0 ? 1 : column[column.Length - 1] + 1;
            int pages = (columnCount + across - 1) / across;
            page = System.Math.Clamp(page, 0, pages - 1);
            int visibleColumns = System.Math.Min(across, columnCount - page * across);

            float totalWidth = visibleColumns * columnWidth + (visibleColumns - 1) * margin;
            float left = System.MathF.Max(margin, (width - totalWidth) / 2f);
            Vector4 sectionColour = new Vector4(1f, 0.85f, 0.45f, 1f);
            Vector4 keyColour = new Vector4(0.65f, 0.85f, 1f, 1f);
            int currentColumn = -1;
            float rowY = top;
            string lastSection = null;
            for (int i = 0; i < rows.Count; i++) {
                if (column[i] / across != page) {
                    continue;
                }
                Row row = rows[i];
                float columnX = left + (column[i] % across) * (columnWidth + margin);
                if (column[i] != currentColumn) {
                    currentColumn = column[i];
                    rowY = top;
                    lastSection = null;
                }
                if (row.Section != lastSection) {
                    if (rowY > top) {
                        rowY += lineHeight;
                    }
                    ImGui.SetCursorPos(new Vector2(columnX, rowY));
                    Overlay.textColoured(sectionColour, row.Section);
                    rowY += lineHeight;
                    lastSection = row.Section;
                }
                ImGui.SetCursorPos(new Vector2(columnX, rowY));
                Overlay.textColoured(keyColour, row.Keys);
                ImGui.SetCursorPos(new Vector2(columnX + keyColumn + gutter, rowY));
                ImGui.TextUnformatted(row.Action);
                rowY += lineHeight;
            }

            string hint = pages > 1 ? CloseHint + "  Page " + (page + 1) + " of " + pages : CloseHint;
            Overlay.textCentered(hint, Overlay.BaseFontSize, buttonY - Overlay.lineHeight() * 0.75f, width);
            float centreX = width / 2f;
            if (Overlay.buttonAt(CloseLabel, centreX - buttonWidth / 2f, buttonY, buttonWidth, buttonHeight)) {
                action = UiAction.CloseControls;
            }
            if (pages > 1) {
                float pagerWidth = System.MathF.Min(buttonWidth, (width - buttonWidth) / 2f - margin * 2f);
                if (page > 0 && Overlay.buttonAt(PreviousLabel, centreX - buttonWidth / 2f - margin - pagerWidth, buttonY, pagerWidth, buttonHeight)) {
                    page--;
                }
                if (page < pages - 1 && Overlay.buttonAt(NextLabel, centreX + buttonWidth / 2f + margin, buttonY, pagerWidth, buttonHeight)) {
                    page++;
                }
            }
            Overlay.end();
            return action;
        }
    }
}
