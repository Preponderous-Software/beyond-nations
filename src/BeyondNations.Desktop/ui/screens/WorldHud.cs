using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using beyondnations.desktop.ui.boxes;

namespace beyondnations.desktop.ui.screens {

    /**
    * The heads-up display shown over the world, ported from the OnGUI half of
    * Assets/Scripts/screens/WorldScreen.cs.
    *
    * The rest of that file -- the tick loop and the key handling -- already
    * lives in Simulation and PlayerInputController, so only the drawing is here:
    * the command buttons along the bottom, the debug overlay, the settlement and
    * market boxes shown while the player is inside a settlement, the inventory
    * box, the player box, and the status message.
    *
    * The Unity boxes were anchored at fixed pixel offsets against a 1280x720
    * window: the left column at x = 10, the right column at Screen.width - 160,
    * and the lower row at y = 500. The lower row is expressed as an offset from
    * the bottom instead, which is the same place at 720 high and stays on screen
    * when the window is smaller.
    */
    public class WorldHud {
        private const float SideMargin = 10f;
        private const float BoxWidth = 200f;
        private const float LowerRowOffsetFromBottom = 220f;
        private const float SettlementBoxY = 30f;
        private const float BoxGap = 10f;

        private const ImGuiWindowFlags BarFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.AlwaysAutoResize;

        private const ImGuiWindowFlags StatusFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoInputs |
            ImGuiWindowFlags.AlwaysAutoResize;

        public UiAction draw(float width, float height, Simulation simulation, bool debugMode, int framesPerSecond) {
            Player player = simulation.getPlayer();
            float rightColumnX = width - BoxWidth - SideMargin;
            float lowerRowY = height - LowerRowOffsetFromBottom;

            drawCommandButtons(width, height, simulation);
            drawStatus(width, player);

            if (debugMode) {
                DebugInfoBox debugInfoBox = new DebugInfoBox(simulation);
                debugInfoBox.setFramesPerSecond(framesPerSecond);
                debugInfoBox.draw(SideMargin, SideMargin, BoxWidth);
            }

            // The right column used to put the market box at y = 200 and the
            // player box 220 pixels above the bottom, which overlapped once the
            // window was under about 600 pixels high. Each box now starts below
            // the one above it, and the player box only moves down from its
            // usual place, never up into the market box.
            float rightColumnBottom = 0f;
            if (player.isCurrentlyInSettlement()) {
                Settlement settlement = (Settlement) simulation.getEntityRepository().getEntity(player.getCurrentSettlementId());
                float settlementBottom = new SettlementInfoBox(settlement, simulation.getNationRepository(), simulation.getEntityRepository())
                    .draw(rightColumnX, SettlementBoxY, BoxWidth);
                rightColumnBottom = new MarketInfoBox(settlement.getMarket(), simulation.getEntityRepository())
                    .draw(rightColumnX, settlementBottom + BoxGap, BoxWidth);
            }

            new InventoryInfoBox(player.getInventory()).draw(SideMargin, lowerRowY, BoxWidth);

            float playerBoxY = System.MathF.Max(lowerRowY, rightColumnBottom + BoxGap);
            new PlayerInfoBox(player, simulation.getNationRepository()).draw(rightColumnX, playerBoxY, BoxWidth);

            return UiAction.None;
        }

        /**
        * The row of command buttons along the bottom. Which ones are offered is
        * WorldCommandBar's decision; this only lays them out and runs the one
        * that was clicked.
        */
        private void drawCommandButtons(float width, float height, Simulation simulation) {
            List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
            if (commands.Count == 0) {
                return;
            }

            float buttonHeight = height / 20f;
            float minButtonWidth = width / 10f;
            float barX = 100f;
            float maxRowWidth = width - barX - SideMargin;
            float spacing = ImGui.GetStyle().ItemSpacing.X;
            float textPadding = ImGui.GetStyle().FramePadding.X * 2f;

            // A fixed tenth of the window clipped longer labels such as
            // "Create Nation" or "Collect Coins (12)" once the window was
            // narrower than about 1000 pixels, so each button is at least as
            // wide as its label, and a row that would run off the right edge
            // wraps onto another one above it.
            float[] buttonWidths = new float[commands.Count];
            bool[] startsRow = new bool[commands.Count];
            int rows = 1;
            float rowWidth = 0f;
            for (int i = 0; i < commands.Count; i++) {
                buttonWidths[i] = System.MathF.Max(minButtonWidth, ImGui.CalcTextSize(commands[i].getLabel()).X + textPadding);
                if (i > 0 && rowWidth + spacing + buttonWidths[i] > maxRowWidth) {
                    startsRow[i] = true;
                    rows++;
                    rowWidth = buttonWidths[i];
                } else {
                    rowWidth += (i > 0 ? spacing : 0f) + buttonWidths[i];
                }
            }
            float rowStep = buttonHeight + ImGui.GetStyle().ItemSpacing.Y;
            float barY = height - buttonHeight - 10f - rowStep * (rows - 1);

            ImGui.SetNextWindowPos(new Vector2(barX, barY), ImGuiCond.Always);
            if (ImGui.Begin("##world-command-bar", BarFlags)) {
                WorldCommand clicked = null;
                for (int i = 0; i < commands.Count; i++) {
                    if (i > 0 && !startsRow[i]) {
                        ImGui.SameLine();
                    }
                    if (ImGui.Button(commands[i].getLabel(), new Vector2(buttonWidths[i], buttonHeight))) {
                        clicked = commands[i];
                    }
                }
                // Run the command after the row is laid out, so that a command
                // which changes what the bar offers cannot invalidate the list
                // being iterated.
                if (clicked != null) {
                    clicked.execute();
                }
            }
            ImGui.End();
        }

        /**
        * The transient message on Player.getStatus(). Status used to own a
        * Unity Text component and draw itself; it is now just the message and
        * its expiry, and this is what puts it on the screen.
        */
        private void drawStatus(float width, Player player) {
            string status = player.getStatus().getStatus();
            if (string.IsNullOrEmpty(status)) {
                return;
            }

            ImGui.SetNextWindowPos(new Vector2(width / 2f, 20f), ImGuiCond.Always, new Vector2(0.5f, 0f));
            if (ImGui.Begin("##status", StatusFlags)) {
                ImGui.TextUnformatted(status);
            }
            ImGui.End();
        }
    }
}
