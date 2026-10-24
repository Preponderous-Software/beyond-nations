using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Pawns keep a day (home at nightfall), go home by the straight road, hand
    * unsellable wood and stone in to their town, and found new settlements
    * for their nation when every one is full.
    */
    public class TestPawnDayAndColonies {
        private readonly RandomSource random = new RandomSource(20261018);

        private readonly GameConfig gameConfig = new GameConfig();
        private readonly EntityRepository entityRepository;
        private readonly Environment environment;
        private readonly NationRepository nationRepository;
        private readonly TickCounter tickCounter = new TickCounter();
        private readonly PawnBehaviorCalculator calculator;
        private readonly PawnBehaviorExecutor executor;

        private Nation nation;
        private Pawn leader;

        public TestPawnDayAndColonies() {
            entityRepository = new EntityRepository(random);
            environment = new Environment(5, 5, entityRepository, random);
            nationRepository = new NationRepository(random);
            calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);
            executor = new PawnBehaviorExecutor(environment, nationRepository, new EventProducer(new EventRepository()), entityRepository, random, new NationNameGenerator(random), tickCounter);

            leader = new Pawn(new Vector3(500, 0, 500), "Leader", random);
            entityRepository.addEntity(leader);
            nation = new Nation("Homeland", leader.getId(), random);
            nationRepository.addNation(nation);
            leader.setNationId(nation.getId());
        }

        private Settlement addSettlement(Vector3 position) {
            Settlement settlement = new Settlement(position, nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return settlement;
        }

        private Pawn addSerf(Vector3 position, Settlement home) {
            Pawn pawn = new Pawn(position, "Serf", random);
            entityRepository.addEntity(pawn);
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());
            pawn.getInventory().setNumItems(ItemType.COIN, 0);
            if (home != null) {
                pawn.setHomeSettlementId(home.getId());
            }
            return pawn;
        }

        private void setTime(float fraction) {
            calculator.setTimeOfDay(new TimeOfDay(tickCounter, TimeOfDay.DefaultTicksPerDay, fraction));
        }

        // --- the day ----------------------------------------------------------

        [Theory]
        [InlineData(0.1f, true)]
        [InlineData(0.19f, true)]
        [InlineData(0.2f, false)]
        [InlineData(0.5f, false)]
        [InlineData(0.79f, false)]
        [InlineData(0.8f, true)]
        public void testIsNightAt(float fraction, bool night) {
            Assert.Equal(night, TimeOfDay.isNightAt(fraction));
        }

        [Fact]
        public void testAtNightfall_ResidentsHeadHome() {
            Settlement home = addSettlement(Vector3.Zero);
            Pawn serf = addSerf(new Vector3(100, 0, 0), home);

            setTime(0.5f);
            Assert.Equal(BehaviorType.GATHER_RESOURCES, calculator.computeBehaviorType(serf));
            setTime(0.9f);
            Assert.Equal(BehaviorType.GO_TO_HOME_SETTLEMENT, calculator.computeBehaviorType(serf));
        }

        [Fact]
        public void testAtNight_ResidentsStayIn() {
            Settlement home = addSettlement(Vector3.Zero);
            Pawn serf = addSerf(Vector3.Zero, home);
            SettlementLife.admit(serf, home);

            setTime(0.9f);
            for (int i = 0; i < 500; i++) {
                Assert.NotEqual(BehaviorType.EXIT_SETTLEMENT, calculator.computeBehaviorType(serf));
            }
            setTime(0.5f);
            bool leftByDay = false;
            for (int i = 0; i < 500 && !leftByDay; i++) {
                leftByDay = calculator.computeBehaviorType(serf) == BehaviorType.EXIT_SETTLEMENT;
            }
            Assert.True(leftByDay);
        }

        [Fact]
        public void testAtNight_AHungryPawnStillGoesOutForFood() {
            Settlement home = addSettlement(Vector3.Zero);
            Pawn serf = addSerf(Vector3.Zero, home);
            SettlementLife.admit(serf, home);
            serf.setEnergy(10);

            setTime(0.9f);
            Assert.Equal(BehaviorType.EXIT_SETTLEMENT, calculator.computeBehaviorType(serf));
        }

        [Fact]
        public void testIdleExitChance_ByHomeAndTemperament() {
            Pawn pawn = new Pawn(Vector3.Zero, "test", random);
            Assert.Equal(10, PawnTraits.idleExitChancePercent(pawn, false));
            Assert.Equal(2, PawnTraits.idleExitChancePercent(pawn, true));
            pawn.setTraits(new[] { PawnTrait.INDUSTRIOUS });
            Assert.Equal(4, PawnTraits.idleExitChancePercent(pawn, true));
            pawn.setTraits(new[] { PawnTrait.GREGARIOUS });
            Assert.Equal(1, PawnTraits.idleExitChancePercent(pawn, true));
        }

        [Fact]
        public void testGoingHome_DropsWhateverItWasWalkingTo() {
            Settlement home = addSettlement(Vector3.Zero);
            Pawn serf = addSerf(new Vector3(100, 0, 0), home);
            AppleTree tree = new AppleTree(new Vector3(101, 0, 0), 5, random);
            entityRepository.addEntity(tree);
            serf.setTargetEntity(tree);

            executor.executeBehavior(serf, BehaviorType.GO_TO_HOME_SETTLEMENT);

            // it used to keep the tree, stand next to it and be let into town from there
            Assert.Same(home, serf.getTargetEntity());
            Assert.False(serf.isCurrentlyInSettlement());
            Assert.True(serf.getVelocity().X < 0);
        }

        // --- the stockpile ---------------------------------------------------

        [Fact]
        public void testSerfWithUnsellableGoods_HandsThemInAtHome() {
            Settlement home = addSettlement(Vector3.Zero);
            Pawn serf = addSerf(Vector3.Zero, home);
            leader.setHomeSettlementId(home.getId());
            SettlementLife.admit(serf, home);
            serf.getInventory().addItem(ItemType.WOOD, 25);
            serf.getInventory().addItem(ItemType.STONE, 5);
            serf.getInventory().addItem(ItemType.APPLE, 3);

            Assert.Equal(BehaviorType.CONTRIBUTE_TO_STOCKPILE, calculator.computeBehaviorType(serf));
            executor.executeBehavior(serf, BehaviorType.CONTRIBUTE_TO_STOCKPILE);

            Assert.Equal(25, home.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(5, home.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal(0, serf.getInventory().getNumItems(ItemType.WOOD));
            // food is kept
            Assert.Equal(3, serf.getInventory().getNumItems(ItemType.APPLE));
        }

        [Fact]
        public void testLeader_BuildsFromTheStockpile_EvenWithStallsToBuild() {
            Settlement home = addSettlement(Vector3.Zero);
            leader.setHomeSettlementId(home.getId());
            leader.setPosition(Vector3.Zero);
            leader.getInventory().setNumItems(ItemType.COIN, 500);
            SettlementLife.admit(leader, home);
            home.getInventory().addItem(ItemType.WOOD, 25);
            home.getInventory().addItem(ItemType.STONE, 10);

            Assert.Equal(BehaviorType.CONSTRUCT_BUILDING, calculator.computeBehaviorType(leader));
        }

        // --- colonies ----------------------------------------------------------

        private Settlement fullSettlement() {
            Settlement settlement = addSettlement(Vector3.Zero);
            settlement.setResidentCount(settlement.getResidentCap());
            return settlement;
        }

        [Fact]
        public void testHomelessMember_FarAway_FoundsANewSettlementForItsNation() {
            fullSettlement();
            Pawn serf = addSerf(new Vector3(gameConfig.getMinDistanceBetweenSettlements() + 10, 0, 0), null);
            serf.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);

            Assert.Equal(BehaviorType.CONSTRUCT_SETTLEMENT, calculator.computeBehaviorType(serf));
            executor.executeBehavior(serf, BehaviorType.CONSTRUCT_SETTLEMENT);

            Assert.Equal(2, nation.getNumberOfSettlements());
            Assert.NotNull(serf.getHomeSettlementId());
        }

        [Fact]
        public void testHomelessMember_TooClose_WalksAwayToFindLand() {
            fullSettlement();
            Pawn serf = addSerf(new Vector3(50, 0, 0), null);
            serf.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);

            Assert.Equal(BehaviorType.SEEK_SETTLEMENT_SITE, calculator.computeBehaviorType(serf));
            executor.executeBehavior(serf, BehaviorType.SEEK_SETTLEMENT_SITE);
            Assert.True(serf.getVelocity().X > 0);
            serf.setCurrentBehaviorType(BehaviorType.SEEK_SETTLEMENT_SITE);
            Assert.Equal("(looking for land to settle)", serf.getCurrentBehaviorDescription());
        }

        // --- a founder's hunger (#395) -----------------------------------------

        /** A founding leader of a nation with no settlement yet, too close to a rival to build, in winter. */
        private void foundingLeaderNearARivalInWinter() {
            Pawn rivalLeader = new Pawn(Vector3.Zero, "Rival", random);
            entityRepository.addEntity(rivalLeader);
            Nation rival = new Nation("Rivalry", rivalLeader.getId(), random);
            nationRepository.addNation(rival);
            entityRepository.addEntity(new Settlement(Vector3.Zero, rival.getId(), rival.getColor(), rival.getName(), random));

            TimeOfDay timeOfDay = new TimeOfDay(tickCounter, TimeOfDay.DefaultTicksPerDay, 0.5f);
            SeasonCalendar calendar = new SeasonCalendar(timeOfDay);
            calendar.advanceClockTo(tickCounter, Season.WINTER);
            calculator.setSeasonCalendar(calendar);

            leader.setPosition(new Vector3(100, 0, 0));
            leader.getInventory().setNumItems(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            Assert.Equal(0, nation.getNumberOfSettlements());
            Assert.Null(leader.getHomeSettlementId());
        }

        /**
        * Playtest 0.6.1 s02 (seed 103, started in winter): two of the five
        * hunger deaths were leaders who died in the wilds before their nations
        * had a settlement, walking on to a site until they starved. A hungry
        * founder now forages first -- in winter, with the trees bare, the
        * nearest chicken however far it is.
        */
        [Fact]
        public void testHungryFoundingLeader_ForagesBeforeWalkingOnToASite() {
            foundingLeaderNearARivalInWinter();
            // further than SeasonalRules.STORES_HUNT_RANGE: the only food there is
            entityRepository.addEntity(new Chicken(new Vector3(100, 0, 300), random));
            leader.setEnergy(Pawn.HUNGRY_ENERGY_THRESHOLD - 30);
            Assert.True(leader.needsFood());

            Assert.Equal(BehaviorType.GATHER_RESOURCES, calculator.computeBehaviorType(leader));
            executor.executeBehavior(leader, BehaviorType.GATHER_RESOURCES);
            Assert.Equal(EntityType.CHICKEN, leader.getTargetEntity().getType());

            // well fed, it walks on to find land as before
            leader.setEnergy(100);
            Assert.Equal(BehaviorType.SEEK_SETTLEMENT_SITE, calculator.computeBehaviorType(leader));
        }

        [Fact]
        public void testHungryFoundingLeader_WithNothingToForage_StillWalksOnToASite() {
            foundingLeaderNearARivalInWinter();
            leader.setEnergy(Pawn.STARVING_ENERGY_THRESHOLD - 10);

            // no chicken anywhere and the trees bare: a settlement is its only shelter
            Assert.Equal(BehaviorType.SEEK_SETTLEMENT_SITE, calculator.computeBehaviorType(leader));
        }

        [Fact]
        public void testHungrySettler_ForagesBeforeWalkingOnToASite() {
            fullSettlement();
            Pawn serf = addSerf(new Vector3(50, 0, 0), null);
            serf.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            entityRepository.addEntity(new Chicken(new Vector3(50, 0, 40), random));
            serf.setEnergy(Pawn.HUNGRY_ENERGY_THRESHOLD - 30);

            Assert.Equal(BehaviorType.GATHER_RESOURCES, calculator.computeBehaviorType(serf));
        }

        [Fact]
        public void testHomelessMember_WithoutWood_KeepsGathering() {
            fullSettlement();
            Pawn serf = addSerf(new Vector3(50, 0, 0), null);
            Assert.Equal(BehaviorType.GATHER_RESOURCES, calculator.computeBehaviorType(serf));
        }

        [Fact]
        public void testColonist_BuildsAStallFromTheStockpile() {
            Settlement capital = addSettlement(new Vector3(1000, 0, 0));
            leader.setHomeSettlementId(capital.getId());
            Settlement colony = addSettlement(Vector3.Zero);
            Pawn serf = addSerf(Vector3.Zero, colony);
            SettlementLife.admit(serf, colony);
            colony.getInventory().addItem(ItemType.WOOD, Stall.WOOD_COST_TO_BUILD);
            colony.getInventory().addItem(ItemType.STONE, Stall.STONE_COST_TO_BUILD);

            Assert.Equal(BehaviorType.CONSTRUCT_STALL, calculator.computeBehaviorType(serf));
            executor.executeBehavior(serf, BehaviorType.CONSTRUCT_STALL);

            Assert.Equal(1, colony.getMarket().getNumStalls());
            Assert.Equal(0, colony.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(0, colony.getInventory().getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testSerfInTheLeadersTown_LeavesBuildingToTheLeader() {
            Settlement capital = addSettlement(Vector3.Zero);
            leader.setHomeSettlementId(capital.getId());
            Pawn serf = addSerf(Vector3.Zero, capital);
            SettlementLife.admit(serf, capital);
            capital.getInventory().addItem(ItemType.WOOD, 100);
            capital.getInventory().addItem(ItemType.STONE, 100);

            for (int i = 0; i < 100; i++) {
                BehaviorType behaviorType = calculator.computeBehaviorType(serf);
                Assert.NotEqual(BehaviorType.CONSTRUCT_STALL, behaviorType);
                Assert.NotEqual(BehaviorType.CONSTRUCT_BUILDING, behaviorType);
            }
        }

        [Fact]
        public void testStall_PaidFromCarriedFirstThenStockpile() {
            Inventory builder = new Inventory(10);
            Inventory stockpile = new Inventory(10);
            builder.addItem(ItemType.WOOD, 15);
            stockpile.addItem(ItemType.WOOD, 10);
            stockpile.addItem(ItemType.STONE, 5);
            Assert.False(Stall.canAffordToBuild(builder));
            Assert.True(Stall.canAffordToBuild(builder, stockpile));

            Stall.payBuildCost(builder, stockpile);

            Assert.Equal(0, builder.getNumItems(ItemType.WOOD));
            Assert.Equal(5, stockpile.getNumItems(ItemType.WOOD));
            Assert.Equal(0, stockpile.getNumItems(ItemType.STONE));
        }
    }
}
