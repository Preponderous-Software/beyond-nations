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
        private const float MarketBoxY = 200f;

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

            if (player.isCurrentlyInSettlement()) {
                Settlement settlement = (Settlement) simulation.getEntityRepository().getEntity(player.getCurrentSettlementId());
                new SettlementInfoBox(settlement, simulation.getNationRepository(), simulation.getEntityRepository())
                    .draw(rightColumnX, SettlementBoxY, BoxWidth);
                new MarketInfoBox(settlement.getMarket(), simulation.getEntityRepository())
                    .draw(rightColumnX, MarketBoxY, BoxWidth);
            }

            new InventoryInfoBox(player.getInventory()).draw(SideMargin, lowerRowY, BoxWidth);

            new PlayerInfoBox(player, simulation.getNationRepository()).draw(rightColumnX, lowerRowY, BoxWidth);

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
            float buttonWidth = width / 10f;
            float barX = 100f;
            float barY = height - buttonHeight - 10f;

            ImGui.SetNextWindowPos(new Vector2(barX, barY), ImGuiCond.Always);
            if (ImGui.Begin("##world-command-bar", BarFlags)) {
                WorldCommand clicked = null;
                for (int i = 0; i < commands.Count; i++) {
                    if (i > 0) {
                        ImGui.SameLine();
                    }
                    if (ImGui.Button(commands[i].getLabel(), new Vector2(buttonWidth, buttonHeight))) {
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
