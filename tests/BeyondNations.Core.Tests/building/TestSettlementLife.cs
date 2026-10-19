using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Pawns inside a settlement stay visible and mill about it (SettlementLife),
    * and leaders put buildings up (CONSTRUCT_BUILDING).
    */
    public class TestSettlementLife {
        private readonly RandomSource random = new RandomSource(20261018);

        private EntityRepository entityRepository;
        private NationRepository nationRepository;
        private Environment environment;
        private PawnBehaviorExecutor executor;
        private PawnBehaviorCalculator calculator;

        public TestSettlementLife() {
            entityRepository = new EntityRepository(random);
            nationRepository = new NationRepository(random);
            environment = new Environment(5, 5, entityRepository, random);
            executor = new PawnBehaviorExecutor(environment, nationRepository, new EventProducer(new EventRepository()), entityRepository, random, new NationNameGenerator(random), new TickCounter());
            calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, new GameConfig(), new TickCounter(), random);
        }

        private Pawn makePawn(Vector3 position) {
            Pawn pawn = new Pawn(position, "Tess", random);
            pawn.setEnergy(100);
            entityRepository.addEntity(pawn);
            return pawn;
        }

        private Settlement makeSettlement(Nation nation) {
            Settlement settlement = new Settlement(new Vector3(0, 1.5f, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return settlement;
        }

        private Nation makeNation(Pawn leader) {
            Nation nation = new Nation("Testland", leader.getId(), random);
            nationRepository.addNation(nation);
            leader.setNationId(nation.getId());
            return nation;
        }

        [Fact]
        public void testAdmit_KeepsPawnVisibleAndHidesItsNametag() {
            Pawn pawn = makePawn(new Vector3(5, 1.5f, 5));
            Settlement settlement = makeSettlement(makeNation(pawn));

            SettlementLife.admit(pawn, settlement);

            Assert.True(pawn.isVisible());
            Assert.True(pawn.isCurrentlyInSettlement());
            Assert.Contains(pawn.getId(), settlement.getCurrentlyPresentEntities());
            Assert.False(pawn.getAppearance().hasLabel());
        }

        [Fact]
        public void testGoHome_ArrivingPawnStaysVisible() {
            Pawn pawn = makePawn(new Vector3(5, 1.5f, 5));
            Nation nation = makeNation(pawn);
            Settlement settlement = makeSettlement(nation);
            pawn.setHomeSettlementId(settlement.getId());
            pawn.setTargetEntity(settlement);

            executor.executeBehavior(pawn, BehaviorType.GO_TO_HOME_SETTLEMENT);

            Assert.True(pawn.isCurrentlyInSettlement());
            Assert.True(pawn.isVisible());
        }

        [Fact]
        public void testMill_PawnWandersButStaysWithinTheSettlement() {
            Pawn pawn = makePawn(new Vector3(0, 1.5f, 0));
            Settlement settlement = makeSettlement(makeNation(pawn));
            SettlementLife.admit(pawn, settlement);
            SettlementLife life = new SettlementLife(new RandomSource(7));

            float furthest = 0f;
            Vector3 start = pawn.getPosition();
            bool moved = false;
            for (int tick = 0; tick < 5000; tick++) {
                life.mill(pawn, settlement, 0.02f, 1.5f);
                Vector3 offset = pawn.getPosition() - settlement.getPosition();
                offset.Y = 0;
                furthest = System.Math.Max(furthest, offset.Length());
                moved |= pawn.getPosition() != start;
                Assert.Equal(1.5f, pawn.getPosition().Y);
            }

            Assert.True(moved);
            Assert.True(furthest <= SettlementLayout.MillMaxRadius + 0.01f);
        }

        [Fact]
        public void testCountResidents_CountsPawnsAndPlayerByHome() {
            Pawn leader = makePawn(Vector3.Zero);
            Settlement settlement = makeSettlement(makeNation(leader));
            leader.setHomeSettlementId(settlement.getId());
            makePawn(Vector3.Zero).setHomeSettlementId(settlement.getId());
            makePawn(Vector3.Zero); // homeless
            Player player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.setHomeSettlementId(settlement.getId());
            entityRepository.addEntity(player);

            Assert.Equal(3, SettlementLife.countResidents(entityRepository, settlement.getId()));
        }

        [Fact]
        public void testJoinRandomSettlement_FullSettlement_PawnStaysHomeless() {
            Pawn leader = makePawn(Vector3.Zero);
            Nation nation = makeNation(leader);
            Settlement settlement = makeSettlement(nation);
            settlement.setResidentCount(settlement.getResidentCap());
            Pawn pawn = makePawn(Vector3.Zero);
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());

            Assert.NotEqual(BehaviorType.JOIN_RANDOM_SETTLEMENT, calculator.computeBehaviorType(pawn));
            executor.executeBehavior(pawn, BehaviorType.JOIN_RANDOM_SETTLEMENT);

            Assert.Null(pawn.getHomeSettlementId());
        }

        [Fact]
        public void testJoinRandomSettlement_WithRoom_CountsTheNewResident() {
            Pawn leader = makePawn(Vector3.Zero);
            Nation nation = makeNation(leader);
            Settlement settlement = makeSettlement(nation);
            Pawn pawn = makePawn(Vector3.Zero);
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());

            executor.executeBehavior(pawn, BehaviorType.JOIN_RANDOM_SETTLEMENT);

            Assert.Equal(settlement.getId(), pawn.getHomeSettlementId());
            Assert.Equal(1, settlement.getResidentCount());
        }

        [Fact]
        public void testLeader_FullMarketAndMaterials_ConstructsBuilding() {
            Pawn leader = makePawn(Vector3.Zero);
            Nation nation = makeNation(leader);
            Settlement settlement = makeSettlement(nation);
            while (settlement.getMarket().createStall()) {
            }
            leader.getInventory().addItem(ItemType.COIN, 1000);
            leader.getInventory().addItem(ItemType.WOOD, 30);
            leader.getInventory().addItem(ItemType.STONE, 30);
            SettlementLife.admit(leader, settlement);

            Assert.Equal(BehaviorType.CONSTRUCT_BUILDING, calculator.computeBehaviorType(leader));
            executor.executeBehavior(leader, BehaviorType.CONSTRUCT_BUILDING);

            Assert.Equal(1, settlement.getNumBuildings());
            Assert.True(leader.getInventory().getNumItems(ItemType.STONE) < 30);
        }
    }
}
