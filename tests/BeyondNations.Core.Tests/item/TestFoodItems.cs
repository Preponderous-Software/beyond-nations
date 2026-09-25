
using System;
using System.Collections.Generic;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestFoodItems {

        [Theory]
        [InlineData(ItemType.APPLE, true)]
        [InlineData(ItemType.CHICKEN_MEAT, true)]
        [InlineData(ItemType.WOOD, false)]
        [InlineData(ItemType.STONE, false)]
        [InlineData(ItemType.COIN, false)]
        [InlineData(ItemType.SAPLING, false)]
        public void testOnlyEdibleItemTypesCountAsFood(ItemType itemType, bool expectedFood) {
            // run / check
            Assert.Equal(expectedFood, FoodItems.isFood(itemType));
        }

        [Fact]
        public void testAnAppleRestoresTheSameEnergyItAlwaysHas() {
            // check: the value the player and the pawn energy step used before
            // food became a table, pinned so a table edit cannot change it silently
            Assert.Equal(10, FoodItems.getEnergyRestored(ItemType.APPLE));
        }

        [Fact]
        public void testChickenMeatRestoresMoreEnergyThanAnApple() {
            // check
            Assert.True(FoodItems.getEnergyRestored(ItemType.CHICKEN_MEAT) > FoodItems.getEnergyRestored(ItemType.APPLE));
        }

        [Fact]
        public void testSomethingInedibleRestoresNoEnergy() {
            // run / check
            Assert.Equal(0, FoodItems.getEnergyRestored(ItemType.WOOD));
        }

        [Fact]
        public void testEveryItemTypeIsAnsweredRatherThanThrowing() {
            // run / check: getEnergyRestored is called from the per-frame energy
            // step, so an unmapped item type has to return zero and not throw
            foreach (ItemType itemType in Enum.GetValues(typeof(ItemType))) {
                Assert.True(FoodItems.getEnergyRestored(itemType) >= 0);
            }
        }

        [Fact]
        public void testFoodOrderIsActuallyDescendingByEnergyRestored() {
            // check: consumeMostNourishingFood trusts the order of this list, so
            // adding a food out of order has to fail here rather than in play
            IReadOnlyList<ItemType> order = FoodItems.getFoodInDescendingEnergyOrder();
            for (int i = 1; i < order.Count; i++) {
                Assert.True(FoodItems.getEnergyRestored(order[i - 1]) >= FoodItems.getEnergyRestored(order[i]));
            }
        }

        [Fact]
        public void testEveryListedFoodIsFood() {
            // check: the ordered list and the energy table describe the same set
            IReadOnlyList<ItemType> order = FoodItems.getFoodInDescendingEnergyOrder();
            foreach (ItemType itemType in order) {
                Assert.True(FoodItems.isFood(itemType));
            }

            int numFoodTypes = 0;
            foreach (ItemType itemType in Enum.GetValues(typeof(ItemType))) {
                if (FoodItems.isFood(itemType)) {
                    numFoodTypes++;
                }
            }
            Assert.Equal(numFoodTypes, order.Count);
        }

        [Fact]
        public void testHasFoodIsFalseForAnInventoryOfOnlyMaterials() {
            // prepare
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.WOOD, 10);
            inventory.addItem(ItemType.STONE, 10);

            // run / check
            Assert.False(FoodItems.hasFood(inventory));
        }

        [Fact]
        public void testHasFoodIsTrueForChickenMeatAlone() {
            // prepare: no apples at all, which is the case the pawn behavior
            // calculator used to read as "this pawn needs food"
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.CHICKEN_MEAT, 1);

            // run / check
            Assert.True(FoodItems.hasFood(inventory));
        }

        [Fact]
        public void testConsumingFromAnEmptyInventoryRestoresNothing() {
            // prepare
            Inventory inventory = new Inventory(0);

            // run
            int energyRestored = FoodItems.consumeMostNourishingFood(inventory);

            // check
            Assert.Equal(0, energyRestored);
            Assert.Equal(0, inventory.getTotalNumItems());
        }

        [Fact]
        public void testConsumingLeavesInedibleItemsAlone() {
            // prepare
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.WOOD, 7);

            // run
            int energyRestored = FoodItems.consumeMostNourishingFood(inventory);

            // check
            Assert.Equal(0, energyRestored);
            Assert.Equal(7, inventory.getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testConsumingChickenMeatRemovesExactlyOneAndRestoresItsEnergy() {
            // prepare
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.CHICKEN_MEAT, 3);

            // run
            int energyRestored = FoodItems.consumeMostNourishingFood(inventory);

            // check
            Assert.Equal(FoodItems.getEnergyRestored(ItemType.CHICKEN_MEAT), energyRestored);
            Assert.Equal(2, inventory.getNumItems(ItemType.CHICKEN_MEAT));
        }

        [Fact]
        public void testTheMostNourishingFoodIsEatenFirst() {
            // prepare: both foods held at once
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.APPLE, 2);
            inventory.addItem(ItemType.CHICKEN_MEAT, 2);

            // run
            int energyRestored = FoodItems.consumeMostNourishingFood(inventory);

            // check: the meat goes, the apples are untouched
            Assert.Equal(FoodItems.getEnergyRestored(ItemType.CHICKEN_MEAT), energyRestored);
            Assert.Equal(1, inventory.getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Equal(2, inventory.getNumItems(ItemType.APPLE));
        }

        [Fact]
        public void testApplesAreEatenOnceTheMeatRunsOut() {
            // prepare
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.APPLE, 1);
            inventory.addItem(ItemType.CHICKEN_MEAT, 1);

            // run
            FoodItems.consumeMostNourishingFood(inventory);
            int energyRestored = FoodItems.consumeMostNourishingFood(inventory);

            // check
            Assert.Equal(FoodItems.getEnergyRestored(ItemType.APPLE), energyRestored);
            Assert.False(FoodItems.hasFood(inventory));
        }

        [Fact]
        public void testMostNourishingFoodIsMeatWhenBothAreHeld() {
            // prepare
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.APPLE, 5);
            inventory.addItem(ItemType.CHICKEN_MEAT, 1);

            // run / check
            Assert.Equal(ItemType.CHICKEN_MEAT, FoodItems.getMostNourishingFood(inventory));
        }

        [Fact]
        public void testMostNourishingFoodIsNullWithoutFood() {
            // prepare
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.WOOD, 10);

            // run / check
            Assert.Null(FoodItems.getMostNourishingFood(inventory));
        }
    }
}
