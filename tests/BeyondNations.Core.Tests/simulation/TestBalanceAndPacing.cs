using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The numbers that decide how the game paces: hunger, the cost of the
    * first settlement and stall, market prices, when pawns trade and how
    * often they think.
    */
    public class TestBalanceAndPacing {
        private readonly RandomSource random = new RandomSource(20261018);

        // --- hunger -------------------------------------------------------------

        private Player makePlayer() {
            return new Player(25, 50, new TickCounter(), 500, 200, random);
        }

        [Fact]
        public void testPlayerMetabolism_IsFixedAndAFullBarLastsAnInGameDay() {
            Player first = makePlayer();
            Player second = makePlayer();
            Assert.Equal(first.getMetabolism(), second.getMetabolism());

            Player player = makePlayer();
            int ticksPerDay = TimeOfDay.DefaultTicksPerDay;
            for (int i = 0; i < ticksPerDay - 100; i++) {
                player.fixedUpdate();
            }
            Assert.True(player.getEnergy() > 0, "a full bar should last almost a whole day");
            for (int i = 0; i < 200; i++) {
                player.fixedUpdate();
            }
            Assert.True(player.getEnergy() <= 0, "a full bar should run out after about a day");
        }

        [Fact]
        public void testSimulation_AppliesConfiguredPlayerMetabolism() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(7);
            Assert.Equal(100f / TimeOfDay.DefaultTicksPerDay, config.getPlayerMetabolism(), 6);
            config.setPlayerMetabolism(0.02f);

            Simulation simulation = new Simulation(config);

            Assert.Equal(0.02f, simulation.getPlayer().getMetabolism());
        }

        [Fact]
        public void testPawnMetabolism_FullBarLastsAtLeastTwentyThousandTicks() {
            for (int i = 0; i < 200; i++) {
                Pawn pawn = new Pawn(Vector3.Zero, "test", random);
                Assert.InRange(pawn.getMetabolism(), 0.002f, 0.005f);
                Assert.True(100f / pawn.getMetabolism() >= 20000f);
            }
        }

        // --- costs and yields -------------------------------------------------

        [Fact]
        public void testFirstSettlement_TakesAboutTenTrees() {
            Assert.Equal(50, Settlement.WOOD_COST_TO_BUILD);
            for (int i = 0; i < 100; i++) {
                AppleTree tree = new AppleTree(Vector3.Zero, 5, random);
                Assert.InRange(tree.getInventory().getNumItems(ItemType.WOOD), 4, 6);
                Assert.InRange(tree.getInventory().getNumItems(ItemType.APPLE), 1, 3);
            }
        }

        [Fact]
        public void testStall_CostsWoodAndStone() {
            Inventory inventory = new Inventory(10);
            inventory.addItem(ItemType.WOOD, Stall.WOOD_COST_TO_BUILD);
            inventory.addItem(ItemType.STONE, Stall.STONE_COST_TO_BUILD - 1);
            Assert.False(Stall.canAffordToBuild(inventory));

            inventory.addItem(ItemType.STONE, 1);
            Assert.True(Stall.canAffordToBuild(inventory));

            Stall.payBuildCost(inventory);
            Assert.Equal(0, inventory.getNumItems(ItemType.WOOD));
            Assert.Equal(0, inventory.getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testSapling_GrowsWithinFiveMinutes() {
            TickCounter tickCounter = new TickCounter();
            for (int i = 0; i < 100; i++) {
                Sapling sapling = new Sapling(Vector3.Zero, 3, random, tickCounter);
                Assert.InRange(sapling.getGrowTime(), 60, 299);
            }
        }

        // --- prices -----------------------------------------------------------

        [Theory]
        [InlineData(40, 0, 40)]
        [InlineData(40, 1, 36)]
        [InlineData(40, 10, 20)]
        [InlineData(40, 20, 13)]
        [InlineData(40, 40, 10)]   // was 1 coin: base / quantity
        [InlineData(40, 1000, 10)] // floor: a quarter of the base cost
        [InlineData(2, 1000, 1)]   // never below one coin
        public void testSupplyCurve_IsGentleWithAFloor(int baseCost, int quantity, int expected) {
            Assert.Equal(expected, ItemCostCalculator.calculateCost(baseCost, quantity));
        }

        [Fact]
        public void testSupplyCurve_NeverRisesWithSupply() {
            int previous = int.MaxValue;
            for (int quantity = 0; quantity < 200; quantity++) {
                int cost = ItemCostCalculator.calculateCost(60, quantity);
                Assert.True(cost <= previous);
                Assert.True(cost >= 15);
                previous = cost;
            }
        }

        // --- when pawns trade -------------------------------------------------

        [Fact]
        public void testAbundance_AnyMixOfGoodsOverTheThreshold() {
            Inventory woodOnly = new Inventory(10);
            woodOnly.addItem(ItemType.WOOD, Inventory.ABUNDANCE_THRESHOLD);
            Assert.True(woodOnly.containsAbundanceOfResources());

            Inventory mixed = new Inventory(10);
            mixed.addItem(ItemType.WOOD, 10);
            mixed.addItem(ItemType.APPLE, 5);
            mixed.addItem(ItemType.SAPLING, 4);
            Assert.False(mixed.containsAbundanceOfResources());
            mixed.addItem(ItemType.STONE, 1);
            Assert.True(mixed.containsAbundanceOfResources());

            Inventory coinsOnly = new Inventory(10);
            coinsOnly.addItem(ItemType.COIN, 500);
            Assert.False(coinsOnly.containsAbundanceOfResources());
        }

        private Pawn makePawnInOwnSettlement(NationRole role, EntityRepository entityRepository, NationRepository nationRepository, out Settlement settlement) {
            Pawn leader = new Pawn(Vector3.Zero, "leader", random);
            entityRepository.addEntity(leader);
            Nation nation = new Nation("test", leader.getId(), random);
            leader.setNationId(nation.getId());
            nationRepository.addNation(nation);
            settlement = new Settlement(Vector3.Zero, nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());

            Pawn pawn = leader;
            if (role != NationRole.LEADER) {
                pawn = new Pawn(Vector3.Zero, "member", random);
                entityRepository.addEntity(pawn);
                nation.addMember(pawn.getId());
                nation.setRole(pawn.getId(), role);
                pawn.setNationId(nation.getId());
            }
            pawn.setHomeSettlementId(settlement.getId());
            pawn.setCurrentSettlementId(settlement.getId());
            return pawn;
        }

        private int countIdleExits(bool atHome) {
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);
            Settlement settlement;
            Pawn serf = makePawnInOwnSettlement(NationRole.SERF, entityRepository, nationRepository, out settlement);
            serf.getInventory().setNumItems(ItemType.COIN, 0);
            if (!atHome) {
                serf.setHomeSettlementId(null);
            }
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, new GameConfig(), new TickCounter(), random);

            int exits = 0;
            for (int i = 0; i < 1000; i++) {
                if (calculator.computeBehaviorType(serf) == BehaviorType.EXIT_SETTLEMENT) {
                    exits++;
                }
            }
            return exits;
        }

        [Fact]
        public void testIdleVisitor_LeavesTownWithinSeconds() {
            // 10% per decision; the old 1% would give about 10
            Assert.InRange(countIdleExits(false), 60, 140);
        }

        [Fact]
        public void testIdleSerfAtHome_LingersLongerThanAVisitor() {
            // 2% per decision at home: a staged town no longer empties in seconds
            Assert.InRange(countIdleExits(true), 8, 35);
        }

        [Fact]
        public void testLeaderWithWoodButNoStone_LeavesToGatherStone() {
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);
            Settlement settlement;
            Pawn leader = makePawnInOwnSettlement(NationRole.LEADER, entityRepository, nationRepository, out settlement);
            leader.getInventory().setNumItems(ItemType.COIN, 500);
            leader.getInventory().addItem(ItemType.WOOD, Stall.WOOD_COST_TO_BUILD);
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, new GameConfig(), new TickCounter(), random);

            Assert.Equal(BehaviorType.EXIT_SETTLEMENT, calculator.computeBehaviorType(leader));
        }

        private BehaviorType computeOutside(NationRole role, int stalls, int stallCoins, int wood, int stone) {
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);
            Settlement settlement;
            Pawn pawn = makePawnInOwnSettlement(role, entityRepository, nationRepository, out settlement);
            pawn.setCurrentSettlementId(null);
            pawn.getInventory().setNumItems(ItemType.COIN, 0);
            pawn.getInventory().addItem(ItemType.WOOD, wood);
            pawn.getInventory().addItem(ItemType.STONE, stone);
            for (int i = 0; i < stalls; i++) {
                settlement.getMarket().createStall();
            }
            if (stalls > 0) {
                Stall stall = settlement.getMarket().getStallForSale();
                stall.setOwnerId(new EntityId());
                stall.getInventory().addItem(ItemType.COIN, stallCoins);
            }
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, new GameConfig(), new TickCounter(), random);
            return calculator.computeBehaviorType(pawn);
        }

        [Fact]
        public void testLeader_GoesHomeOnlyToBuildAStall() {
            // materials and room: go and build
            Assert.Equal(BehaviorType.GO_TO_HOME_SETTLEMENT, computeOutside(NationRole.LEADER, 0, 0, 30, 10));
            // market full but materials for a building (a farm): go and build that
            Assert.Equal(BehaviorType.GO_TO_HOME_SETTLEMENT, computeOutside(NationRole.LEADER, 4, 0, 30, 10));
            // market full and too little stone for any building: no reason to walk in and idle
            Assert.Equal(BehaviorType.GATHER_RESOURCES, computeOutside(NationRole.LEADER, 4, 0, 30, 5));
            // short of stone: keep gathering
            Assert.Equal(BehaviorType.GATHER_RESOURCES, computeOutside(NationRole.LEADER, 0, 0, 30, 0));
        }

        [Fact]
        public void testSerf_GoesHomeToSellOrToHandInWoodAndStone() {
            Assert.Equal(BehaviorType.GO_TO_HOME_SETTLEMENT, computeOutside(NationRole.SERF, 1, 100, 30, 0));
            // no coin at the market: it goes anyway, to hand its wood in to the stockpile
            Assert.Equal(BehaviorType.GO_TO_HOME_SETTLEMENT, computeOutside(NationRole.SERF, 1, 0, 30, 0));
            Assert.Equal(BehaviorType.GATHER_RESOURCES, computeOutside(NationRole.SERF, 1, 100, 5, 0));
        }

        [Fact]
        public void testMerchantWithOnlyWood_StocksStall() {
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);
            Settlement settlement;
            Pawn merchant = makePawnInOwnSettlement(NationRole.MERCHANT, entityRepository, nationRepository, out settlement);
            settlement.getMarket().createStall();
            settlement.getMarket().getStallForSale().setOwnerId(merchant.getId());
            merchant.getInventory().addItem(ItemType.WOOD, 5);
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, new GameConfig(), new TickCounter(), random);

            Assert.Equal(BehaviorType.TRANSFER_ITEMS_TO_STALL, calculator.computeBehaviorType(merchant));
        }

        [Fact]
        public void testPawnShortOfStone_TargetsRockOverCloserTree() {
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);
            PawnBehaviorExecutor executor = new PawnBehaviorExecutor(environment, nationRepository, new EventProducer(new EventRepository()), entityRepository, random, new NationNameGenerator(random), new TickCounter());
            Pawn pawn = new Pawn(Vector3.Zero, "test", random);
            pawn.getInventory().addItem(ItemType.WOOD, Stall.WOOD_COST_TO_BUILD);
            entityRepository.addEntity(pawn);
            AppleTree tree = new AppleTree(new Vector3(20, 0, 0), 3, random);
            entityRepository.addEntity(tree);
            Rock rock = new Rock(new Vector3(60, 0, 0));
            entityRepository.addEntity(rock);

            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);

            Assert.Same(rock, pawn.getTargetEntity());
        }

        // --- cadence ----------------------------------------------------------

        [Fact]
        public void testBehaviorCalculationCadence_CountsTotalTicks() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(7);
            Assert.Equal(50, config.getTicksBetweenBehaviorCalculations());
            config.setTicksBetweenBehaviorCalculations(200);
            Simulation simulation = new Simulation(config);
            Pawn pawn = new Pawn(new Vector3(0, 1.5f, 0), "test", simulation.getRandom());
            pawn.setCurrentBehaviorType(BehaviorType.WANDER);
            simulation.getEntityRepository().addEntity(pawn);

            // the calculator never chooses WANDER, so it stays until recalculated
            for (int i = 0; i < 199; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            // before the fix this was recalculated at tick 50 (tick-within-second == 0)
            Assert.Equal(BehaviorType.WANDER, pawn.getCurrentBehaviorType());

            simulation.fixedUpdate(1f / 50f);
            Assert.Equal(200, simulation.getTickCounter().getTotalTicks());
            Assert.NotEqual(BehaviorType.WANDER, pawn.getCurrentBehaviorType());
        }

        [Fact]
        public void testSettlementIncome_OneCoinPerStallEveryFiveHundredTicks() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(7);
            Simulation simulation = new Simulation(config);
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), new NationId(), Rgba.White, "test", simulation.getRandom());
            settlement.getMarket().createStall();
            settlement.getMarket().createStall();
            simulation.getEntityRepository().addEntity(settlement);

            for (int i = 0; i < 499; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            Assert.Equal(0, settlement.getFunds());
            simulation.fixedUpdate(1f / 50f);
            Assert.Equal(2, settlement.getFunds());
        }
    }
}
