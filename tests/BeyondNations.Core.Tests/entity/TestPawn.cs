
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestPawn {
        private readonly RandomSource random = new RandomSource(20260816);



        [Fact]
        public void testInstantiation() {
            // run
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "Pawn", random);

            // check
            Assert.Equal(EntityType.PAWN, pawn.getType());
            Assert.Equal("Pawn", pawn.getName());
            Assert.Equal(new Vector3(0, 0, 0), pawn.getPosition());

            AppearancePart part = pawn.getAppearance().getPrimaryPart();
            Assert.Equal(PrimitiveKind.Capsule, part.getKind());
            Assert.Equal(Vector3.One, part.getScale());
            Assert.Equal(Rgba.Gray, part.getColor());

            // the name tag is a label on the appearance, not a child object
            Assert.Equal("Pawn", pawn.getAppearance().getLabel());

        }

        [Fact]
        public void testGetSpeed() {
            // run
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "Pawn", random);

            // check
            Assert.True(pawn.getSpeed() > 0);

        }

        [Fact]
        public void testHasTargetEntity() {
            // run
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "Pawn", random);

            // check
            Assert.False(pawn.hasTargetEntity());

        }

        [Fact]
        public void testGetTargetEntity() {
            // run
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "Pawn", random);

            // check
            Assert.Null(pawn.getTargetEntity());

        }

        [Fact]
        public void testSetTargetEntity() {
            // run
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "Pawn", random);
            Pawn targetEntity = new Pawn(new Vector3(0, 0, 0), "Target Entity", random);
            pawn.setTargetEntity(targetEntity);

            // check
            Assert.Equal(targetEntity, pawn.getTargetEntity());

        }

        [Fact]
        public void testNeedsFood_BelowThresholdWithNothingEdible() {
            // prepare: a new pawn carries coins only
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "Pawn", random);
            pawn.setEnergy(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);

            // check
            Assert.True(pawn.needsFood());
        }

        [Fact]
        public void testNeedsFood_AtThreshold_IsFalse() {
            // prepare
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "Pawn", random);
            pawn.setEnergy(Pawn.HUNGRY_ENERGY_THRESHOLD);

            // check
            Assert.False(pawn.needsFood());
        }

        [Fact]
        public void testNeedsFood_BelowThresholdCarryingMeat_IsFalse() {
            // prepare: any edible item counts, not only an apple
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "Pawn", random);
            pawn.setEnergy(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);
            pawn.getInventory().addItem(ItemType.CHICKEN_MEAT, 1);

            // check
            Assert.False(pawn.needsFood());
        }
    }
}