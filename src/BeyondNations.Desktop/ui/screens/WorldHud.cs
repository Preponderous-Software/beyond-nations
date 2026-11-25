using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using beyondnations.desktop.input;
using beyondnations.desktop.text;
using beyondnations.desktop.ui.boxes;
using beyondnations.desktop.ui.feel;
using beyondnations.desktop.ui.hud;
using beyondnations.desktop.ui.journal;
using beyondnations.desktop.ui.progression;

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

        // the command bar button waiting for its second click
        private readonly ConfirmGate confirmGate = new ConfirmGate();
        private readonly ButtonSettle buttonSettle = new ButtonSettle();
        // the More sheet's buttons settle on their own (CommandSheet, #512)
        private readonly ButtonSettle sheetSettle = new ButtonSettle();
        // a phone's bar in the order it was laid out, and which of it is on
        // the bar, while the rest are in the More sheet (null: not paged)
        private List<WorldCommand> sheetCommands;
        private bool[] sheetOnBar;
        private CommandBarLayout.Settings sheetSettings;
        private CommandBarLayout.Rect sheetMore;
        // the lowest the HUD's boxes and text above the bar reached last frame, 0 before then
        private float lastBarCeiling;

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

        private static float lastBarTop;

        /** The top of the command bar as last drawn (0 before the HUD is drawn). */
        public static float getBarTop() {
            return lastBarTop;
        }

        private readonly ChroniclePanel chroniclePanel = new ChroniclePanel();
        // the era box keeps whether its detail is open from frame to frame
        // (the Journal's Era tab draws it; the world HUD shows the era only
        // as a card when it changes)
        private readonly EraBox eraBox = new EraBox();

        public EraBox getEraBox() { return eraBox; }

        // --- the contextual shell -------------------------------------------------

        // every chip and contextual box goes through one path, off the bar and the touch controls
        private readonly HudPanels hudPanels = new HudPanels();
        // one overlay at a time: unlock cards, tips, toasts (OverlayQueue)
        private readonly OverlayQueue overlays = new OverlayQueue();
        private readonly HudFeed feed = new HudFeed();
        private readonly StatusLine statusLine = new StatusLine();
        private readonly MeterTracker energyMeter = new MeterTracker();
        private readonly MeterTracker foodMeter = new MeterTracker();
        private readonly MeterTracker coinMeter = new MeterTracker();
        private readonly ButtonSettle overlaySettle = new ButtonSettle();
        private IHudUnlocks unlocks = new EverythingUnlocked();
        private FeelDirector feel;
        private Simulation fedFrom;
        private string shownTipKey;
        // the command bar as last drawn, which no chip, box or card may cover
        private HudRect barRect;

        public const string JournalButtonLabel = "Journal";
        public const string MenuButtonLabel = "Menu";

        /** What decides which HUD parts and commands are shown yet (lane C's HudUnlocks); everything until it is set. */
        public void setUnlocks(IHudUnlocks value) {
            unlocks = value ?? new EverythingUnlocked();
        }

        public IHudUnlocks getUnlocks() { return unlocks; }

        /**
        * A world was started or loaded. A new, unstaged game unlocks as it
        * goes (HudUnlocks). A loaded save or --continue of a young world
        * (HudUnlocks.isYoungWorld) keeps that ramp, re-deriving what holds now
        * with no cards; an older save, a staged or advanced world and
        * --all-unlocked get everything with no cards.
        */
        public void beginWorld(Simulation simulation, bool newWorld, bool everything) {
            bool keepRamp = newWorld || HudUnlocks.isYoungWorld(simulation);
            HudUnlocks table = everything || !keepRamp ? HudUnlocks.allUnlocked() : new HudUnlocks();
            table.begin(simulation, newWorld && !everything);
            unlocks = table;
        }

        /** Tips come from the feel director's onboarding hints and show through the overlay queue. */
        public void setFeel(FeelDirector director) {
            feel = director;
            director?.setTipCardDrawnElsewhere(true);
        }

        public OverlayQueue getOverlays() { return overlays; }

        private readonly ChronicleView defaultChronicleView = new ChronicleView();

        /**
        * chronicleView is the input layer's; null is the recent view, whose
        * news now arrives as toasts (the full history is the Journal's
        * Chronicle tab, and the Full view still opens over the world).
        */
        public UiAction draw(float width, float height, Simulation simulation, bool debugMode, int framesPerSecond, ChronicleView chronicleView = null) {
            Player player = simulation.getPlayer();
            Layout layout = Layout.compute(width, height, UiScale.get());
            UiAction action = UiAction.None;
            double now = ImGui.GetTime();
            ChronicleView view = chronicleView ?? defaultChronicleView;

            bool touch = TouchLayout.isActive();
            // a window held upright (a phone's browser, a narrow desktop window) stacks like a touch screen
            bool stackCentre = isStackCentre(width, layout, touch || isPortrait(width, height));
            float bottom = height - TouchLayout.getBottomReserve();
            // the bar has the whole width: nothing sits beside it any more
            float barTop = drawCommandButtons(width, layout.Margin, width - layout.Margin, bottom, touch ? lastBarCeiling : 0f, simulation, layout);
            // the tip card keeps above it (FeelDirector.hintCardTopAboveBar)
            lastBarTop = barTop;

            float touchBand = TouchLayout.getBottomReserve();
            hudPanels.begin(width, height, layout.Gap, now, new[] {
                barRect,
                new HudRect(0f, height - touchBand, width, touchBand),
            });
            // the More sheet (and the caravan and market panels) are drawn
            // over the HUD, so while one is open no chip takes a tap
            hudPanels.setBlocked(CommandSheet.isOpen() || CaravanDispatchPanel.isOpen() || MarketTradePanel.isOpen());

            feedOverlays(simulation, view, now);

            // 1. the thin top strip: season, meters, Journal (and Menu, ?)
            float y = drawTopStrip(width, simulation, layout, touch, now, ref action);

            // 2. ONE next line, under it
            float centreWidth = stackCentre ? width - layout.Margin * 2f : System.MathF.Min(layout.CentreWrapWidth, width - layout.Margin * 2f);
            y = drawNextLine(width, simulation, layout, centreWidth, y, ref action);

            // 3. what must never be missed: hunger, the nation's stores, a siege or a collapse
            float centreY = drawHungerWarning(width, player, simulation.getSeasonCalendar().isWinter(), layout, y);
            centreY = drawResidentHungerWarning(width, simulation, layout, centreY);
            centreY = drawRealmAlert(width, simulation, layout, centreWidth, centreY, ref action);

            // 4. the status line: feedback for a few seconds, never overwritten by a hint
            centreY = drawStatus(width, player, layout, centreY, now);

            // 5. the chips and boxes shown only while they matter
            Settlement settlement = player.isCurrentlyInSettlement()
                ? simulation.getEntityRepository().getEntity(player.getCurrentSettlementId()) as Settlement
                : null;
            float columnTop = stackCentre ? centreY + layout.Gap * 0.5f : y;
            float chipRoom = stackCentre ? (width - layout.Margin * 2f) : layout.BoxWidth * 1.4f;
            if (!stackCentre) {
                // beside the centred banners (hunger, realm alert, status), never under them:
                // narrower to keep out of their band, or below them when even a box's width does not fit
                chipRoom = chipRoomBeside(layout, width, centreWidth);
                if (chipRoom < layout.BoxWidth) {
                    chipRoom = layout.BoxWidth * 1.4f;
                    columnTop = System.MathF.Max(y, centreY + layout.Gap * 0.5f);
                }
            }
            float leftBottom = drawContextChips(simulation, layout, layout.LeftX, columnTop, chipRoom, stackCentre, ref action);
            if (debugMode) {
                DebugInfoBox debugInfoBox = new DebugInfoBox(simulation);
                debugInfoBox.setFramesPerSecond(framesPerSecond);
                leftBottom = hudPanels.box(debugInfoBox, layout.LeftX, leftBottom, layout.BoxWidth) + layout.Gap;
            }
            float rightBottom = columnTop;
            if (settlement != null) {
                // stacked, the boxes start under the chips (which start under the Next line)
                rightBottom = drawSettlementBoxes(settlement, simulation, layout, stackCentre, stackCentre ? leftBottom : columnTop, barTop, width);
            }
            float contextBottom = stackCentre ? System.MathF.Max(leftBottom, rightBottom) : centreY;

            // the full chronicle (Y, News) still opens over the world
            if (view.getMode() == ChronicleMode.Full) {
                chroniclePanel.draw(width, height, simulation, view, ChroniclePanel.compactTop(centreY), layout.CentreWrapWidth);
            }

            // a town's floating name keeps below what the HUD stacks at the top (LabelPlacement)
            LabelPlacement.setTopReserve(System.MathF.Max(centreY, y) / height);
            TouchLayout.setTipTop(0f);
            TouchLayout.setTipFallbackTop(0f);
            TouchLayout.setTipSeasonTop(0f);
            // where the bar's rows may reach up to next frame on a touch screen
            lastBarCeiling = System.MathF.Max(contextBottom, centreY);

            // 6. one overlay at a time, under all of that and clear of the bar
            OverlayContext context = new OverlayContext {
                Busy = hudPanels.isBlocked() || view.getMode() == ChronicleMode.Full || WorldEventSheet.getView(simulation) != null,
                Danger = player.getEnergy() < StarvingEnergy || EraBox.siegeUnderWay(simulation) != null,
            };
            overlays.update(now, context);
            // a toast or card wider than the band clear of the chips starts below them
            float overlayTopEdge = System.MathF.Max(contextBottom, centreY);
            if (!stackCentre && overlaysChips(layout, width, chipRoom, FeelDirector.hintCardWidth(width))) {
                overlayTopEdge = System.MathF.Max(overlayTopEdge, leftBottom);
            }
            drawOverlay(width, height, layout, overlayTopEdge + layout.Gap, centreY + layout.Gap, now, touch);
            hudPanels.end();

            // the caravan dispatch panel, when Send Caravan has opened it, over the rest
            // (from the top on a touch screen, whose controls step aside while
            // it is open, so its Send and Close stay above the screen's edge)
            CaravanDispatchPanel.draw(width, height, simulation, touch ? layout.Margin : centreY + layout.Gap);
            // likewise the market panel (Trade at Market)
            MarketTradePanel.draw(width, height, simulation, touch ? layout.Margin : centreY + layout.Gap);
            // and a phone's More sheet (#512), which neither panel is open with
            drawCommandSheet(width, simulation, layout);

            return action;
        }

        // --- feeding the overlay queue ---------------------------------------------

        /**
        * What changed in the world since last frame, as overlay items: an
        * objective done, the season turning, a new era, the player's news,
        * new unlocks, and the onboarding tip showing now. A new world or a
        * loaded save only takes note of what is already so.
        */
        private void feedOverlays(Simulation simulation, ChronicleView view, double now) {
            if (simulation != fedFrom) {
                fedFrom = simulation;
                feed.reset();
            }
            ObjectiveTracker tracker = simulation.getObjectiveTracker();
            Objective completed = tracker.getLastCompleted();
            enqueue(feed.objective(tracker.getLastCompletedTick(), completed?.getTitle(),
                completed == null || completed.getReward().isEmpty() ? null : completed.getReward().describe()), now);
            SeasonCalendar calendar = simulation.getSeasonCalendar();
            OverlayItem season = feed.season(SeasonCalendar.name(calendar.getSeason()), calendar.getYear());
            if (season == null || unlocks.isUnlocked(UnlockId.Winter) || calendar.getSeason() != Season.AUTUMN) {
                enqueue(season, now);
            }
            Era era = simulation.getEraTracker().getEra();
            OverlayItem eraCard = feed.era(Eras.getDisplayName(era), EraRules.describeUnlocks(era));
            if (unlocks.isUnlocked(UnlockId.Era)) {
                enqueue(eraCard, now);
            }
            feedChronicle(simulation, view, now);
            feedLand(simulation, now);
            (unlocks as HudUnlocks)?.update(simulation);
            foreach (UnlockCard card in unlocks.drainNewCards()) {
                enqueue(OverlayItem.unlock("unlock:" + card.id, card.title, card.text), now);
            }
            feedTip(now);
        }

        private void enqueue(OverlayItem item, double now) {
            if (item != null) {
                overlays.enqueue(item, now);
            }
        }

        /** The player's news since the last look, as one toast (Hidden: none). */
        private void feedChronicle(Simulation simulation, ChronicleView view, double now) {
            Chronicle chronicle = simulation.getChronicle();
            if (chronicle == null) {
                return;
            }
            IReadOnlyList<ChronicleEntry> entries = chronicle.getEntries();
            int newest = entries.Count == 0 ? -1 : entries[entries.Count - 1].getSequence();
            if (!feed.chronicleChanged(newest)) {
                return;
            }
            List<KeyValuePair<int, string>> news = new List<KeyValuePair<int, string>>();
            int since = feed.getLastSequence();
            if (since >= 0 && view.getMode() == ChronicleMode.Recent && unlocks.isUnlocked(UnlockId.Basics)) {
                ChronicleFocus focus = ChronicleFocus.of(simulation);
                for (int i = entries.Count - 1; i >= 0 && entries[i].getSequence() > since; i--) {
                    if (focus.isPlayers(entries[i]) || focus.includes(entries[i])) {
                        news.Insert(0, new KeyValuePair<int, string>(entries[i].getSequence(), ChroniclePanel.displayLine(entries[i], focus)));
                    }
                }
            }
            enqueue(feed.chronicle(newest, news), now);
        }

        /**
        * A toast when the player crosses onto other land or into another
        * region (HudFeed.land), besides the status line; a newer crossing
        * takes the place of one still waiting or showing.
        */
        private void feedLand(Simulation simulation, double now) {
            Player player = simulation.getPlayer();
            Settlement claim = Territory.findClaimAt(simulation.getEntityRepository(), player.getPosition());
            bool own = claim != null && player.getNationId() != null && claim.getNationId() == player.getNationId();
            Region region = simulation.getRegionMap().regionAt(player.getPosition());
            string stale = feed.getLandKey();
            OverlayItem toast = feed.land(claim?.getNationName(), own, RegionCatalog.name(region), RegionCatalog.describe(region),
                player.isCurrentlyInSettlement(), now);
            if (toast != null) {
                if (stale != null) {
                    overlays.withdraw(stale);
                }
                enqueue(toast, now);
            }
        }

        /** The onboarding tip showing now, queued as a tip card; one that has gone is taken back. */
        private void feedTip(double now) {
            OnboardingHints hints = feel?.getHints();
            OnboardingHints.Step? step = hints?.getVisible();
            string key = step == null ? null : "tip:" + OnboardingHints.id(step.Value);
            if (shownTipKey != null && shownTipKey != key) {
                overlays.withdraw(shownTipKey);
            }
            shownTipKey = key;
            if (key == null || overlays.contains(key)) {
                return;
            }
            string title = "Tip " + hints.getNumber() + " of " + hints.getTotal() + ": " + OnboardingHints.title(step.Value);
            overlays.enqueue(new OverlayItem(OverlayKind.Tip, key, title, OnboardingHints.text(step.Value, feel.isTouch()), 0), now);
        }

        // --- the top strip ---------------------------------------------------------

        private const ImGuiWindowFlags StripFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoScrollbar;

        private static readonly Vector4 StripFill = new Vector4(0.06f, 0.07f, 0.09f, 0.62f);
        private static readonly Vector4 MeterDim = new Vector4(0.75f, 0.77f, 0.80f, 0.45f);
        private static readonly Vector4 EnergyColour = new Vector4(1f, 0.85f, 0.30f, 1f);
        private static readonly Vector4 FoodColour = new Vector4(0.55f, 0.90f, 0.45f, 1f);
        private static readonly Vector4 CoinColour = new Vector4(1f, 0.78f, 0.25f, 1f);
        private static readonly Vector4 NextColour = new Vector4(1f, 0.84f, 0.35f, 1f);

        /** One icon meter: which icon, its value, and whether it is lit (low or changing) and low. */
        private struct Meter {
            public char Icon;
            public int Value;
            public bool Lit;
            public bool Low;
            public Vector4 Colour;
        }

        /**
        * The thin strip along the top: the season and day (a tap opens the
        * Journal), two or three icon meters lit only when low or changing,
        * and the Journal button (with Menu and ? on a desktop; a touch screen
        * has those in its button row). Returns its bottom.
        */
        private float drawTopStrip(float width, Simulation simulation, Layout layout, bool touch, double now, ref UiAction action) {
            Player player = simulation.getPlayer();
            SeasonCalendar calendar = simulation.getSeasonCalendar();
            Inventory inventory = player.getInventory();
            float h = HudPanels.stripHeight();
            HudRect rect = hudPanels.place(new HudRect(layout.Margin, layout.Margin, width - layout.Margin * 2f, h));

            List<Meter> meters = new List<Meter>(3);
            float energy = player.getEnergy();
            bool energyLow = energy < HungryEnergy;
            meters.Add(new Meter { Icon = 'E', Value = (int) energy, Low = energyLow, Lit = energyMeter.update(energy, energyLow, now, 5f),
                Colour = energy < StarvingEnergy ? HudPanels.DangerColour : energyLow ? HudPanels.WarningColour : EnergyColour });
            int food = FoodItems.countFood(inventory);
            bool foodLow = food == 0;
            meters.Add(new Meter { Icon = 'F', Value = food, Low = foodLow && energyLow, Lit = foodMeter.update(food, foodLow && energyLow, now),
                Colour = foodLow ? HudPanels.WarningColour : FoodColour });
            int coins = inventory.getNumItems(ItemType.COIN);
            if (HudRelevance.showCoins(unlocks.isUnlocked(UnlockId.Basics), coins)) {
                meters.Add(new Meter { Icon = 'C', Value = coins, Lit = coinMeter.update(coins, false, now), Colour = CoinColour });
            }

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.SetNextWindowPos(new Vector2(rect.X, rect.Y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(rect.W, rect.H), ImGuiCond.Always);
            bool open = ImGui.Begin("##hud-top-strip", StripFlags);
            ImGui.PopStyleVar();
            if (open) {
                ImDrawListPtr draw = ImGui.GetWindowDrawList();
                draw.AddRectFilled(new Vector2(rect.X, rect.Y), new Vector2(rect.Right, rect.Bottom), ImGui.GetColorU32(StripFill), h * 0.3f);
                ImGuiStylePtr style = ImGui.GetStyle();
                float x = 0f;
                // the season and day: a chip, the touch path to the season in the Journal
                string season = SeasonCalendar.name(calendar.getSeason()) + " " + (calendar.getDayOfSeason() + 1) + "/" + SeasonCalendar.DAYS_PER_SEASON;
                float seasonWidth = ImGui.CalcTextSize(season).X + style.FramePadding.X * 2f;
                ImGui.SetCursorPos(new Vector2(x, 0f));
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0f)));
                if (ImGui.Button(season + "###hud-season", new Vector2(seasonWidth, h))) {
                    action = JournalScreen.open(JournalTab.Era);
                }
                ImGui.PopStyleColor();
                x += seasonWidth + style.ItemSpacing.X;
                // the meters: an icon and a number, dim unless low or changing
                float meterWidth = h * 0.8f + ImGui.CalcTextSize("000").X + style.ItemSpacing.X;
                foreach (Meter meter in meters) {
                    drawMeter(draw, new Vector2(rect.X + x, rect.Y), h, meter);
                    ImGui.SetCursorPos(new Vector2(x, 0f));
                    if (ImGui.InvisibleButton("##meter-" + meter.Icon, new Vector2(meterWidth, h))) {
                        action = JournalScreen.open(JournalTab.Inventory);
                    }
                    x += meterWidth;
                }
                // the buttons, from the right
                float right = rect.W;
                if (!touch) {
                    right -= buttonWidth(MenuButtonLabel, style);
                    ImGui.SetCursorPos(new Vector2(right, 0f));
                    if (ImGui.Button(MenuButtonLabel + "###hud-menu", new Vector2(buttonWidth(MenuButtonLabel, style), h))) {
                        action = UiAction.OpenPause;
                    }
                    right -= buttonWidth("?", style) + style.ItemSpacing.X;
                    ImGui.SetCursorPos(new Vector2(right, 0f));
                    if (ImGui.Button("?###hud-controls", new Vector2(buttonWidth("?", style), h))) {
                        action = UiAction.OpenControls;
                    }
                    right -= style.ItemSpacing.X;
                }
                right -= buttonWidth(JournalButtonLabel, style);
                ImGui.SetCursorPos(new Vector2(System.MathF.Max(x, right), 0f));
                if (ImGui.Button(JournalButtonLabel + "###hud-journal", new Vector2(buttonWidth(JournalButtonLabel, style), h))) {
                    action = JournalScreen.open(JournalTab.Goals);
                }
            }
            ImGui.End();
            return rect.Bottom + layout.Gap * 0.5f;
        }

        private static float buttonWidth(string label, ImGuiStylePtr style) {
            return ImGui.CalcTextSize(label).X + style.FramePadding.X * 2f + UiScale.px(8f);
        }

        /** A meter's icon (a bolt, an apple, a coin) and its number at origin, h tall. */
        private static void drawMeter(ImDrawListPtr draw, Vector2 origin, float h, Meter meter) {
            Vector4 colour = meter.Lit ? meter.Colour : MeterDim;
            uint c = ImGui.GetColorU32(colour);
            float r = h * 0.3f;
            Vector2 centre = new Vector2(origin.X + h * 0.4f, origin.Y + h * 0.5f);
            switch (meter.Icon) {
                case 'E':
                    // a lightning bolt
                    draw.AddTriangleFilled(centre + new Vector2(r * 0.25f, -r * 1.1f), centre + new Vector2(-r * 0.6f, r * 0.15f), centre + new Vector2(r * 0.05f, r * 0.15f), c);
                    draw.AddTriangleFilled(centre + new Vector2(-r * 0.05f, -r * 0.15f), centre + new Vector2(r * 0.6f, -r * 0.15f), centre + new Vector2(-r * 0.25f, r * 1.1f), c);
                    break;
                case 'F':
                    // an apple and its leaf
                    draw.AddCircleFilled(centre + new Vector2(0f, r * 0.15f), r * 0.85f, c);
                    draw.AddTriangleFilled(centre + new Vector2(0f, -r * 0.55f), centre + new Vector2(r * 0.7f, -r * 1.1f), centre + new Vector2(r * 0.15f, -r * 0.85f), c);
                    break;
                default:
                    // a coin, with its rim
                    draw.AddCircleFilled(centre, r * 0.95f, c);
                    draw.AddCircle(centre, r * 0.6f, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.45f)), 0, 1.5f);
                    break;
            }
            string number = meter.Value.ToString();
            Vector2 size = ImGui.CalcTextSize(number);
            draw.AddText(new Vector2(origin.X + h * 0.8f, origin.Y + (h - size.Y) * 0.5f),
                ImGui.GetColorU32(meter.Lit ? new Vector4(1f, 1f, 1f, 1f) : MeterDim), number);
            if (meter.Low) {
                // a low meter carries a ring as well as its colour
                draw.AddCircle(centre, r * 1.35f, ImGui.GetColorU32(meter.Colour), 0, 2f);
            }
        }

        // --- the NEXT line ---------------------------------------------------------

        private const ImGuiWindowFlags LineFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.AlwaysAutoResize;

        /**
        * ONE line saying what to do next, with a thin bar for how far along it
        * is; a tap opens the Journal's Goals tab, where the hint is. Returns
        * its bottom (y when there is nothing next).
        */
        private float drawNextLine(float width, Simulation simulation, Layout layout, float maxWidth, float y, ref UiAction action) {
            string line = NextLine.of(simulation.getObjectiveTracker());
            if (line == null) {
                return y;
            }
            ImGuiStylePtr style = ImGui.GetStyle();
            string text = ChroniclePanel.fitToWidth(line, maxWidth - style.WindowPadding.X * 2f, s => ImGui.CalcTextSize(s).X);
            Vector2 size = ImGui.CalcTextSize(text) + style.WindowPadding * 2f + new Vector2(0f, UiScale.px(4f));
            HudRect rect = hudPanels.place(new HudRect((width - size.X) / 2f, y, size.X, size.Y));
            float bottom = y;
            ImGui.SetNextWindowPos(new Vector2(rect.X, rect.Y), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.6f);
            if (ImGui.Begin("##hud-next", LineFlags)) {
                Vector2 origin = ImGui.GetCursorScreenPos();
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(NextColour));
                ImGui.TextUnformatted(text);
                ImGui.PopStyleColor();
                if (ImGui.IsItemClicked() || (ImGui.IsWindowHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))) {
                    action = JournalScreen.open(JournalTab.Goals);
                }
                float fraction = NextLine.fraction(simulation.getObjectiveTracker());
                float barWidth = ImGui.CalcTextSize(text).X;
                ImDrawListPtr draw = ImGui.GetWindowDrawList();
                Vector2 barMin = new Vector2(origin.X, origin.Y + ImGui.GetTextLineHeight() + UiScale.px(1f));
                draw.AddRectFilled(barMin, barMin + new Vector2(barWidth, UiScale.px(3f)), ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.15f)));
                draw.AddRectFilled(barMin, barMin + new Vector2(barWidth * fraction, UiScale.px(3f)), ImGui.GetColorU32(NextColour));
                ImGui.Dummy(new Vector2(barWidth, UiScale.px(4f)));
                bottom = ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y;
            }
            ImGui.End();
            return bottom + layout.Gap * 0.5f;
        }

        // --- contextual chips and boxes --------------------------------------------

        /**
        * A siege under way or a nation collapsing (a danger), or the countdown
        * to sieges (a warning), as one chip in the middle: only while it is
        * true. A tap opens the Journal's Era tab. Returns its bottom.
        */
        private float drawRealmAlert(float width, Simulation simulation, Layout layout, float maxWidth, float y, ref UiAction action) {
            string underWay = EraBox.siegeUnderWay(simulation);
            string collapse = EraBox.collapseWarning(simulation);
            string countdown = underWay == null ? EraBox.siegeWarning(simulation) : null;
            string text = collapse ?? underWay ?? countdown;
            if (text == null) {
                return y;
            }
            HudBadge badge = EraBox.badgeFor(collapse, underWay, countdown);
            float chipWidth = System.MathF.Min(maxWidth, ImGui.CalcTextSize(text).X + UiScale.px(48f));
            if (hudPanels.chip("realm-alert", text, badge, (width - chipWidth) / 2f, y, chipWidth, HudAlign.Centre, out float bottom)) {
                action = JournalScreen.open(JournalTab.Era);
            }
            return bottom + layout.Gap * 0.5f;
        }

        /**
        * The chips down the left (full width on a phone held upright): winter
        * coming and the food for it (autumn and winter only), and what the
        * player is carrying (only while they carry something). Each tap
        * opens the Journal. Returns the bottom of the last.
        */
        private float drawContextChips(Simulation simulation, Layout layout, float x, float y, float room, bool stackCentre, ref UiAction action) {
            SeasonCalendar calendar = simulation.getSeasonCalendar();
            Player player = simulation.getPlayer();
            float bottom = y;
            if (HudRelevance.showWinterLines(calendar.getSeason(), unlocks.isUnlocked(UnlockId.Winter))) {
                string winter = SeasonInfoBox.getWinterLine(calendar);
                if (winter != null && hudPanels.chip("winter", winter, calendar.isWinter() ? HudBadge.None : HudBadge.Warning, x, bottom, room, HudAlign.Left, out bottom)) {
                    action = JournalScreen.open(JournalTab.Nation);
                }
                if (winter != null) {
                    bottom += layout.Gap * 0.5f;
                }
                string stores = SeasonInfoBox.getStoresLine(calendar, simulation.getEntityRepository(), player);
                string famine = SeasonInfoBox.getFamineLine(calendar, simulation.getEntityRepository(), player);
                string food = famine != null ? (stores != null ? stores + " - " : "") + famine : stores;
                if (food != null && hudPanels.chip("stores", food, famine != null || winterStoresShort ? HudBadge.Warning : HudBadge.None, x, bottom, room, HudAlign.Left, out bottom)) {
                    action = JournalScreen.open(JournalTab.Nation);
                }
                if (food != null) {
                    bottom += layout.Gap * 0.5f;
                }
            }
            string carrying = HudRelevance.carryingLine(carried(player.getInventory()));
            if (HudRelevance.showCarrying(carrying == null ? 0 : 1)
                    && hudPanels.chip("carrying", carrying, HudBadge.None, x, bottom, room, HudAlign.Left, out bottom)) {
                action = JournalScreen.open(JournalTab.Inventory);
            }
            if (carrying != null) {
                bottom += layout.Gap * 0.5f;
            }
            return bottom;
        }

        /**
        * How wide the chips down the left may be on a wide screen, where the
        * centred banners (hunger, realm alert, status) share their rows:
        * up to 1.4 boxes, but never into the centred band, so a long chip
        * is not drawn under a banner. Less than a box's width means the
        * band leaves no room beside it, and the chips go below the banners.
        */
        public static float chipRoomBeside(Layout layout, float width, float centreWidth) {
            float centreLeft = (width - centreWidth) / 2f;
            return System.MathF.Min(layout.BoxWidth * 1.4f, centreLeft - layout.Gap - layout.LeftX);
        }

        /** Whether a centred item itemWidth wide would reach over chips chipRoom wide down the left. */
        public static bool overlaysChips(Layout layout, float width, float chipRoom, float itemWidth) {
            return (width - itemWidth) / 2f < layout.LeftX + chipRoom + layout.Gap;
        }

        /** What the player carries, most first, coins left out (they are a meter). */
        public static List<KeyValuePair<string, int>> carried(Inventory inventory) {
            List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>();
            foreach (ItemType type in System.Enum.GetValues(typeof(ItemType))) {
                if (type == ItemType.COIN) {
                    continue;
                }
                int count = inventory.getNumItems(type);
                if (count > 0) {
                    items.Add(new KeyValuePair<string, int>(ItemNames.getDisplayName(type).ToLowerInvariant(), count));
                }
            }
            items.Sort((a, b) => b.Value.CompareTo(a.Value));
            return items;
        }

        /**
        * The settlement and market boxes, only while the player is inside a
        * settlement (the market once markets are unlocked): down the right on
        * a wide screen, side by side under the middle on a phone held
        * upright, kept off the bar. Returns the bottom of the lower one.
        */
        private float drawSettlementBoxes(Settlement settlement, Simulation simulation, Layout layout, bool stackCentre, float top, float barTop, float width) {
            SettlementInfoBox settlementBox = new SettlementInfoBox(settlement, simulation.getNationRepository(), simulation.getEntityRepository());
            MarketInfoBox marketBox = HudRelevance.showMarket(true, settlement.getMarket() != null, unlocks.isUnlocked(UnlockId.Market))
                ? new MarketInfoBox(settlement.getMarket(), simulation.getEntityRepository())
                : null;
            InfoBox.Metrics metrics = InfoBox.Metrics.current();
            float room = barTop - layout.Gap - top;
            // on a phone held upright, while a card shows, the market lists fewer prices, so the card
            // has room under the boxes rather than covering their rows
            if (stackCentre && overlays.getCurrent() != null && overlays.getCurrent().Kind != OverlayKind.Toast) {
                room -= lastCardHeight + layout.Gap;
            }
            if (stackCentre) {
                float half = (width - layout.Margin * 2f - layout.Gap) / 2f;
                settlementBox.setCompact(true);
                // the market goes compact and lists only the prices there is room for; when even
                // that is too tall it is left out (Trade at Market still shows it), never drawn
                // over the card or the bar
                if (marketBox != null) {
                    marketBox.setCompact(true);
                    marketBox.fitHeight(room, half, metrics);
                    if (!boxFits(marketBox.estimateHeight(half, metrics), room)) {
                        marketBox = null;
                    }
                }
                float settlementWidth = marketBox == null ? width - layout.Margin * 2f : half;
                // likewise the settlement box, when a tall bar (a pawn in reach) and a card leave no
                // room for it (the bar's Settlement button still opens it in full)
                if (!boxFits(settlementBox.estimateHeight(settlementWidth, metrics), room)) {
                    return top;
                }
                float bottom = hudPanels.box(settlementBox, layout.LeftX, top, settlementWidth);
                if (marketBox != null) {
                    bottom = System.MathF.Max(bottom, hudPanels.box(marketBox, layout.LeftX + half + layout.Gap, top, half));
                }
                return bottom + layout.Gap;
            }
            float y = hudPanels.box(settlementBox, layout.RightX, top, layout.BoxWidth) + layout.Gap;
            if (marketBox != null) {
                marketBox.fitHeight(barTop - layout.Gap - y, layout.BoxWidth, metrics);
                y = hudPanels.box(marketBox, layout.RightX, y, layout.BoxWidth) + layout.Gap;
            }
            return y;
        }

        /** Whether a stacked box height tall fits in the room left under the boxes' top. */
        public static bool boxFits(float height, float room) {
            return height <= room;
        }

        // --- the overlay card -------------------------------------------------------

        private static readonly Vector4 TipHeadingColour = new Vector4(1f, 0.84f, 0.35f, 1f);
        private static readonly Vector4 UnlockHeadingColour = new Vector4(0.55f, 0.85f, 1f, 1f);
        private static readonly Vector4 ToastHeadingColour = new Vector4(0.65f, 0.95f, 0.65f, 1f);

        private const ImGuiWindowFlags CardFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoNav;

        /**
        * The one overlay showing (OverlayQueue): a toast fades in and out on
        * its own; a card (an unlock, a new era, a tip) has Got it, and a tip
        * No tips as well, touch-sized on a phone. Centred under the HUD's
        * middle, never over the command bar.
        */
        private void drawOverlay(float width, float height, Layout layout, float top, float floor, double now, bool touch) {
            OverlayItem item = overlays.getCurrent();
            if (item == null) {
                TouchLayout.setTipHeight(0f);
                overlaySettle.endFrame();
                return;
            }
            float cardWidth = FeelDirector.hintCardWidth(width);
            bool card = item.Kind != OverlayKind.Toast;
            // a toast just under the HUD's middle; a card where the tip card
            // always went (below the player on a desktop, in the sky on a
            // phone), never higher than the HUD's middle and never on the bar
            float cardHeight = lastCardHeight > 0f ? lastCardHeight : UiScale.px(90f);
            float cardTop = overlayTop(card, top, floor, FeelDirector.hintCardTop(height, touch), cardHeight, lastBarTop, layout.Gap);
            HudRect rect = new HudRect((width - cardWidth) / 2f, cardTop, cardWidth, cardHeight);
            double age = overlays.getShownFor(now);
            float alpha = item.Seconds > 0 ? (float) System.Math.Clamp(System.Math.Min(age / 0.25, (item.Seconds - age) / 0.5), 0.0, 1.0) : 1f;
            ImGui.SetNextWindowPos(new Vector2(rect.X, rect.Y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(cardWidth, 0f), ImGuiCond.Always);
            // all but opaque, so a box it covers on a small screen does not show through
            ImGui.SetNextWindowBgAlpha(0.97f * alpha);
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, System.MathF.Max(0.05f, alpha));
            // in front of the HUD's boxes, which would otherwise draw over its text and its
            // Got it on a small screen; not while a button is held, so a press on the bar or
            // a chip is not cut short by the focus moving
            if (!ImGui.IsAnyItemActive() && !hudPanels.isBlocked()) {
                ImGui.SetNextWindowFocus();
            }
            bool dismiss = false;
            bool skipTips = false;
            if (ImGui.Begin("##hud-overlay", CardFlags | (card ? 0 : ImGuiWindowFlags.NoInputs))) {
                Vector4 heading = item.Kind == OverlayKind.Tip ? TipHeadingColour : item.Kind == OverlayKind.Toast ? ToastHeadingColour : UnlockHeadingColour;
                Overlay.textColoured(heading, item.Title);
                if (!string.IsNullOrEmpty(item.Body)) {
                    ImGui.PushTextWrapPos(0f);
                    ImGui.TextUnformatted(item.Body);
                    ImGui.PopTextWrapPos();
                }
                if (card) {
                    float buttonHeight = touch ? UiScale.px(TouchLayout.MinTarget) : 0f;
                    float buttonWidth = UiScale.px(touch ? 110f : 84f);
                    Vector2 at = ImGui.GetCursorScreenPos();
                    bool settled = overlaySettle.isSettled("overlay-" + item.Key, System.MathF.Round(at.X), System.MathF.Round(at.Y), now);
                    if (ImGui.Button("Got it###overlay-ok", new Vector2(buttonWidth, buttonHeight)) && settled) {
                        dismiss = true;
                    }
                    if (item.Kind == OverlayKind.Tip) {
                        ImGui.SameLine();
                        if (ImGui.Button("No tips###overlay-no-tips", new Vector2(buttonWidth, buttonHeight)) && settled) {
                            dismiss = true;
                            skipTips = true;
                        }
                    }
                }
                lastCardHeight = ImGui.GetWindowSize().Y;
            }
            ImGui.End();
            ImGui.PopStyleVar();
            overlaySettle.endFrame();
            TouchLayout.setTipHeight(item.Kind == OverlayKind.Tip ? lastCardHeight : 0f);
            if (dismiss) {
                overlays.dismiss(now);
                if (item.Kind == OverlayKind.Tip && feel != null) {
                    if (skipTips) {
                        feel.getHints().skipAll();
                    } else {
                        feel.getHints().dismiss();
                    }
                    shownTipKey = null;
                }
            }
        }

        /**
        * Where the overlay goes: a toast just under everything the HUD
        * stacks at the top (top); a card no higher than that and no higher
        * than where the tip card always went (preferred), both kept above the
        * command bar (barTop, 0 for none). With no room for it there it rises,
        * over the contextual chips if need be, but never above floor, the
        * bottom of the Next line, the warnings and the status line, which it
        * must not hide.
        */
        public static float overlayTop(bool card, float top, float floor, float preferred, float height, float barTop, float gap) {
            float y = card ? System.MathF.Max(top, preferred) : top;
            if (barTop > 0f && y + height + gap > barTop) {
                y = barTop - gap - height;
            }
            return System.MathF.Max(y, floor);
        }

        private float lastCardHeight;
        // this frame: winter is a day off and members are short, shown as the food chip's badge
        private bool winterStoresShort;
        /**
        * Whether the boxes, as they are set now (full or compact), fit one
        * above another in height at boxWidth. Judged on the boxes' wrapped
        * line counts, not last frame's heights, so a column that has just
        * gone compact does not flip back next frame.
        */
        public static bool fits(float height, float gap, float boxWidth, InfoBox.Metrics metrics, params InfoBox[] boxes) {
            return stackedHeight(gap, boxWidth, metrics, boxes) <= height;
        }

        /** The height of the boxes one above another, gap apart. */
        public static float stackedHeight(float gap, float boxWidth, InfoBox.Metrics metrics, params InfoBox[] boxes) {
            float needed = gap * (boxes.Length - 1);
            foreach (InfoBox box in boxes) {
                needed += box.estimateHeight(boxWidth, metrics);
            }
            return needed;
        }

        /**
        * Whether a box boxHeight tall fits between top and bottom. The
        * inventory box used to be pushed down to top when it did not, over
        * the command bar under it.
        */
        public static bool fitsAbove(float top, float bottom, float boxHeight) {
            return top + boxHeight <= bottom;
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

        /** A window taller than it is wide: laid out like a phone held upright, touch or not. */
        public static bool isPortrait(float width, float height) {
            return height > width;
        }

        /**
        * How wide the left column (era, objective) is: a box's width, or
        * on a phone held upright (isStackCentre) everything left of the
        * player box, since nothing sits between the two there.
        */
        public static float getLeftColumnWidth(Layout layout, bool stackCentre) {
            return stackCentre ? System.MathF.Max(layout.BoxWidth, layout.RightX - layout.Gap - layout.LeftX) : layout.BoxWidth;
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
        * Where the command bar starts on the left: the margin, except on a
        * phone on its side, whose left column (era, objective) fills the
        * height above the touch controls, so the bar keeps between the
        * columns, under the chronicle (#466).
        */
        public static float getBarLeft(Layout layout, bool touch, bool stackCentre) {
            return touch && !stackCentre ? layout.LeftX + layout.BoxWidth + layout.Gap : layout.Margin;
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

        /**
        * The command buttons along the bottom. Which ones are offered is
        * WorldCommandBar's decision and where each goes is CommandBarLayout's;
        * this draws them and runs the one that was clicked, asking for a
        * second click first for the ones that cannot be taken back. Returns
        * the y of the top of the bar (the bottom margin when there are no
        * commands).
        */
        private float drawCommandButtons(float width, float barLeft, float barRight, float height, float ceiling, Simulation simulation, Layout layout) {
            // a "Click again" prompt whose window has lapsed is taken back,
            // even when its button has since gone from the bar
            confirmGate.update(ImGui.GetTime(), simulation.getPlayer().getStatus());
            // only the commands that apply here and are unlocked (IHudUnlocks)
            List<WorldCommand> commands = WorldCommandBar.getCommands(simulation).FindAll(command => unlocks.commandUnlocked(command.getLabel()));
            barRect = new HudRect();
            sheetCommands = null;
            if (commands.Count == 0) {
                CommandSheet.close();
                buttonSettle.endFrame();
                return height - layout.Margin;
            }

            ImGuiStylePtr style = ImGui.GetStyle();
            CommandBarLayout.Settings settings = barSettings(width, barRight, height, layout,
                UiScale.get(), Overlay.frameHeight(), style.ItemSpacing.X, style.ItemSpacing.Y, style.FramePadding.X * 2f);
            settings.Left = System.MathF.Min(barLeft, settings.Right - 1f);
            // inside a settlement the pawn rows are kept, pawn or no pawn (#486);
            // worked out for this bar's width, so after Left is set
            settings.ReservedPeopleRows = WorldCommandBar.getReservedPeopleRows(simulation, label => ImGui.CalcTextSize(label).X, settings);
            // the rows left under what the HUD drew above last frame (0: no limit)
            settings.MaxRows = ceiling > 0f
                ? CommandBarLayout.rowsBetween(ceiling + layout.Gap, settings.Bottom, settings.ButtonHeight, settings.SpacingY)
                : 0;
            CommandBarLayout.Result placed = arrangeBar(ref commands, label => ImGui.CalcTextSize(label).X, settings, WorldCommandBar.contextOf(simulation));
            if (placed.isPaged()) {
                // the rest are in the More sheet, drawn over the HUD at the end (drawCommandSheet)
                sheetCommands = commands;
                sheetOnBar = placed.Shown;
                sheetSettings = settings;
                sheetMore = placed.More;
            } else {
                CommandSheet.close();
            }
            // what docked boxes keep off (HudPanels)
            barRect = new HudRect(settings.Left, placed.Top, settings.Right - settings.Left, settings.Bottom - placed.Top);
            Vector2 padding = style.WindowPadding;
            double now = ImGui.GetTime();

            // while the sheet is open it covers the bar, and holds its Close
            if (placed.isPaged() && CommandSheet.isOpen()) {
                buttonSettle.endFrame();
                return placed.Top;
            }

            ImGui.SetNextWindowPos(new Vector2(settings.Left - padding.X, placed.Top - padding.Y), ImGuiCond.Always);
            if (ImGui.Begin("##world-command-bar", BarFlags)) {
                WorldCommand clicked = null;
                if (placed.isPaged()) {
                    // everything not on the bar, in a sheet (#512); the button
                    // waits like the rest before it takes a tap (ButtonSettle)
                    CommandBarLayout.Rect more = placed.More;
                    ImGui.SetCursorPos(new Vector2(more.X - settings.Left + padding.X, more.Y - placed.Top + padding.Y));
                    if (sheetToggle(CommandSheet.MoreLabel, more, buttonSettle, now)) {
                        CommandSheet.setOpen(true);
                        confirmGate.disarm();
                    }
                }
                for (int i = 0; i < commands.Count; i++) {
                    if (!placed.Shown[i]) {
                        continue;
                    }
                    CommandBarLayout.Rect rect = placed.Buttons[i];
                    ImGui.SetCursorPos(new Vector2(rect.X - settings.Left + padding.X, rect.Y - placed.Top + padding.Y));
                    if (drawCommandButton(commands[i], rect, settings, buttonSettle, simulation.getPlayer().getStatus(), now)) {
                        clicked = commands[i];
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
            buttonSettle.endFrame();
            return placed.Top;
        }

        /**
        * Where the bar's buttons go: in the group order, or, on a phone
        * whose bar has more buttons than rows, in its front-first order for
        * where the player stands, a page of it on the bar and the rest in
        * the More sheet (#512). commands is set to the order laid out.
        */
        public static CommandBarLayout.Result arrangeBar(ref List<WorldCommand> commands, System.Func<string, float> measure,
                CommandBarLayout.Settings settings, WorldCommandBar.BarContext context) {
            CommandBarLayout.Result placed = CommandBarLayout.compute(commands, measure, settings);
            if (!placed.isPaged()) {
                return placed;
            }
            commands = WorldCommandBar.frontFirst(commands, context);
            return CommandBarLayout.firstPage(commands, measure, settings);
        }

        /** More, or the sheet's Close in its place: true when tapped once it has settled. */
        private static bool sheetToggle(string label, CommandBarLayout.Rect rect, ButtonSettle settle, double now) {
            bool usable = settle.isSettled(label, rect.X, rect.Y, now);
            if (!usable) {
                ImGui.BeginDisabled();
            }
            bool tapped = ImGui.Button(label + "###bar-more", new Vector2(rect.Width, rect.Height)) && usable;
            if (!usable) {
                ImGui.EndDisabled();
            }
            return tapped;
        }

        /**
        * One command button where the cursor is, rect being where it is on
        * the screen: coloured for its group, greyed while it cannot be used
        * or is still settling (ButtonSettle), its label cut to fit. A tap on
        * a greyed one says why on the status line. Returns true when it was
        * clicked and can run.
        */
        private bool drawCommandButton(WorldCommand command, CommandBarLayout.Rect rect, CommandBarLayout.Settings settings,
                ButtonSettle settle, Status status, double now) {
            bool armed = command.needsConfirm() && confirmGate.isArmed(command.getLabel(), now);
            string full = armed ? CommandBarLayout.getArmedLabel(command) : command.getLabel();
            // a pawn's long name is cut to the button rather than widening it (CommandBarLayout)
            string text = CommandBarLayout.fitLabel(full, label => ImGui.CalcTextSize(label).X, rect.Width - settings.TextPadding);
            string tooltip = command.getHoverText(text, full);
            // a button that has just appeared or moved waits a moment
            // before it takes a click (ButtonSettle)
            bool usable = command.isEnabled() && settle.isSettled(command.getLabel(), rect.X, rect.Y, now);
            ButtonColours colours = UiTheme.commandButton(usable, armed, command.getGroup() == CommandGroup.Drastic);
            ImGui.PushStyleColor(ImGuiCol.Button, colours.Fill);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, colours.Hovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, colours.Active);
            ImGui.PushStyleColor(ImGuiCol.Text, colours.Text);
            if (!usable) {
                // opaque as well: UiTheme.Disabled is already the dimmed look
                ImGui.PushStyleVar(ImGuiStyleVar.DisabledAlpha, 1f);
                ImGui.BeginDisabled();
            }
            bool clicked = false;
            // the id is the plain label, so arming keeps the same button
            if (ImGui.Button(text + "###" + command.getLabel(), new Vector2(rect.Width, rect.Height)) && usable) {
                clicked = true;
            }
            // an armed button stays armed while the pointer rests on it,
            // however slowly the frames come (ConfirmGate.hold)
            if (armed && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                confirmGate.hold(command.getLabel(), now);
            }
            // on a touch screen a tap leaves the pointer resting on the
            // button, and its tooltip covered the buttons beside it and
            // ran off the screen's edge; the tap's status line (below)
            // already says why a greyed one cannot be used
            if (tooltip != null && !TouchLayout.isActive() && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(tooltip);
                ImGui.EndTooltip();
            }
            // a touch screen has no hover to show that tooltip on (#444)
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                string reason = explainDisabled(command, status);
                if (CommandSheet.isOpen() && reason != null) {
                    // the sheet covers the status line, so it says why too
                    CommandSheet.setNote(reason);
                }
            }
            if (!usable) {
                ImGui.EndDisabled();
                ImGui.PopStyleVar();
            }
            ImGui.PopStyleColor(4);
            return clicked;
        }

        /** The sheet's heading colour. */
        private static readonly Vector4 SheetHeadingColour = new Vector4(0.95f, 0.80f, 0.35f, 1f);

        private const ImGuiWindowFlags SheetFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse;

        /**
        * The More sheet (CommandSheet, #512), when it is open: over the HUD
        * from the top margin down to the bar's bottom, the full width, its
        * buttons under headings in a list a finger drags to scroll, a line
        * over them saying why the last tapped one cannot be used, and Close
        * where More was. Drawn last, so it sits over the boxes.
        */
        private void drawCommandSheet(float width, Simulation simulation, Layout layout) {
            if (sheetCommands == null || !CommandSheet.isOpen()) {
                sheetSettle.endFrame();
                return;
            }
            List<WorldCommand> commands = sheetCommands;
            CommandBarLayout.Settings bar = sheetSettings;
            ImGuiStylePtr style = ImGui.GetStyle();
            Vector2 padding = style.WindowPadding;
            double now = ImGui.GetTime();
            float left = layout.Margin;
            float right = width - layout.Margin;
            float top = layout.Margin;
            float bottom = bar.Bottom;
            CommandBarLayout.Settings settings = bar;
            settings.Left = 0f;
            settings.Right = right - left;
            List<CommandSheet.Section> sections = CommandSheet.sections(commands, sheetOnBar);
            float headingHeight = ImGui.GetTextLineHeight();
            CommandSheet.Placement placement = CommandSheet.layout(commands, sections, label => ImGui.CalcTextSize(label).X, settings, headingHeight);

            ImGui.SetNextWindowPos(new Vector2(left - padding.X, top - padding.Y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(right - left + padding.X * 2f, bottom - top + padding.Y * 2f), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.94f);
            // over the HUD's boxes, which would otherwise draw over it
            ImGui.SetNextWindowFocus();
            WorldCommand clicked = null;
            bool close = false;
            if (ImGui.Begin("##command-sheet", SheetFlags)) {
                ImGui.PushTextWrapPos(right - left + padding.X);
                ImGui.TextUnformatted(CommandSheet.getNote());
                ImGui.PopTextWrapPos();
                float listTop = ImGui.GetCursorPosY();
                // the list runs down to the row Close is on
                float footerTop = sheetMore.Y - top + padding.Y - bar.SpacingY;
                float listHeight = System.MathF.Max(bar.ButtonHeight, footerTop - listTop);
                if (ImGui.BeginChild("##command-sheet-list", new Vector2(right - left, listHeight))) {
                    float scrollY = ImGui.GetScrollY();
                    Vector2 origin = ImGui.GetWindowPos();
                    for (int s = 0; s < sections.Count; s++) {
                        ImGui.SetCursorPos(new Vector2(0f, placement.HeadingTops[s]));
                        Overlay.textColoured(SheetHeadingColour, sections[s].Title);
                        foreach (int i in sections[s].Commands) {
                            CommandBarLayout.Rect rect = placement.Buttons[i];
                            ImGui.SetCursorPos(new Vector2(rect.X, rect.Y));
                            // where it is on the screen, so a scroll counts as a move (ButtonSettle)
                            CommandBarLayout.Rect onScreen = rect;
                            onScreen.X = origin.X + rect.X;
                            onScreen.Y = origin.Y + rect.Y - scrollY;
                            if (drawCommandButton(commands[i], onScreen, settings, sheetSettle, simulation.getPlayer().getStatus(), now)) {
                                clicked = commands[i];
                            }
                        }
                    }
                    // room under the last row
                    ImGui.SetCursorPos(new Vector2(0f, placement.Height));
                    ImGui.Dummy(new Vector2(1f, bar.SpacingY));
                    // drag anywhere in the list to scroll it: the touch path
                    if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) && ImGui.IsMouseDragging(ImGuiMouseButton.Left)) {
                        ImGui.SetScrollY(EndOfRunScreen.dragScroll(ImGui.GetScrollY(), ImGui.GetScrollMaxY(), ImGui.GetIO().MouseDelta.Y));
                    }
                }
                ImGui.EndChild();
                // Close where More was, so the same spot opens and shuts the sheet
                ImGui.SetCursorPos(new Vector2(sheetMore.X - left + padding.X, sheetMore.Y - top + padding.Y));
                if (sheetToggle(CommandSheet.CloseLabel, sheetMore, sheetSettle, now)) {
                    close = true;
                }
            }
            ImGui.End();
            sheetSettle.endFrame();
            if (close) {
                CommandSheet.close();
                confirmGate.disarm();
                return;
            }
            if (clicked != null) {
                // a button that ran closes the sheet, so its result on the
                // status line (or the panel it opened) can be seen; one
                // waiting for its second tap keeps it open, saying so
                if (runClicked(clicked, simulation.getPlayer(), now)) {
                    CommandSheet.close();
                } else {
                    CommandSheet.setNote(getConfirmPrompt(clicked, true));
                }
            }
        }

        /** A click on a bar button: run it, or arm it when it needs a second click. True when it ran. */
        private bool runClicked(WorldCommand command, Player player, double now) {
            if (!command.needsConfirm()) {
                confirmGate.disarm();
                command.execute();
                return true;
            }
            if (confirmGate.press(command.getLabel(), now)) {
                command.execute();
                return true;
            }
            confirmGate.prompt(player.getStatus(), getConfirmPrompt(command, TouchLayout.isActive()));
            return false;
        }

        /**
        * A click or tap on a disabled button puts why on the status line. It
        * is said as a prompt, so a second click does not read "(x2)". An
        * enabled button, even one still settling (ButtonSettle), says nothing.
        * A button with a tap explanation (WorldCommand.setTapExplanation)
        * says that fuller one, worked out only now (#521). Returns what it
        * said, or null.
        */
        public static string explainDisabled(WorldCommand command, Status status) {
            string reason = command.explainTap();
            if (reason != null) {
                status.prompt(reason);
            }
            return reason;
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
            return getHungerWarning(player, false);
        }

        /**
        * In winter the trees are bare for the player too
        * (SeasonalRules.treesBearFruit), so the warning stops sending a
        * hungry player to one.
        */
        public static string getHungerWarning(Player player, bool winter) {
            float energy = player.getEnergy();
            if (energy >= HungryEnergy) {
                return null;
            }
            string howToEat = ObjectiveKeyNames.resolve(winter
                ? "Trees are bare in winter: press {Interact} at a chicken for meat, or buy food in a settlement."
                : "Press {Interact} at a tree or a chicken for food, or buy food in a settlement.");
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
        private float drawHungerWarning(float width, Player player, bool winter, Layout layout, float y) {
            string warning = getHungerWarning(player, winter);
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
            // where the player stands, so the banner counts those here the way Feed Residents does
            Settlement here = new FeedResidentsCommand(simulation.getEntityRepository()).getOwnSettlement(player);
            string warning = ResidentHunger.describeWarning(simulation.getEntityRepository(), nation, simulation.getSeasonCalendar().isWinter(), here);
            // and a day before winter, the nation's stores while any member is short (WinterStores):
            // a banner for the whole day sat over the chips, so while the winter food chip shows
            // it is that chip's warning badge instead (drawContextChips)
            string stores = warning == null
                ? WinterStores.describeWarning(simulation.getEntityRepository(), nation, simulation.getSeasonCalendar(), here)
                : null;
            winterStoresShort = stores != null
                && HudRelevance.showWinterLines(simulation.getSeasonCalendar().getSeason(), unlocks.isUnlocked(UnlockId.Winter));
            if (!winterStoresShort) {
                warning ??= stores;
            }
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
        * The status line: what the player's last command did, or why it could
        * not, for a few seconds of real time (StatusLine), then gone, even
        * while the world stands still; a refusal on red with a "!" in front,
        * and a new message flashing brighter for a moment so a repeated
        * press is noticed. A hint never takes the place of feedback.
        */
        private float drawStatus(float width, Player player, Layout layout, float y, double now) {
            string said = ObjectiveKeyNames.resolve(player.getStatus().getStatus());
            // a line can join several messages said in one step: it is a
            // refusal if it starts with one or the newest one is
            bool refusal = !string.IsNullOrEmpty(said) && (StatusTone.isFailure(said)
                || StatusTone.isFailure(ObjectiveKeyNames.resolve(player.getStatus().getLatest())));
            statusLine.observe(said, refusal, now);
            string status = statusLine.visible(now);
            if (string.IsNullOrEmpty(status)) {
                return y;
            }
            bool failure = statusLine.isRefusal(now);
            float flash = (float) System.Math.Clamp(1.0 - statusLine.age(now) / StatusFlashSeconds, 0.0, 1.0);
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
