using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestBuildBuildingCommand {
        private readonly RandomSource random = new RandomSource(20261018);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private BuildBuildingCommand command;
        private Player player;
        private Nation nation;
        private Settlement settlement;

        public TestBuildBuildingCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            command = new BuildBuildingCommand(nationRepository, entityRepository);
            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            entityRepository.addEntity(player);

            nation = new Nation("Testland", player.getId(), random);
            nationRepository.addNation(nation);
            settlement = new Settlement(Vector3.Zero, nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
        }

        private void enter() {
            player.setNationId(nation.getId());
            player.setCurrentSettlementId(settlement.getId());
        }

        [Fact]
        public void testNotInNation_BuildsNothing() {
            Assert.False(command.execute(player, BuildingType.HOUSE));
            Assert.Equal("You are not in a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void testNotInsideSettlement_BuildsNothing() {
            player.setNationId(nation.getId());

            Assert.False(command.execute(player, BuildingType.HOUSE));
            Assert.Equal("You are not inside a settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testNotEnoughStone_SaysHowMuchIsMissing() {
            enter();
            player.getInventory().addItem(ItemType.WOOD, 100);

            Assert.False(command.execute(player, BuildingType.HOUSE));
            Assert.Equal(0, settlement.getNumBuildings());
            Assert.Contains("15 more stone", player.getStatus().getStatus());
        }

        [Fact]
        public void testSuccess_PaysAndBuilds() {
            enter();
            player.getInventory().addItem(ItemType.WOOD, 20);
            player.getInventory().addItem(ItemType.STONE, 15);

            Assert.True(command.execute(player, BuildingType.HOUSE));

            Assert.Equal(1, settlement.getNumBuildings(BuildingType.HOUSE));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal("House built.", player.getStatus().getStatus());
        }

        [Fact]
        public void testNullType_BuildsWhatThePlannerSuggests_UsingTheStockpile() {
            enter();
            settlement.getInventory().addItem(ItemType.WOOD, 100);
            settlement.getInventory().addItem(ItemType.STONE, 100);

            Assert.True(command.execute(player, null));

            Assert.Equal(1, settlement.getNumBuildings(BuildingType.FARM));
            Assert.Equal(100 - BuildingCatalog.getStoneCost(BuildingType.FARM), settlement.getInventory().getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testReachingVillage_AnnouncesTheNewTier() {
            enter();
            settlement.addBuilding(BuildingType.FARM);
            player.getInventory().addItem(ItemType.WOOD, 20);
            player.getInventory().addItem(ItemType.STONE, 15);

            command.execute(player, BuildingType.HOUSE);

            Assert.Equal(SettlementTier.VILLAGE, settlement.getTier());
            Assert.Contains("now a Village", player.getStatus().getStatus());
        }

        [Fact]
        public void testOtherNationsSettlement_BuildsNothing() {
            Nation other = new Nation("Otherland", new EntityId(), random);
            nationRepository.addNation(other);
            player.setNationId(other.getId());
            player.setCurrentSettlementId(settlement.getId());
            player.getInventory().addItem(ItemType.WOOD, 100);
            player.getInventory().addItem(ItemType.STONE, 100);

            Assert.False(command.execute(player, BuildingType.HOUSE));
            Assert.Equal(0, settlement.getNumBuildings());
        }
    }
}
