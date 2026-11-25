using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Upgrading buildings with coins (BuildingUpgrades, UpgradeBuildingCommand):
    * the coin sink once a settlement's twelve slots are full.
    */
    public class TestUpgradeBuildingCommand {
        private readonly RandomSource random = new RandomSource(20261124);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private UpgradeBuildingCommand command;
        private Player player;
        private Nation nation;
        private Settlement settlement;

        public TestUpgradeBuildingCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            command = new UpgradeBuildingCommand(nationRepository, entityRepository);
            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            entityRepository.addEntity(player);
            player.getInventory().clear();

            nation = new Nation("Testland", player.getId(), random);
            nationRepository.addNation(nation);
            settlement = new Settlement(Vector3.Zero, nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            settlement.getInventory().clear();
            settlement.setBuildingEra(Era.TOWN);
            player.setNationId(nation.getId());
            player.setCurrentSettlementId(settlement.getId());
        }

        [Fact]
        public void testPrices_AreInTheThousands_AndLevel2CostsDouble() {
            Assert.Equal(1500, BuildingUpgrades.getPrice(BuildingType.HOUSE, 1));
            Assert.Equal(3000, BuildingUpgrades.getPrice(BuildingType.HOUSE, 2));
            Assert.Equal(1200, BuildingUpgrades.getPrice(BuildingType.FARM, 1));
            Assert.Equal(4000, BuildingUpgrades.getPrice(BuildingType.STOREHOUSE, 2));
            Assert.False(BuildingUpgrades.isUpgradable(BuildingType.WORKSHOP));
        }

        [Fact]
        public void testUpgrade_WithAShortPurse_ChangesNothing() {
            settlement.addBuilding(BuildingType.HOUSE);
            Inventory purse = new Inventory(0);
            purse.addItem(ItemType.COIN, 500);

            Assert.Null(BuildingUpgrades.upgrade(settlement, purse, BuildingType.HOUSE));

            Assert.Equal(500, purse.getNumItems(ItemType.COIN));
            Assert.Equal(0, settlement.getBuildings()[0].getLevel());
        }

        [Fact]
        public void testHouseUpgrade_MakesRoomForMoreResidents_AndTakesTheCoins() {
            settlement.addBuilding(BuildingType.HOUSE);
            Assert.Equal(BuildingCatalog.BASE_RESIDENTS + 4, settlement.getResidentCap());
            player.getInventory().addItem(ItemType.COIN, 2000);

            Assert.True(command.execute(player, BuildingType.HOUSE));

            Assert.Equal(1, settlement.getBuildings()[0].getLevel());
            Assert.Equal(BuildingCatalog.BASE_RESIDENTS + 6, settlement.getResidentCap());
            Assert.Equal(500, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Contains("House upgraded to level 1", player.getStatus().getLatest());
        }

        [Fact]
        public void testFarmUpgrade_StocksMoreApplesEachHarvest() {
            settlement.addBuilding(BuildingType.FARM);
            Assert.True(settlement.getMarket().createStall());
            settlement.getMarket().getStalls()[0].setOwnerId(player.getId());
            Assert.Equal(1, settlement.harvestFarms());

            player.getInventory().addItem(ItemType.COIN, 1200);
            Assert.True(command.execute(player, BuildingType.FARM));

            Assert.Equal(2, settlement.harvestFarms());
        }

        [Fact]
        public void testStorehouseUpgrade_AllowsAnotherStall() {
            settlement.addBuilding(BuildingType.STOREHOUSE);
            int before = settlement.getMarket().getMaxNumStalls();
            player.getInventory().addItem(ItemType.COIN, 2000);

            Assert.True(command.execute(player, BuildingType.STOREHOUSE));

            Assert.Equal(before + 1, settlement.getMarket().getMaxNumStalls());
        }

        [Fact]
        public void testTheLowestLevelGoesFirst_SoEveryHouseReachesLevel1BeforeAnyReachesLevel2() {
            settlement.setBuildingEra(Era.KINGDOM);
            settlement.addBuilding(BuildingType.HOUSE);
            settlement.addBuilding(BuildingType.HOUSE);
            player.getInventory().addItem(ItemType.COIN, 1500 + 1500 + 3000);

            Assert.True(command.execute(player, BuildingType.HOUSE));
            Assert.True(command.execute(player, BuildingType.HOUSE));
            Assert.Equal(1, settlement.getBuildings()[0].getLevel());
            Assert.Equal(1, settlement.getBuildings()[1].getLevel());
            Assert.True(command.execute(player, BuildingType.HOUSE));
            Assert.Equal(2, settlement.getBuildings()[0].getLevel());
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testLevel2_WaitsForTheKingdomEra() {
            settlement.addBuilding(BuildingType.HOUSE);
            player.getInventory().addItem(ItemType.COIN, 10000);
            Assert.True(command.execute(player, BuildingType.HOUSE));

            Assert.False(command.execute(player, BuildingType.HOUSE));
            Assert.Equal("Level 2 comes with the Kingdom era.",
                BuildingUpgrades.describeWhyNot(settlement, player.getInventory(), BuildingType.HOUSE));
            Assert.Equal(1, settlement.getBuildings()[0].getLevel());

            settlement.setBuildingEra(Era.KINGDOM);
            Assert.True(command.execute(player, BuildingType.HOUSE));
            Assert.Equal(2, settlement.getBuildings()[0].getLevel());
            Assert.Equal("Every house here is fully upgraded.",
                BuildingUpgrades.describeWhyNot(settlement, player.getInventory(), BuildingType.HOUSE));
        }

        [Fact]
        public void testBeforeTheTownEra_NothingUpgrades() {
            settlement.setBuildingEra(Era.VILLAGE);
            settlement.addBuilding(BuildingType.HOUSE);
            player.getInventory().addItem(ItemType.COIN, 10000);

            Assert.False(BuildingUpgrades.areUpgradesUnlocked(settlement));
            Assert.False(command.execute(player, BuildingType.HOUSE));
            Assert.Equal(0, settlement.getBuildings()[0].getLevel());
            Assert.Equal(10000, player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testShortOfCoins_SaysWhatItNeeds_AndChangesNothing() {
            settlement.addBuilding(BuildingType.FARM);
            player.getInventory().addItem(ItemType.COIN, 700);

            Assert.False(command.execute(player, BuildingType.FARM));
            Assert.Equal("Needs 1,200 coins; you have 700.",
                BuildingUpgrades.describeWhyNot(settlement, player.getInventory(), BuildingType.FARM));
            Assert.Equal(700, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(0, settlement.getBuildings()[0].getLevel());
        }

        [Fact]
        public void testNoBuildingOfThatType_NothingToUpgrade() {
            player.getInventory().addItem(ItemType.COIN, 5000);
            Assert.False(command.execute(player, BuildingType.STOREHOUSE));
            Assert.Equal("No storehouse here to upgrade.",
                BuildingUpgrades.describeWhyNot(settlement, player.getInventory(), BuildingType.STOREHOUSE));
        }

        [Fact]
        public void testOutsideTheSettlement_UpgradesNothing() {
            settlement.addBuilding(BuildingType.HOUSE);
            player.getInventory().addItem(ItemType.COIN, 5000);
            player.setCurrentSettlementId(null);

            Assert.False(command.execute(player, BuildingType.HOUSE));
            Assert.Equal(0, settlement.getBuildings()[0].getLevel());
        }

        [Fact]
        public void testAnUpgradedBuilding_ChangesTheLayoutSignature_SoItRedraws() {
            settlement.addBuilding(BuildingType.HOUSE);
            long before = SettlementLayout.computeSignature(settlement);
            player.getInventory().addItem(ItemType.COIN, 1500);
            Assert.True(command.execute(player, BuildingType.HOUSE));
            Assert.NotEqual(before, SettlementLayout.computeSignature(settlement));
        }
    }
}
