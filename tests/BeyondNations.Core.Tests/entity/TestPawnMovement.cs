
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Covers the movement half of Pawn -- the half #220 adds -- separately
    * from TestPawn's construction/state coverage.
    */
    public class TestPawnMovement {
        private readonly RandomSource random = new RandomSource(20260816);

        [Fact]
        public void testMoveTowardsTargetSetsHorizontalVelocityAtItsOwnSpeed() {
            // prepare
            Pawn pawn = new Pawn(new Vector3(0, 1.5f, 0), "Pawn", random);
            Pawn target = new Pawn(new Vector3(100, 1.5f, 0), "Target", random);
            pawn.setTargetEntity(target);

            // run
            pawn.moveTowardsTargetEntity();

            // check: moving straight toward +X at exactly the pawn's own speed
            Vector3 velocity = pawn.getVelocity();
            Assert.Equal((float) pawn.getSpeed(), velocity.X, 3);
            Assert.Equal(0f, velocity.Z, 4);
        }

        [Fact]
        public void testMoveTowardsTargetPreservesVerticalVelocity() {
            // prepare: pawn already falling
            Pawn pawn = new Pawn(new Vector3(0, 1.5f, 0), "Pawn", random);
            Pawn target = new Pawn(new Vector3(100, 1.5f, 0), "Target", random);
            pawn.setTargetEntity(target);
            pawn.setVelocity(new Vector3(0, -3.2f, 0));

            // run
            pawn.moveTowardsTargetEntity();

            // check
            Assert.Equal(-3.2f, pawn.getVelocity().Y, 4);
        }

        [Fact]
        public void testMoveTowardsTargetIgnoresTargetsVerticalOffset() {
            // prepare: target is far above -- a pawn should never be given
            // upward velocity just because its target happens to be elevated;
            // that is the integrator's job (gravity/ground clamp), not
            // pathing's.
            Pawn pawn = new Pawn(new Vector3(0, 1.5f, 0), "Pawn", random);
            Pawn target = new Pawn(new Vector3(10, 500f, 0), "Target", random);
            pawn.setTargetEntity(target);

            // run
            pawn.moveTowardsTargetEntity();

            // check
            Assert.Equal(0f, pawn.getVelocity().Y, 4);
        }

        [Fact]
        public void testPawnClosesDistanceToTargetAndStopsWithinThreshold() {
            // prepare: repeatedly recompute the steering velocity and hand it
            // to the same integrator the player uses, exactly as
            // PawnBehaviorExecutor and Simulation do each fixed tick.
            Pawn pawn = new Pawn(new Vector3(0, 1.5f, 0), "Pawn", random);
            Pawn target = new Pawn(new Vector3(80, 1.5f, 0), "Target", random);
            pawn.setTargetEntity(target);

            float dt = 1f / 60f;
            float previousDistance = Vector3.Distance(pawn.getPosition(), target.getPosition());
            bool reachedThreshold = false;

            for (int i = 0; i < 6000 && !reachedThreshold; i++) {
                pawn.moveTowardsTargetEntity();
                MovementIntegrator.step(pawn, dt, 1.5f);

                float distance = Vector3.Distance(pawn.getPosition(), target.getPosition());
                // check: distance is non-increasing (steering never overshoots
                // and circles back out) once the ground clamp has settled
                Assert.True(distance <= previousDistance + 0.01f);
                previousDistance = distance;

                if (pawn.isAtTargetEntity()) {
                    reachedThreshold = true;
                }
            }

            // check
            Assert.True(reachedThreshold, "pawn never reached its target within the distance threshold");
        }
    }
}
