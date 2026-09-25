using System.Diagnostics;

using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestMarket {
        private readonly RandomSource random = new RandomSource(20260816);



        [Fact]
        public void testInstantiation() {
            Market market = new Market(10, random);
            Assert.NotNull(market);
            Assert.Equal(0, market.getNumStalls());
            Assert.Equal(10, market.getMaxNumStalls());
        }

        [Fact]
        public void testCreateStall() {
            // prepare
            Market market = new Market(10, random);

            // execute
            bool result = market.createStall();

            // verify
            Assert.True(result);
            Assert.Equal(1, market.getNumStalls());
            Assert.Equal(1, market.getNumStallsForSale());
        }

        [Fact]
        public void testGetStallForSale() {
            // prepare
            Market market = new Market(10, random);
            market.createStall();

            // execute
            Stall stall = market.getStallForSale();

            // verify
            Assert.NotNull(stall);
            Assert.Null(stall.getOwnerId());
        }

        [Fact]
        public void testGetStall() {
            // prepare
            Market market = new Market(10, random);
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
            market.createStall();
            market.getStallForSale().setOwnerId(pawn.getId());

            // execute
            Stall stall = market.getStall(pawn.getId());

            // verify
            Assert.NotNull(stall);

            // cleanup
        }

        /**
            * A market with one stall, owned by a fresh pawn, stocked as given.
        */
        private Market createMarketWithStall(int numApples, int numMeat) {
            Market market = new Market(10, random);
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(new Pawn(new Vector3(0, 0, 0), "merchant", random).getId());
            if (numApples > 0) {
                stall.getInventory().addItem(ItemType.APPLE, numApples);
            }
            if (numMeat > 0) {
                stall.getInventory().addItem(ItemType.CHICKEN_MEAT, numMeat);
            }
            return market;
        }

        private Pawn createBuyer(int coins) {
            Pawn buyer = new Pawn(new Vector3(0, 0, 0), "buyer", random);
            buyer.getInventory().setNumItems(ItemType.COIN, coins);
            return buyer;
        }

        [Fact]
        public void testPurchaseFood_OnlyMeatStocked_BuysMeat() {
            // prepare (#255: meat at a stall used to be invisible to purchaseFood)
            Market market = createMarketWithStall(0, 1);
            Pawn buyer = createBuyer(100);

            // execute
            EntityId stallOwnerId = market.purchaseFood(buyer);

            // verify
            Assert.NotNull(stallOwnerId);
            Assert.Equal(1, buyer.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Equal(100 - ItemCostCalculator.getBaseCost(ItemType.CHICKEN_MEAT), buyer.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testPurchaseFood_BothStocked_BuysMostEnergyPerCoin() {
            // prepare: one of each, so meat costs 50 for 25 energy and an apple 40 for 10
            Market market = createMarketWithStall(1, 1);
            Pawn buyer = createBuyer(100);

            // execute
            market.purchaseFood(buyer);

            // verify
            Assert.Equal(1, buyer.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Equal(0, buyer.getInventory().getNumItems(ItemType.APPLE));
        }

        [Fact]
        public void testPurchaseFood_PlentifulApples_BuysApplesWhenCheaperPerEnergy() {
            // prepare: 40 apples price each at 1 coin, far cheaper per energy than one meat at 50
            Market market = createMarketWithStall(40, 1);
            Pawn buyer = createBuyer(100);

            // execute
            market.purchaseFood(buyer);

            // verify
            Assert.Equal(1, buyer.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, buyer.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
        }

        [Fact]
        public void testPurchaseFood_PreferredFoodUnaffordable_FallsBackToTheOther() {
            // prepare: meat is preferred at these prices but costs 50; the buyer has 45
            Market market = createMarketWithStall(1, 1);
            Pawn buyer = createBuyer(45);

            // execute
            EntityId stallOwnerId = market.purchaseFood(buyer);

            // verify
            Assert.NotNull(stallOwnerId);
            Assert.Equal(1, buyer.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(5, buyer.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testCanPurchaseFood_TracksPriceNotJustStock() {
            // prepare
            Market market = createMarketWithStall(1, 0);

            // run / check: an apple costs 40, so 39 coins cannot buy one
            Assert.False(market.canPurchaseFood(createBuyer(39)));
            Assert.True(market.canPurchaseFood(createBuyer(40)));
        }

        [Fact]
        public void testCanPurchaseFood_OwnStallDoesNotCount() {
            // prepare
            Market market = new Market(10, random);
            market.createStall();
            Pawn owner = createBuyer(100);
            Stall stall = market.getStallForSale();
            stall.setOwnerId(owner.getId());
            stall.getInventory().addItem(ItemType.CHICKEN_MEAT, 3);

            // run / check
            Assert.False(market.canPurchaseFood(owner));
            Assert.Null(market.purchaseFood(owner));
        }
    }
}