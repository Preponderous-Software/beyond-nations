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

            // a cylinder trunk standing on the ground, then a canopy of
            // leaf spheres above it (#140; TestTreeShape covers the shapes)
            Appearance appearance = tree.getAppearance();
            Assert.InRange(appearance.getParts().Count, 3, 5);

            AppearancePart trunk = appearance.getParts()[0];
            Assert.Equal(PrimitiveKind.Cylinder, trunk.getKind());
            Assert.Equal(TreeShape.GroundY, trunk.getOffset().Y - trunk.getScale().Y, 3);
            Assert.Equal(new Rgba(0.5f, 0.25f, 0), trunk.getColor());

            for (int i = 1; i < appearance.getParts().Count; i++) {
                AppearancePart leaves = appearance.getParts()[i];
                Assert.Equal(PrimitiveKind.Sphere, leaves.getKind());
                Assert.True(leaves.getOffset().Y > trunk.getOffset().Y);
                Assert.Equal(0f, leaves.getColor().r);
                Assert.Equal(0f, leaves.getColor().b);
            }
        }

        [Fact]
        public void testCarriesHarvestableResources() {
            AppleTree tree = new AppleTree(new Vector3(0, 0, 0), 5, random);

            Assert.InRange(tree.getInventory().getNumItems(ItemType.WOOD), 4, 6);
            Assert.InRange(tree.getInventory().getNumItems(ItemType.APPLE), 1, 3);
        }
    }
}
