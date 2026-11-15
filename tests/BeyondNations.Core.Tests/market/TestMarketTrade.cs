using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Trading a chosen quantity of one good, with a quote first and a
    * receipt after (#182). Playtest 0.5.0 s05: each Sell Resources press sold
    * one unit and said only "You sold resources to Abigail Davies", and 31
    * apples never sold.
    */
    public class TestMarketTrade {
        private readonly RandomSource random = new RandomSource(182);
        private readonly EntityRepository entityRepository;
        private readonly NationRepository nationRepository;
        private readonly Nation nation;
        private readonly Settlement town;
        private readonly Market market;
        private readonly Player player;
        private readonly Pawn ann;
        private readonly Pawn bob;

        public TestMarketTrade() {
            entityRepository = new EntityRepository(random);
            nationRepository = new NationRepository(random);
            nation = new Nation("Testland", new EntityId(), random);
            nationRepository.addNation(nation);
            town = new Settlement(new Vector3(0, 1, 0), nation.getId(), nation.getColor(), "Whiteburg", random);
            entityRepository.addEntity(town);
            nation.addSettlement(town.getId());
            market = town.getMarket();

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            entityRepository.addEntity(player);
            player.getInventory().clear();
            player.setCurrentSettlementId(town.getId());

            ann = new Pawn(Vector3.Zero, "Ann Lee", random);
            bob = new Pawn(Vector3.Zero, "Bob Roberts", random);
            entityRepository.addEntity(ann);
            entityRepository.addEntity(bob);
        }

        private Stall giveStall(Entity owner, int coins) {
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(owner.getId());
            stall.getInventory().addItem(ItemType.COIN, coins);
            return stall;
        }

        private TradeAtMarketCommand command() {
            return new TradeAtMarketCommand(nationRepository, entityRepository);
        }

        // --- selling a chosen quantity ---

        [Fact]
        public void testSellingFive_SellsFiveForWhatTheQuoteSaid_AndSaysSo() {
            giveStall(ann, 500);
            player.getInventory().addItem(ItemType.WOOD, 12);
            TradeReceipt quote = MarketTrade.quoteSale(market, player, ItemType.WOOD, 5);
            Assert.Equal(5, quote.getQuantity());

            TradeReceipt receipt = command().sell(player, ItemType.WOOD, 5);

            Assert.Equal(5, receipt.getQuantity());
            Assert.Equal(quote.getCoins(), receipt.getCoins());
            Assert.Equal(7, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(receipt.getCoins(), player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Sold 5 wood to Ann Lee for " + receipt.getCoins() + " coins.", player.getStatus().getLatest());
            Assert.Equal(1, player.getMarketSales());
        }

        [Fact]
        public void testSellingFiveAtOnce_PaysWhatFiveSingleSalesWould() {
            giveStall(ann, 1000);
            player.getInventory().addItem(ItemType.APPLE, 10);
            int oneAtATime = 0;
            for (int i = 0; i < 5; i++) {
                oneAtATime += market.quoteSale(player, ItemType.APPLE, 1, 0).getCoins();
                market.sellUnits(player, ItemType.APPLE, 1, 0);
            }
            // put them back and try again in one go
            Stall stall = market.getStall(ann.getId());
            stall.getInventory().removeItem(ItemType.APPLE, 5);
            stall.getInventory().addItem(ItemType.COIN, oneAtATime);
            player.getInventory().removeItem(ItemType.COIN, oneAtATime);
            player.getInventory().addItem(ItemType.APPLE, 5);

            Assert.Equal(oneAtATime, market.sellUnits(player, ItemType.APPLE, 5, 0).getCoins());
        }

        [Fact]
        public void testASale_SpreadsOverTheTills_WhenOneRunsDry() {
            // each till can pay for one unit, and no more
            int price = market.getSellPrice(ItemType.WOOD);
            giveStall(ann, price + 1);
            giveStall(bob, price + 1);
            player.getInventory().addItem(ItemType.WOOD, 10);

            TradeReceipt receipt = command().sell(player, ItemType.WOOD, 10);

            Assert.Equal(2, receipt.getCounterparties().Count);
            Assert.Equal(2, receipt.getQuantity());
            Assert.Contains("to Ann Lee and Bob Roberts", player.getStatus().getLatest());
        }

        [Fact]
        public void testASale_StopsWhereTheTillsCanPayNoMore() {
            giveStall(ann, 30);
            player.getInventory().addItem(ItemType.STONE, 20);
            TradeReceipt receipt = command().sell(player, ItemType.STONE, 20);
            Assert.InRange(receipt.getQuantity(), 1, 19);
            Assert.True(market.getStall(ann.getId()).getInventory().getNumItems(ItemType.COIN) >= 0);
            Assert.Equal(20 - receipt.getQuantity(), player.getInventory().getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testTools_AreNeverSold() {
            giveStall(ann, 500);
            player.getInventory().addItem(ItemType.AXE, 1);
            Assert.True(command().sell(player, ItemType.AXE, 1).isEmpty());
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.AXE));
        }

        // --- buying a chosen quantity ---

        [Fact]
        public void testBuyingThree_CostsWhatGetBuyCostSays_FromWhicheverStallsHoldThem() {
            Stall annStall = giveStall(ann, 0);
            Stall bobStall = giveStall(bob, 0);
            annStall.getInventory().addItem(ItemType.APPLE, 2);
            bobStall.getInventory().addItem(ItemType.APPLE, 2);
            player.getInventory().addItem(ItemType.COIN, 1000);
            int expected = market.getBuyCost(ItemType.APPLE, 3);

            TradeReceipt receipt = command().buy(player, ItemType.APPLE, 3);

            Assert.Equal(3, receipt.getQuantity());
            Assert.Equal(expected, receipt.getCoins());
            Assert.Equal(3, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(1000 - expected, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(expected, annStall.getInventory().getNumItems(ItemType.COIN) + bobStall.getInventory().getNumItems(ItemType.COIN));
            Assert.StartsWith("Bought 3 apples from ", player.getStatus().getLatest());
        }

        [Fact]
        public void testBuying_StopsWhenThePurseRunsOut() {
            Stall stall = giveStall(ann, 0);
            stall.getInventory().addItem(ItemType.WOOD, 20);
            int oneUnit = market.getBuyPrice(ItemType.WOOD);
            player.getInventory().addItem(ItemType.COIN, oneUnit * 2 + 1);

            TradeReceipt receipt = command().buy(player, ItemType.WOOD, 10);

            Assert.InRange(receipt.getQuantity(), 2, 3);
            Assert.True(player.getInventory().getNumItems(ItemType.COIN) >= 0);
        }

        [Fact]
        public void testBuying_NeverFromThePlayersOwnStall() {
            Stall own = giveStall(player, 0);
            own.getInventory().addItem(ItemType.WOOD, 10);
            player.getInventory().addItem(ItemType.COIN, 500);
            Assert.True(command().buy(player, ItemType.WOOD, 1).isEmpty());
            Assert.Equal("No one here is selling wood.", player.getStatus().getLatest());
        }

        // --- the list and its lines ---

        [Fact]
        public void testTheList_HasWhatThePlayerCarriesAndWhatTheStallsHold() {
            Stall stall = giveStall(ann, 0);
            stall.getInventory().addItem(ItemType.APPLE, 5);
            stall.getInventory().addItem(ItemType.PICKAXE, 1);
            player.getInventory().addItem(ItemType.WOOD, 3);
            player.getInventory().addItem(ItemType.AXE, 1);

            List<ItemType> goods = MarketTrade.listGoods(market, player);

            Assert.Contains(ItemType.APPLE, goods);
            Assert.Contains(ItemType.WOOD, goods);
            Assert.Contains(ItemType.PICKAXE, goods);
            // the player's own axe is not for sale, and no stall sells one
            Assert.DoesNotContain(ItemType.AXE, goods);
            Assert.DoesNotContain(ItemType.COIN, goods);
        }

        [Fact]
        public void testTheRowLabel_IsBuySlashSellAndWhatYouCarry() {
            giveStall(ann, 100);
            player.getInventory().addItem(ItemType.WOOD, 12);
            MarketTrade.Row row = MarketTrade.describeRow(market, player, ItemType.WOOD);
            Assert.Equal("Wood  " + market.getBuyPrice(ItemType.WOOD) + "/" + market.getSellPrice(ItemType.WOOD) + "  you 12",
                MarketTrade.describeRowLabel(row));
        }

        [Fact]
        public void testTheButtons_NameTheTotals() {
            TradeReceipt sale = new TradeReceipt(ItemType.WOOD, true);
            Assert.Equal("Can't sell", MarketTrade.describeSellButton(sale));
            giveStall(ann, 500);
            player.getInventory().addItem(ItemType.WOOD, 5);
            sale = MarketTrade.quoteSale(market, player, ItemType.WOOD, 5);
            Assert.Equal("Sell 5 for " + sale.getCoins(), MarketTrade.describeSellButton(sale));
        }

        [Fact]
        public void testTheShortfall_SaysWhyAButtonCannotDealInFull() {
            giveStall(ann, 0);
            player.getInventory().addItem(ItemType.WOOD, 5);
            TradeReceipt sale = MarketTrade.quoteSale(market, player, ItemType.WOOD, 5);
            TradeReceipt purchase = MarketTrade.quotePurchase(market, player, ItemType.WOOD, 5);
            Assert.Equal("No till here can pay for one; no stall has any to sell.",
                MarketTrade.describeShortfall(market, player, ItemType.WOOD, 5, sale, purchase));
        }

        [Fact]
        public void testTheMargin_ComparesWithTheBestMarketInReach() {
            Settlement yland = new Settlement(new Vector3(300, 1, 0), nation.getId(), nation.getColor(), "Yland", random);
            entityRepository.addEntity(yland);
            giveStall(ann, 100);
            // a merchant there with coin enough to pay the full price (Market.getBid)
            yland.getMarket().createStall();
            Stall ylandStall = yland.getMarket().getStallForSale();
            ylandStall.setOwnerId(bob.getId());
            ylandStall.getInventory().addItem(ItemType.COIN, 5000);
            yland.getMarket().getPrices().setPressure(ItemType.WOOD, MarketPrices.MAX_PRESSURE);
            List<Settlement> markets = Traders.getMarkets(entityRepository, nationRepository, town, nation.getId());

            int price;
            Settlement best = MarketTrade.findBestElsewhere(markets, ItemType.WOOD, out price);

            Assert.Same(yland, best);
            Assert.Equal(yland.getMarket().getSellPrice(ItemType.WOOD), price);
            MarketTrade.Row row = MarketTrade.describeRow(market, player, ItemType.WOOD);
            Assert.Equal("Best price in reach: " + price + " at the " + yland.getDisplayName() + ".", MarketTrade.describeBestElsewhere(best, price));
            Assert.True(MarketTrade.getCarryMargin(row, price) > 0);
            Assert.Equal("Buy here, sell there: +" + (price - row.buyPrice) + " a unit.", MarketTrade.describeCarryMargin(row, price));
            row.buyPrice = price + 2;
            Assert.Equal("Buy here, sell there: -2 a unit (a loss).", MarketTrade.describeCarryMargin(row, price));
        }

        [Fact]
        public void testReceiptsAndTotals_ReadAsSentences() {
            Assert.Equal("Ann, Bob and 2 others", MarketTrade.joinNames(new List<string> { "Ann", "Bob", "Cy", "Di" }));
            Assert.Equal("This visit: spent 40, took 120, net +80.", MarketTrade.describeVisit(40, 120));
            Assert.Equal("This visit: spent 50, took 20, net -30.", MarketTrade.describeVisit(50, 20));
            Assert.Equal(1, MarketTrade.stepQuantity(1, -5));
            Assert.Equal(MarketTrade.MAX_QUANTITY, MarketTrade.stepQuantity(98, 5));
        }

        // --- the quick button's receipt ---

        [Fact]
        public void testSellResources_NowSaysWhatWasSoldAndForHowMuch() {
            giveStall(ann, 500);
            player.getInventory().addItem(ItemType.WOOD, 3);
            player.getInventory().addItem(ItemType.STONE, 3);

            new SellResourcesAtMarketCommand(nationRepository, entityRepository).execute(player);

            string line = player.getStatus().getLatest();
            Assert.StartsWith("Sold 1 wood and 1 stone to Ann Lee for ", line);
            Assert.Contains(" coins. Relationship increased to ", line);
        }
    }
}
