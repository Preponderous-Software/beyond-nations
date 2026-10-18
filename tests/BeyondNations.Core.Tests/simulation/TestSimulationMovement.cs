
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * End-to-end coverage for #220: the same checks TestMovementIntegrator and
    * TestPlayer make in isolation, but driven through Simulation.fixedUpdate()
    * exactly as the host calls it, to prove the wiring -- not just the pieces
    * -- works.
    */
    public class TestSimulationMovement {
        private const float FixedDeltaTime = 1f / 60f;

        private Simulation makeSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(20260816);
            return new Simulation(config);
        }

        [Fact]
        public void testPlayerFallsFromSpawnAndSettlesAboveGround() {
            // prepare: Player's constructor spawns at y = 2, outside the
            // grounded band, so gravity has somewhere to pull it from
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            Assert.False(player.isGrounded());

            // run
            for (int i = 0; i < 300; i++) {
                simulation.fixedUpdate(FixedDeltaTime);
            }

            // check: settled, grounded, never dipped below the tile plane
            Assert.True(player.isGrounded());
            Assert.True(player.getPosition().Y >= 0f);
        }

        [Fact]
        public void testPlayerNeverFallsBelowGroundOverManyTicks() {
            // prepare
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();

            // run
            bool everBelowGround = false;
            for (int i = 0; i < 1000; i++) {
                simulation.fixedUpdate(FixedDeltaTime);
                if (player.getPosition().Y < -0.01f) {
                    everBelowGround = true;
                }
            }

            // check
            Assert.False(everBelowGround);
        }

        [Fact]
        public void testJumpRaisesThePlayerThenTheyComeBackDown() {
            // prepare: let the player land first
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            for (int i = 0; i < 120; i++) {
                simulation.fixedUpdate(FixedDeltaTime);
            }
            Assert.True(player.isGrounded());
            float groundedHeight = player.getPosition().Y;

            // run: request a jump and watch it play out
            player.requestJump();
            float maxHeight = groundedHeight;
            for (int i = 0; i < 300; i++) {
                simulation.fixedUpdate(FixedDeltaTime);
                if (player.getPosition().Y > maxHeight) {
                    maxHeight = player.getPosition().Y;
                }
            }

            // check: rose well above the resting height, and is back on the
            // ground by the end of the window
            Assert.True(maxHeight > groundedHeight + 1f, "jump did not reach a plausible height: " + maxHeight);
            Assert.True(player.isGrounded());
        }

        [Fact]
        public void testJumpOnlyTakesEffectWhileGrounded() {
            // prepare: mid-air (falling from spawn), request a jump immediately
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            Assert.False(player.isGrounded());

            // run
            player.requestJump();
            simulation.fixedUpdate(FixedDeltaTime);

            // check: the jump request was consumed but had no effect, since a
            // Rigidbody impulse never fired mid-air either -- gravity is still
            // the only thing that acted on Y this tick
            Assert.False(player.consumeJumpRequest());
            Assert.Equal(MovementIntegrator.Gravity * FixedDeltaTime, player.getVelocity().Y, 3);
        }

        [Fact]
        public void testWalkAndRunProduceDistinctDisplacementOverTheSameWindow() {
            // prepare: two independent simulations, one walking and one
            // running, both moving straight for the same amount of time
            Simulation walking = makeSimulation();
            Simulation running = makeSimulation();
            walking.getPlayer().setSprinting(false);
            running.getPlayer().setSprinting(true);
            walking.getPlayer().setMovementInput(0f, 1f);
            running.getPlayer().setMovementInput(0f, 1f);

            Vector3 walkStart = walking.getPlayer().getPosition();
            Vector3 runStart = running.getPlayer().getPosition();

            // run: past landing, then a further fixed window of forward movement
            for (int i = 0; i < 400; i++) {
                walking.getPlayer().setMovementInput(0f, 1f);
                running.getPlayer().setSprinting(true);
                running.getPlayer().setMovementInput(0f, 1f);
                walking.fixedUpdate(FixedDeltaTime);
                running.fixedUpdate(FixedDeltaTime);
            }

            // check: running covered noticeably more horizontal ground than
            // walking did, matching GameConfig's run/walk speed ratio
            float walkDistance = Horizontal(walking.getPlayer().getPosition() - walkStart);
            float runDistance = Horizontal(running.getPlayer().getPosition() - runStart);
            Assert.True(runDistance > walkDistance * 1.5f, "run distance " + runDistance + " was not clearly faster than walk distance " + walkDistance);
        }

        private float Horizontal(Vector3 v) {
            return new Vector3(v.X, 0, v.Z).Length();
        }
    }
}
