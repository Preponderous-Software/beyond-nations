using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using beyondnations.desktop.input;
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
        private const float DesignBoxWidth = 200f;
        private const float BoxGap = 10f;
        public const string ControlsButtonLabel = "? Controls";

        /**
        * Energy below which the HUD warns that the player is going hungry,
        * and below which it says they are starving. Energy falls every tick
        * and the player dies at zero; before these warnings the only sign was
        * the number in the player box, so death arrived unannounced.
        */
        public const float HungryEnergy = 30f;
        public const float StarvingEnergy = 10f;

        /** How long a new status message flashes brighter, in seconds. */
        private const double StatusFlashSeconds = 0.5;

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

        private string lastStatus;
        private double lastStatusChangedAt;
        private double clock;

        private static List<string> wrapForCentre(string text, Layout layout) {
            float maxWidth = layout.CentreWrapWidth - ImGui.GetStyle().WindowPadding.X * 2f;
            return TextWrap.wrap(text, maxWidth, line => ImGui.CalcTextSize(line).X);
        }

        /**
        * Where the HUD's columns go, as plain numbers, so the arithmetic can
        * be checked for a window of any size without ImGui. Boxes are scaled
        * with UiScale but never take more than a third of a narrow window,
        * and the text in the middle wraps in the space the columns leave.
        */
        public struct Layout {
            public float Margin;
            public float Gap;
            public float BoxWidth;
            public float LeftX;
            public float RightX;
            public float CentreWrapWidth;

            public static Layout compute(float width, float height, float uiScale) {
                Layout layout = new Layout();
                layout.Margin = SideMargin * uiScale;
                layout.Gap = BoxGap * uiScale;
                float third = System.MathF.Max(0f, (width - layout.Margin * 4f) / 3f);
                float half = System.MathF.Max(0f, (width - layout.Margin * 3f) / 2f);
                // The design width, unless a third of the window is less; and
                // never under 120 design pixels unless even half the window is.
                layout.BoxWidth = System.MathF.Min(half, System.MathF.Max(System.MathF.Min(DesignBoxWidth * uiScale, third), 120f * uiScale));
                layout.LeftX = layout.Margin;
                layout.RightX = width - layout.BoxWidth - layout.Margin;
                float middle = width - 2f * (layout.BoxWidth + layout.Margin * 2f);
                layout.CentreWrapWidth = System.MathF.Max(220f * uiScale, System.MathF.Min(middle, 640f * uiScale));
                layout.CentreWrapWidth = System.MathF.Min(layout.CentreWrapWidth, width - layout.Margin * 2f);
                return layout;
            }
        }

        public UiAction draw(float width, float height, Simulation simulation, bool debugMode, int framesPerSecond) {
            Player player = simulation.getPlayer();
            Layout layout = Layout.compute(width, height, UiScale.get());
            UiAction action = UiAction.None;

            // The command bar is laid out first, because both columns of
            // boxes stop above wherever its rows end. The inventory and player
            // boxes used to sit a fixed 220 pixels above the bottom and the
            // debug box a fixed distance from the top, so on a short window
            // they overlapped each other and the bar.
            float barTop = drawCommandButtons(width, height, simulation, layout);
            float columnBottom = barTop - layout.Gap;
            float centreY = drawStatus(width, player, layout, layout.Margin);
            drawHungerWarning(width, player, layout, centreY);

            // Left column, from the top: the controls button (the touch path
            // to the controls list, and a reminder that it exists), then the
            // debug box when it is on. The inventory box sits at the bottom,
            // above the bar, and is pushed down rather than up into them.
            float leftTop = layout.Margin;
            ImGui.SetNextWindowPos(new Vector2(layout.LeftX - ImGui.GetStyle().WindowPadding.X, leftTop - ImGui.GetStyle().WindowPadding.Y), ImGuiCond.Always);
            if (ImGui.Begin("##controls-button", BarFlags)) {
                if (ImGui.Button(ControlsButtonLabel)) {
                    action = UiAction.OpenControls;
                }
                leftTop = ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y;
            }
            ImGui.End();

            if (debugMode) {
                DebugInfoBox debugInfoBox = new DebugInfoBox(simulation);
                debugInfoBox.setFramesPerSecond(framesPerSecond);
                leftTop = debugInfoBox.draw(layout.LeftX, leftTop, layout.BoxWidth) + layout.Gap;
            }

            // On a short window with the debug box open there is no room for
            // the inventory under it, so it moves into a second column beside
            // the debug box rather than off the bottom of the screen.
            InventoryInfoBox inventoryBox = new InventoryInfoBox(player.getInventory());
            float inventoryX = layout.LeftX;
            if (leftTop + inventoryBox.getExpectedHeight() > columnBottom && layout.LeftX + layout.BoxWidth * 2f + layout.Gap < layout.RightX) {
                inventoryX = layout.LeftX + layout.BoxWidth + layout.Gap;
                leftTop = layout.Margin;
            }
            inventoryBox.drawAbove(inventoryX, columnBottom, layout.BoxWidth, leftTop);

            // Right column: the settlement and market boxes stack down from
            // the top while the player is inside a settlement, and the player
            // box sits at the bottom, never rising into them.
            float rightTop = layout.Margin;
            if (player.isCurrentlyInSettlement()) {
                Settlement settlement = (Settlement) simulation.getEntityRepository().getEntity(player.getCurrentSettlementId());
                float settlementBottom = new SettlementInfoBox(settlement, simulation.getNationRepository(), simulation.getEntityRepository())
                    .draw(layout.RightX, rightTop, layout.BoxWidth);
                rightTop = new MarketInfoBox(settlement.getMarket(), simulation.getEntityRepository())
                    .draw(layout.RightX, settlementBottom + layout.Gap, layout.BoxWidth) + layout.Gap;
            }

            new PlayerInfoBox(player, simulation.getNationRepository()).drawAbove(layout.RightX, columnBottom, layout.BoxWidth, rightTop);

            return action;
        }

        /**
        * The row of command buttons along the bottom. Which ones are offered is
        * WorldCommandBar's decision; this only lays them out and runs the one
        * that was clicked. Returns the y of the top of the bar (the bottom
        * margin when there are no commands).
        */
        private float drawCommandButtons(float width, float height, Simulation simulation, Layout layout) {
            List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
            if (commands.Count == 0) {
                return height - layout.Margin;
            }

            // A twentieth of the height, as the Unity bar had, but never less
            // than a comfortable touch target nor more than a chunky button.
            float buttonHeight = System.Math.Clamp(height / 20f, Overlay.frameHeight() + UiScale.px(8f), UiScale.px(44f));
            float minButtonWidth = System.MathF.Min(width / 10f, UiScale.px(110f));
            float maxRowWidth = width - layout.Margin * 2f;
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
            float widestRow = 0f;
            for (int i = 0; i < commands.Count; i++) {
                buttonWidths[i] = System.MathF.Max(minButtonWidth, ImGui.CalcTextSize(commands[i].getLabel()).X + textPadding);
                if (i > 0 && rowWidth + spacing + buttonWidths[i] > maxRowWidth) {
                    startsRow[i] = true;
                    rows++;
                    rowWidth = buttonWidths[i];
                } else {
                    rowWidth += (i > 0 ? spacing : 0f) + buttonWidths[i];
                }
                widestRow = System.MathF.Max(widestRow, rowWidth);
            }
            float rowStep = buttonHeight + ImGui.GetStyle().ItemSpacing.Y;
            Vector2 padding = ImGui.GetStyle().WindowPadding;
            float barY = height - buttonHeight - layout.Margin - rowStep * (rows - 1);
            // Centred, so it sits between the inventory and player boxes
            // rather than starting under one of them.
            float barX = System.MathF.Max(layout.Margin, (width - widestRow) / 2f);

            ImGui.SetNextWindowPos(new Vector2(barX - padding.X, barY - padding.Y), ImGuiCond.Always);
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
            return barY;
        }

        /**
        * The hunger warning, or null when the player is not hungry. The
        * player eats on their own whenever they carry food, so a hungry
        * player is one with none, and the warning says where to get some.
        */
        public static string getHungerWarning(Player player) {
            float energy = player.getEnergy();
            if (energy >= HungryEnergy) {
                return null;
            }
            string howToEat = "Press " + KeyBindings.Interact + " at a tree or a chicken for food, or buy food in a settlement.";
            if (energy < StarvingEnergy) {
                return "Starving! Energy " + (int) energy + ". " + howToEat;
            }
            return "Hungry: energy " + (int) energy + " and falling. " + howToEat;
        }

        /**
        * Drawn under the status line and held there, rather than posted as a
        * status, so a later message cannot push it off screen while the
        * danger is still real.
        */
        private void drawHungerWarning(float width, Player player, Layout layout, float y) {
            string warning = getHungerWarning(player);
            if (warning == null) {
                return;
            }
            Vector4 colour = player.getEnergy() < StarvingEnergy
                ? new Vector4(1f, 0.35f, 0.35f, 1f)
                : new Vector4(1f, 0.8f, 0.3f, 1f);
            ImGui.SetNextWindowPos(new Vector2(width / 2f, y), ImGuiCond.Always, new Vector2(0.5f, 0f));
            if (ImGui.Begin("##hunger-warning", StatusFlags)) {
                foreach (string line in wrapForCentre(warning, layout)) {
                    ImGui.TextColored(colour, line);
                }
            }
            ImGui.End();
        }

        /**
        * The transient message on Player.getStatus(). Status used to own a
        * Unity Text component and draw itself; it is now just the message and
        * its expiry, and this is what puts it on the screen.
        */
        private float drawStatus(float width, Player player, Layout layout, float y) {
            string status = player.getStatus().getStatus();
            if (string.IsNullOrEmpty(status)) {
                lastStatus = null;
                return y;
            }

            // A refusal ("Not enough wood. Need 20.") used to look exactly like
            // a success: small white text gone in a few seconds, so a key that
            // did nothing looked broken. Refusals are now drawn on red with a
            // "!" in front, successes on the usual dark panel, and a new
            // message flashes brighter for a moment so a repeated press is
            // noticed. Long messages wrap in the space between the columns of
            // boxes instead of running under them.
            clock += ImGui.GetIO().DeltaTime;
            double now = clock;
            if (status != lastStatus) {
                lastStatus = status;
                lastStatusChangedAt = now;
            }
            float flash = (float) System.Math.Clamp(1.0 - (now - lastStatusChangedAt) / StatusFlashSeconds, 0.0, 1.0);
            bool failure = StatusTone.isFailure(status);
            Vector4 background = failure
                ? new Vector4(0.55f + 0.3f * flash, 0.10f, 0.10f, 0.90f)
                : new Vector4(0.08f + 0.25f * flash, 0.08f + 0.25f * flash, 0.10f + 0.25f * flash, 0.85f);

            // The panel is painted with the draw list under the text, in a
            // window with no background of its own.
            ImGui.SetNextWindowPos(new Vector2(width / 2f, y), ImGuiCond.Always, new Vector2(0.5f, 0f));
            float bottom = y;
            if (ImGui.Begin("##status", StatusFlags | ImGuiWindowFlags.NoBackground)) {
                Overlay.fillLocalRect(Vector2.Zero, ImGui.GetWindowSize(), background);
                foreach (string line in wrapForCentre(failure ? "! " + status : status, layout)) {
                    ImGui.TextUnformatted(line);
                }
                bottom = ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y;
            }
            ImGui.End();
            return bottom + layout.Gap * 0.5f;
        }
    }
}
