using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Living prices (MarketPricing, MarketPrices): a glut pays less, scarcity
    * pays more, prices drift rather than jump, and no sequence of buying and
    * selling at one market turns coins into more coins.
    */
    public class TestMarketPricing {
        private readonly RandomSource random = new RandomSource(7);

        private Settlement makeSettlement() {
            return new Settlement(Vector3.Zero, new NationId(), Rgba.White, "Testland", random);
        }

        private static Stall addMerchant(Market market, int coins) {
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(new EntityId());
            stall.getInventory().addItem(ItemType.COIN, coins);
            return stall;
        }

        private Player makePlayer(int coins) {
            Player player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().clear();
            if (coins > 0) {
                player.getInventory().addItem(ItemType.COIN, coins);
            }
            return player;
        }

        // --- supply and demand ---

        [Fact]
        public void testAGluttedMarket_PaysLessThanAnEmptyOne() {
            Settlement empty = makeSettlement();
            addMerchant(empty.getMarket(), 1000);
            Settlement glutted = makeSettlement();
            addMerchant(glutted.getMarket(), 1000).getInventory().addItem(ItemType.WOOD, 40);

            Assert.True(glutted.getMarket().getSellPrice(ItemType.WOOD) < empty.getMarket().getSellPrice(ItemType.WOOD));
            Assert.True(glutted.getMarket().getBuyPrice(ItemType.WOOD) < empty.getMarket().getBuyPrice(ItemType.WOOD));
        }

        [Fact]
        public void testAHungryCrowdedTown_DriftsTowardDearFood() {
            Settlement town = makeSettlement();
            addMerchant(town.getMarket(), 1000).getInventory().addItem(ItemType.APPLE, 2);
            town.setResidentCount(12);
            int before = town.getMarket().getBuyPrice(ItemType.APPLE);
            Assert.True(MarketPricing.getTargetPressure(town, ItemType.APPLE) > MarketPrices.DEFAULT_PRESSURE);

            MarketPricing.driftPrices(town);
            Assert.Equal(MarketPrices.DEFAULT_PRESSURE + MarketPricing.DRIFT_STEP, town.getMarket().getPrices().getPressure(ItemType.APPLE));
            Assert.Equal(1, MarketPricing.getTrend(town.getMarket(), ItemType.APPLE));

            for (int i = 0; i < 40; i++) {
                MarketPricing.driftPrices(town);
            }
            Assert.True(town.getMarket().getBuyPrice(ItemType.APPLE) > before);
            Assert.True(town.getMarket().getPrices().getPressure(ItemType.APPLE) <= MarketPrices.MAX_PRESSURE);
        }

        [Fact]
        public void testAWoodpile_DriftsTowardCheapWood() {
            Settlement camp = makeSettlement();
            addMerchant(camp.getMarket(), 1000);
            camp.getInventory().addItem(ItemType.WOOD, 200);
            int before = camp.getMarket().getBuyPrice(ItemType.WOOD);

            for (int i = 0; i < 20; i++) {
                MarketPricing.driftPrices(camp);
            }
            Assert.Equal(MarketPrices.MIN_PRESSURE, camp.getMarket().getPrices().getPressure(ItemType.WOOD));
            Assert.True(camp.getMarket().getBuyPrice(ItemType.WOOD) < before);
            Assert.Equal(0, MarketPricing.getTrend(camp.getMarket(), ItemType.WOOD)); // settled at the floor
        }

        [Fact]
        public void testPrices_DriftAStepAtATime_NotAllAtOnce() {
            Settlement camp = makeSettlement();
            camp.getInventory().addItem(ItemType.STONE, 500);
            MarketPricing.driftPrices(camp);
            Assert.Equal(MarketPrices.DEFAULT_PRESSURE - MarketPricing.DRIFT_STEP, camp.getMarket().getPrices().getPressure(ItemType.STONE));
            Assert.Equal(-1, MarketPricing.getTrend(camp.getMarket(), ItemType.STONE));
        }

        [Fact]
        public void testANewMarket_ShowsNoTrend_AndThePlainCurve() {
            Settlement settlement = makeSettlement();
            addMerchant(settlement.getMarket(), 100).getInventory().addItem(ItemType.WOOD, 10);
            Assert.Equal(0, MarketPricing.getTrend(settlement.getMarket(), ItemType.WOOD));
            Assert.Equal(ItemCostCalculator.calculateCost(ItemCostCalculator.getBaseCost(ItemType.WOOD), 10),
                settlement.getMarket().getBuyPrice(ItemType.WOOD));
        }

        [Fact]
        public void testPricingRounds_RunOnTheirCadence() {
            EntityRepository repository = new EntityRepository(random);
            Settlement camp = makeSettlement();
            camp.getInventory().addItem(ItemType.STONE, 500);
            repository.addEntity(camp);
            MarketPricing.update(repository, MarketPricing.ROUND_TICKS - 1);
            Assert.Equal(MarketPrices.DEFAULT_PRESSURE, camp.getMarket().getPrices().getPressure(ItemType.STONE));
            MarketPricing.update(repository, MarketPricing.ROUND_TICKS);
            Assert.True(camp.getMarket().getPrices().getPressure(ItemType.STONE) < MarketPrices.DEFAULT_PRESSURE);
        }

        // --- no coins from nowhere (#316) ---

        /**
        * Buying one and selling it straight back, at every stock level and
        * every pressure, never leaves the player better off. Before marginal
        * pricing a sale fetched the price at the stock the unit was bought
        * from, which was higher, so a player could walk a merchant's purse
        * out of the stall a unit at a time.
        */
        [Fact]
        public void testBuyingAndSellingStraightBack_NeverMakesMoney() {
            foreach (int pressure in new[] { MarketPrices.MIN_PRESSURE, 80, 100, 137, MarketPrices.MAX_PRESSURE }) {
                for (int supply = 1; supply <= 60; supply++) {
                    Market market = new Market(4, random);
                    Stall seller = addMerchant(market, 0);
                    seller.getInventory().addItem(ItemType.WOOD, supply);
                    addMerchant(market, 100000);
                    market.getPrices().setPressure(ItemType.WOOD, pressure);

                    Player player = makePlayer(10000);
                    int coinsInWorld = player.getInventory().getNumItems(ItemType.COIN) + market.getTotalCoins();
                    Assert.NotNull(market.buyItem(player, ItemType.WOOD, 1));
                    Assert.NotNull(market.sellResources(player));
                    Assert.True(player.getInventory().getNumItems(ItemType.COIN) <= 10000,
                        "a round trip at supply " + supply + ", pressure " + pressure + " made " + (player.getInventory().getNumItems(ItemType.COIN) - 10000));
                    Assert.Equal(coinsInWorld, player.getInventory().getNumItems(ItemType.COIN) + market.getTotalCoins());
                }
            }
        }

        [Fact]
        public void testBuyingInBulk_CostsWhatBuyingOneAtATimeWould() {
            Market bulk = new Market(4, random);
            addMerchant(bulk, 0).getInventory().addItem(ItemType.STONE, 30);
            Market single = new Market(4, random);
            addMerchant(single, 0).getInventory().addItem(ItemType.STONE, 30);
            Player bulkBuyer = makePlayer(10000);
            Player singleBuyer = makePlayer(10000);

            Assert.NotNull(bulk.buyItem(bulkBuyer, ItemType.STONE, 10));
            for (int i = 0; i < 10; i++) {
                Assert.NotNull(single.buyItem(singleBuyer, ItemType.STONE, 1));
            }
            Assert.Equal(singleBuyer.getInventory().getNumItems(ItemType.COIN), bulkBuyer.getInventory().getNumItems(ItemType.COIN));
            Assert.True(bulkBuyer.getInventory().getNumItems(ItemType.COIN) < 10000 - 10 * ItemCostCalculator.calculateCost(30, 30));
        }

        [Fact]
        public void testBuyingInBulkThenSellingBack_NeverMakesMoney() {
            Market market = new Market(4, random);
            addMerchant(market, 0).getInventory().addItem(ItemType.APPLE, 25);
            addMerchant(market, 100000);
            Player player = makePlayer(10000);
            Assert.NotNull(market.buyItem(player, ItemType.APPLE, 12));
            while (player.getInventory().getNumItems(ItemType.APPLE) > 0) {
                Assert.NotNull(market.sellResources(player));
            }
            Assert.True(player.getInventory().getNumItems(ItemType.COIN) <= 10000);
        }

        [Fact]
        public void testAPrice_NeverFallsBelowOneCoin() {
            Assert.Equal(1, ItemCostCalculator.applyPressure(1, MarketPrices.MIN_PRESSURE));
            Assert.Equal(1, ItemCostCalculator.applyPressure(0, MarketPrices.MAX_PRESSURE));
            Assert.Equal(30, ItemCostCalculator.applyPressure(60, 50));
        }

        [Fact]
        public void testANationsLand_ShiftsDemandAndWhatItKeepsBack() {
            // shortage: wants half as much again, keeps half as much again back; surplus: two thirds
            Assert.Equal(30, TradeNeeds.scaleByStanding(20, ResourceStanding.SHORTAGE, true));
            Assert.Equal(13, TradeNeeds.scaleByStanding(20, ResourceStanding.SURPLUS, true));
            Assert.Equal(20, TradeNeeds.scaleByStanding(20, ResourceStanding.NORMAL, true));
            Assert.Equal(Commodity.FOOD, TradeNeeds.commodityOf(ItemType.COOKED_MEAT));
            Assert.Null(TradeNeeds.commodityOf(ItemType.PLANKS));
            // with no land to read (a bare market), every nation stands NORMAL
            Settlement settlement = makeSettlement();
            Assert.Equal(ResourceStanding.NORMAL, TradeNeeds.getStanding(null, settlement, ItemType.WOOD));
            Assert.Equal(TradeNeeds.WOOD_RESERVE, TradeNeeds.getReserve(null, settlement, ItemType.WOOD));
        }

        [Fact]
        public void testInARealWorld_DemandFollowsTheNationsLand() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            for (int i = 0; i < 6000; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            TradeNeeds needs = simulation.getTradeCaravans().getNeeds();
            int checkedSettlements = 0;
            foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT)) {
                Settlement settlement = (Settlement) entity;
                foreach (ItemType itemType in new[] { ItemType.WOOD, ItemType.STONE, ItemType.APPLE }) {
                    int plain = MarketPricing.getDemand(settlement, itemType);
                    int expected = TradeNeeds.scaleByStanding(plain, TradeNeeds.getStanding(needs, settlement, itemType), true);
                    Assert.Equal(expected, MarketPricing.getDemand(settlement, itemType, needs));
                }
                checkedSettlements++;
            }
            Assert.True(checkedSettlements > 0);
        }

        [Fact]
        public void testPressure_IsClampedToItsRange() {
            MarketPrices prices = new MarketPrices();
            prices.setPressure(ItemType.WOOD, 9999);
            Assert.Equal(MarketPrices.MAX_PRESSURE, prices.getPressure(ItemType.WOOD));
            prices.setPressure(ItemType.WOOD, -5);
            Assert.Equal(MarketPrices.MIN_PRESSURE, prices.getPressure(ItemType.WOOD));
        }
    }
}
