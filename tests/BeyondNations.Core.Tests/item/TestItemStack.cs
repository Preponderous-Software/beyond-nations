
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestItemStack {


        [Fact]
        public void testInitializationWithItem() {
            // setup
            Item item = new Item(ItemType.WOOD);
            
            // run
            ItemStack stack = new ItemStack(item, 5);

            // check
            Assert.NotNull(stack);
            Assert.Equal(item, stack.getItem());
            Assert.Equal(5, stack.getQuantity());
        }

        [Fact]
        public void testInitializationWithItemType() {
            // run
            ItemStack stack = new ItemStack(ItemType.STONE, 10);

            // check
            Assert.NotNull(stack);
            Assert.Equal(ItemType.STONE, stack.getItemType());
            Assert.Equal(10, stack.getQuantity());
        }

        [Fact]
        public void testGetQuantity() {
            // setup
            ItemStack stack = new ItemStack(ItemType.COIN, 100);

            // check
            Assert.Equal(100, stack.getQuantity());
        }

        [Fact]
        public void testSetQuantity() {
            // setup
            ItemStack stack = new ItemStack(ItemType.APPLE, 5);

            // run
            stack.setQuantity(15);

            // check
            Assert.Equal(15, stack.getQuantity());
        }

        [Fact]
        public void testAddQuantity() {
            // setup
            ItemStack stack = new ItemStack(ItemType.WOOD, 10);

            // run
            stack.addQuantity(5);

            // check
            Assert.Equal(15, stack.getQuantity());
        }

        [Fact]
        public void testRemoveQuantity() {
            // setup
            ItemStack stack = new ItemStack(ItemType.STONE, 20);

            // run
            stack.removeQuantity(8);

            // check
            Assert.Equal(12, stack.getQuantity());
        }

        [Fact]
        public void testAddNegativeQuantity() {
            // setup
            ItemStack stack = new ItemStack(ItemType.WOOD, 10);

            // run - addQuantity can accept negative values
            stack.addQuantity(-3);

            // check
            Assert.Equal(7, stack.getQuantity());
        }

        [Fact]
        public void testRemoveExcessQuantity() {
            // setup
            ItemStack stack = new ItemStack(ItemType.STONE, 5);

            // run - removing more than available results in negative quantity
            stack.removeQuantity(10);

            // check - isEmpty() returns true for quantities <= 0 (including negative)
            Assert.Equal(-5, stack.getQuantity());
            Assert.True(stack.isEmpty());
        }

        [Fact]
        public void testIsEmpty() {
            // setup
            ItemStack stack1 = new ItemStack(ItemType.COIN, 0);
            ItemStack stack2 = new ItemStack(ItemType.WOOD, 5);
            ItemStack stack3 = new ItemStack(ItemType.STONE, -1);

            // check
            Assert.True(stack1.isEmpty());
            Assert.False(stack2.isEmpty());
            Assert.True(stack3.isEmpty());
        }

        [Fact]
        public void testCanStackWith() {
            // setup
            ItemStack woodStack1 = new ItemStack(ItemType.WOOD, 5);
            ItemStack woodStack2 = new ItemStack(ItemType.WOOD, 10);
            ItemStack stoneStack = new ItemStack(ItemType.STONE, 7);

            // check
            Assert.True(woodStack1.canStackWith(woodStack2));
            Assert.False(woodStack1.canStackWith(stoneStack));
            Assert.False(woodStack1.canStackWith(null));
        }
    }
}
