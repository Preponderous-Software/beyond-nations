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
        // the command bar button waiting for its second click
        private readonly ConfirmGate confirmGate = new ConfirmGate();
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

        private readonly ChroniclePanel chroniclePanel = new ChroniclePanel();
        // the era box keeps whether its detail is open from frame to frame
        private readonly EraBox eraBox = new EraBox();

        public EraBox getEraBox() { return eraBox; }
        private readonly ChronicleView defaultChronicleView = new ChronicleView();

        /**
        * chronicleView is the input layer's; null draws the recent entries,
        * which is what a new view starts on.
        */
        public UiAction draw(float width, float height, Simulation simulation, bool debugMode, int framesPerSecond, ChronicleView chronicleView = null) {
            Player player = simulation.getPlayer();
            Layout layout = Layout.compute(width, height, UiScale.get());
            UiAction action = UiAction.None;

            // The command bar is laid out first, because both columns of
            // boxes stop above wherever its rows end. The inventory and player
            // boxes used to sit a fixed 220 pixels above the bottom and the
            // debug box a fixed distance from the top, so on a short window
            // they overlapped each other and the bar.
            // On a touch screen (TouchLayout) the joystick and buttons take a
            // band along the bottom, so the bar and both columns stop above
            // it; the band is 0 everywhere else.
            bool touch = TouchLayout.isActive();
            // A phone held upright has no room for the status line and the
            // chronicle between the box columns, where they covered the
            // objective tracker; on a narrow touch screen they go under it.
            bool stackCentre = isStackCentre(width, layout, touch);
            // The bar stops short of the right-hand column, which runs down
            // to the bottom beside it (inside a settlement it holds four
            // boxes, and with the bar under it the last button was cut off
            // and the season box drawn over it). Upright, the right column is
            // at the top, and the bar has the whole width.
            float bottom = height - TouchLayout.getBottomReserve();
            float barRight = getBarRight(width, layout, stackCentre);
            float barTop = drawCommandButtons(width, barRight, bottom, simulation, layout);
            float columnBottom = barTop - layout.Gap;
            float rightColumnBottom = stackCentre ? columnBottom : bottom - layout.Margin;
            float centreBottom = layout.Margin;
            if (!stackCentre) {
                float centreY = drawStatus(width, player, layout, layout.Margin);
                centreBottom = drawResidentHungerWarning(width, simulation, layout, drawHungerWarning(width, player, layout, centreY));
            }

            // Left column, from the top: the controls button (the touch path
            // to the controls list, and a reminder that it exists), then the
            // debug box when it is on. The inventory box sits at the bottom,
            // above the bar, and is pushed down rather than up into them.
            float leftTop = layout.Margin;
            // touch screens have a ? in their button row instead
            if (!touch) {
                ImGui.SetNextWindowPos(new Vector2(layout.LeftX - ImGui.GetStyle().WindowPadding.X, leftTop - ImGui.GetStyle().WindowPadding.Y), ImGuiCond.Always);
                if (ImGui.Begin("##controls-button", BarFlags)) {
                    if (ImGui.Button(ControlsButtonLabel)) {
                        action = UiAction.OpenControls;
                    }
                    leftTop = ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y;
                }
                ImGui.End();
            }

            if (debugMode) {
                DebugInfoBox debugInfoBox = new DebugInfoBox(simulation);
                debugInfoBox.setFramesPerSecond(framesPerSecond);
                leftTop = debugInfoBox.draw(layout.LeftX, leftTop, layout.BoxWidth) + layout.Gap;
            }

            // The objective tracker comes next in the left column, under the
            // controls button (and the debug box when F1 is on), so the
            // inventory below is pushed down rather than drawn over it.
            // The era (Camp to Kingdom) and how near the next one is sit just
            // above it, compact unless tapped open.
            leftTop = eraBox.draw(simulation, layout.LeftX, leftTop, layout.BoxWidth) + layout.Gap;
            ObjectiveTrackerBox objectiveTracker = new ObjectiveTrackerBox(simulation);
            leftTop = objectiveTracker.draw(layout.LeftX, leftTop, layout.BoxWidth) + layout.Gap;
            PlayerInfoBox playerBox = new PlayerInfoBox(player, simulation.getNationRepository(), simulation.getEntityRepository());
            if (stackCentre) {
                // Upright, the player box sits at the top right beside the
                // tracker, and the status line, warnings and chronicle run
                // full width under the two of them.
                float underBoxes = System.MathF.Max(leftTop, layout.Margin + playerBox.getExpectedHeight() + layout.Gap);
                float centreY = drawStatus(width, player, layout, underBoxes);
                centreBottom = drawResidentHungerWarning(width, simulation, layout, drawHungerWarning(width, player, layout, centreY));
            }

            // On a short window with the debug box open there is no room for
            // the inventory under it, so it moves into a second column beside
            // the debug box rather than off the bottom of the screen.
            //
            // On a touch screen there is no room for it above the controls,
            // and the Bag button opens the full inventory a tap away.
            if (!touch) {
                InventoryInfoBox inventoryBox = new InventoryInfoBox(player.getInventory());
                float inventoryX = layout.LeftX;
                if (leftTop + inventoryBox.getExpectedHeight() > columnBottom && layout.LeftX + layout.BoxWidth * 2f + layout.Gap < layout.RightX) {
                    inventoryX = layout.LeftX + layout.BoxWidth + layout.Gap;
                    leftTop = layout.Margin;
                }
                inventoryBox.drawAbove(inventoryX, columnBottom, layout.BoxWidth, leftTop);
            }

            // Right column: the settlement and market boxes stack down from
            // the top while the player is inside a settlement, and the player
            // box sits at the bottom, never rising into them.
            float rightTop = layout.Margin;
            SeasonInfoBox seasonBox = new SeasonInfoBox(simulation);
            if (player.isCurrentlyInSettlement()) {
                Settlement settlement = (Settlement) simulation.getEntityRepository().getEntity(player.getCurrentSettlementId());
                SettlementInfoBox settlementBox = new SettlementInfoBox(settlement, simulation.getNationRepository(), simulation.getEntityRepository());
                MarketInfoBox marketBox = new MarketInfoBox(settlement.getMarket(), simulation.getEntityRepository());
                // four boxes do not fit a short window (960x540: the season box
                // covered the player box and ran off the bottom, #350), so the
                // market, season and player boxes drop their least needed lines
                if (!stackCentre && !fitsInFull(rightColumnBottom - rightTop, layout.Gap, settlementBox, marketBox, seasonBox, playerBox)) {
                    marketBox.setCompact(true);
                    seasonBox.setCompact(true);
                    playerBox.setCompact(true);
                }
                float settlementBottom = settlementBox.draw(layout.RightX, rightTop, layout.BoxWidth);
                rightTop = marketBox.draw(layout.RightX, settlementBottom + layout.Gap, layout.BoxWidth) + layout.Gap;
            }

            float playerTop = stackCentre
                ? drawAt(playerBox, layout.RightX, rightTop, layout.BoxWidth)
                : playerBox.drawAbove(layout.RightX, rightColumnBottom, layout.BoxWidth, rightTop);
            // the season, the winter warning and the region, just above the player box
            bool seasonUnderChronicle = stackCentre;
            if (stackCentre) {
                // drawn below, under the chronicle strip
            } else if (touch && playerTop - layout.Gap - seasonBox.getExpectedHeight() < rightTop) {
                // A phone on its side has no room above the player box once
                // the controls take the bottom (TouchLayout), so the season
                // goes under the objective tracker, where the inventory box
                // would have been.
                seasonBox.draw(layout.LeftX, leftTop, layout.BoxWidth);
            } else {
                seasonBox.drawAbove(layout.RightX, playerTop - layout.Gap, layout.BoxWidth, rightTop);
            }

            // Last, so the full chronicle window sits over the boxes it covers.
            // The top-centre column stacks, top down: the status line, the
            // hunger warning, the recent chronicle strip (no wider than the
            // space between the box columns, so it never covers the objective
            // tracker), then the objective toast, so none of them overlap.
            float stripBottom = chroniclePanel.draw(width, height, simulation, chronicleView ?? defaultChronicleView,
                ChroniclePanel.compactTop(centreBottom), layout.CentreWrapWidth);
            TouchLayout.setTipTop(0f);
            if (seasonUnderChronicle) {
                // full width under the chronicle on a phone held upright, and
                // a tip, while one shows, under that (FeelDirector reads
                // TouchLayout.getTipTop) rather than over the status line
                float seasonTop = System.MathF.Max(stripBottom, centreBottom) + layout.Gap;
                float seasonBottom = seasonBox.draw(layout.LeftX, seasonTop, width - layout.Margin * 2f);
                TouchLayout.setTipTop(seasonBottom + layout.Gap);
            }
            objectiveTracker.drawToast(width, height, stripBottom + layout.Gap);

            // the caravan dispatch panel, when Send Caravan has opened it, over the rest
            // (from the top on a touch screen, whose controls step aside while
            // it is open, so its Send and Close stay above the screen's edge)
            CaravanDispatchPanel.draw(width, height, simulation, touch ? layout.Margin : centreBottom + layout.Gap);

            return action;
        }

        /**
        * Whether the boxes, each with every line, fit one above another in
        * height. Judged on full line counts, not last frame's heights, so a
        * column that has just gone compact does not flip back next frame.
        */
        public static bool fitsInFull(float height, float gap, params InfoBox[] boxes) {
            float needed = gap * (boxes.Length - 1);
            foreach (InfoBox box in boxes) {
                needed += box.estimateHeight();
            }
            return needed <= height;
        }

        /** Draws an info box from the top down; returns its top, as drawAbove does. */
        private static float drawAt(InfoBox box, float x, float top, float width) {
            box.draw(x, top, width);
            return top;
        }

        /**
        * Whether the status line, chronicle and season go in one column under
        * the top boxes, as on a phone held upright, rather than between the
        * left and right columns.
        */
        public static bool isStackCentre(float width, Layout layout, bool touch) {
            return touch && layout.CentreWrapWidth + 2f * (layout.BoxWidth + layout.Margin * 2f) > width;
        }

        /**
        * Where the command bar must stop on the right: short of the right
        * column of boxes, which runs to the bottom of the window beside it,
        * or the margin when that column is at the top (isStackCentre).
        */
        public static float getBarRight(float width, Layout layout, bool stackCentre) {
            return stackCentre ? width - layout.Margin : layout.RightX - layout.Gap;
        }

        /**
        * The settings CommandBarLayout lays the bar out with, for a bar
        * between the left margin and barRight whose bottom row sits a margin
        * above bottom. measure is the label width (ImGui.CalcTextSize in the
        * game); the rest are ImGui's style values, passed in so the layout
        * can be checked without a window.
        */
        public static CommandBarLayout.Settings barSettings(float width, float barRight, float bottom, Layout layout,
                float uiScale, float frameHeight, float spacingX, float spacingY, float textPadding) {
            CommandBarLayout.Settings settings = new CommandBarLayout.Settings();
            settings.Left = layout.Margin;
            settings.Right = System.MathF.Max(layout.Margin + 1f, barRight);
            settings.Bottom = bottom - layout.Margin;
            // A twentieth of the height, as the Unity bar had, but never less
            // than a comfortable touch target nor more than a chunky button.
            settings.ButtonHeight = System.Math.Clamp(bottom / 20f, frameHeight + 8f * uiScale, System.MathF.Max(frameHeight + 8f * uiScale, 44f * uiScale));
            settings.MinButtonWidth = System.MathF.Min(width / 10f, 110f * uiScale);
            settings.SpacingX = spacingX;
            settings.SpacingY = spacingY;
            settings.TextPadding = textPadding;
            settings.WidthStep = 16f * uiScale;
            settings.DrasticGap = layout.Gap;
            return settings;
        }

        /** Hostile and irreversible buttons are drawn red; armed, brighter. */
        private static readonly Vector4 DrasticColour = new Vector4(0.55f, 0.16f, 0.14f, 0.90f);
        private static readonly Vector4 DrasticHoverColour = new Vector4(0.70f, 0.22f, 0.18f, 1f);
        private static readonly Vector4 ArmedColour = new Vector4(0.85f, 0.30f, 0.10f, 1f);

        /**
        * The command buttons along the bottom. Which ones are offered is
        * WorldCommandBar's decision and where each goes is CommandBarLayout's;
        * this draws them and runs the one that was clicked, asking for a
        * second click first for the ones that cannot be taken back. Returns
        * the y of the top of the bar (the bottom margin when there are no
        * commands).
        */
        private float drawCommandButtons(float width, float barRight, float height, Simulation simulation, Layout layout) {
            List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
            if (commands.Count == 0) {
                return height - layout.Margin;
            }

            ImGuiStylePtr style = ImGui.GetStyle();
            CommandBarLayout.Settings settings = barSettings(width, barRight, height, layout,
                UiScale.get(), Overlay.frameHeight(), style.ItemSpacing.X, style.ItemSpacing.Y, style.FramePadding.X * 2f);
            CommandBarLayout.Result placed = CommandBarLayout.compute(commands, label => ImGui.CalcTextSize(label).X, settings);
            Vector2 padding = style.WindowPadding;
            double now = ImGui.GetTime();

            ImGui.SetNextWindowPos(new Vector2(settings.Left - padding.X, placed.Top - padding.Y), ImGuiCond.Always);
            if (ImGui.Begin("##world-command-bar", BarFlags)) {
                WorldCommand clicked = null;
                for (int i = 0; i < commands.Count; i++) {
                    WorldCommand command = commands[i];
                    CommandBarLayout.Rect rect = placed.Buttons[i];
                    ImGui.SetCursorPos(new Vector2(rect.X - settings.Left + padding.X, rect.Y - placed.Top + padding.Y));
                    bool armed = command.needsConfirm() && confirmGate.isArmed(command.getLabel(), now);
                    string text = armed ? CommandBarLayout.getArmedLabel(command) : command.getLabel();
                    int colours = 0;
                    if (armed) {
                        ImGui.PushStyleColor(ImGuiCol.Button, ArmedColour);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ArmedColour);
                        colours = 2;
                    } else if (command.getGroup() == CommandGroup.Drastic) {
                        ImGui.PushStyleColor(ImGuiCol.Button, DrasticColour);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, DrasticHoverColour);
                        colours = 2;
                    }
                    if (!command.isEnabled()) {
                        ImGui.BeginDisabled();
                    }
                    // the id is the plain label, so arming keeps the same button
                    if (ImGui.Button(text + "###" + command.getLabel(), new Vector2(rect.Width, rect.Height)) && command.isEnabled()) {
                        clicked = command;
                    }
                    if (!command.isEnabled()) {
                        ImGui.EndDisabled();
                    }
                    if (colours > 0) {
                        ImGui.PopStyleColor(colours);
                    }
                }
                // Run the command after the row is laid out, so that a command
                // which changes what the bar offers cannot invalidate the list
                // being iterated.
                if (clicked != null) {
                    runClicked(clicked, simulation.getPlayer(), now);
                }
            }
            ImGui.End();
            return placed.Top;
        }

        /** A click on a bar button: run it, or arm it when it needs a second click. */
        private void runClicked(WorldCommand command, Player player, double now) {
            if (!command.needsConfirm()) {
                confirmGate.disarm();
                command.execute();
                return;
            }
            if (confirmGate.press(command.getLabel(), now)) {
                command.execute();
                return;
            }
            player.getStatus().update(getConfirmPrompt(command, TouchLayout.isActive()));
        }

        /** "Click again to confirm: Raid Caravan." */
        public static string getConfirmPrompt(WorldCommand command, bool touch) {
            return (touch ? "Tap" : "Click") + " again to confirm: " + command.getLabel() + ".";
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
            string howToEat = ObjectiveKeyNames.resolve("Press {Interact} at a tree or a chicken for food, or buy food in a settlement.");
            if (energy < StarvingEnergy) {
                return "Starving! Energy " + (int) energy + ". " + howToEat;
            }
            return "Hungry: energy " + (int) energy + " and falling. " + howToEat;
        }

        /**
        * Drawn under the status line and held there, rather than posted as a
        * status, so a later message cannot push it off screen while the
        * danger is still real. Returns the warning's bottom edge, or y when
        * there is none, so the chronicle strip can sit below it.
        */
        private float drawHungerWarning(float width, Player player, Layout layout, float y) {
            string warning = getHungerWarning(player);
            if (warning == null) {
                return y;
            }
            Vector4 colour = player.getEnergy() < StarvingEnergy
                ? new Vector4(1f, 0.35f, 0.35f, 1f)
                : new Vector4(1f, 0.8f, 0.3f, 1f);
            float bottom = y;
            ImGui.SetNextWindowPos(new Vector2(width / 2f, y), ImGuiCond.Always, new Vector2(0.5f, 0f));
            if (ImGui.Begin("##hunger-warning", StatusFlags)) {
                foreach (string line in wrapForCentre(warning, layout)) {
                    Overlay.textColoured(colour, line);
                }
                bottom = ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y;
            }
            ImGui.End();
            return bottom;
        }

        /**
        * The player's nation's hungry and starving pawns (ResidentHunger), under
        * the player's own hunger warning: before #353 the first sign of a
        * starving winter was the death notice.
        */
        private float drawResidentHungerWarning(float width, Simulation simulation, Layout layout, float y) {
            Player player = simulation.getPlayer();
            Nation nation = player.getNationId() == null ? null : simulation.getNationRepository().getNation(player.getNationId());
            string warning = ResidentHunger.describeWarning(simulation.getEntityRepository(), nation, simulation.getSeasonCalendar().isWinter());
            if (warning == null) {
                return y;
            }
            Vector4 colour = warning.Contains("starving") ? new Vector4(1f, 0.35f, 0.35f, 1f) : new Vector4(1f, 0.8f, 0.3f, 1f);
            float bottom = y;
            ImGui.SetNextWindowPos(new Vector2(width / 2f, y), ImGuiCond.Always, new Vector2(0.5f, 0f));
            if (ImGui.Begin("##resident-hunger-warning", StatusFlags)) {
                foreach (string line in wrapForCentre(warning, layout)) {
                    Overlay.textColoured(colour, line);
                }
                bottom = ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y;
            }
            ImGui.End();
            return bottom;
        }

        /**
        * The transient message on Player.getStatus(). Status used to own a
        * Unity Text component and draw itself; it is now just the message and
        * its expiry, and this is what puts it on the screen.
        */
        private float drawStatus(float width, Player player, Layout layout, float y) {
            string status = ObjectiveKeyNames.resolve(player.getStatus().getStatus());
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
            // a line can join several messages said in one step: it is a
            // refusal if it starts with one or the newest one is
            bool failure = StatusTone.isFailure(status)
                || StatusTone.isFailure(ObjectiveKeyNames.resolve(player.getStatus().getLatest()));
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
