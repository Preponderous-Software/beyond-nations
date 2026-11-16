using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace beyondnations.desktop.ui {

    /**
    * The market panel (#182): pick a good, pick how many, and buy or sell
    * them in one press, with the total on the button and a receipt after.
    * Opened by Trade at Market on the command bar in any settlement with
    * stalls; the world keeps running behind it.
    *
    * Top to bottom: one button per good (the ones the player carries that
    * the market buys, and the ones the stalls hold), each with its buy/sell
    * price and how many the player carries; then, for the chosen good, its
    * prices and stock, what it fetches at the best market in a trader's
    * reach and the margin on carrying it there; the quantity (-5 -1 +1 +5
    * All); Buy N for X and Sell N for Y; the last receipt and the visit's
    * running total spent, taken and net.
    *
    * It holds still while open (0.8.0 playtest s05, where rows moved under
    * the pointer and Close ran off a 540-pixel window): the goods keep the
    * order they were first shown in (MarketTrade.keepOrder), and the panel
    * is a fixed size worked out from the window (computeLayout), with the
    * list, the detail lines and the messages each in a box of their own
    * that scrolls rather than pushing the buttons down. A Buy or Sell press
    * deals exactly what its label said, or nothing and says why
    * (TradeAtMarketCommand.sellAsQuoted). Escape closes it, and so does the
    * corner Close on a touch screen.
    *
    * Everything is a button, so it works the same by touch as with a
    * mouse; no part needs hovering or a keyboard. What it shows comes from
    * MarketTrade (Core) and plain methods here, which tests check without a
    * window. Nothing here is saved: closing it forgets the choice and the
    * visit's totals.
    */
    public static class MarketTradePanel {
        public const string OpenLabel = "Trade at Market";
        public const string Title = "MARKET";

        /** Why the player stands still while the panel is open (playtest 0.8.0 s05 held W for 25 s). */
        public const string StayLine = "You are inside the settlement: close this, then Exit Settlement, to walk on.";

        private static bool isOpenFlag;
        private static EntityId settlementId;
        private static ItemType? selected;
        private static int quantity = 1;
        private static int spent;
        private static int earned;
        private static string lastReceipt;
        private static bool lastReceiptDealt;
        // the goods in the order first shown, kept until the panel closes
        private static readonly List<ItemType> shownGoods = new List<ItemType>();
        // how many goods there were when it opened: the list's height is set by it
        private static int goodsAtOpen;
        // the quotes the Buy and Sell labels were drawn from on the last frame:
        // a press is of what was seen, which at a few frames a second is half
        // a second old
        private static TradeReceipt shownSale;
        private static TradeReceipt shownPurchase;
        // where the panel's top was put on its first frame, and for which
        // window size (holdTop)
        private static float heldTop = -1f;
        private static float heldForWidth;
        private static float heldForHeight;

        private const ImGuiWindowFlags PanelFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse;

        public static bool isOpen() {
            return isOpenFlag;
        }

        /** Opens the panel on the market the player is standing in, with the first good chosen. */
        public static void open(Simulation simulation) {
            Settlement here = getSettlement(simulation);
            if (here == null || here.getMarket().getNumStalls() == 0) {
                simulation.getPlayer().getStatus().update("There is no market here to trade at.");
                return;
            }
            CaravanDispatchPanel.close();
            isOpenFlag = true;
            settlementId = here.getId();
            quantity = 1;
            spent = 0;
            earned = 0;
            lastReceipt = null;
            shownGoods.Clear();
            shownSale = null;
            shownPurchase = null;
            heldTop = -1f;
            List<ItemType> goods = getGoods(simulation);
            goodsAtOpen = goods.Count;
            selected = goods.Count > 0 ? goods[0] : (ItemType?) null;
        }

        public static void close() {
            isOpenFlag = false;
            selected = null;
            settlementId = null;
            shownGoods.Clear();
            shownSale = null;
            shownPurchase = null;
            heldTop = -1f;
        }

        /**
        * Where the panel's top goes this frame: where it was put on its first
        * frame open, for as long as the window keeps its size. The HUD offers
        * a top below the status line and the warnings, and a sale's receipt
        * coming up on the status line would push it down a line, moving
        * every button under the pointer (the panel's own receipt line says
        * the same). A new window size takes the offered top again.
        */
        public static float holdTop(float offeredTop, float windowWidth, float windowHeight) {
            if (heldTop < 0f || windowWidth != heldForWidth || windowHeight != heldForHeight) {
                heldTop = offeredTop;
                heldForWidth = windowWidth;
                heldForHeight = windowHeight;
            }
            return heldTop;
        }

        /**
        * Escape while the panel is open closes the panel, rather than opening
        * the pause menu behind it (playtest 0.8.0 s05). Returns whether it
        * did, so the host goes no further with the press.
        */
        public static bool escapePressed() {
            if (!isOpenFlag) {
                return false;
            }
            close();
            return true;
        }

        // --- what it shows ----------------------------------------------------

        /** The settlement the player is standing in, or null. */
        public static Settlement getSettlement(Simulation simulation) {
            Player player = simulation.getPlayer();
            if (player.getCurrentSettlementId() == null) {
                return null;
            }
            return simulation.getEntityRepository().getEntity(player.getCurrentSettlementId()) as Settlement;
        }

        /**
        * The goods the panel lists: while it is open, in the order they were
        * first shown, with newcomers at the end and nothing taken away
        * (MarketTrade.keepOrder), so no row moves under the pointer.
        */
        public static List<ItemType> getGoods(Simulation simulation) {
            Settlement here = getSettlement(simulation);
            List<ItemType> current = here == null ? new List<ItemType>() : MarketTrade.listGoods(here.getMarket(), simulation.getPlayer());
            if (!isOpenFlag) {
                return current;
            }
            return new List<ItemType>(MarketTrade.keepOrder(shownGoods, current));
        }

        /**
        * The chosen good. It stays chosen when the last of it is sold or
        * bought, as its row stays, rather than the choice jumping to a good
        * the player did not pick.
        */
        public static ItemType? getSelected(Simulation simulation) {
            List<ItemType> goods = getGoods(simulation);
            if (goods.Count == 0) {
                return null;
            }
            if (selected == null || !goods.Contains(selected.Value)) {
                selected = goods[0];
            }
            return selected;
        }

        public static void select(ItemType itemType) {
            selected = itemType;
        }

        public static int getQuantity() {
            return quantity;
        }

        public static void changeQuantity(int step) {
            quantity = MarketTrade.stepQuantity(quantity, step);
        }

        /** All: as many as the player carries to sell or the stalls hold to buy, whichever is more. */
        public static void setQuantityToAll(Simulation simulation) {
            ItemType? good = getSelected(simulation);
            Settlement here = getSettlement(simulation);
            if (good == null || here == null) {
                return;
            }
            MarketTrade.Row row = MarketTrade.describeRow(here.getMarket(), simulation.getPlayer(), good.Value);
            int most = System.Math.Max(Market.isSoldInBulk(good.Value) ? row.carried : 0, row.forSale);
            quantity = System.Math.Clamp(most, 1, MarketTrade.MAX_QUANTITY);
        }

        public static TradeReceipt getSaleQuote(Simulation simulation) {
            ItemType? good = getSelected(simulation);
            Settlement here = getSettlement(simulation);
            if (good == null || here == null) {
                return new TradeReceipt(ItemType.COIN, true);
            }
            return MarketTrade.quoteSale(here.getMarket(), simulation.getPlayer(), good.Value, quantity);
        }

        public static TradeReceipt getPurchaseQuote(Simulation simulation) {
            ItemType? good = getSelected(simulation);
            Settlement here = getSettlement(simulation);
            if (good == null || here == null) {
                return new TradeReceipt(ItemType.COIN, false);
            }
            return MarketTrade.quotePurchase(here.getMarket(), simulation.getPlayer(), good.Value, quantity);
        }

        /** One line of the profit-or-loss block: its text and whether it is a gain (+1), a loss (-1) or neither (0). */
        public class MarginLine {
            public string text;
            public int sign;
        }

        /**
        * The profit or loss in carrying the chosen good to the best market in
        * a trader's reach (Traders.getMarkets): where that is and what it
        * pays, the margin on buying here to sell there, and, for what the
        * player carries, the margin on selling there rather than here. Empty
        * when no market is in reach or the market does not buy the good.
        */
        public static List<MarginLine> getMarginLines(Simulation simulation) {
            List<MarginLine> lines = new List<MarginLine>();
            ItemType? good = getSelected(simulation);
            Settlement here = getSettlement(simulation);
            if (good == null || here == null) {
                return lines;
            }
            List<Settlement> markets = Traders.getMarkets(simulation.getEntityRepository(), simulation.getNationRepository(), here,
                simulation.getPlayer().getNationId());
            int price;
            Settlement best = MarketTrade.findBestElsewhere(markets, good.Value, out price);
            if (best == null) {
                return lines;
            }
            MarketTrade.Row row = MarketTrade.describeRow(here.getMarket(), simulation.getPlayer(), good.Value);
            lines.Add(new MarginLine { text = MarketTrade.describeBestElsewhere(best, price), sign = 0 });
            if (row.forSale > 0) {
                lines.Add(new MarginLine { text = MarketTrade.describeCarryMargin(row, price), sign = System.Math.Sign(MarketTrade.getCarryMargin(row, price)) });
            }
            if (row.carried > 0) {
                lines.Add(new MarginLine { text = MarketTrade.describeSellThereMargin(row, price), sign = System.Math.Sign(price - row.sellPrice) });
            }
            return lines;
        }

        public static string getVisitLine() {
            return spent == 0 && earned == 0 ? null : MarketTrade.describeVisit(spent, earned);
        }

        public static string getLastReceipt() {
            return lastReceipt;
        }

        // --- what it does -----------------------------------------------------

        /** Sells what the Sell button says right now (a press with no frame between the label and the click). */
        public static TradeReceipt sell(Simulation simulation) {
            return sell(simulation, getSaleQuote(simulation));
        }

        /**
        * A press of Sell whose label said shown: sells exactly that, or
        * nothing, with the reason (the tills or prices moved) as the receipt
        * line. The label then shows the new terms, so a second press takes
        * them.
        */
        public static TradeReceipt sell(Simulation simulation, TradeReceipt shown) {
            if (shown == null || shown.isEmpty()) {
                return new TradeReceipt(shown == null ? ItemType.COIN : shown.getItemType(), true);
            }
            TradeReceipt receipt = new TradeAtMarketCommand(simulation.getNationRepository(), simulation.getEntityRepository())
                .sellAsQuoted(simulation.getPlayer(), shown);
            earned += receipt.getCoins();
            lastReceipt = simulation.getPlayer().getStatus().getLatest();
            lastReceiptDealt = !receipt.isEmpty();
            return receipt;
        }

        /** Buys what the Buy button says right now. */
        public static TradeReceipt buy(Simulation simulation) {
            return buy(simulation, getPurchaseQuote(simulation));
        }

        /** A press of Buy whose label said shown: buys exactly that, or nothing, with the reason. */
        public static TradeReceipt buy(Simulation simulation, TradeReceipt shown) {
            if (shown == null || shown.isEmpty()) {
                return new TradeReceipt(shown == null ? ItemType.COIN : shown.getItemType(), false);
            }
            TradeReceipt receipt = new TradeAtMarketCommand(simulation.getNationRepository(), simulation.getEntityRepository())
                .buyAsQuoted(simulation.getPlayer(), shown);
            spent += receipt.getCoins();
            lastReceipt = simulation.getPlayer().getStatus().getLatest();
            lastReceiptDealt = !receipt.isEmpty();
            return receipt;
        }

        // --- the layout -------------------------------------------------------

        /** The panel's size and the heights of its parts, fixed for a window size while it is open. */
        public class Layout {
            public float width;
            public float height;
            public int columns;
            public float rowHeight;
            public float buttonHeight;
            public float listHeight;
            public float infoHeight;
            public float messagesHeight;
            /** Whether the stay line is shown (it gives way first on a short window). */
            public bool stayLine;
            /** How many lines the title and the stay line wrap to at this width (a phone wraps each to two). */
            public int titleLines = 1;
            public int stayLines = 1;
        }

        /** Goods go two to a row when the panel is at least this wide (1x pixels). */
        public const float TwoColumnWidth = 400f;

        /**
        * Lays the panel out for a window: its width, the goods' columns and
        * row height, and how tall the list, the detail lines and the messages
        * are, so the whole panel, Close included, fits between topY and the
        * bottom of the window. It stays the same for as long as the window
        * keeps its size, whatever is traded: the list is sized for the goods
        * there were at opening and one row more, and scrolls when more come
        * up; the detail and message boxes scroll when their lines run long.
        * On a short window the stay line gives way first, then rows down to
        * two, a shorter row (not on a touch screen), detail and message lines
        * down to two each, and the last row but one.
        */
        public static Layout computeLayout(float windowWidth, float windowHeight, float topY, int goodsAtOpen,
                float lineHeight, float frameHeight, float spacing, float padding, float scale, bool touch,
                int titleLines = 1, int stayLines = 1) {
            Layout layout = new Layout();
            layout.titleLines = System.Math.Max(1, titleLines);
            layout.stayLines = System.Math.Max(1, stayLines);
            layout.width = System.MathF.Min(windowWidth - 32f * scale, 480f * scale);
            layout.columns = layout.width >= TwoColumnWidth * scale ? 2 : 1;
            float minTarget = touch ? TouchLayout.MinTarget * scale : 0f;
            layout.buttonHeight = System.MathF.Max(System.MathF.Max(frameHeight + 8f * scale, 36f * scale), minTarget);
            float shortRow = System.MathF.Max(frameHeight + 6f * scale, minTarget);
            float available = windowHeight - topY - 8f * scale;

            int rows = System.Math.Max(1, (goodsAtOpen + layout.columns - 1) / layout.columns) + 1;
            float rowHeight = layout.buttonHeight;
            int infoLines = 4;
            int messageLines = 3;
            bool stay = true;
            for (int attempt = 0; attempt < 32; attempt++) {
                layout.rowHeight = rowHeight;
                layout.listHeight = rows * rowHeight + (rows - 1) * spacing;
                layout.infoHeight = infoLines * lineHeight;
                layout.messagesHeight = messageLines * lineHeight;
                layout.stayLine = stay;
                layout.height = totalHeight(layout, lineHeight, spacing, padding);
                if (layout.height <= available) {
                    break;
                }
                if (stay) { stay = false; continue; }
                if (rows > 2) { rows--; continue; }
                if (rowHeight > shortRow) { rowHeight = shortRow; continue; }
                if (infoLines > 2) { infoLines--; continue; }
                if (messageLines > 2) { messageLines--; continue; }
                if (rows > 1) { rows--; continue; }
                break;
            }
            return layout;
        }

        private static int linesFor(string text, float wrapWidth, float lineHeight) {
            float textHeight = ImGui.CalcTextSize(text, false, wrapWidth).Y;
            return System.Math.Max(1, (int) System.MathF.Round(textHeight / System.MathF.Max(1f, lineHeight)));
        }

        private static float totalHeight(Layout layout, float lineHeight, float spacing, float padding) {
            return 2f * padding
                + layout.titleLines * lineHeight + spacing           // title
                + (layout.stayLine ? layout.stayLines * lineHeight + spacing : 0f) // stay line
                + layout.listHeight + spacing
                + spacing                                            // separator
                + layout.infoHeight + spacing
                + 2f * (layout.buttonHeight + spacing)               // quantity, Buy and Sell
                + layout.messagesHeight + spacing
                + layout.buttonHeight;                               // Close
        }

        // --- drawing ----------------------------------------------------------

        private static readonly Vector4 ChosenFill = new Vector4(0.80f, 0.62f, 0.20f, 1f);
        private static readonly Vector4 ChosenHover = new Vector4(0.88f, 0.70f, 0.28f, 1f);
        private static readonly Vector4 Gain = new Vector4(0.55f, 0.95f, 0.55f, 1f);
        private static readonly Vector4 Muted = new Vector4(0.85f, 0.85f, 0.75f, 1f);
        private static readonly Vector4 Warn = new Vector4(1f, 0.72f, 0.35f, 1f);

        /** A Buy or Sell button, drawn faded and doing nothing when it cannot deal. */
        private static bool dealButton(string label, float width, float height, bool enabled) {
            if (!enabled) {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.45f);
            }
            bool pressed = ImGui.Button(label, new Vector2(width, height));
            if (!enabled) {
                ImGui.PopStyleVar();
            }
            return pressed && enabled;
        }

        /** Draws the panel when open, centred under topY. */
        public static void draw(float width, float height, Simulation simulation, float topY) {
            if (!isOpenFlag) {
                return;
            }
            // the player walked out, or into another settlement
            Settlement here = getSettlement(simulation);
            if (here == null || settlementId == null || !here.getId().Equals(settlementId)) {
                close();
                return;
            }
            // the corner Close on a touch screen, as on the full screens
            if (Overlay.touchCloseButton("market", width)) {
                close();
                return;
            }
            // below the corner Close, which would otherwise sit on the title
            topY = System.MathF.Max(topY, Overlay.touchCloseBottom());
            // and where it was when it opened, whatever comes up on the status line since
            topY = holdTop(topY, width, height);
            Player player = simulation.getPlayer();
            Market market = here.getMarket();
            ImGuiStylePtr style = ImGui.GetStyle();
            bool touch = TouchLayout.isActive();
            // the title and the stay line wrap on a narrow window (a phone held
            // upright), so the panel's height counts their lines as drawn
            string title = Title + " - the " + here.getDisplayName() + " - you have "
                + player.getInventory().getNumItems(ItemType.COIN) + " coins";
            float wrapWidth = System.MathF.Min(width - UiScale.px(32f), UiScale.px(480f));
            float lineHeight = ImGui.GetTextLineHeight();
            // measured with a five-digit purse, so a sale that adds a digit does not move the rows
            int titleLines = linesFor(Title + " - the " + here.getDisplayName() + " - you have 99999 coins", wrapWidth, lineHeight);
            int stayLines = linesFor(StayLine, wrapWidth, lineHeight);
            Layout layout = computeLayout(width, height, topY, goodsAtOpen, lineHeight, Overlay.frameHeight(),
                style.ItemSpacing.Y, style.WindowPadding.Y, UiScale.get(), touch, titleLines, stayLines);
            float panelWidth = layout.width;
            float buttonHeight = layout.buttonHeight;
            float small = (panelWidth - 5f * style.ItemSpacing.X) / 6f;
            float half = (panelWidth - style.ItemSpacing.X) / 2f;

            ImGui.SetNextWindowPos(new Vector2(width / 2f, topY), ImGuiCond.Always, new Vector2(0.5f, 0f));
            ImGui.SetNextWindowSize(new Vector2(panelWidth + 2f * style.WindowPadding.X, layout.height), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.92f);
            // in front of the HUD boxes it overlaps on a narrow window, whose
            // own windows would otherwise draw over its Close
            ImGui.SetNextWindowFocus();
            if (ImGui.Begin("##market-trade", PanelFlags)) {
                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + panelWidth);
                ImGui.TextUnformatted(title);
                if (layout.stayLine) {
                    Overlay.textColoured(Muted, StayLine);
                }
                ImGui.PopTextWrapPos();

                List<ItemType> goods = getGoods(simulation);
                ItemType? chosen = getSelected(simulation);

                // the goods, in a box of their own that scrolls when more come up
                // than it was sized for; the scrollbar's width is kept free, so a
                // scrollbar coming up does not narrow the rows
                float listWidth = panelWidth;
                float cellWidth = (listWidth - style.ScrollbarSize - (layout.columns - 1) * style.ItemSpacing.X) / layout.columns;
                ImGui.BeginChild("##market-goods", new Vector2(listWidth, layout.listHeight));
                if (goods.Count == 0) {
                    ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + cellWidth * layout.columns);
                    Overlay.textColoured(Warn, "Nothing to trade: the stalls are empty and you carry nothing they buy.");
                    ImGui.PopTextWrapPos();
                }
                for (int i = 0; i < goods.Count; i++) {
                    ItemType good = goods[i];
                    MarketTrade.Row row = MarketTrade.describeRow(market, player, good);
                    bool isChosen = chosen.HasValue && chosen.Value == good;
                    if (i % layout.columns != 0) {
                        ImGui.SameLine();
                    }
                    if (isChosen) {
                        ImGui.PushStyleColor(ImGuiCol.Button, ChosenFill);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ChosenHover);
                    }
                    if (ImGui.Button(MarketTrade.describeRowLabel(row) + "##good-" + good, new Vector2(cellWidth, layout.rowHeight))) {
                        select(good);
                    }
                    if (isChosen) {
                        ImGui.PopStyleColor(2);
                    }
                }
                ImGui.EndChild();
                ImGui.Separator();

                // the chosen good's prices and margins, in a box of fixed height
                ImGui.BeginChild("##market-info", new Vector2(panelWidth, layout.infoHeight));
                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + panelWidth - style.ScrollbarSize);
                if (chosen.HasValue) {
                    ImGui.TextUnformatted(MarketTrade.describeDetail(MarketTrade.describeRow(market, player, chosen.Value)));
                    foreach (MarginLine line in getMarginLines(simulation)) {
                        Overlay.textColoured(line.sign > 0 ? Gain : line.sign < 0 ? Warn : Muted, line.text);
                    }
                }
                ImGui.PopTextWrapPos();
                ImGui.EndChild();

                // the quantity, then Buy and Sell: always drawn, in the same
                // place, so nothing below them moves when a good is chosen
                if (ImGui.Button("-5##qty", new Vector2(small, buttonHeight))) {
                    changeQuantity(-5);
                }
                ImGui.SameLine();
                if (ImGui.Button("-1##qty", new Vector2(small, buttonHeight))) {
                    changeQuantity(-1);
                }
                ImGui.SameLine();
                ImGui.Button(quantity + "##qty-shown", new Vector2(small, buttonHeight));
                ImGui.SameLine();
                if (ImGui.Button("+1##qty", new Vector2(small, buttonHeight))) {
                    changeQuantity(1);
                }
                ImGui.SameLine();
                if (ImGui.Button("+5##qty", new Vector2(small, buttonHeight))) {
                    changeQuantity(5);
                }
                ImGui.SameLine();
                if (ImGui.Button("All##qty", new Vector2(small, buttonHeight))) {
                    setQuantityToAll(simulation);
                }

                TradeReceipt purchase = getPurchaseQuote(simulation);
                TradeReceipt sale = getSaleQuote(simulation);
                // a press is of the label the player saw, drawn last frame; on
                // the first frame there is none, and this frame's is the one
                TradeReceipt seenPurchase = shownPurchase ?? purchase;
                TradeReceipt seenSale = shownSale ?? sale;
                if (dealButton(MarketTrade.describeBuyButton(purchase) + "##buy", half, buttonHeight, !purchase.isEmpty())) {
                    buy(simulation, seenPurchase);
                    purchase = getPurchaseQuote(simulation);
                    sale = getSaleQuote(simulation);
                }
                ImGui.SameLine();
                if (dealButton(MarketTrade.describeSellButton(sale) + "##sell", half, buttonHeight, !sale.isEmpty())) {
                    sell(simulation, seenSale);
                    purchase = getPurchaseQuote(simulation);
                    sale = getSaleQuote(simulation);
                }
                shownPurchase = purchase;
                shownSale = sale;

                // why a button is short, the last receipt and the visit's total
                ImGui.BeginChild("##market-messages", new Vector2(panelWidth, layout.messagesHeight));
                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + panelWidth - style.ScrollbarSize);
                if (chosen.HasValue) {
                    string shortfall = MarketTrade.describeShortfall(market, player, chosen.Value, quantity, sale, purchase);
                    if (shortfall != null) {
                        Overlay.textColoured(Warn, shortfall);
                    }
                }
                if (lastReceipt != null) {
                    Overlay.textColoured(lastReceiptDealt ? Gain : Warn, lastReceipt);
                }
                string visit = getVisitLine();
                if (visit != null) {
                    Overlay.textColoured(Muted, visit);
                }
                ImGui.PopTextWrapPos();
                ImGui.EndChild();

                if (ImGui.Button("Close##market", new Vector2(panelWidth, buttonHeight))) {
                    close();
                }
            }
            ImGui.End();
        }
    }
}
