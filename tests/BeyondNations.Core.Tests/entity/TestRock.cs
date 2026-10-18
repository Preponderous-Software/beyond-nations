using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestRock {

        [Fact]
        public void testInstantiation() {
            // run
            Rock rock = new Rock(new Vector3(0, 0, 0));

            // check
            Assert.Equal(EntityType.ROCK, rock.getType());
            Assert.Equal("Rock", rock.getName());
            Assert.Equal(new Vector3(0, 0, 0), rock.getPosition());

            AppearancePart part = rock.getAppearance().getPrimaryPart();
            Assert.Equal(PrimitiveKind.Cube, part.getKind());
            Assert.Equal(Vector3.One, part.getScale());
            Assert.Equal(Rgba.Gray, part.getColor());
        }

        [Fact]
        public void testCarriesStone() {
            Rock rock = new Rock(new Vector3(0, 0, 0));

            Assert.Equal(1, rock.getInventory().getNumItems(ItemType.STONE));
        }
    }
}
