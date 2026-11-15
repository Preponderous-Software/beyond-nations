using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Xunit;
using beyondnations;

namespace beyondnationstests {

    /**
    * Trees and saplings are a trunk and a layered canopy in a few shapes,
    * picked from where they stand rather than from the world's random
    * stream (#140).
    */
    public class TestTreeShape {

        private static float topOf(Appearance appearance) {
            float top = float.MinValue;
            foreach (AppearancePart part in appearance.getParts()) {
                // spheres and cylinders both reach scale.y / 2 or scale.y above their
                // centre; the larger is a safe upper bound for either
                top = System.Math.Max(top, part.getOffset().Y + part.getScale().Y);
            }
            return top;
        }

        [Fact]
        public void testTheSameSpot_AlwaysGivesTheSameTree() {
            Vector3 at = new Vector3(12.5f, 3f, -40f);
            Appearance a = TreeShape.tree(at, 5);
            Appearance b = TreeShape.tree(at, 5);
            Assert.Equal(a.getParts().Count, b.getParts().Count);
            for (int i = 0; i < a.getParts().Count; i++) {
                Assert.Equal(a.getParts()[i].getOffset(), b.getParts()[i].getOffset());
                Assert.Equal(a.getParts()[i].getScale(), b.getParts()[i].getScale());
                Assert.Equal(a.getParts()[i].getColor(), b.getParts()[i].getColor());
            }
        }

        [Fact]
        public void testAForest_HasEveryShape_AndMoreThanOneSize() {
            HashSet<int> shapes = new HashSet<int>();
            HashSet<float> heights = new HashSet<float>();
            for (int x = 0; x < 20; x++) {
                for (int z = 0; z < 20; z++) {
                    Vector3 at = new Vector3(x * 5f, 0, z * 5f);
                    shapes.Add(TreeShape.shapeAt(at));
                    heights.Add(topOf(TreeShape.tree(at, 5)));
                }
            }
            Assert.Equal(TreeShape.ShapeCount, shapes.Count);
            Assert.True(heights.Count > 20, "only " + heights.Count + " distinct tree heights");
        }

        [Fact]
        public void testASapling_IsSmallerThanTheTreeItGrowsInto() {
            for (int i = 0; i < 50; i++) {
                Vector3 at = new Vector3(i * 7f, 0, i * 3f);
                Appearance sapling = TreeShape.sapling(at, 3);
                Appearance tree = TreeShape.tree(at, 5);
                Assert.True(topOf(sapling) < topOf(tree) * 0.6f);
                Assert.True(sapling.getParts()[0].getScale().X < tree.getParts()[0].getScale().X);
            }
        }

        [Fact]
        public void testEveryTrunk_StandsOnTheGround_AndEveryLeafIsAShadeOfGreen() {
            for (int i = 0; i < 50; i++) {
                Vector3 at = new Vector3(i * 11f, 0, -i * 2f);
                foreach (Appearance a in new[] { TreeShape.tree(at, 5), TreeShape.sapling(at, 3) }) {
                    AppearancePart trunk = a.getParts()[0];
                    Assert.Equal(PrimitiveKind.Cylinder, trunk.getKind());
                    Assert.Equal(TreeShape.GroundY, trunk.getOffset().Y - trunk.getScale().Y, 3);
                    for (int p = 1; p < a.getParts().Count; p++) {
                        Rgba c = a.getParts()[p].getColor();
                        Assert.True(c.r == 0f && c.b == 0f && c.g > 0f && c.g <= 1f, c.ToString());
                    }
                }
            }
        }

        [Fact]
        public void testShadedLeaves_FollowTheSeason_AtTheirShade() {
            TickCounter ticks = new TickCounter();
            TimeOfDay timeOfDay = new TimeOfDay(ticks, TimeOfDay.DefaultTicksPerDay, 0f);
            for (long i = 0; i < 2L * SeasonCalendar.DAYS_PER_SEASON * TimeOfDay.DefaultTicksPerDay; i++) {
                ticks.increment();
            }
            WorldPalette autumn = new WorldPalette(new SeasonCalendar(timeOfDay), new RegionMap(1, 105f));
            Rgba full = autumn.partColour(EntityType.TREE, Rgba.Green);
            Rgba shaded = autumn.partColour(EntityType.TREE, new Rgba(0f, 0.7f, 0f));
            Assert.True(full.r > full.g, full.ToString());
            Assert.Equal(full.r * 0.7f, shaded.r, 4);
            Assert.Equal(full.g * 0.7f, shaded.g, 4);
            // a pawn's green is not a leaf
            Assert.Equal(new Rgba(0f, 0.7f, 0f), autumn.partColour(EntityType.PAWN, new Rgba(0f, 0.7f, 0f)));
        }

        [Fact]
        public void testATreeFromAnOldSave_IsRebuiltInTheNewShape() {
            // the v4 fixture was written when a tree was a cylinder and a cube
            Simulation loaded = SaveGame.decode(File.ReadAllBytes(SaveFixtures.pathFor(4)));
            int trees = 0;
            EntityRepository repository = loaded.getEntityRepository();
            for (int e = 0; e < repository.getEntityCount(); e++) {
                Entity entity = repository.getEntityAt(e);
                if (entity.getType() != EntityType.TREE && entity.getType() != EntityType.SAPLING) {
                    continue;
                }
                trees++;
                Appearance expected = entity.getType() == EntityType.TREE
                    ? TreeShape.tree(entity.getPosition(), ((AppleTree) entity).getHeight())
                    : TreeShape.sapling(entity.getPosition(), ((Sapling) entity).getHeight());
                Assert.Equal(expected.getParts().Count, entity.getAppearance().getParts().Count);
                Assert.Equal(expected.getParts()[1].getOffset(), entity.getAppearance().getParts()[1].getOffset());
                Assert.DoesNotContain(entity.getAppearance().getParts(), part => part.getKind() == PrimitiveKind.Cube);
            }
            Assert.True(trees > 0);
        }
    }
}
