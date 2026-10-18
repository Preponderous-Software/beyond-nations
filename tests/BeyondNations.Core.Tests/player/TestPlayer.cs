
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestPlayer {
        private readonly RandomSource random = new RandomSource(20260816);

        private Player makePlayer(int walkSpeed, int runSpeed) {
            TickCounter tickCounter = new TickCounter();
            return new Player(walkSpeed, runSpeed, tickCounter, 500, 200, random);
        }

        [Fact]
        public void testWalkingMatchesConfiguredWalkSpeed() {
            // prepare: the same ratio GameConfig ships with (25 / 50)
            Player player = makePlayer(25, 50);

            // run
            player.setMovementInput(0f, 1f);
            player.setSprinting(false);
            player.fixedUpdate();

            // check: horizontal speed equals walk speed, vertical untouched by input
            Vector3 velocity = player.getVelocity();
            Assert.Equal(25f, new Vector3(velocity.X, 0, velocity.Z).Length(), 3);
        }

        [Fact]
        public void testRunningMatchesConfiguredRunSpeed() {
            // prepare
            Player player = makePlayer(25, 50);

            // run
            player.setMovementInput(0f, 1f);
            player.setSprinting(true);
            player.fixedUpdate();

            // check
            Vector3 velocity = player.getVelocity();
            Assert.Equal(50f, new Vector3(velocity.X, 0, velocity.Z).Length(), 3);
        }

        [Fact]
        public void testRunSpeedIsExactlyConfiguredRatioOfWalkSpeed() {
            // prepare
            int walkSpeed = 25;
            int runSpeed = 50;
            Player player = makePlayer(walkSpeed, runSpeed);
            player.setMovementInput(0f, 1f);

            // run: walk
            player.setSprinting(false);
            player.fixedUpdate();
            float walkVelocity = new Vector3(player.getVelocity().X, 0, player.getVelocity().Z).Length();

            // run: run
            player.setSprinting(true);
            player.fixedUpdate();
            float runVelocity = new Vector3(player.getVelocity().X, 0, player.getVelocity().Z).Length();

            // check
            Assert.Equal((float) runSpeed / walkSpeed, runVelocity / walkVelocity, 3);
        }

        [Fact]
        public void testFixedUpdateOnlySetsHorizontalVelocityAndPreservesVertical() {
            // prepare: mid-air, falling
            Player player = makePlayer(25, 50);
            player.setVelocity(new Vector3(0, -7.5f, 0));

            // run
            player.setMovementInput(0f, 1f);
            player.fixedUpdate();

            // check: vertical component is exactly what it was -- fixedUpdate()
            // never touches it, since gravity and the ground clamp own it
            Assert.Equal(-7.5f, player.getVelocity().Y, 4);
        }

        [Fact]
        public void testStandingStillProducesZeroHorizontalVelocity() {
            // prepare
            Player player = makePlayer(25, 50);

            // run
            player.setMovementInput(0f, 0f);
            player.fixedUpdate();

            // check
            Vector3 velocity = player.getVelocity();
            Assert.Equal(0f, velocity.X, 4);
            Assert.Equal(0f, velocity.Z, 4);
        }

        [Theory]
        [InlineData(-1f, false)]
        [InlineData(0f, false)]
        [InlineData(0.01f, true)]
        [InlineData(1f, true)]
        [InlineData(1.99f, true)]
        [InlineData(2f, false)]
        [InlineData(3f, false)]
        public void testIsGroundedMatchesTheDocumentedBand(float y, bool expectedGrounded) {
            // prepare
            Player player = makePlayer(25, 50);
            player.setPosition(new Vector3(0, y, 0));

            // run / check
            Assert.Equal(expectedGrounded, player.isGrounded());
        }

        [Fact]
        public void testJumpRequestIsConsumedExactlyOnce() {
            // prepare
            Player player = makePlayer(25, 50);

            // run / check
            Assert.False(player.consumeJumpRequest());

            player.requestJump();
            Assert.True(player.consumeJumpRequest());
            Assert.False(player.consumeJumpRequest());
        }

        [Fact]
        public void testHungryPlayerEatsAnApple() {
            // prepare: energy under the 90 threshold that triggers a meal
            Player player = makePlayer(25, 50);
            player.setEnergy(50);
            player.getInventory().addItem(ItemType.APPLE, 2);

            // run
            player.fixedUpdate();

            // check: one apple gone, its energy gained less one step of metabolism
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.True(player.getEnergy() > 50 + FoodItems.getEnergyRestored(ItemType.APPLE) - 1);
        }

        [Fact]
        public void testHungryPlayerEatsChickenMeat() {
            // prepare: meat and nothing else edible, the case that used to leave
            // the player starving beside a full inventory (#83)
            Player player = makePlayer(25, 50);
            player.setEnergy(50);
            player.getInventory().addItem(ItemType.CHICKEN_MEAT, 2);

            // run
            player.fixedUpdate();

            // check
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
            Assert.True(player.getEnergy() > 50 + FoodItems.getEnergyRestored(ItemType.CHICKEN_MEAT) - 1);
        }

        [Fact]
        public void testWellFedPlayerEatsNothing() {
            // prepare: above the threshold
            Player player = makePlayer(25, 50);
            player.setEnergy(95);
            player.getInventory().addItem(ItemType.CHICKEN_MEAT, 2);
            player.getInventory().addItem(ItemType.APPLE, 2);

            // run
            player.fixedUpdate();

            // check
            Assert.Equal(2, player.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Equal(2, player.getInventory().getNumItems(ItemType.APPLE));
        }

        [Fact]
        public void testHungryPlayerWithNoFoodEatsNothingAndKeepsItsMaterials() {
            // prepare
            Player player = makePlayer(25, 50);
            player.setEnergy(50);
            player.getInventory().addItem(ItemType.WOOD, 5);

            // run
            player.fixedUpdate();

            // check: wood is not food, and energy only fell by metabolism
            Assert.Equal(5, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.True(player.getEnergy() < 50);
        }
    }
}
