using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Early markets have coin to pay sellers, and prices fall gradually
    * (#369). The 0.6.0 playtest (s05, seed 105) found every Day 1 market
    * holding 0-1 coins, so Sell Resources failed, and apples going from 42
    * to 8 within a game day.
    */
    public class TestMarketLiquidity {
        private readonly RandomSource random = new RandomSource(369);

        private EntityRepository entityRepository;
        private NationRepository nationRepository;
        private PawnBehaviorExecutor executor;

        public TestMarketLiquidity() {
            entityRepository = new EntityRepository(random);
            nationRepository = new NationRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            executor = new PawnBehaviorExecutor(environment, nationRepository, new EventProducer(new EventRepository()),
                entityRepository, random, new NationNameGenerator(random), new TickCounter());
        }

        private Nation makeNation() {
            Nation nation = new Nation("Testland", new EntityId(), random);
            nationRepository.addNation(nation);
            return nation;
        }

        private Settlement makeSettlement(Nation nation) {
            Settlement settlement = new Settlement(new Vector3(0, 1, 0), nation.getId(), nation.getColor(), "Home", random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return settlement;
        }

        private Pawn makeMember(Nation nation, NationRole role, Settlement inside, int coins) {
            Pawn pawn = new Pawn(Vector3.Zero, "test", random);
            pawn.setEnergy(100);
            pawn.getInventory().clear();
            if (coins > 0) {
                pawn.getInventory().addItem(ItemType.COIN, coins);
            }
            entityRepository.addEntity(pawn);
            nation.addMember(pawn.getId());
            nation.setRole(pawn.getId(), role);
            pawn.setNationId(nation.getId());
            pawn.setCurrentSettlementId(inside.getId());
            return pawn;
        }

        private static Stall giveStall(Market market, Entity owner, int coins) {
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(owner.getId());
            if (coins > 0) {
                stall.getInventory().addItem(ItemType.COIN, coins);
            }
            return stall;
        }

        // --- the opening float ---

        [Fact]
        public void testASettlerBuyingAStall_PutsThePriceInTheTill() {
            Nation nation = makeNation();
            Settlement home = makeSettlement(nation);
            home.getMarket().createStall();
            Pawn settler = makeMember(nation, NationRole.SERF, home, Stall.COIN_COST_TO_PURCHASE);

            executor.executeBehavior(settler, BehaviorType.PURCHASE_STALL);

            Stall stall = home.getMarket().getStall(settler.getId());
            Assert.NotNull(stall);
            Assert.Equal(Stall.COIN_COST_TO_PURCHASE, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(0, settler.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testANewMarket_CanPayThePlayerForAFewGoods() {
            Nation nation = makeNation();
            Settlement home = makeSettlement(nation);
            home.getMarket().createStall();
            Pawn settler = makeMember(nation, NationRole.SERF, home, Stall.COIN_COST_TO_PURCHASE);
            executor.executeBehavior(settler, BehaviorType.PURCHASE_STALL);

            Player player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().clear();
            player.getInventory().addItem(ItemType.APPLE, 1);
            player.getInventory().addItem(ItemType.WOOD, 1);

            Assert.Null(home.getMarket().describeWhyNoSale(player));
            Assert.NotNull(home.getMarket().sellResources(player));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.True(player.getInventory().getNumItems(ItemType.COIN) > 0);
        }

        // --- merchants leave a float when they take their profit ---

        [Fact]
        public void testAMerchantCollectingProfit_LeavesTheReserveInTheTill() {
            Nation nation = makeNation();
            Settlement home = makeSettlement(nation);
            Pawn merchant = makeMember(nation, NationRole.MERCHANT, home, 0);
            Stall stall = giveStall(home.getMarket(), merchant, 300);

            executor.executeBehavior(merchant, BehaviorType.COLLECT_PROFIT_FROM_STALL);

            Assert.Equal(Stall.TILL_RESERVE, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(300 - Stall.TILL_RESERVE, merchant.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testAMerchantStockingItsStall_TakesHalfOfWhatIsAboveTheReserve() {
            Nation nation = makeNation();
            Settlement home = makeSettlement(nation);
            Pawn merchant = makeMember(nation, NationRole.MERCHANT, home, 0);
            Stall stall = giveStall(home.getMarket(), merchant, Stall.TILL_RESERVE + 60);
            merchant.getInventory().addItem(ItemType.WOOD, 2);

            executor.executeBehavior(merchant, BehaviorType.TRANSFER_ITEMS_TO_STALL);

            Assert.Equal(Stall.TILL_RESERVE + 30, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(30, merchant.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(2, stall.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testAMerchantStockingItsStall_KeepsItsPurseOutOfTheTill() {
            Nation nation = makeNation();
            Settlement home = makeSettlement(nation);
            Pawn merchant = makeMember(nation, NationRole.MERCHANT, home, 40);
            Stall stall = giveStall(home.getMarket(), merchant, 0);
            merchant.getInventory().addItem(ItemType.WOOD, 2);

            executor.executeBehavior(merchant, BehaviorType.TRANSFER_ITEMS_TO_STALL);

            Assert.Equal(0, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(40, merchant.getInventory().getNumItems(ItemType.COIN));
        }

        // --- settlers selling surplus do not empty a till ---

        [Fact]
        public void testASettlerSellingSurplus_LeavesTheReserve() {
            Nation nation = makeNation();
            Settlement home = makeSettlement(nation);
            Pawn merchant = makeMember(nation, NationRole.MERCHANT, home, 0);
            Stall stall = giveStall(home.getMarket(), merchant, Stall.TILL_RESERVE + 10);
            Pawn settler = makeMember(nation, NationRole.SERF, home, 0);
            settler.getInventory().addItem(ItemType.WOOD, Inventory.ABUNDANCE_THRESHOLD);

            // a wood sells for more than the ten coins above the reserve
            Assert.True(home.getMarket().getSellPrice(ItemType.WOOD) > 10);
            Assert.False(home.getMarket().canSellTo(settler, 0, Stall.TILL_RESERVE));
            executor.executeBehavior(settler, BehaviorType.SELL_RESOURCES);

            Assert.Equal(Stall.TILL_RESERVE + 10, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(Inventory.ABUNDANCE_THRESHOLD, settler.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testAHungrySettler_MaySellIntoTheReserve() {
            Nation nation = makeNation();
            Settlement home = makeSettlement(nation);
            Pawn merchant = makeMember(nation, NationRole.MERCHANT, home, 0);
            Stall stall = giveStall(home.getMarket(), merchant, Stall.TILL_RESERVE);
            Pawn settler = makeMember(nation, NationRole.SERF, home, 0);
            settler.getInventory().addItem(ItemType.WOOD, 3);
            settler.setEnergy(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);

            executor.executeBehavior(settler, BehaviorType.SELL_RESOURCES);

            Assert.Equal(2, settler.getInventory().getNumItems(ItemType.WOOD));
            Assert.True(stall.getInventory().getNumItems(ItemType.COIN) < Stall.TILL_RESERVE);
        }

        private BehaviorType settlerWithSurplusDecides(int coinsInTill) {
            Nation nation = makeNation();
            Settlement home = makeSettlement(nation);
            Pawn merchant = makeMember(nation, NationRole.MERCHANT, home, 0);
            giveStall(home.getMarket(), merchant, coinsInTill);
            Pawn settler = makeMember(nation, NationRole.SERF, home, 0);
            settler.getInventory().addItem(ItemType.WOOD, Inventory.ABUNDANCE_THRESHOLD);
            settler.setHomeSettlementId(home.getId());
            Environment environment = new Environment(5, 5, entityRepository, random);
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, new GameConfig(), new TickCounter(), random);
            return calculator.computeBehaviorType(settler);
        }

        [Fact]
        public void testASettlerWithSurplus_SellsWhereATillCanSpareIt() {
            Assert.Equal(BehaviorType.SELL_RESOURCES, settlerWithSurplusDecides(Stall.TILL_RESERVE + 200));
        }

        [Fact]
        public void testASettlerWithSurplus_DoesNotGoToSellWhereNoTillCanSpareIt() {
            Assert.NotEqual(BehaviorType.SELL_RESOURCES, settlerWithSurplusDecides(Stall.TILL_RESERVE));
        }

        // --- prices fall gradually, and not to nothing ---

        [Fact]
        public void testPrices_NeverFallBelowTheFloor_EvenUnderAGlut() {
            Nation nation = makeNation();
            Settlement glutted = makeSettlement(nation);
            Pawn merchant = makeMember(nation, NationRole.MERCHANT, glutted, 0);
            Stall stall = giveStall(glutted.getMarket(), merchant, 1000);
            stall.getInventory().addItem(ItemType.APPLE, 500);
            stall.getInventory().addItem(ItemType.WOOD, 500);
            glutted.getInventory().addItem(ItemType.WOOD, 2000);
            glutted.getInventory().addItem(ItemType.APPLE, 2000);
            for (int i = 0; i < 200; i++) {
                MarketPricing.driftPrices(glutted);
            }
            Assert.Equal(MarketPrices.MIN_PRESSURE, glutted.getMarket().getPrices().getPressure(ItemType.APPLE));

            foreach (ItemType itemType in new[] { ItemType.APPLE, ItemType.WOOD }) {
                int floor = ItemCostCalculator.getBaseCost(itemType) * ItemCostCalculator.PRICE_FLOOR_PERCENT / 100;
                Assert.Equal(floor, glutted.getMarket().getBuyPrice(itemType));
                Assert.Equal(floor, glutted.getMarket().getSellPrice(itemType));
            }
            // a glutted apple used to sell for 5 (a quarter of the base, halved again by pressure)
            Assert.True(glutted.getMarket().getBuyPrice(ItemType.APPLE) >= 16);
        }

        [Fact]
        public void testPrices_FallGraduallyOverTheFirstDay() {
            // the playtest's market: a stall stocked with a dozen-odd apples,
            // in a settlement whose stockpile is far beyond its demand
            Nation nation = makeNation();
            Settlement town = makeSettlement(nation);
            Pawn merchant = makeMember(nation, NationRole.MERCHANT, town, 0);
            Stall stall = giveStall(town.getMarket(), merchant, 100);
            stall.getInventory().addItem(ItemType.APPLE, 15);
            town.getInventory().addItem(ItemType.APPLE, 400);
            int opening = town.getMarket().getBuyPrice(ItemType.APPLE);

            int roundsPerQuarterDay = TimeOfDay.DefaultTicksPerDay / 4 / MarketPricing.ROUND_TICKS;
            for (int i = 0; i < roundsPerQuarterDay; i++) {
                MarketPricing.driftPrices(town);
            }
            int afterAQuarterDay = town.getMarket().getBuyPrice(ItemType.APPLE);
            for (int i = 0; i < 3 * roundsPerQuarterDay; i++) {
                MarketPricing.driftPrices(town);
            }
            int afterADay = town.getMarket().getBuyPrice(ItemType.APPLE);

            // it falls, but a quarter of a day takes off well under half of it,
            // and a whole day leaves it above a third of where it opened
            Assert.True(afterAQuarterDay < opening);
            Assert.True(afterAQuarterDay * 100 >= opening * 70, "quarter day: " + opening + " -> " + afterAQuarterDay);
            Assert.True(afterADay * 3 > opening, "a day: " + opening + " -> " + afterADay);
        }
    }
}
