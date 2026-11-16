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
            // not the player's settlement: nothing about caravans at all
            Assert.Null(CaravanCommands.getBlockedSendLabel(simulation));
        }

        [Fact]
        public void testOwnSettlementInTheCampEra_ShowsSendCaravanLockedWithTheReason() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();
            Nation nation = new Nation("Testland", player.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            player.setNationId(nation.getId());
            Settlement settlement = new Settlement(player.getPosition(), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
            simulation.getEraTracker().applyRules(simulation);
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);

            // playtest 0.4.1 s05: the button and its refusal were both hidden
            Assert.Contains("Send Caravan (Village era)", labels(simulation));
            string reason = "This settlement needs a Storehouse before it can send caravans. The Storehouse is unlocked in the Village era.";
            WorldCommand send = find(simulation, "Send Caravan (Village era)");
            // #479: greyed like any other button that cannot be used yet, with the whole reason
            Assert.False(send.isEnabled());
            Assert.Equal(reason, send.getDisabledReason());
            Assert.Equal(reason, send.getTooltip());
            // a click or a tap on it says why (WorldHud.explainDisabled), with no keys named
            beyondnations.desktop.ui.screens.WorldHud.explainDisabled(send, player.getStatus());
            Assert.Equal(reason, player.getStatus().getStatus());
            Assert.False(CaravanDispatchPanel.isOpen());
            // and the panel still refuses if something presses it anyway
            send.execute();
            Assert.False(CaravanDispatchPanel.isOpen());
            Assert.StartsWith(reason, player.getStatus().getStatus());

            // in the Town era, the Storehouse is what is missing; still greyed
            settlement.setBuildingEra(Era.TOWN);
            Assert.Contains("Send Caravan (needs Storehouse)", labels(simulation));
            Assert.False(find(simulation, "Send Caravan (needs Storehouse)").isEnabled());
            Assert.Equal("This settlement needs a Storehouse before it can send caravans.",
                find(simulation, "Send Caravan (needs Storehouse)").getDisabledReason());
        }

        [Fact]
        public void testSendCaravan_IsEnabled_WhenACaravanCanGo() {
            Simulation simulation = stagedWorld(out TradeShowcase.Staged staged);
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(simulation.getPlayer(), staged.playerSettlement);
            Assert.True(find(simulation, CaravanCommands.SendCaravanLabel).isEnabled());
        }

        [Fact]
        public void testTheEstimate_SaysWhenTheMarketCannotPayForItAll() {
            Simulation simulation = stagedWorld(out TradeShowcase.Staged staged);
            Player player = simulation.getPlayer();
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, staged.playerSettlement);
            find(simulation, CaravanCommands.SendCaravanLabel).execute();
            Settlement destination = CaravanDispatchPanel.getDestination(simulation);
            // a poor market: empty tills and treasury
            foreach (Stall stall in destination.getMarket().getStalls()) {
                stall.getInventory().removeItem(ItemType.COIN, stall.getInventory().getNumItems(ItemType.COIN));
            }
            destination.removeFunds(destination.getFunds());
            destination.addFunds(3);
            CaravanDispatchPanel.changeLoad(simulation, ItemType.STONE, CaravanDispatchPanel.LoadStep);

            string estimate = CaravanDispatchPanel.getEstimate(simulation);
            // #485: the cap is said, and what happens to the rest
            Assert.Contains("can pay only about 3", estimate);
            Assert.EndsWith("What it cannot pay for comes home.", estimate);
            CaravanDispatchPanel.close();
        }

        [Fact]
        public void testThePanel_HoldsItsTopAndRows_WhileOpen() {
            Simulation simulation = stagedWorld(out TradeShowcase.Staged staged);
            Player player = simulation.getPlayer();
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, staged.playerSettlement);
            find(simulation, CaravanCommands.SendCaravanLabel).execute();
            Assert.True(CaravanDispatchPanel.isOpen());

            // #486: a new status line or warning offers a lower top; the panel stays put
            Assert.Equal(120f, CaravanDispatchPanel.holdTop(120f, 1280f, 720f));
            Assert.Equal(120f, CaravanDispatchPanel.holdTop(154f, 1280f, 720f));
            // a new window size takes the offered top again
            Assert.Equal(90f, CaravanDispatchPanel.holdTop(90f, 390f, 844f));

            // a good's row stays when the last of it is gone (eaten, sold) while the panel is open
            List<ItemType> before = CaravanDispatchPanel.getShownGoods(simulation);
            Assert.Contains(ItemType.STONE, before);
            player.getInventory().removeItem(ItemType.STONE, player.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal(before, CaravanDispatchPanel.getShownGoods(simulation));

            // reopening starts afresh
            CaravanDispatchPanel.close();
            find(simulation, CaravanCommands.SendCaravanLabel).execute();
            Assert.DoesNotContain(ItemType.STONE, CaravanDispatchPanel.getShownGoods(simulation));
            Assert.Equal(154f, CaravanDispatchPanel.holdTop(154f, 1280f, 720f));
            CaravanDispatchPanel.close();
        }

        [Fact]
        public void testThePanelWidth_IsFixedForTheWindow() {
            Assert.Equal(CaravanDispatchPanel.MaxPanelWidth, CaravanDispatchPanel.getPanelWidth(1920f, 1f));
            Assert.Equal(390f - 32f, CaravanDispatchPanel.getPanelWidth(390f, 1f));
        }
    }
}
