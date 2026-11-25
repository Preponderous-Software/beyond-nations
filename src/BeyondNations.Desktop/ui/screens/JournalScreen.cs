using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using beyondnations.desktop.input;
using beyondnations.desktop.ui.journal;

namespace beyondnations.desktop.ui.screens {

    /**
    * The Journal: one picture book of how the run is going, in five tabs --
    * Goals (the objective tracker: the goal being worked on, the ones done
    * and the ones ahead, with a vine for the first arc), Chronicle (the
    * chronicle's history), Era (the ladder of eras, what the next one asks
    * with the calendar's wait among it, the season and, late on, the
    * victories), Inventory (what the player carries) and Nation (the
    * nation's numbers).
    *
    * Everything is a tile: a picture drawn in code (JournalIcons), a big
    * number, a progress ring where there is progress, and a word. The
    * sentences -- a goal's hint, an entry's line and date, an era's
    * requirements -- show only on the card a tapped (or clicked) tile opens,
    * after the owner's note on Primordial Rise's journal: more pictures,
    * less text. What the tiles say comes from JournalContent, which reads the
    * same sources as the HUD's boxes and the other screens.
    *
    * Touch first: tabs and tiles are at least a fingertip (44 px) each way,
    * a sideways swipe turns the page, an upward drag scrolls, and the
    * corner Close closes it (TouchLayout). On the desktop the host reads J
    * and Escape to close it and readKeys turns pages with the arrow keys or
    * Tab; no text a phone player sees names a key.
    *
    * Opened like the other full screens, over the stopped world
    * (ScreenType.JOURNAL): with J, or from anything that can return a
    * UiAction -- the HUD's Journal button calls open(tab).
    */
    public class JournalScreen {
        public const string Title = "Journal";

        /** A drag this far sideways, and mostly sideways, turns the page. */
        public const float SwipeDistance = 70f;
        /** A press that moves no further than this is a tap. */
        public const float TapSlop = 10f;

        // asked for by open() or --stage-journal, taken when the screen next draws
        private static JournalTab? requestedTab;
        // --stage-journal tab:N opens the card of the N-th tile (from 1), for screenshots
        private static int requestedCard;
        // keys read by the host between frames (readKeys)
        private static int pendingTurn;
        private static float pendingScrollRows;

        private JournalTab tab = JournalTab.Goals;
        private string selectedId;
        private float scroll;
        private ChronicleScope chronicleScope = ChronicleScope.YourNews;

        // the press being followed: where it began and how far it has wandered
        private bool pressing;
        private Vector2 pressStart;
        private float pressWander;

        /**
        * Opens the Journal on a tab: returns the UiAction the hosts turn into
        * the screen change, so a HUD button is one line:
        * <c>return JournalScreen.open(JournalTab.Goals);</c>
        */
        public static UiAction open(JournalTab tab = JournalTab.Goals) {
            requestedTab = tab;
            return UiAction.OpenJournal;
        }

        /** --stage-journal: the tab the screen opens on, and the tile whose card is open (from 1; 0 for none). */
        public static void requestTab(JournalTab tab, int card = 0) {
            requestedTab = tab;
            requestedCard = card;
        }

        /** The arrow keys and Tab turn the page; Up/Down and Page Up/Down scroll. Called by the host while the screen is open. */
        public static void readKeys(InputService input) {
            if (input.wasPressedThisFrame(Silk.NET.Input.Key.Left)) {
                pendingTurn--;
            }
            if (input.wasPressedThisFrame(Silk.NET.Input.Key.Right) || input.wasPressedThisFrame(Silk.NET.Input.Key.Tab)) {
                pendingTurn++;
            }
            if (input.wasPressedThisFrame(Silk.NET.Input.Key.Up)) {
                pendingScrollRows -= 1f;
            }
            if (input.wasPressedThisFrame(Silk.NET.Input.Key.Down)) {
                pendingScrollRows += 1f;
            }
            if (input.wasPressedThisFrame(Silk.NET.Input.Key.PageUp)) {
                pendingScrollRows -= 3f;
            }
            if (input.wasPressedThisFrame(Silk.NET.Input.Key.PageDown)) {
                pendingScrollRows += 3f;
            }
        }

        // --- state, kept apart from drawing so the tests can drive it ---------------

        public JournalTab getTab() {
            return tab;
        }

        /** Shows a tab: the card closes and the page starts at the top. */
        public void showTab(JournalTab value) {
            if (value != tab) {
                selectedId = null;
                scroll = 0f;
            }
            tab = value;
        }

        /** Turns forward (steps > 0) or back through the tabs, wrapping round. */
        public void turn(int steps) {
            JournalTab value = tab;
            for (int i = 0; i < System.Math.Abs(steps); i++) {
                value = steps > 0 ? JournalTabs.next(value) : JournalTabs.previous(value);
            }
            showTab(value);
        }

        /** Takes a tab asked for by open() or --stage-journal, if any. */
        public void takeRequest() {
            if (requestedTab.HasValue) {
                selectedId = null;
                scroll = 0f;
                tab = requestedTab.Value;
                requestedTab = null;
                requestedCard = 0;
            }
        }

        /** A tap on a tile opens its card; a tap on the tile whose card is open closes it. */
        public void tap(JournalTile tile) {
            selectedId = tile == null || tile.id == selectedId ? null : tile.id;
        }

        public string getSelectedId() {
            return selectedId;
        }

        /** The tile whose card is open, among these, or null. */
        public JournalTile selected(List<JournalTile> tiles) {
            if (selectedId == null) {
                return null;
            }
            foreach (JournalTile tile in tiles) {
                if (tile.id == selectedId) {
                    return tile;
                }
            }
            return null;
        }

        public void closeCard() {
            selectedId = null;
        }

        public ChronicleScope getChronicleScope() {
            return chronicleScope;
        }

        public void setChronicleScope(ChronicleScope scope) {
            if (scope != chronicleScope) {
                selectedId = null;
                scroll = 0f;
            }
            chronicleScope = scope;
        }

        /** Whether a drag of (dx, dy) is a swipe that turns the page: far enough, and mostly sideways. */
        public static bool isSwipe(float dx, float dy, float distance) {
            return System.MathF.Abs(dx) >= distance && System.MathF.Abs(dx) > System.MathF.Abs(dy) * 1.5f;
        }

        /** The tiles the current tab shows. */
        public List<JournalTile> tiles(Simulation simulation) {
            return JournalContent.tilesFor(tab, simulation, chronicleScope);
        }

        // --- drawing ---------------------------------------------------------------

        private static readonly Vector4 PanelColour = new Vector4(0.09f, 0.10f, 0.13f, 0.94f);
        private static readonly Vector4 TileColour = new Vector4(0.17f, 0.19f, 0.24f, 0.96f);
        private static readonly Vector4 TileDoneColour = new Vector4(0.15f, 0.25f, 0.17f, 0.96f);
        private static readonly Vector4 TileLockedColour = new Vector4(0.12f, 0.13f, 0.16f, 0.9f);
        private static readonly Vector4 TitleColour = new Vector4(1f, 0.86f, 0.45f, 1f);
        private static readonly Vector4 TextColour = new Vector4(0.94f, 0.94f, 0.92f, 1f);
        private static readonly Vector4 DimText = new Vector4(0.68f, 0.70f, 0.74f, 1f);
        private static readonly Vector4 CurrentEdge = new Vector4(1f, 0.80f, 0.30f, 1f);
        private static readonly Vector4 DoneEdge = new Vector4(0.42f, 0.78f, 0.40f, 1f);
        private static readonly Vector4 WarningEdge = new Vector4(1f, 0.62f, 0.20f, 1f);
        private static readonly Vector4 DangerEdge = new Vector4(1f, 0.32f, 0.28f, 1f);
        private static readonly Vector4 SelectedEdge = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 TabColour = new Vector4(0.20f, 0.22f, 0.27f, 1f);
        private static readonly Vector4 TabChosenColour = new Vector4(0.36f, 0.30f, 0.16f, 1f);

        /** The desktop footer: keys, with tokens, never shown on a touch screen. */
        public static string keyboardHint() {
            return "(" + KeyNames.describe(KeyBindings.ToggleJournal) + " or {Pause} closes; arrows or Tab turn the page; click a tile for more)";
        }

        public UiAction draw(float width, float height, Simulation simulation) {
            bool staged = requestedTab.HasValue && requestedCard > 0;
            int card = requestedCard;
            takeRequest();
            if (pendingTurn != 0) {
                turn(pendingTurn);
                pendingTurn = 0;
            }
            bool touch = TouchLayout.isActive();
            List<JournalTile> tiles = this.tiles(simulation);
            if (staged && card <= tiles.Count) {
                tap(tiles[card - 1]);
            }
            JournalTile chosen = selected(tiles);
            if (chosen == null) {
                selectedId = null;
            }
            JournalLayout layout = JournalLayout.compute(width, height, touch, chosen != null);

            Overlay.beginFullScreen("##journal-screen", width, height);
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(Vector2.Zero, new Vector2(width, height), ImGui.GetColorU32(PanelColour));
            float baseSize = ImGui.GetFontSize();

            // the title, with the book's own picture
            float titleIcon = layout.titleHeight * 0.8f;
            JournalPainter.icon(drawList, JournalIcon.Feather, new Vector2(layout.margin, layout.titleY + (layout.titleHeight - titleIcon) / 2f), titleIcon);
            float titleSize = System.MathF.Max(baseSize, layout.titleHeight * 0.55f);
            JournalPainter.text(drawList, Title, new Vector2(layout.margin + titleIcon + UiScale.px(8f), layout.titleY + (layout.titleHeight - titleSize) / 2f), titleSize, TitleColour);

            drawTabs(drawList, layout, baseSize);
            drawSummary(drawList, layout, simulation, tiles, baseSize);

            // scrolling: wheel, keys and a vertical drag on the grid; a sideways swipe turns the page
            float max = layout.maxScroll(tiles.Count);
            ImGuiIOPtr io = ImGui.GetIO();
            Vector2 mouse = io.MousePos;
            bool overGrid = mouse.X >= layout.gridX && mouse.X <= layout.gridX + layout.gridWidth
                && mouse.Y >= layout.gridY && mouse.Y <= layout.gridY + layout.gridHeight;
            if (overGrid && io.MouseWheel != 0f) {
                scroll -= io.MouseWheel * (layout.tile + layout.gap) * 0.5f;
            }
            if (pendingScrollRows != 0f) {
                scroll += pendingScrollRows * (layout.tile + layout.gap);
                pendingScrollRows = 0f;
            }
            int tapped = followPress(io, layout, tiles.Count, overGrid);
            scroll = System.Math.Clamp(scroll, 0f, max);

            drawGrid(drawList, layout, tiles, baseSize);
            if (tapped >= 0 && tapped < tiles.Count) {
                tap(tiles[tapped]);
                chosen = selected(tiles);
            }
            if (chosen != null) {
                drawCard(drawList, layout, chosen, baseSize);
            }

            if (!touch) {
                Overlay.textCentered(ObjectiveKeyNames.closeHint(keyboardHint()), baseSize, layout.footerY + (layout.height - layout.footerY) / 2f, width);
            }
            Overlay.end();
            // a finger has no J or Escape (TouchLayout)
            return Overlay.touchCloseButton("journal", width) ? UiAction.ResumeWorld : UiAction.None;
        }

        /**
        * Follows a press on the grid: dragging scrolls it, a sideways swipe
        * turns the page, and a press that hardly moved is a tap. Returns the
        * tile tapped this frame, or -1.
        */
        private int followPress(ImGuiIOPtr io, JournalLayout layout, int count, bool overGrid) {
            int tapped = -1;
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) {
                pressing = overGrid;
                pressStart = io.MousePos;
                pressWander = 0f;
            }
            if (!pressing) {
                return -1;
            }
            Vector2 moved = io.MousePos - pressStart;
            pressWander = System.MathF.Max(pressWander, moved.Length());
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
                // only an up-and-down drag scrolls; a sideways one is waiting to be a swipe
                if (pressWander > UiScale.px(TapSlop) && System.MathF.Abs(moved.Y) >= System.MathF.Abs(moved.X)) {
                    scroll -= io.MouseDelta.Y;
                }
            }
            else {
                if (isSwipe(moved.X, moved.Y, UiScale.px(SwipeDistance))) {
                    turn(moved.X < 0f ? 1 : -1);
                }
                else if (pressWander <= UiScale.px(TapSlop)) {
                    tapped = layout.hitTest(count, scroll, pressStart.X, pressStart.Y);
                }
                pressing = false;
            }
            return tapped;
        }

        private void drawTabs(ImDrawListPtr drawList, JournalLayout layout, float baseSize) {
            for (int i = 0; i < JournalTabs.All.Length; i++) {
                JournalTab each = JournalTabs.All[i];
                bool chosen = each == tab;
                float x = layout.tabX(i);
                Vector2 min = new Vector2(x, layout.tabsY);
                Vector2 max = new Vector2(x + layout.tabWidth, layout.tabsY + layout.tabHeight);
                ImGui.SetCursorPos(min);
                if (ImGui.InvisibleButton("##journal-tab-" + i, max - min)) {
                    showTab(each);
                    chosen = true;
                }
                bool hovered = ImGui.IsItemHovered();
                drawList.AddRectFilled(min, max, ImGui.GetColorU32(chosen ? TabChosenColour : hovered ? TabColour * 1.25f : TabColour), UiScale.px(8f));
                if (chosen) {
                    drawList.AddRect(min, max, ImGui.GetColorU32(CurrentEdge), UiScale.px(8f), ImDrawFlags.None, UiScale.px(2f));
                }
                // the picture, and the name beside it when there is room or under it when not
                string name = JournalTabs.label(each);
                float iconSize = layout.tabHeight * 0.6f;
                float textWidth = JournalPainter.measure(name, baseSize);
                bool beside = iconSize + UiScale.px(6f) + textWidth + UiScale.px(12f) <= layout.tabWidth;
                if (beside) {
                    float startX = x + (layout.tabWidth - iconSize - UiScale.px(6f) - textWidth) / 2f;
                    JournalPainter.icon(drawList, JournalTabs.icon(each), new Vector2(startX, layout.tabsY + (layout.tabHeight - iconSize) / 2f), iconSize, chosen ? 1f : 0.75f);
                    JournalPainter.text(drawList, name, new Vector2(startX + iconSize + UiScale.px(6f), layout.tabsY + (layout.tabHeight - baseSize) / 2f), baseSize, chosen ? TitleColour : TextColour);
                }
                else {
                    // a narrow tab: the picture big and alone, the name only when chosen and it fits
                    string shortName = JournalTabs.shortLabel(each);
                    bool named = JournalPainter.measure(shortName, baseSize) <= layout.tabWidth - UiScale.px(4f);
                    float size = named ? layout.tabHeight * 0.5f : layout.tabHeight * 0.66f;
                    float top = layout.tabsY + (named ? UiScale.px(3f) : (layout.tabHeight - size) / 2f);
                    JournalPainter.icon(drawList, JournalTabs.icon(each), new Vector2(x + (layout.tabWidth - size) / 2f, top), size, chosen ? 1f : 0.75f);
                    if (named) {
                        JournalPainter.textCentred(drawList, shortName, x + layout.tabWidth / 2f, layout.tabsY + layout.tabHeight - baseSize - UiScale.px(3f), baseSize, chosen ? TitleColour : DimText);
                    }
                }
            }
        }

        /** The strip over the grid: the page's headline as a picture and a number or a bar. */
        private void drawSummary(ImDrawListPtr drawList, JournalLayout layout, Simulation simulation, List<JournalTile> tiles, float baseSize) {
            float y = layout.summaryY;
            float h = layout.summaryHeight;
            float left = layout.margin;
            float right = layout.width - layout.margin;
            float big = System.MathF.Max(baseSize, h * 0.5f);
            float textY = y + (h - big) / 2f;
            switch (tab) {
                case JournalTab.Goals: {
                    JournalContent.arcProgress(simulation, out int done, out int total);
                    string count = done + "/" + total;
                    JournalPainter.icon(drawList, JournalIcon.Check, new Vector2(left, y + h * 0.1f), h * 0.8f);
                    JournalPainter.text(drawList, count, new Vector2(left + h, textY), big, TextColour);
                    float vineLeft = left + h + JournalPainter.measure(count, big) + UiScale.px(14f);
                    JournalPainter.vine(drawList, new Vector2(vineLeft, y), new Vector2(right, y + h), done, total);
                    break;
                }
                case JournalTab.Chronicle: {
                    JournalPainter.icon(drawList, JournalIcon.Scroll, new Vector2(left, y + h * 0.1f), h * 0.8f);
                    JournalPainter.text(drawList, tiles.Count.ToString(), new Vector2(left + h, textY), big, TextColour);
                    // whose news: two buttons, a fingertip tall
                    float buttonWidth = System.MathF.Min(UiScale.px(120f), (right - left - h * 2.2f) / 2f);
                    float buttonsX = right - buttonWidth * 2f - UiScale.px(6f);
                    if (scopeButton(drawList, "Yours", ChronicleScope.YourNews, buttonsX, y, buttonWidth, h, baseSize)) {
                        setChronicleScope(ChronicleScope.YourNews);
                    }
                    if (scopeButton(drawList, "Everyone", ChronicleScope.AllNations, buttonsX + buttonWidth + UiScale.px(6f), y, buttonWidth, h, baseSize)) {
                        setChronicleScope(ChronicleScope.AllNations);
                    }
                    break;
                }
                case JournalTab.Era: {
                    Era era = simulation.getEraTracker().getEra();
                    JournalPainter.icon(drawList, JournalContent.iconForEra(era), new Vector2(left, y + h * 0.1f), h * 0.8f);
                    string name = Eras.getDisplayName(era);
                    JournalPainter.text(drawList, name, new Vector2(left + h, textY), big, TextColour);
                    float barLeft = left + h + JournalPainter.measure(name, big) + UiScale.px(14f);
                    JournalPainter.bar(drawList, new Vector2(barLeft, y + h * 0.35f), new Vector2(right, y + h * 0.65f),
                        beyondnations.desktop.ui.boxes.EraBox.progressFraction(simulation), CurrentEdge);
                    break;
                }
                case JournalTab.Inventory: {
                    int coins = simulation.getPlayer().getInventory().getNumItems(ItemType.COIN);
                    JournalPainter.icon(drawList, JournalIcon.Coin, new Vector2(left, y + h * 0.1f), h * 0.8f);
                    JournalPainter.text(drawList, coins.ToString(), new Vector2(left + h, textY), big, TitleColour);
                    break;
                }
                case JournalTab.Nation: {
                    NationStanding standing = NationStanding.of(simulation);
                    JournalPainter.icon(drawList, JournalIcon.Flag, new Vector2(left, y + h * 0.1f), h * 0.8f);
                    string name = standing == null ? "No nation" : standing.nation.getName();
                    JournalPainter.text(drawList, ChroniclePanel.fitToWidth(name, right - left - h, s => JournalPainter.measure(s, big)),
                        new Vector2(left + h, textY), big, standing == null ? DimText : TextColour);
                    break;
                }
            }
        }

        private bool scopeButton(ImDrawListPtr drawList, string label, ChronicleScope scope, float x, float y, float w, float h, float baseSize) {
            ImGui.SetCursorPos(new Vector2(x, y));
            bool clicked = ImGui.InvisibleButton("##journal-scope-" + label, new Vector2(w, h));
            bool chosen = chronicleScope == scope;
            drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + w, y + h), ImGui.GetColorU32(chosen ? TabChosenColour : TabColour), UiScale.px(8f));
            JournalPainter.textCentred(drawList, label, x + w / 2f, y + (h - baseSize) / 2f, baseSize, chosen ? TitleColour : DimText);
            return clicked;
        }

        private void drawGrid(ImDrawListPtr drawList, JournalLayout layout, List<JournalTile> tiles, float baseSize) {
            Vector2 clipMin = new Vector2(layout.gridX, layout.gridY);
            Vector2 clipMax = new Vector2(layout.gridX + layout.gridWidth, layout.gridY + layout.gridHeight);
            drawList.PushClipRect(clipMin, clipMax, true);
            for (int i = 0; i < tiles.Count; i++) {
                layout.tileOffset(i, out float tx, out float ty);
                Vector2 min = new Vector2(layout.gridX + tx, layout.gridY + ty - scroll);
                if (min.Y > clipMax.Y || min.Y + layout.tile < clipMin.Y) {
                    continue;
                }
                drawTile(drawList, tiles[i], min, layout.tile, baseSize, tiles[i].id == selectedId);
            }
            drawList.PopClipRect();
            // a hint of more below or above: a fade at the edge
            float max = layout.maxScroll(tiles.Count);
            if (scroll < max - 1f) {
                drawList.AddRectFilledMultiColor(new Vector2(clipMin.X, clipMax.Y - UiScale.px(18f)), clipMax,
                    0u, 0u, ImGui.GetColorU32(PanelColour), ImGui.GetColorU32(PanelColour));
            }
            if (tiles.Count == 0) {
                JournalPainter.textCentred(drawList, emptyLine(), layout.gridX + layout.gridWidth / 2f, layout.gridY + UiScale.px(20f), baseSize, DimText);
            }
        }

        private string emptyLine() {
            return tab == JournalTab.Chronicle ? ChroniclePanel.emptyMessage(chronicleScope, false) : "Nothing here yet.";
        }

        private static void drawTile(ImDrawListPtr drawList, JournalTile tile, Vector2 min, float size, float baseSize, bool selected) {
            Vector2 max = min + new Vector2(size, size);
            float round = size * 0.12f;
            Vector4 fill = tile.state == TileState.Done ? TileDoneColour : tile.state == TileState.Locked ? TileLockedColour : TileColour;
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(fill), round);
            Vector4? edge = selected ? SelectedEdge
                : tile.state == TileState.Current ? CurrentEdge
                : tile.state == TileState.Warning ? WarningEdge
                : tile.state == TileState.Danger ? DangerEdge
                : tile.state == TileState.Done ? DoneEdge * new Vector4(1f, 1f, 1f, 0.6f)
                : (Vector4?) null;
            if (edge.HasValue) {
                drawList.AddRect(min, max, ImGui.GetColorU32(edge.Value), round, ImDrawFlags.None, UiScale.px(selected ? 3f : 2f));
            }

            // the picture, in a ring when there is progress
            bool hasValue = !string.IsNullOrEmpty(tile.value);
            float labelSize = baseSize;
            float valueSize = System.MathF.Max(baseSize, System.MathF.Min(size * 0.2f, baseSize * 2.4f));
            float iconSize = hasValue ? size * 0.40f : size * 0.52f;
            float iconTop = min.Y + size * (hasValue ? 0.08f : 0.1f);
            Vector2 iconMin = new Vector2(min.X + (size - iconSize) / 2f, iconTop);
            float alpha = tile.state == TileState.Locked ? 0.35f : 1f;
            if (tile.hasProgress()) {
                Vector2 centre = iconMin + new Vector2(iconSize / 2f, iconSize / 2f);
                Vector4 ringColour = tile.state == TileState.Done || tile.progress >= 1f ? DoneEdge : CurrentEdge;
                JournalPainter.ring(drawList, centre, iconSize * 0.62f, System.MathF.Max(2f, size * 0.04f), tile.progress, ringColour);
                float inner = iconSize * 0.78f;
                JournalPainter.icon(drawList, tile.icon, centre - new Vector2(inner / 2f, inner / 2f), inner, alpha);
            }
            else {
                JournalPainter.icon(drawList, tile.icon, iconMin, iconSize, alpha);
            }

            // the corner badge: a tick once done, a padlock while ahead, a ! when it needs you
            float badge = System.MathF.Max(UiScale.px(14f), size * 0.2f);
            Vector2 badgeMin = new Vector2(max.X - badge - size * 0.04f, min.Y + size * 0.04f);
            if (tile.state == TileState.Done) {
                JournalPainter.icon(drawList, JournalIcon.Check, badgeMin, badge);
            }
            else if (tile.state == TileState.Locked) {
                JournalPainter.icon(drawList, JournalIcon.Lock, badgeMin, badge, 0.8f);
            }
            else if (tile.state == TileState.Warning || tile.state == TileState.Danger) {
                JournalPainter.icon(drawList, JournalIcon.Warning, badgeMin, badge);
            }

            float textWidth = size - size * 0.08f;
            if (hasValue) {
                string value = ChroniclePanel.fitToWidth(tile.value, textWidth, s => JournalPainter.measure(s, valueSize));
                JournalPainter.textCentred(drawList, value, min.X + size / 2f, iconTop + iconSize + size * 0.08f, valueSize,
                    tile.state == TileState.Locked ? DimText : TextColour);
            }
            string label = ChroniclePanel.fitToWidth(tile.label, textWidth, s => JournalPainter.measure(s, labelSize));
            JournalPainter.textCentred(drawList, label, min.X + size / 2f, max.Y - labelSize - size * 0.06f, labelSize,
                tile.state == TileState.Locked ? DimText : new Vector4(0.85f, 0.87f, 0.9f, 1f));
        }

        /** The card a tapped tile opens: its picture, its title and its lines, wrapped. A tap on it closes it. */
        private void drawCard(ImDrawListPtr drawList, JournalLayout layout, JournalTile tile, float baseSize) {
            Vector2 min = new Vector2(layout.detailX, layout.detailY);
            Vector2 max = min + new Vector2(layout.detailWidth, layout.detailHeight);
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0.14f, 0.15f, 0.19f, 0.98f)), UiScale.px(10f));
            drawList.AddRect(min, max, ImGui.GetColorU32(CurrentEdge * new Vector4(1f, 1f, 1f, 0.7f)), UiScale.px(10f), ImDrawFlags.None, UiScale.px(2f));
            ImGui.SetCursorPos(min);
            if (ImGui.InvisibleButton("##journal-card", max - min)) {
                closeCard();
            }
            float pad = UiScale.px(12f);
            float iconSize = System.MathF.Min(UiScale.px(56f), layout.detailHeight * 0.4f);
            JournalPainter.icon(drawList, tile.icon, min + new Vector2(pad, pad), iconSize);
            float titleSize = System.MathF.Max(baseSize, baseSize * 1.3f);
            float textLeft = min.X + pad + iconSize + UiScale.px(10f);
            float textWidth = max.X - pad - textLeft;
            float y = min.Y + pad;
            foreach (string line in TextWrap.wrap(tile.detailTitle ?? tile.label, textWidth, s => JournalPainter.measure(s, titleSize))) {
                JournalPainter.text(drawList, line, new Vector2(textLeft, y), titleSize, TitleColour);
                y += titleSize + UiScale.px(3f);
            }
            if (!string.IsNullOrEmpty(tile.value)) {
                float big = System.MathF.Max(baseSize, baseSize * 2f);
                JournalPainter.text(drawList, tile.value, new Vector2(textLeft, y), big, TextColour);
                y += big + UiScale.px(4f);
            }
            y = System.MathF.Max(y, min.Y + pad + iconSize + UiScale.px(6f));
            // the lines use the card's full width under the picture
            float lineLeft = min.X + pad;
            float lineWidth = max.X - pad - lineLeft;
            float bottom = max.Y - pad;
            drawList.PushClipRect(min, max, true);
            foreach (string paragraph in tile.detailLines) {
                foreach (string line in TextWrap.wrap(paragraph, lineWidth, s => JournalPainter.measure(s, baseSize))) {
                    if (y + baseSize > bottom) {
                        break;
                    }
                    JournalPainter.text(drawList, line, new Vector2(lineLeft, y), baseSize, TextColour);
                    y += baseSize + UiScale.px(3f);
                }
                y += UiScale.px(4f);
            }
            drawList.PopClipRect();
        }
    }
}
