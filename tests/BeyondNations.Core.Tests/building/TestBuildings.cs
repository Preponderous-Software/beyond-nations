using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Buildings, settlement tiers and resident caps (Settlement, BuildingCatalog,
    * BuildingPlanner, SettlementTiers).
    */
    public class TestBuildings {
        private readonly RandomSource random = new RandomSource(20261018);

        private Settlement makeSettlement() {
            return new Settlement(new Vector3(0, 1, 0), new NationId(), Rgba.Red, "Testland", random);
        }

        [Fact]
        public void testEveryBuildingCostsStone() {
            foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType))) {
                Assert.True(BuildingCatalog.getStoneCost(type) > 0, type + " should cost stone");
                Assert.True(BuildingCatalog.getWoodCost(type) > 0, type + " should cost wood");
            }
        }

        [Fact]
        public void testCanAfford_NeedsBothWoodAndStone() {
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.WOOD, 100);
            Assert.False(BuildingCatalog.canAfford(inventory, BuildingType.HOUSE));

            inventory.addItem(ItemType.STONE, BuildingCatalog.getStoneCost(BuildingType.HOUSE));
            Assert.True(BuildingCatalog.canAfford(inventory, BuildingType.HOUSE));
        }

        [Fact]
        public void testDeductCost_TakesFromBuilderFirstThenStockpile() {
            Inventory builder = new Inventory(0);
            Inventory stockpile = new Inventory(0);
            builder.addItem(ItemType.WOOD, 5);
            builder.addItem(ItemType.STONE, 100);
            stockpile.addItem(ItemType.WOOD, 100);

            Assert.False(BuildingCatalog.canAfford(builder, BuildingType.HOUSE));
            Assert.True(BuildingCatalog.canAfford(builder, stockpile, BuildingType.HOUSE));
            BuildingCatalog.deductCost(builder, stockpile, BuildingType.HOUSE);

            Assert.Equal(0, builder.getNumItems(ItemType.WOOD));
            Assert.Equal(100 - (BuildingCatalog.getWoodCost(BuildingType.HOUSE) - 5), stockpile.getNumItems(ItemType.WOOD));
            Assert.Equal(100 - BuildingCatalog.getStoneCost(BuildingType.HOUSE), builder.getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testHouse_RaisesResidentCap() {
            Settlement settlement = makeSettlement();
            int before = settlement.getResidentCap();

            Assert.True(settlement.addBuilding(BuildingType.HOUSE));

            Assert.Equal(before + BuildingCatalog.HOUSE_RESIDENTS, settlement.getResidentCap());
        }

        [Fact]
        public void testStorehouse_AddsMarketStalls() {
            Settlement settlement = makeSettlement();
            int before = settlement.getMarket().getMaxNumStalls();

            settlement.addBuilding(BuildingType.STOREHOUSE);

            Assert.Equal(before + BuildingCatalog.STOREHOUSE_STALLS, settlement.getMarket().getMaxNumStalls());
        }

        [Fact]
        public void testAddBuilding_RespectsPerTypeLimit() {
            Settlement settlement = makeSettlement();
            int limit = BuildingCatalog.getLimit(BuildingType.STOREHOUSE);
            for (int i = 0; i < limit; i++) {
                Assert.True(settlement.addBuilding(BuildingType.STOREHOUSE));
            }

            Assert.False(settlement.canAddBuilding(BuildingType.STOREHOUSE));
            Assert.False(settlement.addBuilding(BuildingType.STOREHOUSE));
            Assert.Equal(limit, settlement.getNumBuildings());
        }

        [Fact]
        public void testAddBuilding_RespectsTotalLimitAndAssignsSlotsInOrder() {
            Settlement settlement = makeSettlement();
            foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType))) {
                while (settlement.addBuilding(type)) {
                }
            }

            Assert.True(settlement.getNumBuildings() <= BuildingCatalog.MAX_BUILDINGS);
            for (int i = 0; i < settlement.getNumBuildings(); i++) {
                Assert.Equal(i, settlement.getBuildings()[i].getSlot());
            }
        }

        [Theory]
        [InlineData(0, SettlementTier.CAMP)]
        [InlineData(1, SettlementTier.CAMP)]
        [InlineData(2, SettlementTier.VILLAGE)]
        [InlineData(4, SettlementTier.VILLAGE)]
        [InlineData(5, SettlementTier.TOWN)]
        [InlineData(12, SettlementTier.TOWN)]
        public void testTierFromBuildingCount(int buildings, SettlementTier expected) {
            Assert.Equal(expected, SettlementTiers.fromBuildingCount(buildings));
        }

        [Fact]
        public void testNameTag_ShowsTierAndResidents() {
            Settlement settlement = makeSettlement();
            settlement.addBuilding(BuildingType.HOUSE);
            settlement.addBuilding(BuildingType.FARM);
            settlement.setResidentCount(3);

            Assert.Equal("Village of Testland\n0 here, 3/12 residents", settlement.getNameTagText());
        }

        [Fact]
        public void testHasRoomForResident_FollowsCap() {
            Settlement settlement = makeSettlement();
            settlement.setResidentCount(BuildingCatalog.BASE_RESIDENTS);
            Assert.False(settlement.hasRoomForResident());

            settlement.addBuilding(BuildingType.HOUSE);
            Assert.True(settlement.hasRoomForResident());
        }

        [Fact]
        public void testHarvestFarms_StocksEmptiestOwnedStall() {
            Settlement settlement = makeSettlement();
            settlement.addBuilding(BuildingType.FARM);
            settlement.addBuilding(BuildingType.FARM);
            Market market = settlement.getMarket();
            market.createStall();
            market.createStall();
            market.createStall();
            market.getStalls()[0].setOwnerId(new EntityId());
            market.getStalls()[0].getInventory().addItem(ItemType.APPLE, 5);
            market.getStalls()[1].setOwnerId(new EntityId());
            // stall 2 is for sale and gets nothing

            int stocked = settlement.harvestFarms();

            Assert.Equal(2, stocked);
            Assert.Equal(5, market.getStalls()[0].getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(2, market.getStalls()[1].getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, market.getStalls()[2].getInventory().getNumItems(ItemType.APPLE));
        }

        [Fact]
        public void testHarvestFarms_NoMerchant_NothingProduced() {
            Settlement settlement = makeSettlement();
            settlement.addBuilding(BuildingType.FARM);
            settlement.getMarket().createStall();

            Assert.Equal(0, settlement.harvestFarms());
        }

        [Fact]
        public void testPlanner_NearlyFull_SuggestsHouseFirst() {
            Settlement settlement = makeSettlement();
            settlement.setResidentCount(settlement.getResidentCap() - 1);

            Assert.Equal(BuildingType.HOUSE, BuildingPlanner.getPriorityOrder(settlement)[0]);
        }

        [Fact]
        public void testPlanner_NewSettlement_SuggestsFarmFirst() {
            Settlement settlement = makeSettlement();

            Assert.Equal(BuildingType.FARM, BuildingPlanner.suggestNext(settlement));
        }

        [Fact]
        public void testPlanner_ChooseNext_SkipsWhatCannotBeAfforded() {
            Settlement settlement = makeSettlement();
            Inventory inventory = new Inventory(0);
            Assert.Null(BuildingPlanner.chooseNext(settlement, inventory));

            // enough for a storehouse only: lots of stone, little wood
            inventory.addItem(ItemType.WOOD, BuildingCatalog.getWoodCost(BuildingType.STOREHOUSE));
            inventory.addItem(ItemType.STONE, BuildingCatalog.getStoneCost(BuildingType.STOREHOUSE));

            Assert.Equal(BuildingType.STOREHOUSE, BuildingPlanner.chooseNext(settlement, inventory));
        }

        [Fact]
        public void testPlanner_ChooseNext_CountsStockpile() {
            Settlement settlement = makeSettlement();
            settlement.getInventory().addItem(ItemType.WOOD, 100);
            settlement.getInventory().addItem(ItemType.STONE, 100);

            Assert.NotNull(BuildingPlanner.chooseNext(settlement, new Inventory(0)));
        }

        [Fact]
        public void testPlanner_FullSettlement_SuggestsNothing() {
            Settlement settlement = makeSettlement();
            foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType))) {
                while (settlement.addBuilding(type)) {
                }
            }

            Assert.Null(BuildingPlanner.suggestNext(settlement));
        }
    }
}
