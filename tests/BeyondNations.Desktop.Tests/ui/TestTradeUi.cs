using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.boxes;

namespace beyondnationstests.desktop.ui {

    /**
    * The trade lane's UI decisions, without a window: the caravan buttons on
    * the command bar, the price list in the market box, and the dispatch
    * panel's choices.
    */
    [Collection("CaravanDispatchPanel")]
    public class TestTradeUi {

        private static Simulation stagedWorld(out TradeShowcase.Staged staged) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            staged = TradeShowcase.stage(simulation);
            return simulation;
        }

        private static List<string> labels(Simulation simulation) {
            List<string> labels = new List<string>();
            foreach (WorldCommand command in WorldCommandBar.getCommands(simulation)) {
                labels.Add(command.getLabel());
            }
            return labels;
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
        public void testBesideACaravan_TheBarOffersTradeEscortAndRaid() {
            Simulation simulation = stagedWorld(out TradeShowcase.Staged staged);
            TradeCaravans trade = simulation.getTradeCaravans();
            Assert.Same(staged.caravan, trade.findCaravanInReach());

            List<string> bar = labels(simulation);
            int price = trade.getPriceToBuyFrom(staged.caravan, ItemType.WOOD);
            Assert.Contains("Buy wood (" + price + ")", bar);
            Assert.Contains("Sell to Caravan", bar);
            Assert.Contains("Escort Caravan", bar);
            Assert.Contains("Raid Caravan", bar);

            int wood = simulation.getPlayer().getInventory().getNumItems(ItemType.WOOD);
            find(simulation, "Buy wood (" + price + ")").execute();
            Assert.Equal(wood + 1, simulation.getPlayer().getInventory().getNumItems(ItemType.WOOD));

            find(simulation, "Escort Caravan").execute();
            List<string> guarding = labels(simulation);
            Assert.DoesNotContain("Escort Caravan", guarding);
            Assert.DoesNotContain("Raid Caravan", guarding);
            Assert.Contains("(you ride guard)", staged.caravan.getAppearance().getLabel());
        }

        [Fact]
        public void testAwayFromCaravans_TheBarHasNoTradeButtons() {
            Simulation simulation = stagedWorld(out TradeShowcase.Staged staged);
            simulation.getPlayer().setPosition(simulation.getPlayer().getPosition() + new System.Numerics.Vector3(0, 0, -500));
            foreach (string label in labels(simulation)) {
                Assert.DoesNotContain("Caravan", label);
            }
        }

        [Fact]
        public void testTheMarketBox_ListsPricesWithTrends() {
            Simulation simulation = stagedWorld(out TradeShowcase.Staged staged);
            Market market = staged.playerSettlement.getMarket();
            List<string> lines = new MarketInfoBox(market, simulation.getEntityRepository()).getLines();
            int heading = lines.IndexOf(MarketInfoBox.PricesHeading);
            Assert.True(heading >= 0);
            string apples = lines.Find(line => line.StartsWith("Apples "));
            Assert.Equal("Apples " + market.getBuyPrice(ItemType.APPLE) + "/" + market.getSellPrice(ItemType.APPLE)
                + " " + MarketInfoBox.trendMark(MarketPricing.getTrend(market, ItemType.APPLE)), apples);
            // a crowded village with no food: apples have been climbing
            Assert.EndsWith(" ^", apples);
            Assert.Equal("v", MarketInfoBox.trendMark(-1));
            Assert.Equal("-", MarketInfoBox.trendMark(0));
        }

        [Fact]
        public void testTheDispatchPanel_LoadsSendsAndCloses() {
            Simulation simulation = stagedWorld(out TradeShowcase.Staged staged);
            Player player = simulation.getPlayer();
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, staged.playerSettlement);
            Assert.True(player.isCurrentlyInSettlement());

            Assert.Contains(CaravanCommands.SendCaravanLabel, labels(simulation));
            find(simulation, CaravanCommands.SendCaravanLabel).execute();
            Assert.True(CaravanDispatchPanel.isOpen());

            Assert.Same(staged.traderSettlement, CaravanDispatchPanel.getDestination(simulation));
            Assert.StartsWith("To the " + staged.traderSettlement.getDisplayName(), CaravanDispatchPanel.getDestinationLine(simulation));

            CaravanDispatchPanel.changeLoad(simulation, ItemType.STONE, CaravanDispatchPanel.LoadStep);
            CaravanDispatchPanel.changeLoad(simulation, ItemType.STONE, CaravanDispatchPanel.LoadStep);
            CaravanDispatchPanel.changeLoad(simulation, ItemType.STONE, CaravanDispatchPanel.LoadStep); // carries only 10
            Assert.Equal(10, CaravanDispatchPanel.getLoad(ItemType.STONE));
            Assert.StartsWith("Stone: 10 of 10", CaravanDispatchPanel.getGoodLine(simulation, ItemType.STONE));
            Assert.StartsWith("10 goods, worth about", CaravanDispatchPanel.getEstimate(simulation));

            Assert.True(CaravanDispatchPanel.send(simulation));
            Assert.False(CaravanDispatchPanel.isOpen());
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal(1, simulation.getTradeCaravans().getNumPlayerCaravans());
        }

        [Fact]
        public void testNoSendButton_WithoutAStorehouse() {
            Simulation simulation = stagedWorld(out TradeShowcase.Staged staged);
            Player player = simulation.getPlayer();
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, staged.traderSettlement);
            Assert.DoesNotContain(CaravanCommands.SendCaravanLabel, labels(simulation));
        }
    }
}
