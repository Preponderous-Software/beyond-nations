using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using beyondnations.desktop.input;

namespace beyondnations.desktop.ui.screens {

    /**
    * The crafting screen, opened from the world with C (or the Craft button
    * on touch screens). One row per recipe in RecipeBook: what it costs, why
    * it is worth making, and either a Craft button or exactly what is still
    * missing. Like the inventory screen it is drawn over the paused world.
    *
    * What each row says is decided by getRows(), which needs no window, so
    * the text is asserted in a unit test and only the drawing needs a screen.
    * It sticks to fixed-signature ImGui calls the other screens already
    * make (coloured text goes through Overlay.textColoured, not the variadic
    * TextColored), since the browser build crashes on a call whose native
    * signature does not match its P/Invoke.
    */
    public class CraftingScreen {
        public const string Title = "CRAFTING";
        public const string CloseHint = "{ToggleCrafting} or {Pause} closes";
        public const string CloseLabel = "Close";
        public const string TouchHint = "Tap a \"Craft >\" row to make it; drag to scroll";

        /** What one recipe row shows. */
        public class Row {
            public Recipe Recipe;
            public bool CanCraft;
            /** "Axe: 4 wood, 2 stone" */
            public string Heading;
            /** "+3 wood per tree, lasts 20 trees" (and the building it needs) */
            public string Benefit;
            /** "Ready: you can make 3" or "Need 2 more stone", plus what is already carried */
            public string Status;
        }

        public static List<Row> getRows(Simulation simulation) {
            Player player = simulation.getPlayer();
            Inventory inventory = player.getInventory();
            Settlement settlement = CraftItemCommand.getCurrentSettlement(player, simulation.getEntityRepository());
            List<Row> rows = new List<Row>();
            foreach (Recipe recipe in RecipeBook.getAll()) {
                Row row = new Row();
                row.Recipe = recipe;
                row.Heading = recipe.getName() + (recipe.getOutputQuantity() > 1 ? " x" + recipe.getOutputQuantity() : "")
                    + ": " + recipe.describeInputs();
                row.Benefit = recipe.getBenefit()
                    + (recipe.getRequiredBuilding() != null ? " (needs a " + BuildingCatalog.getDisplayName(recipe.getRequiredBuilding().Value) + ")" : "");
                List<string> missing = Crafting.getMissing(recipe, inventory, settlement);
                row.CanCraft = missing.Count == 0;
                row.Status = row.CanCraft
                    ? "Ready: you can make " + Crafting.getMaxCrafts(recipe, inventory, settlement)
                    : "Need " + string.Join(", ", missing);
                string held = describeHeld(inventory, recipe.getOutput());
                if (held != null) {
                    row.Status += "  |  " + held;
                }
                rows.Add(row);
            }
            return rows;
        }

        /** "have 1 (14 uses left)", "have 3", or null when none are carried. */
        public static string describeHeld(Inventory inventory, ItemType itemType) {
            int count = inventory.getNumItems(itemType);
            if (count <= 0) {
                return null;
            }
            if (Tools.isTool(itemType)) {
                return "have " + count + " (" + Tools.getUsesLeft(inventory, itemType) + " uses left)";
            }
            return "have " + count;
        }

        /** Where the player is, which decides the Workshop recipes. */
        public static string getLocationLine(Simulation simulation) {
            Settlement settlement = CraftItemCommand.getCurrentSettlement(simulation.getPlayer(), simulation.getEntityRepository());
            if (settlement == null) {
                return "Out in the world: tools and food can be made anywhere. Planks and bricks need a settlement with a Workshop.";
            }
            if (settlement.getNumBuildings(BuildingType.WORKSHOP) > 0) {
                return "In the " + settlement.getDisplayName() + ", which has a Workshop: every recipe is open.";
            }
            return "In the " + settlement.getDisplayName() + ", which has no Workshop yet ("
                + (TouchLayout.isActive() ? "build one from the command bar" : ObjectiveKeyNames.resolve("build one with {BuildBuilding} or the command bar")) + ").";
        }

        public UiAction draw(float width, float height, Simulation simulation) {
            List<Row> rows = getRows(simulation);
            float scale = System.Math.Clamp(System.MathF.Min(width / 700f, height / 560f), 1f, 1.6f);
            float panelWidth = System.MathF.Min(width - 32f, 760f * scale);
            float x0 = (width - panelWidth) * 0.5f;
            float lineHeight = Overlay.BaseFontSize * scale + 3f;
            float top = System.MathF.Max(height * 0.08f, 16f);
            float bottomReserve = lineHeight * 2f + 44f * scale;
            float rowHeight = System.MathF.Min(lineHeight * 3f + 14f * scale,
                (height - top - lineHeight * 3.5f - bottomReserve) / System.MathF.Max(1, rows.Count));
            rowHeight = System.MathF.Max(rowHeight, lineHeight * 3f + 4f);

            Overlay.beginFullScreen("##crafting-screen", width, height);
            // twice: the rows are dense, and nametags behind them showed through one layer
            Overlay.dimBackground(width, height);
            Overlay.dimBackground(width, height);

            float y = top;
            Overlay.textCentered(Title, lineHeight * 1.6f, y, width);
            y += lineHeight * 1.2f;
            Overlay.textCentered(ObjectiveKeyNames.closeHint(CloseHint, TouchHint), lineHeight * 0.8f, y, width);
            y += lineHeight * 0.9f;
            y = System.MathF.Max(y, Overlay.touchCloseBottom());
            ImGui.SetWindowFontScale(scale * 0.9f);
            drawWrapped(getLocationLine(simulation), x0, ref y, panelWidth, lineHeight * 0.9f, new Vector4(0.85f, 0.85f, 0.85f, 1f));
            y += lineHeight * 0.4f;

            // A craftable row is one big button, so a tap anywhere on it
            // crafts: on a phone the on-screen controls sit over the edges of
            // the screen, where a small Craft button would end up under them.
            Recipe clicked = null;
            float craftLabelWidth = 70f * scale;
            float craftableTextWidth = panelWidth - craftLabelWidth - 20f;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            foreach (Row row in rows) {
                float textWidth = row.CanCraft ? craftableTextWidth : panelWidth - 16f;
                ImGui.SetWindowFontScale(scale);
                if (row.CanCraft) {
                    if (Overlay.buttonAt("##craft-" + row.Recipe.getName(), x0, y, panelWidth, rowHeight - 4f)) {
                        clicked = row.Recipe;
                    }
                    string craftLabel = "Craft >";
                    Vector2 labelSize = ImGui.CalcTextSize(craftLabel);
                    ImGui.SetCursorPos(new Vector2(x0 + panelWidth - labelSize.X - 12f, y + (rowHeight - 4f - labelSize.Y) * 0.5f));
                    ImGui.TextUnformatted(craftLabel);
                } else {
                    drawList.AddRectFilled(new Vector2(x0, y), new Vector2(x0 + panelWidth, y + rowHeight - 4f),
                        ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.18f, 0.85f)));
                }

                float ty = y + 4f;
                ImGui.SetCursorPos(new Vector2(x0 + 8f, ty));
                ImGui.TextUnformatted(fit(row.Heading, textWidth));
                ty += lineHeight;
                ImGui.SetWindowFontScale(scale * 0.9f);
                ImGui.SetCursorPos(new Vector2(x0 + 8f, ty));
                Overlay.textColoured(new Vector4(0.85f, 0.85f, 0.75f, 1f), fit(row.Benefit, textWidth));
                ty += lineHeight * 0.95f;
                ImGui.SetCursorPos(new Vector2(x0 + 8f, ty));
                Overlay.textColoured(row.CanCraft ? new Vector4(0.6f, 1f, 0.6f, 1f) : new Vector4(1f, 0.72f, 0.35f, 1f), fit(row.Status, textWidth));
                y += rowHeight;
            }
            ImGui.SetWindowFontScale(1f);

            // the result of the last craft, since the world's status line is hidden behind this screen
            string status = ObjectiveKeyNames.resolve(simulation.getPlayer().getStatus().getStatus());
            if (!string.IsNullOrEmpty(status)) {
                ImGui.SetWindowFontScale(scale * 0.9f);
                y += 2f;
                drawWrapped(status, x0, ref y, panelWidth, lineHeight * 0.9f, new Vector4(1f, 1f, 1f, 1f));
                ImGui.SetWindowFontScale(1f);
            }

            UiAction action = UiAction.None;
            ImGui.SetWindowFontScale(scale);
            float closeWidth = 120f * scale;
            float closeHeight = 36f * scale;
            // On touch the list scrolls by dragging, so Close follows the
            // last row instead of being pinned over it (and the corner Close
            // stays in reach).
            float closeY = TouchLayout.isActive() ? y + 6f : System.MathF.Min(y + 6f, height - closeHeight - 8f);
            if (Overlay.buttonAt(CloseLabel + "##crafting", (width - closeWidth) * 0.5f, closeY, closeWidth, closeHeight)) {
                action = UiAction.ResumeWorld;
            }
            ImGui.SetWindowFontScale(1f);
            Overlay.end();
            if (Overlay.touchCloseButton("crafting", width)) {
                action = UiAction.ResumeWorld;
            }

            // craft after drawing, so the rows drawn this frame stay consistent
            if (clicked != null) {
                new CraftItemCommand(simulation.getEntityRepository()).execute(simulation.getPlayer(), clicked);
            }
            return action;
        }

        /**
        * Shortens a line with "..." until it fits, so a narrow phone screen
        * clips text rather than running it under the Craft button.
        */
        private static string fit(string text, float maxWidth) {
            if (ImGui.CalcTextSize(text).X <= maxWidth) {
                return text;
            }
            string cut = text;
            while (cut.Length > 4 && ImGui.CalcTextSize(cut + "...").X > maxWidth) {
                cut = cut.Substring(0, cut.Length - 1);
            }
            return cut + "...";
        }

        /** Splits text into lines on word boundaries to fit the panel. */
        private static void drawWrapped(string text, float x, ref float y, float maxWidth, float lineHeight, Vector4 colour) {
            string[] words = text.Split(' ');
            string line = "";
            foreach (string word in words) {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && ImGui.CalcTextSize(candidate).X > maxWidth) {
                    ImGui.SetCursorPos(new Vector2(x, y));
                    Overlay.textColoured(colour, line);
                    y += lineHeight;
                    line = word;
                } else {
                    line = candidate;
                }
            }
            if (line.Length > 0) {
                ImGui.SetCursorPos(new Vector2(x, y));
                Overlay.textColoured(colour, line);
                y += lineHeight;
            }
        }
    }
}
