
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * A minimal concrete Entity, since Entity itself is abstract and carries no
    * behavior of its own beyond the position/velocity pair the integrator
    * operates on.
    */
    public class DummyMovable : Entity {
        public DummyMovable() : base(EntityType.NONE, "Dummy") { }
    }

    public class TestMovementIntegrator {
        private const float Dt60 = 1f / 60f;

        [Fact]
        public void testFallingEntityNeverGoesBelowGround() {
            // prepare
            DummyMovable entity = new DummyMovable();
            entity.setPosition(new Vector3(0, 50, 0));
            entity.setVelocity(Vector3.Zero);

            // run: 10 seconds of fixed steps, far more than enough to land
            bool everBelowGround = false;
            for (int i = 0; i < 600; i++) {
                MovementIntegrator.step(entity, Dt60, 0f);
                if (entity.getPosition().Y < -0.0001f) {
                    everBelowGround = true;
                }
            }

            // check
            Assert.False(everBelowGround);
            Assert.Equal(0f, entity.getPosition().Y, 3);
            Assert.Equal(0f, entity.getVelocity().Y, 3);
        }

        [Fact]
        public void testFallingEntityAccelerates() {
            // prepare: ground far below so the clamp never engages during this window
            DummyMovable entity = new DummyMovable();
            entity.setPosition(new Vector3(0, 1000, 0));
            entity.setVelocity(Vector3.Zero);

            // run / check: downward speed strictly increases each step
            float previousVelocityY = 0f;
            for (int i = 0; i < 30; i++) {
                MovementIntegrator.step(entity, Dt60, -1000000f);
                Assert.True(entity.getVelocity().Y < previousVelocityY);
                previousVelocityY = entity.getVelocity().Y;
            }
        }

        [Fact]
        public void testFallDistanceMatchesKinematics() {
            // prepare: a single small step, far from the ground, is compared
            // against the closed-form semi-implicit Euler step rather than
            // continuous kinematics, since that is the integration this class
            // performs: v1 = g*dt, x1 = v1*dt.
            DummyMovable entity = new DummyMovable();
            entity.setPosition(new Vector3(0, 1000, 0));
            entity.setVelocity(Vector3.Zero);

            // run
            MovementIntegrator.step(entity, Dt60, -1000000f);

            // check
            float expectedVelocity = MovementIntegrator.Gravity * Dt60;
            float expectedPosition = 1000f + expectedVelocity * Dt60;
            Assert.Equal(expectedVelocity, entity.getVelocity().Y, 4);
            Assert.Equal(expectedPosition, entity.getPosition().Y, 4);
        }

        [Fact]
        public void testLandingZeroesVerticalVelocityButKeepsHorizontal() {
            // prepare: just above the ground, moving down and sideways
            DummyMovable entity = new DummyMovable();
            entity.setPosition(new Vector3(0, 0.05f, 0));
            entity.setVelocity(new Vector3(3f, -5f, 2f));

            // run
            MovementIntegrator.step(entity, Dt60, 0f);

            // check
            Assert.Equal(0f, entity.getPosition().Y, 4);
            Assert.Equal(0f, entity.getVelocity().Y, 4);
            Assert.Equal(3f, entity.getVelocity().X, 4);
            Assert.Equal(2f, entity.getVelocity().Z, 4);
        }

        [Fact]
        public void testJumpReachesAPlausibleApexThenReturnsToGround() {
            // prepare: resting on the ground
            DummyMovable entity = new DummyMovable();
            entity.setPosition(new Vector3(0, 0, 0));
            entity.setVelocity(Vector3.Zero);

            // run
            MovementIntegrator.jump(entity);
            Assert.Equal(MovementIntegrator.JumpSpeed, entity.getVelocity().Y, 4);

            float maxHeight = 0f;
            bool landedAgain = false;
            for (int i = 0; i < 600 && !landedAgain; i++) {
                MovementIntegrator.step(entity, Dt60, 0f);
                if (entity.getPosition().Y > maxHeight) {
                    maxHeight = entity.getPosition().Y;
                }
                if (i > 2 && entity.getPosition().Y <= 0f && entity.getVelocity().Y == 0f) {
                    landedAgain = true;
                }
            }

            // check: apex within a discretization tolerance of the closed-form
            // v0^2 / (2 * |g|), and the jump comes back down on its own.
            float expectedApex = (MovementIntegrator.JumpSpeed * MovementIntegrator.JumpSpeed) / (2f * -MovementIntegrator.Gravity);
            Assert.True(landedAgain, "jump never returned to the ground within 10 seconds");
            Assert.InRange(maxHeight, expectedApex - 0.3f, expectedApex + 0.05f);
            Assert.True(maxHeight > 1f, "jump apex was implausibly small: " + maxHeight);
        }

        [Fact]
        public void testHorizontalMovementIsExactlyFrameRateIndependent() {
            // Constant velocity has no acceleration term, so semi-implicit
            // Euler integrates it exactly regardless of step size: this is the
            // walking/running case, and it should land in the same place
            // whether the host runs at 30, 60 or 120 fixed steps per second.
            float totalTime = 2f;
            float speed = 25f;
            float[] stepsPerSecond = { 30f, 60f, 120f, 144f };

            float[] finalX = new float[stepsPerSecond.Length];
            for (int i = 0; i < stepsPerSecond.Length; i++) {
                float dt = 1f / stepsPerSecond[i];
                int steps = (int) System.MathF.Round(totalTime / dt);

                DummyMovable entity = new DummyMovable();
                entity.setPosition(Vector3.Zero);
                entity.setVelocity(new Vector3(speed, 0, 0));

                for (int s = 0; s < steps; s++) {
                    // ground far below: this test is about the horizontal axis
                    MovementIntegrator.step(entity, dt, -1000000f);
                }
                finalX[i] = entity.getPosition().X;
            }

            // check: every run covers the same ground
            for (int i = 1; i < finalX.Length; i++) {
                Assert.Equal(finalX[0], finalX[i], 3);
            }
            Assert.Equal(speed * totalTime, finalX[0], 3);
        }

        [Fact]
        public void testFallOverFixedTimeIsApproximatelyFrameRateIndependent() {
            // Gravity is an acceleration, so semi-implicit Euler is only an
            // approximation of continuous motion, and finer steps should
            // converge toward the same answer rather than being identical.
            float totalTime = 1f;
            float startHeight = 10000f; // never reaches the ground in this window

            float fallWithCoarseStep = FallDistanceAfter(totalTime, startHeight, 1f / 15f);
            float fallWithFineStep = FallDistanceAfter(totalTime, startHeight, 1f / 240f);

            // check: both are within a few percent of the closed-form distance,
            // and the finer step is the closer of the two.
            float continuousDistance = 0.5f * -MovementIntegrator.Gravity * totalTime * totalTime;
            float coarseError = System.MathF.Abs(fallWithCoarseStep - continuousDistance);
            float fineError = System.MathF.Abs(fallWithFineStep - continuousDistance);

            Assert.True(fineError < coarseError, "finer steps should track continuous kinematics more closely");
            Assert.True(fineError < 0.1f, "fine-step fall distance drifted too far from continuous kinematics: " + fineError);
        }

        private float FallDistanceAfter(float totalTime, float startHeight, float dt) {
            DummyMovable entity = new DummyMovable();
            entity.setPosition(new Vector3(0, startHeight, 0));
            entity.setVelocity(Vector3.Zero);

            int steps = (int) System.MathF.Round(totalTime / dt);
            for (int s = 0; s < steps; s++) {
                MovementIntegrator.step(entity, dt, -1000000f);
            }
            return startHeight - entity.getPosition().Y;
        }
    }
}
