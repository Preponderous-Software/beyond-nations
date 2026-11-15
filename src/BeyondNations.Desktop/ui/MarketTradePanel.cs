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
    * Everything is a button, so it works the same by touch as with a
    * mouse; no part needs hovering or a keyboard. What it shows comes from
    * MarketTrade (Core) and plain methods here, which tests check without a
    * window. Nothing here is saved: closing it forgets the choice and the
    * visit's totals.
    */
    public static class MarketTradePanel {
        public const string OpenLabel = "Trade at Market";
        public const string Title = "MARKET";

        private static bool isOpenFlag;
        private static EntityId settlementId;
        private static ItemType? selected;
        private static int quantity = 1;
        private static int spent;
        private static int earned;
        private static string lastReceipt;
        private static bool lastReceiptDealt;

        private const ImGuiWindowFlags PanelFlags =
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.AlwaysAutoResize;

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
            List<ItemType> goods = getGoods(simulation);
            selected = goods.Count > 0 ? goods[0] : (ItemType?) null;
        }

        public static void close() {
            isOpenFlag = false;
            selected = null;
            settlementId = null;
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

        public static List<ItemType> getGoods(Simulation simulation) {
            Settlement here = getSettlement(simulation);
            return here == null ? new List<ItemType>() : MarketTrade.listGoods(here.getMarket(), simulation.getPlayer());
        }

        /** The chosen good, kept to one the list still has (the last of something sold or bought out moves it on). */
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

        public static TradeReceipt sell(Simulation simulation) {
            ItemType? good = getSelected(simulation);
            if (good == null) {
                return new TradeReceipt(ItemType.COIN, true);
            }
            TradeReceipt receipt = new TradeAtMarketCommand(simulation.getNationRepository(), simulation.getEntityRepository())
                .sell(simulation.getPlayer(), good.Value, quantity);
            earned += receipt.getCoins();
            lastReceipt = simulation.getPlayer().getStatus().getLatest();
            lastReceiptDealt = !receipt.isEmpty();
            return receipt;
        }

        public static TradeReceipt buy(Simulation simulation) {
            ItemType? good = getSelected(simulation);
            if (good == null) {
                return new TradeReceipt(ItemType.COIN, false);
            }
            TradeReceipt receipt = new TradeAtMarketCommand(simulation.getNationRepository(), simulation.getEntityRepository())
                .buy(simulation.getPlayer(), good.Value, quantity);
            spent += receipt.getCoins();
            lastReceipt = simulation.getPlayer().getStatus().getLatest();
            lastReceiptDealt = !receipt.isEmpty();
            return receipt;
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
            Player player = simulation.getPlayer();
            Market market = here.getMarket();
            float buttonHeight = System.MathF.Max(Overlay.frameHeight() + UiScale.px(8f), UiScale.px(36f));
            float small = UiScale.px(48f);
            float panelWidth = System.MathF.Min(width - UiScale.px(32f), UiScale.px(480f));
            float half = (panelWidth - ImGui.GetStyle().ItemSpacing.X) / 2f;

            ImGui.SetNextWindowPos(new Vector2(width / 2f, topY), ImGuiCond.Always, new Vector2(0.5f, 0f));
            ImGui.SetNextWindowBgAlpha(0.92f);
            if (ImGui.Begin("##market-trade", PanelFlags)) {
                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + panelWidth);
                ImGui.TextUnformatted(Title + " - the " + here.getDisplayName() + " - you have "
                    + player.getInventory().getNumItems(ItemType.COIN) + " coins");

                List<ItemType> goods = getGoods(simulation);
                ItemType? chosen = getSelected(simulation);
                if (goods.Count == 0) {
                    Overlay.textColoured(Warn, "Nothing to trade: the stalls are empty and you carry nothing they buy.");
                }
                foreach (ItemType itemType in goods) {
                    ItemType good = itemType;
                    MarketTrade.Row row = MarketTrade.describeRow(market, player, good);
                    bool isChosen = chosen.HasValue && chosen.Value == good;
                    if (isChosen) {
                        ImGui.PushStyleColor(ImGuiCol.Button, ChosenFill);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ChosenHover);
                    }
                    if (ImGui.Button(MarketTrade.describeRowLabel(row) + "##good-" + good, new Vector2(panelWidth, buttonHeight))) {
                        select(good);
                    }
                    if (isChosen) {
                        ImGui.PopStyleColor(2);
                    }
                }

                if (chosen.HasValue) {
                    MarketTrade.Row row = MarketTrade.describeRow(market, player, chosen.Value);
                    ImGui.Separator();
                    ImGui.TextUnformatted(MarketTrade.describeDetail(row));
                    foreach (MarginLine line in getMarginLines(simulation)) {
                        Overlay.textColoured(line.sign > 0 ? Gain : line.sign < 0 ? Warn : Muted, line.text);
                    }

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
                    if (dealButton(MarketTrade.describeBuyButton(purchase) + "##buy", half, buttonHeight, !purchase.isEmpty())) {
                        buy(simulation);
                    }
                    ImGui.SameLine();
                    if (dealButton(MarketTrade.describeSellButton(sale) + "##sell", half, buttonHeight, !sale.isEmpty())) {
                        sell(simulation);
                    }
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

                if (ImGui.Button("Close##market", new Vector2(panelWidth, buttonHeight))) {
                    close();
                }
                ImGui.PopTextWrapPos();
            }
            ImGui.End();
        }
    }
}
