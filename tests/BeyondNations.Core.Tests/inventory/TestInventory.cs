
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestInventory {


        [Fact]
        public void testInitialization() {
            // run
            Inventory inventory = new Inventory(0);

            // check
            Assert.Equal(0, inventory.getNumItems(ItemType.COIN));
            Assert.Equal(0, inventory.getNumItems(ItemType.WOOD));
            Assert.Equal(0, inventory.getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testAddWood() {
            // setup
            Inventory inventory = new Inventory(0);

            // run
            inventory.addItem(ItemType.WOOD, 5);

            // check
            Assert.Equal(5, inventory.getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testRemoveWood() {
            // setup
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.WOOD, 5);

            // run
            inventory.removeItem(ItemType.WOOD, 3);

            // check
            Assert.Equal(2, inventory.getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testAddStone() {
            // setup
            Inventory inventory = new Inventory(0);

            // run
            inventory.addItem(ItemType.STONE, 5);

            // check
            Assert.Equal(5, inventory.getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testRemoveStone() {
            // setup
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.STONE, 5);

            // run
            inventory.removeItem(ItemType.STONE, 3);

            // check
            Assert.Equal(2, inventory.getNumItems(ItemType.STONE));
        }
        
        [Fact]
        public void testSetNumWood() {
            // setup
            Inventory inventory = new Inventory(0);

            // run
            inventory.setNumItems(ItemType.WOOD, 5);

            // check
            Assert.Equal(5, inventory.getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testSetNumStone() {
            // setup
            Inventory inventory = new Inventory(0);

            // run
            inventory.setNumItems(ItemType.STONE, 5);

            // check
            Assert.Equal(5, inventory.getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testTransferContentsOfInventory() {
            // setup
            Inventory inventory1 = new Inventory(0);
            Inventory inventory2 = new Inventory(10);
            inventory1.addItem(ItemType.WOOD, 5);
            inventory2.addItem(ItemType.STONE, 3);

            // run
            inventory1.transferContentsOfInventory(inventory2);

            // check
            Assert.Equal(10, inventory1.getNumItems(ItemType.COIN));
            Assert.Equal(5, inventory1.getNumItems(ItemType.WOOD));
            Assert.Equal(3, inventory1.getNumItems(ItemType.STONE));
            Assert.Equal(0, inventory2.getNumItems(ItemType.COIN));
            Assert.Equal(0, inventory2.getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testContainsAbundanceOfResources() {
            // setup
            Inventory inventory1 = new Inventory(0);
            Inventory inventory2 = new Inventory(0);
            inventory2.addItem(ItemType.WOOD, 15);
            inventory2.addItem(ItemType.STONE, 15);
            inventory2.addItem(ItemType.APPLE, 15);

            // check
            Assert.False(inventory1.containsAbundanceOfResources());
            Assert.True(inventory2.containsAbundanceOfResources());
        }

        [Fact]
        public void testGetTotalNumItems() {
            // setup
            Inventory inventory = new Inventory(10);
            inventory.addItem(ItemType.WOOD, 5);
            inventory.addItem(ItemType.STONE, 3);

            // check
            Assert.Equal(18, inventory.getTotalNumItems());
        }

        [Fact]
        public void testHasItem() {
            // setup
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.WOOD, 5);

            // check
            Assert.True(inventory.hasItem(ItemType.WOOD));
            Assert.False(inventory.hasItem(ItemType.STONE));
        }

        [Fact]
        public void testClear() {
            // setup
            Inventory inventory = new Inventory(10);
            inventory.addItem(ItemType.WOOD, 5);
            inventory.addItem(ItemType.STONE, 3);

            // run
            inventory.clear();

            // check
            Assert.Equal(0, inventory.getNumItems(ItemType.COIN));
            Assert.Equal(0, inventory.getNumItems(ItemType.WOOD));
            Assert.Equal(0, inventory.getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testGetSlots() {
            // setup
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.WOOD, 5);

            // check - Inventory initializes with 5 slots (one for each ItemType: COIN, WOOD, STONE, APPLE, SAPLING)
            Assert.NotNull(inventory.getSlots());
            Assert.Equal(5, inventory.getSlots().Count);
        }
    }
}