using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestTree {
        private readonly RandomSource random = new RandomSource(20260816);

        [Fact]
        public void testInstantiation() {
            // run
            int height = 5;
            AppleTree tree = new AppleTree(new Vector3(0, 0, 0), height, random);

            // check
            Assert.Equal(EntityType.TREE, tree.getType());
            Assert.Equal("Tree", tree.getName());
            Assert.Equal(new Vector3(0, 0, 0), tree.getPosition());

            // a cylinder trunk with a cube of leaves above it, as two parts of
            // one appearance rather than two child game objects
            Appearance appearance = tree.getAppearance();
            Assert.Equal(2, appearance.getParts().Count);

            AppearancePart trunk = appearance.getParts()[0];
            Assert.Equal(PrimitiveKind.Cylinder, trunk.getKind());
            Assert.Equal(Vector3.Zero, trunk.getOffset());
            Assert.Equal(new Vector3(1, height, 1), trunk.getScale());
            Assert.Equal(new Rgba(0.5f, 0.25f, 0), trunk.getColor());

            AppearancePart leaves = appearance.getParts()[1];
            Assert.Equal(PrimitiveKind.Cube, leaves.getKind());
            Assert.Equal(new Vector3(0, height - 1, 0), leaves.getOffset());
            Assert.Equal(new Vector3(3, 3, 3), leaves.getScale());
            Assert.Equal(Rgba.Green, leaves.getColor());
        }

        [Fact]
        public void testCarriesHarvestableResources() {
            AppleTree tree = new AppleTree(new Vector3(0, 0, 0), 5, random);

            Assert.True(tree.getInventory().getNumItems(ItemType.WOOD) >= 3);
            Assert.True(tree.getInventory().getNumItems(ItemType.WOOD) <= 5);
        }
    }
}
