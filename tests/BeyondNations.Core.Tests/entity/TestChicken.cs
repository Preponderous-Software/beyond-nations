
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestChicken {
        private readonly RandomSource random = new RandomSource(20260816);

        [Fact]
        public void testWanderSetsHorizontalVelocityAtItsOwnSpeed() {
            // prepare
            Chicken chicken = new Chicken(new Vector3(0, 0.5f, 0), random);

            // run: first call always picks a target, since wanderTimer starts
            // past the interval threshold of a freshly constructed chicken
            chicken.wander(3f);

            // check: whatever direction it picked, the horizontal speed is
            // exactly the chicken's own speed (or it is already close enough
            // to be considered arrived, in which case velocity is zero)
            Vector3 velocity = chicken.getVelocity();
            float horizontalSpeed = new Vector3(velocity.X, 0, velocity.Z).Length();
            Assert.True(horizontalSpeed < 0.001f || System.MathF.Abs(horizontalSpeed - chicken.getSpeed()) < 0.01f);
        }

        [Fact]
        public void testWanderPreservesVerticalVelocity() {
            // prepare: falling chicken
            Chicken chicken = new Chicken(new Vector3(0, 0.5f, 0), random);
            chicken.setVelocity(new Vector3(0, -2f, 0));

            // run
            chicken.wander(3f);

            // check
            Assert.Equal(-2f, chicken.getVelocity().Y, 4);
        }

        [Fact]
        public void testChickenNeverFallsThroughGroundOverManyWanderSteps() {
            // prepare
            Chicken chicken = new Chicken(new Vector3(0, 0.5f, 0), random);
            float dt = 1f / 60f;
            float groundHeight = 0.5f;

            // run
            bool everBelowGround = false;
            for (int i = 0; i < 3000; i++) {
                chicken.wander(dt);
                MovementIntegrator.step(chicken, dt, groundHeight);
                if (chicken.getPosition().Y < groundHeight - 0.01f) {
                    everBelowGround = true;
                }
            }

            // check
            Assert.False(everBelowGround);
        }
    }
}
