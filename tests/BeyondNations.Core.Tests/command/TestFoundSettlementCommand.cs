using System.Numerics;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestFoundSettlementCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private EventRepository eventRepository;
        private GameConfig gameConfig;
        private FoundSettlementCommand command;
        private Player player;

        public TestFoundSettlementCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            eventRepository = new EventRepository();
            EventProducer eventProducer = new EventProducer(eventRepository);
            gameConfig = new GameConfig();
            command = new FoundSettlementCommand(nationRepository, eventProducer, entityRepository, gameConfig, random);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
        }

        private Nation makePlayerLeader() {
            Nation nation = new Nation("Testland", player.getId(), random);
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            return nation;
        }

        private Settlement addSettlementAt(Vector3 position) {
            Nation otherNation = new Nation("Otherland", new EntityId(), random);
            nationRepository.addNation(otherNation);
            Settlement settlement = new Settlement(position, otherNation.getId(), otherNation.getColor(), otherNation.getName(), random);
            entityRepository.addEntity(settlement);
            return settlement;
        }

        [Fact]
        public void testExecute_NotInNation_NoSettlement() {
            // prepare
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, entityRepository.getNumEntitiesOfType(EntityType.SETTLEMENT));
            Assert.Equal(Settlement.WOOD_COST_TO_BUILD, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("You are not in a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_MemberButNotLeader_NoSettlement() {
            // prepare
            Nation nation = new Nation("Testland", new EntityId(), random);
            nation.addMember(player.getId());
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, entityRepository.getNumEntitiesOfType(EntityType.SETTLEMENT));
            Assert.Equal(0, nation.getNumberOfSettlements());
            Assert.Equal(Settlement.WOOD_COST_TO_BUILD, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("You are not the leader of a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_LeaderShortOfWood_ReportsWoodStillNeeded() {
            // prepare
            Nation nation = makePlayerLeader();
            player.getInventory().addItem(ItemType.WOOD, 40);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, entityRepository.getNumEntitiesOfType(EntityType.SETTLEMENT));
            Assert.Equal(0, nation.getNumberOfSettlements());
            Assert.Equal(40, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("Not enough wood. Need " + (Settlement.WOOD_COST_TO_BUILD - 40) + " more.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_TooCloseToAnotherSettlement_KeepsWood() {
            // prepare
            Nation nation = makePlayerLeader();
            player.setPosition(Vector3.Zero);
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            addSettlementAt(new Vector3(gameConfig.getMinDistanceBetweenSettlements() - 1, 0, 0));

            // run
            command.execute(player);

            // check
            Assert.Equal(1, entityRepository.getNumEntitiesOfType(EntityType.SETTLEMENT));
            Assert.Equal(0, nation.getNumberOfSettlements());
            Assert.Equal(Settlement.WOOD_COST_TO_BUILD, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Null(player.getHomeSettlementId());
            Assert.Equal("Too close to another settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_ExactlyMinDistanceAway_FoundsSettlement() {
            // prepare
            Nation nation = makePlayerLeader();
            player.setPosition(Vector3.Zero);
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            addSettlementAt(new Vector3(gameConfig.getMinDistanceBetweenSettlements(), 0, 0));

            // run
            command.execute(player);

            // check
            Assert.Equal(2, entityRepository.getNumEntitiesOfType(EntityType.SETTLEMENT));
            Assert.Equal(1, nation.getNumberOfSettlements());
            // founding this near another nation's settlement is resented
            Assert.StartsWith("Settlement founded. ", player.getStatus().getStatus());
            Assert.EndsWith(" resent a new settlement so near their land.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_LeaderWithEnoughWood_FoundsSettlementAndMakesItHome() {
            // prepare
            Nation nation = makePlayerLeader();
            player.setPosition(new Vector3(30, 0, -20));
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD + 15);

            // run
            command.execute(player);

            // check
            Assert.Equal(1, nation.getNumberOfSettlements());
            EntityId settlementId = nation.getSettlement(0);
            Settlement settlement = (Settlement) entityRepository.getEntity(settlementId);
            Assert.NotNull(settlement);
            Assert.Equal(nation.getId(), settlement.getNationId());
            Assert.Equal(nation.getColor(), settlement.getColor());
            Assert.Equal(new Vector3(30, 0, -20), settlement.getPosition());
            Assert.Equal(settlementId, player.getHomeSettlementId());
            Assert.Equal(15, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(0, eventRepository.getTotalNumberOfEvents());
            Assert.Equal("Settlement founded.", player.getStatus().getStatus());
        }
    }
}
