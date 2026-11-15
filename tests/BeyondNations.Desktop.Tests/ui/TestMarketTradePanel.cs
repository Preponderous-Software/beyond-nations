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
