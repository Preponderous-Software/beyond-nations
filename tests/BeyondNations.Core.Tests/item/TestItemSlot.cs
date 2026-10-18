
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestItemSlot {


        [Fact]
        public void testEmptyInitialization() {
            // run
            ItemSlot slot = new ItemSlot();

            // check
            Assert.NotNull(slot);
            Assert.True(slot.isEmpty());
            Assert.Null(slot.getItemStack());
        }

        [Fact]
        public void testInitializationWithItemStack() {
            // setup
            ItemStack stack = new ItemStack(ItemType.WOOD, 5);
            
            // run
            ItemSlot slot = new ItemSlot(stack);

            // check
            Assert.NotNull(slot);
            Assert.False(slot.isEmpty());
            Assert.Equal(stack, slot.getItemStack());
        }

        [Fact]
        public void testIsEmpty() {
            // setup
            ItemSlot emptySlot = new ItemSlot();
            ItemSlot fullSlot = new ItemSlot(new ItemStack(ItemType.STONE, 10));
            ItemSlot zeroQuantitySlot = new ItemSlot(new ItemStack(ItemType.COIN, 0));

            // check
            Assert.True(emptySlot.isEmpty());
            Assert.False(fullSlot.isEmpty());
            Assert.True(zeroQuantitySlot.isEmpty());
        }

        [Fact]
        public void testSetItemStack() {
            // setup
            ItemSlot slot = new ItemSlot();
            ItemStack stack = new ItemStack(ItemType.APPLE, 3);

            // run
            slot.setItemStack(stack);

            // check
            Assert.Equal(stack, slot.getItemStack());
            Assert.False(slot.isEmpty());
        }

        [Fact]
        public void testClear() {
            // setup
            ItemSlot slot = new ItemSlot(new ItemStack(ItemType.WOOD, 15));

            // run
            slot.clear();

            // check
            Assert.True(slot.isEmpty());
            Assert.Null(slot.getItemStack());
        }

        [Fact]
        public void testHasItemOfType() {
            // setup
            ItemSlot woodSlot = new ItemSlot(new ItemStack(ItemType.WOOD, 5));
            ItemSlot emptySlot = new ItemSlot();

            // check
            Assert.True(woodSlot.hasItemOfType(ItemType.WOOD));
            Assert.False(woodSlot.hasItemOfType(ItemType.STONE));
            Assert.False(emptySlot.hasItemOfType(ItemType.WOOD));
        }

        [Fact]
        public void testGetQuantity() {
            // setup
            ItemSlot fullSlot = new ItemSlot(new ItemStack(ItemType.COIN, 100));
            ItemSlot emptySlot = new ItemSlot();

            // check
            Assert.Equal(100, fullSlot.getQuantity());
            Assert.Equal(0, emptySlot.getQuantity());
        }
    }
}
