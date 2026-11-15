using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;

namespace beyondnationstests.desktop.ui {

    /**
    * The market panel (#182) without a window: the command bar opens it in
    * a settlement with stalls, it lists the goods, the quantity steps, and
    * Buy and Sell deal what their labels promise. The staged trade world
    * (TradeShowcase) puts a merchant with coins and wood in the trading
    * nation's village; the player is walked into it.
    */
    [Collection("CaravanDispatchPanel")]
    public class TestMarketTradePanel : System.IDisposable {

        public void Dispose() {
            MarketTradePanel.close();
        }

        private static Simulation inTheTradersVillage(out TradeShowcase.Staged staged) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            staged = TradeShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            player.setPosition(staged.traderSettlement.getPosition());
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, staged.traderSettlement);
            Assert.True(player.isCurrentlyInSettlement());
            return simulation;
        }

        private static WorldCommand find(Simulation simulation, string label) {
            foreach (WorldCommand command in WorldCommandBar.getCommands(simulation)) {
                if (command.getLabel() == label) {
                    return command;
                }
            }
            return null;
        }

        [Fact]
        public void testTheBar_OpensThePanelInAMarket() {
            Simulation simulation = inTheTradersVillage(out _);
            WorldCommand open = find(simulation, MarketTradePanel.OpenLabel);
            Assert.NotNull(open);
            Assert.True(open.isEnabled());

            open.execute();

            Assert.True(MarketTradePanel.isOpen());
            // the first good is chosen, so Buy and Sell mean something at once
            Assert.NotNull(MarketTradePanel.getSelected(simulation));
        }

        [Fact]
        public void testThePanel_ListsWhatThePlayerCarriesAndWhatTheStallsHold() {
            Simulation simulation = inTheTradersVillage(out _);
            MarketTradePanel.open(simulation);
            List<ItemType> goods = MarketTradePanel.getGoods(simulation);
            Assert.Contains(ItemType.WOOD, goods);
            Assert.Contains(ItemType.STONE, goods);
            Assert.Contains(ItemType.APPLE, goods);
        }

        [Fact]
        public void testSellingFive_DealsFive_AndKeepsTheVisitsTotal() {
            Simulation simulation = inTheTradersVillage(out _);
            Player player = simulation.getPlayer();
            MarketTradePanel.open(simulation);
            MarketTradePanel.select(ItemType.STONE);
            MarketTradePanel.changeQuantity(5);
            MarketTradePanel.changeQuantity(-1);
            Assert.Equal(5, MarketTradePanel.getQuantity());
            int stone = player.getInventory().getNumItems(ItemType.STONE);
            TradeReceipt quote = MarketTradePanel.getSaleQuote(simulation);
            Assert.Equal(5, quote.getQuantity());

            TradeReceipt receipt = MarketTradePanel.sell(simulation);

            Assert.Equal(quote.getCoins(), receipt.getCoins());
            Assert.Equal(stone - 5, player.getInventory().getNumItems(ItemType.STONE));
            Assert.StartsWith("Sold 5 stone to ", MarketTradePanel.getLastReceipt());
            Assert.Equal(MarketTrade.describeVisit(0, receipt.getCoins()), MarketTradePanel.getVisitLine());
        }

        [Fact]
        public void testBuying_DealsWhatTheButtonSaid() {
            Simulation simulation = inTheTradersVillage(out _);
            Player player = simulation.getPlayer();
            MarketTradePanel.open(simulation);
            MarketTradePanel.select(ItemType.WOOD);
            MarketTradePanel.changeQuantity(2);
            TradeReceipt quote = MarketTradePanel.getPurchaseQuote(simulation);
            int coins = player.getInventory().getNumItems(ItemType.COIN);

            TradeReceipt receipt = MarketTradePanel.buy(simulation);

            Assert.Equal(3, receipt.getQuantity());
            Assert.Equal(MarketTrade.describeBuyButton(quote), MarketTrade.describeBuyButton(receipt));
            Assert.Equal(coins - receipt.getCoins(), player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testAll_SetsTheQuantityToEverythingCarried() {
            Simulation simulation = inTheTradersVillage(out _);
            MarketTradePanel.open(simulation);
            MarketTradePanel.select(ItemType.APPLE);
            MarketTradePanel.setQuantityToAll(simulation);
            Assert.Equal(simulation.getPlayer().getInventory().getNumItems(ItemType.APPLE), MarketTradePanel.getQuantity());
        }

        [Fact]
        public void testTheMargin_NamesTheBestMarketInReach() {
            Simulation simulation = inTheTradersVillage(out TradeShowcase.Staged staged);
            MarketTradePanel.open(simulation);
            MarketTradePanel.select(ItemType.WOOD);
            List<MarketTradePanel.MarginLine> lines = MarketTradePanel.getMarginLines(simulation);
            Assert.NotEmpty(lines);
            Assert.Contains(staged.playerSettlement.getDisplayName(), lines[0].text);
            // wood is on the stall here and in the player's pack: both margins show
            Assert.Equal(3, lines.Count);
            Assert.StartsWith("Buy here, sell there: ", lines[1].text);
            Assert.StartsWith("Sell there, not here: ", lines[2].text);
        }

        // --- 0.8.0 playtest s05: rows, quotes, Escape and the window's edge ---

        [Fact]
        public void testTheRows_KeepTheirOrder_WhenGoodsComeUpOrSellOut() {
            Simulation simulation = inTheTradersVillage(out TradeShowcase.Staged staged);
            Player player = simulation.getPlayer();
            MarketTradePanel.open(simulation);
            List<ItemType> opened = MarketTradePanel.getGoods(simulation);
            MarketTradePanel.select(ItemType.STONE);

            // bricks and an axe reach the stall, and the player sells all their stone
            Stall stall = staged.traderSettlement.getMarket().getStalls()[0];
            stall.getInventory().addItem(ItemType.BRICKS, 4);
            stall.getInventory().addItem(ItemType.AXE, 1);
            MarketTradePanel.setQuantityToAll(simulation);
            MarketTradePanel.sell(simulation);
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.STONE));

            List<ItemType> now = MarketTradePanel.getGoods(simulation);
            // every row that was there is where it was, newcomers below
            Assert.Equal(opened, now.GetRange(0, opened.Count));
            Assert.Contains(ItemType.BRICKS, now.GetRange(opened.Count, now.Count - opened.Count));
            Assert.Contains(ItemType.AXE, now.GetRange(opened.Count, now.Count - opened.Count));
            // and the choice stays on the stone the player picked
            Assert.Equal(ItemType.STONE, MarketTradePanel.getSelected(simulation));
        }

        [Fact]
        public void testAPress_OfALabelWhoseTermsHaveMoved_DealsNothing_ThenTheNewTermsOnTheNext() {
            Simulation simulation = inTheTradersVillage(out TradeShowcase.Staged staged);
            Player player = simulation.getPlayer();
            MarketTradePanel.open(simulation);
            MarketTradePanel.select(ItemType.WOOD);
            MarketTradePanel.changeQuantity(5);
            TradeReceipt shown = MarketTradePanel.getSaleQuote(simulation);
            Assert.Equal(6, shown.getQuantity());
            // a frame later the till has paid someone else
            Stall stall = staged.traderSettlement.getMarket().getStalls()[0];
            stall.getInventory().removeItem(ItemType.COIN, stall.getInventory().getNumItems(ItemType.COIN) - 120);
            int wood = player.getInventory().getNumItems(ItemType.WOOD);

            TradeReceipt first = MarketTradePanel.sell(simulation, shown);

            Assert.True(first.isEmpty());
            Assert.Equal(wood, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.StartsWith("The market moved before your press", MarketTradePanel.getLastReceipt());
            Assert.Null(MarketTradePanel.getVisitLine());

            TradeReceipt now = MarketTradePanel.getSaleQuote(simulation);
            TradeReceipt second = MarketTradePanel.sell(simulation, now);
            Assert.False(second.isEmpty());
            Assert.Equal(now.getQuantity(), second.getQuantity());
            Assert.Equal(now.getCoins(), second.getCoins());
        }

        [Fact]
        public void testEscape_ClosesThePanel_AndOnlyWhenItIsOpen() {
            Simulation simulation = inTheTradersVillage(out _);
            Assert.False(MarketTradePanel.escapePressed());
            MarketTradePanel.open(simulation);

            Assert.True(MarketTradePanel.escapePressed());
            Assert.False(MarketTradePanel.isOpen());
            Assert.False(MarketTradePanel.escapePressed());
        }

        [Fact]
        public void testThePanelsTop_HoldsWhileOpen_WhenTheStatusLinePushesTheOfferedTopDown() {
            Simulation simulation = inTheTradersVillage(out _);
            MarketTradePanel.open(simulation);

            Assert.Equal(22f, MarketTradePanel.holdTop(22f, 960, 540));
            // a sale's receipt comes up on the status line: the HUD offers a lower top
            Assert.Equal(22f, MarketTradePanel.holdTop(58f, 960, 540));
            // a new window size takes the offered top
            Assert.Equal(58f, MarketTradePanel.holdTop(58f, 1280, 720));

            MarketTradePanel.close();
            MarketTradePanel.open(simulation);
            Assert.Equal(40f, MarketTradePanel.holdTop(40f, 1280, 720));
            MarketTradePanel.close();
        }

        [Fact]
        public void testTheStayLine_NamesNoKey() {
            Assert.DoesNotContain("press E", MarketTradePanel.StayLine);
            Assert.Contains("Exit Settlement", MarketTradePanel.StayLine);
        }

        // what ImGui's default style measures at 1x: a 13 px font
        private const float Line = 13f;
        private const float Frame = 19f;
        private const float Spacing = 4f;
        private const float Padding = 8f;

        [Theory]
        [InlineData(960, 540, 22, false)]
        [InlineData(1280, 720, 22, false)]
        [InlineData(390, 844, 60, true)]
        [InlineData(844, 390, 60, true)]
        public void testThePanel_FitsTheWindow_CloseIncluded(int width, int height, int topY, bool touch) {
            foreach (int goods in new[] { 1, 3, 7, 10 }) {
                MarketTradePanel.Layout layout = MarketTradePanel.computeLayout(width, height, topY, goods, Line, Frame, Spacing, Padding, 1f, touch);
                Assert.True(topY + layout.height <= height, width + "x" + height + " with " + goods + " goods: " + (topY + layout.height));
                Assert.True(layout.listHeight >= layout.rowHeight, "at least one row of goods shows");
                if (touch) {
                    Assert.True(layout.rowHeight >= TouchLayout.MinTarget && layout.buttonHeight >= TouchLayout.MinTarget);
                }
            }
        }

        [Fact]
        public void testThePlaytestsMarket_ShowsEveryGoodWithoutScrolling_At960x540() {
            // seven goods at Westernville, two to a row
            MarketTradePanel.Layout layout = MarketTradePanel.computeLayout(960, 540, 22, 7, Line, Frame, Spacing, Padding, 1f, false);
            Assert.Equal(2, layout.columns);
            int rowsShown = (int) ((layout.listHeight + Spacing) / (layout.rowHeight + Spacing));
            Assert.True(rowsShown * layout.columns >= 7, "rows shown: " + rowsShown);
        }

        [Fact]
        public void testOutsideASettlement_TheBarHasNoMarketButton() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            TradeShowcase.stage(simulation);
            Assert.Null(find(simulation, MarketTradePanel.OpenLabel));
        }
    }
}
