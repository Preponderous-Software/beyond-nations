using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Buying a building with coins (BuildingPurchase, BuyBuildingCommand):
    * the coin sink for a purse that piles up with nothing to buy.
    */
    public class TestBuyBuildingCommand {
        private readonly RandomSource random = new RandomSource(20261124);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private BuyBuildingCommand command;
        private Player player;
        private Nation nation;
        private Settlement settlement;

        public TestBuyBuildingCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            command = new BuyBuildingCommand(nationRepository, entityRepository);
            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            entityRepository.addEntity(player);
            player.getInventory().clear();

            nation = new Nation("Testland", player.getId(), random);
            nationRepository.addNation(nation);
            settlement = new Settlement(Vector3.Zero, nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            settlement.getInventory().clear();
        }

        private void enter() {
            player.setNationId(nation.getId());
            player.setCurrentSettlementId(settlement.getId());
        }

        [Fact]
        public void testPrice_AFirstHouseBoughtOutright_Costs1980() {
            // 20 wood at 72 (60 * 120%) and 15 stone at 36 (30 * 120%)
            Assert.Equal(72, BuildingPurchase.getCoinsPerUnit(settlement, ItemType.WOOD));
            Assert.Equal(36, BuildingPurchase.getCoinsPerUnit(settlement, ItemType.STONE));
            Assert.Equal(1980, BuildingPurchase.getCoinPrice(settlement, player.getInventory(), BuildingType.HOUSE));
            Assert.Equal(1980, BuildingPurchase.getFullCoinPrice(settlement, BuildingType.HOUSE));
        }

        [Fact]
        public void testPrice_OnlyWhatIsMissing_CountingTheStockpile() {
            player.getInventory().addItem(ItemType.WOOD, 8);
            settlement.getInventory().addItem(ItemType.STONE, 5);

            // 12 wood and 10 stone short
            Assert.Equal(12 * 72 + 10 * 36, BuildingPurchase.getCoinPrice(settlement, player.getInventory(), BuildingType.HOUSE));
        }

        [Fact]
        public void testPrice_RisesWithEveryBuildingStanding() {
            int first = BuildingPurchase.getFullCoinPrice(settlement, BuildingType.HOUSE);
            settlement.addBuilding(BuildingType.FARM);
            settlement.addBuilding(BuildingType.FARM);

            Assert.Equal(140, BuildingPurchase.getPercent(settlement));
            Assert.True(BuildingPurchase.getFullCoinPrice(settlement, BuildingType.HOUSE) > first);
        }

        [Fact]
        public void testGatheringStaysCheaper_AUnitBoughtCostsMoreThanItsMarketBasePrice() {
            foreach (ItemType itemType in new[] { ItemType.WOOD, ItemType.STONE }) {
                Assert.True(BuildingPurchase.getCoinsPerUnit(settlement, itemType) > ItemCostCalculator.getBaseCost(itemType));
            }
        }

        [Fact]
        public void testNotInNation_BuysNothing() {
            player.getInventory().addItem(ItemType.COIN, 5000);

            Assert.False(command.execute(player, BuildingType.HOUSE));
            Assert.Equal("You are not in a nation.", player.getStatus().getStatus());
            Assert.Equal(5000, player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testTooFewCoins_SaysHowMany() {
            enter();
            player.getInventory().addItem(ItemType.COIN, 100);

            Assert.False(command.execute(player, BuildingType.HOUSE));
            Assert.Equal(0, settlement.getNumBuildings());
            Assert.Equal(100, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("A House: needs 1,980 coins; you have 100.", player.getStatus().getStatus());
        }

        [Fact]
        public void testMaterialsCoverIt_SaysToBuildInstead() {
            enter();
            player.getInventory().addItem(ItemType.COIN, 5000);
            player.getInventory().addItem(ItemType.WOOD, 20);
            player.getInventory().addItem(ItemType.STONE, 15);

            Assert.False(command.execute(player, BuildingType.HOUSE));
            Assert.Contains("build it instead", player.getStatus().getStatus());
            Assert.Equal(5000, player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testSuccess_UsesTheMaterialsFirst_AndCoinsForTheRest() {
            enter();
            player.getInventory().addItem(ItemType.COIN, 2000);
            player.getInventory().addItem(ItemType.WOOD, 8);
            settlement.getInventory().addItem(ItemType.STONE, 5);
            int price = 12 * 72 + 10 * 36;

            Assert.True(command.execute(player, BuildingType.HOUSE));

            Assert.Equal(1, settlement.getNumBuildings(BuildingType.HOUSE));
            Assert.Equal(2000 - price, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(0, settlement.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal("House bought: you paid for the 12 wood and 10 stone you are short of (1,224 coins).", player.getStatus().getStatus());
        }

        [Fact]
        public void testSuccess_LeavesWoodAndStoneItDidNotNeed() {
            enter();
            player.getInventory().addItem(ItemType.COIN, 2000);
            player.getInventory().addItem(ItemType.WOOD, 50);

            Assert.True(command.execute(player, BuildingType.HOUSE));

            Assert.Equal(30, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(2000 - 15 * 36, player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testNoRoom_BuysNothing() {
            enter();
            player.getInventory().addItem(ItemType.COIN, 50000);
            for (int i = 0; i < BuildingCatalog.getLimit(BuildingType.WORKSHOP); i++) {
                settlement.addBuilding(BuildingType.WORKSHOP);
            }

            Assert.False(command.execute(player, BuildingType.WORKSHOP));
            Assert.Equal("No room for another Workshop here.", player.getStatus().getStatus());
            Assert.Equal(50000, player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testLockedByEra_BuysNothing() {
            enter();
            player.getInventory().addItem(ItemType.COIN, 50000);
            settlement.setBuildingEra(Era.CAMP);

            Assert.False(command.execute(player, BuildingType.STOREHOUSE));
            Assert.Equal(EraRules.describeLocked(BuildingType.STOREHOUSE), player.getStatus().getStatus());
        }

        [Fact]
        public void testBuyingEnoughBuildings_LiftsTheTier_AndSaysSo() {
            enter();
            player.getInventory().addItem(ItemType.COIN, 50000);
            SettlementTier before = settlement.getTier();

            Assert.True(command.execute(player, BuildingType.HOUSE));
            for (int i = 0; i < BuildingCatalog.getLimit(BuildingType.FARM) && settlement.getTier() == before; i++) {
                Assert.True(command.execute(player, BuildingType.FARM));
            }

            Assert.NotEqual(before, settlement.getTier());
            Assert.Contains("The settlement is now a", player.getStatus().getStatus());
        }
    }
}
