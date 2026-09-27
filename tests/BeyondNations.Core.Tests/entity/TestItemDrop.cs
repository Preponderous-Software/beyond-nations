using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestItemDrop {

        [Fact]
        public void testInstantiation() {
            // run
            ItemDrop drop = new ItemDrop(new Vector3(3, 0, 4), new ItemStack(ItemType.WOOD, 7));

            // check
            Assert.Equal(EntityType.ITEM_DROP, drop.getType());
            Assert.Equal("WOOD x7", drop.getName());
            Assert.Equal(new Vector3(3, 0, 4), drop.getPosition());
            Assert.Equal(ItemType.WOOD, drop.getItemStack().getItemType());
            Assert.Equal(7, drop.getItemStack().getQuantity());
            Assert.Equal(PrimitiveKind.Sphere, drop.getAppearance().getPrimaryPart().getKind());
        }

        [Fact]
        public void testDropContentsOf_OneDropPerNonEmptySlot() {
            // prepare
            Inventory inventory = new Inventory(13);
            inventory.addItem(ItemType.STONE, 4);

            // run
            List<ItemDrop> drops = ItemDrop.dropContentsOf(inventory, new Vector3(10, 1.5f, -6));

            // check
            Assert.Equal(2, drops.Count);
            Assert.Equal(ItemType.COIN, drops[0].getItemStack().getItemType());
            Assert.Equal(13, drops[0].getItemStack().getQuantity());
            Assert.Equal(ItemType.STONE, drops[1].getItemStack().getItemType());
            Assert.Equal(4, drops[1].getItemStack().getQuantity());
        }

        [Fact]
        public void testDropContentsOf_EmptiesTheInventory() {
            // prepare
            Inventory inventory = new Inventory(13);
            inventory.addItem(ItemType.APPLE, 2);

            // run
            ItemDrop.dropContentsOf(inventory, Vector3.Zero);

            // check
            Assert.Equal(0, inventory.getTotalNumItems());
        }

        [Fact]
        public void testDropContentsOf_RestsOnTheGroundBelowTheGivenPosition() {
            // prepare
            Inventory inventory = new Inventory(1);

            // run
            List<ItemDrop> drops = ItemDrop.dropContentsOf(inventory, new Vector3(10, 1.5f, -6));

            // check
            Assert.Equal(new Vector3(10, ItemDrop.GroundHeight, -6), drops[0].getPosition());
        }

        [Fact]
        public void testDropContentsOf_EmptyInventory_NoDrops() {
            // run
            List<ItemDrop> drops = ItemDrop.dropContentsOf(new Inventory(0), Vector3.Zero);

            // check
            Assert.Empty(drops);
        }
    }
}
