using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The trader role (#167): pawns who buy low in one market and sell high
    * in another. Two settlements 400 apart: Lowford's stalls are full of
    * wood, so it is cheap there; Highford's are empty of it but its tills
    * are full, so it is dear there.
    */
    public class TestTraders {
        private readonly RandomSource random = new RandomSource(167);

        private readonly EntityRepository entityRepository;
        private readonly NationRepository nationRepository;
        private readonly Environment environment;
        private readonly PawnBehaviorExecutor executor;
        private readonly PawnBehaviorCalculator calculator;
        private readonly Nation nation;
        private readonly Settlement lowford;
        private readonly Settlement highford;

        public TestTraders() {
            entityRepository = new EntityRepository(random);
            nationRepository = new NationRepository(random);
            environment = new Environment(5, 5, entityRepository, random);
            executor = new PawnBehaviorExecutor(environment, nationRepository, new EventProducer(new EventRepository()),
                entityRepository, random, new NationNameGenerator(random), new TickCounter());
            calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, new GameConfig(), new TickCounter(), random);

            nation = new Nation("Testland", new EntityId(), random);
            nationRepository.addNation(nation);
            lowford = makeSettlement(new Vector3(0, 1, 0), "Lowford");
            highford = makeSettlement(new Vector3(400, 1, 0), "Highford");

            Pawn lowMerchant = makeMember(NationRole.MERCHANT, lowford, 0);
            Stall lowStall = giveStall(lowford.getMarket(), lowMerchant, Stall.TILL_RESERVE);
            lowStall.getInventory().addItem(ItemType.WOOD, 60);

            Pawn highMerchant = makeMember(NationRole.MERCHANT, highford, 0);
            giveStall(highford.getMarket(), highMerchant, Stall.TILL_RESERVE + 400);
        }

        private Settlement makeSettlement(Vector3 at, string name) {
            Settlement settlement = new Settlement(at, nation.getId(), nation.getColor(), name, random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return settlement;
        }

        private Pawn makeMember(NationRole role, Settlement home, int coins) {
            Pawn pawn = new Pawn(home.getPosition(), "Pawn " + nation.getNumberOfMembers(), random);
            pawn.setEnergy(100);
            pawn.getInventory().clear();
            if (coins > 0) {
                pawn.getInventory().addItem(ItemType.COIN, coins);
            }
            entityRepository.addEntity(pawn);
            nation.addMember(pawn.getId());
            nation.setRole(pawn.getId(), role);
            pawn.setNationId(nation.getId());
            pawn.setHomeSettlementId(home.getId());
            pawn.setCurrentSettlementId(home.getId());
            return pawn;
        }

        private static Stall giveStall(Market market, Entity owner, int coins) {
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(owner.getId());
            stall.getInventory().addItem(ItemType.COIN, coins);
            return stall;
        }

        private List<Settlement> marketsFrom(Settlement here) {
            return Traders.getMarkets(entityRepository, nationRepository, here, nation.getId());
        }

        // --- planning a run ---

        [Fact]
        public void testPlanRun_BuysWhereItIsCheapToSellWhereItIsDear() {
            Pawn trader = makeMember(NationRole.TRADER, lowford, 200);

            Traders.TradeRun run = Traders.planRun(trader, lowford, marketsFrom(lowford));

            Assert.NotNull(run);
            Assert.Equal(ItemType.WOOD, run.good);
            Assert.Same(highford, run.destination);
            Assert.InRange(run.quantity, 1, Traders.MAX_LOAD);
            Assert.True(run.getProfit() >= Traders.MIN_PROFIT);
            Assert.Equal(lowford.getMarket().getBuyCost(ItemType.WOOD, run.quantity), run.cost);
        }

        [Fact]
        public void testPlanRun_FromTheDearMarket_FindsNothingWorthCarrying() {
            Pawn trader = makeMember(NationRole.TRADER, highford, 200);
            // Highford's stalls hold no goods at all
            Assert.Null(Traders.planRun(trader, highford, marketsFrom(highford)));
        }

        [Fact]
        public void testPlanRun_NeverCostsMoreThanThePurse() {
            Pawn trader = makeMember(NationRole.TRADER, lowford, 30);
            Traders.TradeRun run = Traders.planRun(trader, lowford, marketsFrom(lowford));
            Assert.NotNull(run);
            Assert.True(run.cost <= 30);
        }

        [Fact]
        public void testMarketsOutOfReach_AreNotConsidered() {
            highford.setPosition(new Vector3(Traders.MAX_ROUTE_DISTANCE + 100, 1, 0));
            Pawn trader = makeMember(NationRole.TRADER, lowford, 200);
            Assert.Empty(marketsFrom(lowford));
            Assert.Null(Traders.planRun(trader, lowford, marketsFrom(lowford)));
        }

        [Fact]
        public void testAHostileNationsMarket_IsNotConsidered() {
            Nation rivals = new Nation("Rivalland", new EntityId(), random);
            nationRepository.addNation(rivals);
            nationRepository.getRelations().adjust(nation, rivals, -100);
            Settlement rivalTown = new Settlement(new Vector3(0, 1, 300), rivals.getId(), rivals.getColor(), "Spite", random);
            entityRepository.addEntity(rivalTown);
            rivals.addSettlement(rivalTown.getId());

            Assert.DoesNotContain(rivalTown, marketsFrom(lowford));
        }

        // --- the whole loop: buy, walk, sell ---

        [Fact]
        public void testATrader_BuysLow_WalksOver_AndSellsHigh() {
            Pawn trader = makeMember(NationRole.TRADER, lowford, 200);
            int coinsBefore = trader.getInventory().getNumItems(ItemType.COIN);

            Assert.Equal(BehaviorType.BUY_TRADE_GOODS, calculator.computeBehaviorType(trader));
            executor.executeBehavior(trader, BehaviorType.BUY_TRADE_GOODS);
            int wood = trader.getInventory().getNumItems(ItemType.WOOD);
            Assert.True(wood > 0);

            // with the load, it leaves for the dearer market
            Assert.Equal(BehaviorType.EXIT_SETTLEMENT, calculator.computeBehaviorType(trader));
            executor.executeBehavior(trader, BehaviorType.EXIT_SETTLEMENT);
            Assert.False(trader.isCurrentlyInSettlement());
            Assert.Equal(BehaviorType.TRAVEL_TO_MARKET, calculator.computeBehaviorType(trader));

            // walk until it is let into Highford
            for (int step = 0; step < 5000 && !trader.isCurrentlyInSettlement(); step++) {
                executor.executeBehavior(trader, BehaviorType.TRAVEL_TO_MARKET);
                trader.setPosition(trader.getPosition() + trader.getVelocity() * 0.1f);
            }
            Assert.Equal(highford.getId(), trader.getCurrentSettlementId());

            // and sells the whole load there at once, for more than it paid
            Assert.Equal(BehaviorType.SELL_RESOURCES, calculator.computeBehaviorType(trader));
            executor.executeBehavior(trader, BehaviorType.SELL_RESOURCES);
            Assert.Equal(0, trader.getInventory().getNumItems(ItemType.WOOD));
            Assert.True(trader.getInventory().getNumItems(ItemType.COIN) >= coinsBefore + Traders.MIN_PROFIT);
            Assert.Equal(wood, highford.getMarket().getQuantityAvailable(ItemType.WOOD));
        }

        [Fact]
        public void testATrader_LeavesEveryTillItsFloat() {
            Pawn trader = makeMember(NationRole.TRADER, highford, 0);
            trader.getInventory().addItem(ItemType.WOOD, 99);

            executor.executeBehavior(trader, BehaviorType.SELL_RESOURCES);

            foreach (Stall stall in highford.getMarket().getStalls()) {
                Assert.True(stall.getInventory().getNumItems(ItemType.COIN) >= Stall.TILL_RESERVE);
            }
            Assert.True(trader.getInventory().getNumItems(ItemType.WOOD) > 0);
        }

        [Fact]
        public void testCoinsAreOnlyMoved_NeverMade() {
            Pawn trader = makeMember(NationRole.TRADER, lowford, 200);
            int before = countCoins();
            executor.executeBehavior(trader, BehaviorType.BUY_TRADE_GOODS);
            trader.setCurrentSettlementId(highford.getId());
            executor.executeBehavior(trader, BehaviorType.SELL_RESOURCES);
            Assert.Equal(before, countCoins());
        }

        private int countCoins() {
            int total = 0;
            foreach (Entity entity in entityRepository.getEntities()) {
                total += entity.getInventory().getNumItems(ItemType.COIN);
                Settlement settlement = entity as Settlement;
                if (settlement != null) {
                    foreach (Stall stall in settlement.getMarket().getStalls()) {
                        total += stall.getInventory().getNumItems(ItemType.COIN);
                    }
                }
            }
            return total;
        }

        [Fact]
        public void testASellingTraderAtAnotherNationsMarket_WarmsTheNations() {
            Nation hosts = new Nation("Hostland", new EntityId(), random);
            nationRepository.addNation(hosts);
            highford.changeNation(hosts);
            int before = nationRepository.getRelations().getScore(nation, hosts);
            Pawn trader = makeMember(NationRole.TRADER, lowford, 0);
            trader.getInventory().addItem(ItemType.WOOD, 5);
            trader.setCurrentSettlementId(highford.getId());

            executor.executeBehavior(trader, BehaviorType.SELL_RESOURCES);

            Assert.True(nationRepository.getRelations().getScore(nation, hosts) > before);
        }

        // --- becoming a trader ---

        [Fact]
        public void testASerfWithAStake_TakesUpTradeAtHome() {
            Pawn serf = makeMember(NationRole.SERF, lowford, Traders.TRADER_STAKE);

            Assert.Equal(BehaviorType.BUY_TRADE_GOODS, calculator.computeBehaviorType(serf));
            executor.executeBehavior(serf, BehaviorType.BUY_TRADE_GOODS);

            Assert.Equal(NationRole.TRADER, nation.getRole(serf.getId()));
            Assert.True(serf.getInventory().getNumItems(ItemType.WOOD) > 0);
        }

        [Fact]
        public void testANationKeepsOneTraderPerSettlement() {
            makeMember(NationRole.TRADER, lowford, 0);
            makeMember(NationRole.TRADER, highford, 0);
            Assert.False(Traders.hasRoomForTrader(nation));

            Pawn serf = makeMember(NationRole.SERF, lowford, Traders.TRADER_STAKE);
            Assert.NotEqual(BehaviorType.BUY_TRADE_GOODS, calculator.computeBehaviorType(serf));
            executor.executeBehavior(serf, BehaviorType.BUY_TRADE_GOODS);
            Assert.Equal(NationRole.SERF, nation.getRole(serf.getId()));
        }

        [Fact]
        public void testASerfWithoutAStake_StaysASerf() {
            Pawn serf = makeMember(NationRole.SERF, lowford, Traders.TRADER_STAKE - 1);
            Assert.NotEqual(BehaviorType.BUY_TRADE_GOODS, calculator.computeBehaviorType(serf));
        }

        [Fact]
        public void testTheLeaderAppointsATrader_HandingOverTheStake() {
            Player player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().clear();
            entityRepository.addEntity(player);
            Nation playerNation = new Nation("Playerland", player.getId(), random);
            nationRepository.addNation(playerNation);
            player.setNationId(playerNation.getId());
            playerNation.addSettlement(lowford.getId());
            Pawn serf = new Pawn(player.getPosition(), "Ada", random);
            entityRepository.addEntity(serf);
            playerNation.addMember(serf.getId());
            serf.setNationId(playerNation.getId());
            serf.setHomeSettlementId(lowford.getId());
            AppointTraderCommand command = new AppointTraderCommand(nationRepository, new EventProducer(new EventRepository()));

            Assert.True(LeaderPawnCommands.canAppointTrader(player, serf, nationRepository));
            Assert.False(command.execute(player, serf));
            Assert.Equal(NationRole.SERF, playerNation.getRole(serf.getId()));

            player.getInventory().addItem(ItemType.COIN, Traders.TRADER_STAKE + 5);
            int pawnCoins = serf.getInventory().getNumItems(ItemType.COIN);
            Assert.True(command.execute(player, serf));
            Assert.Equal(NationRole.TRADER, playerNation.getRole(serf.getId()));
            Assert.Equal(5, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(pawnCoins + Traders.TRADER_STAKE, serf.getInventory().getNumItems(ItemType.COIN));
            Assert.False(LeaderPawnCommands.canAppointTrader(player, serf, nationRepository));
        }
    }
}
