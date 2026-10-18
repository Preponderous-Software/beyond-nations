
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestItem {


        [Fact]
        public void testInitialization() {
            // run
            Item item = new Item(ItemType.WOOD);

            // check
            Assert.NotNull(item);
            Assert.Equal(ItemType.WOOD, item.getType());
        }

        [Fact]
        public void testGetType() {
            // setup
            Item coinItem = new Item(ItemType.COIN);
            Item stoneItem = new Item(ItemType.STONE);

            // check
            Assert.Equal(ItemType.COIN, coinItem.getType());
            Assert.Equal(ItemType.STONE, stoneItem.getType());
        }

        [Fact]
        public void testIsSameTypeAs() {
            // setup
            Item wood1 = new Item(ItemType.WOOD);
            Item wood2 = new Item(ItemType.WOOD);
            Item stone = new Item(ItemType.STONE);

            // check
            Assert.True(wood1.isSameTypeAs(wood2));
            Assert.False(wood1.isSameTypeAs(stone));
        }

        [Fact]
        public void testIsSameTypeAsWithNull() {
            // setup
            Item wood = new Item(ItemType.WOOD);

            // check
            Assert.False(wood.isSameTypeAs(null));
        }
    }
}
