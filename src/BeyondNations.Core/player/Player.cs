using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    public class Player : Entity {
        private static readonly int MinRenderDistance = 50;
        private static readonly int MaxRenderDistance = 1000;

        private int walkSpeed;
        private int runSpeed;
        private int currentSpeed;
        private int renderDistance;

        // Movement intent, set by the host each frame from whatever the input
        // layer reports. The player no longer polls the engine for it, and no
        // longer owns a Rigidbody to push around; #220 turns this into motion.
        private float horizontalInput = 0;
        private float verticalInput = 0;
        private bool jumpRequested = false;

        // Facing, in radians. Replaces the transform rotation the Rigidbody
        // used to carry.
        private float yaw = 0f;

        private NationId nationId = null;
        private EntityId homeSettlementId = null;
        private Status status = null;
        private bool autoWalk = false;
        private float energy = 100;
        private float metabolism;

        // map of entity id to integer representing relationship strength
        private Dictionary<EntityId, int> relationships = new Dictionary<EntityId, int>();
        private EntityId currentSettlementId = null;

        public Player(int walkSpeed, int runSpeed, TickCounter tickCounter, int statusExpirationTicks, int renderDistance, RandomSource random) : base(EntityType.PLAYER, "Player") {
            this.metabolism = random.range(0.001f, 0.010f);
            this.walkSpeed = walkSpeed;
            this.runSpeed = runSpeed;
            this.currentSpeed = walkSpeed;
            this.renderDistance = renderDistance;

            setPosition(new Vector3(0, 2, 0));
            setAppearance(new Appearance(PrimitiveKind.Capsule, Vector3.One, Rgba.White));

            status = new Status(tickCounter, statusExpirationTicks);
            getInventory().addItem(ItemType.COIN, random.range(100, 400));
        }

        /**
        * Called by the host with this frame's input. Replaces the Input.GetAxis
        * and Input.GetKey polling the player used to do for itself.
        */
        public void setMovementInput(float horizontal, float vertical) {
            this.horizontalInput = horizontal;
            this.verticalInput = vertical;
        }

        public void setSprinting(bool sprinting) {
            currentSpeed = sprinting ? runSpeed : walkSpeed;
        }

        public void requestJump() {
            jumpRequested = true;
        }

        public bool consumeJumpRequest() {
            bool requested = jumpRequested;
            jumpRequested = false;
            return requested;
        }

        public float getYaw() {
            return yaw;
        }

        public int getCurrentSpeed() {
            return currentSpeed;
        }

        /**
        * The direction the player is facing, derived from yaw. This is what
        * transform.forward used to provide.
        */
        public Vector3 getForward() {
            return new Vector3(System.MathF.Sin(yaw), 0, System.MathF.Cos(yaw));
        }

        public void fixedUpdate() {
            if (isCurrentlyInSettlement()) {
                return;
            }

            if (horizontalInput != 0) {
                // Two degrees per step, as the transform rotation did.
                yaw += horizontalInput * 2f * (System.MathF.PI / 180f);
            }

            float forwardAmount = 0f;
            if (verticalInput != 0 && !autoWalk) {
                forwardAmount = verticalInput;
            }
            if (autoWalk) {
                forwardAmount = 1f;
            }
            // Only horizontal velocity is decided here. Vertical velocity
            // belongs to the movement integrator (#220) -- gravity and the
            // jump impulse -- so it is carried over rather than overwritten.
            Vector3 horizontalVelocity = getForward() * forwardAmount * currentSpeed;
            setVelocity(new Vector3(horizontalVelocity.X, getVelocity().Y, horizontalVelocity.Z));

            if (energy < 90) {
                eatIfCarryingFood();
            }

            energy -= metabolism;
        }

        public bool isGrounded() {
            int minY = 0;
            int maxY = 2;
            return getPosition().Y > minY && getPosition().Y < maxY;
        }

        public void setColor(Rgba color) {
            getAppearance().setColor(color);
        }

        public NationId getNationId() {
            return nationId;
        }

        public void setNationId(NationId nationId) {
            this.nationId = nationId;
        }

        public EntityId getHomeSettlementId() {
            return homeSettlementId;
        }

        public void setHomeSettlementId(EntityId settlementId) {
            this.homeSettlementId = settlementId;
        }

        public Status getStatus() {
            return status;
        }

        public void toggleAutoWalk() {
            autoWalk = !autoWalk;
        }

        public bool isAutoWalking() {
            return autoWalk;
        }

        public float getEnergy() {
            return energy;
        }

        public void setEnergy(float energy) {
            this.energy = energy;
        }

        /**
        * Render distance is a plain simulation value, clamped exactly as the
        * camera's far clip plane was. The camera that consumes it belongs to the
        * host and arrives with #219.
        */
        public int getRenderDistance() {
            return renderDistance;
        }

        public void increaseRenderDistance() {
            renderDistance += 10;
            if (renderDistance > MaxRenderDistance) {
                renderDistance = MaxRenderDistance;
            }
        }

        public void decreaseRenderDistance() {
            renderDistance -= 10;
            if (renderDistance < MinRenderDistance) {
                renderDistance = MinRenderDistance;
            }
        }

        public Dictionary<EntityId, int> getRelationships() {
            return relationships;
        }

        public void increaseRelationship(Entity entity, int amount) {
            if (entity == null) {
                Log.error("entity is null in increaseRelationship()");
                return;
            }
            if (entity.getId() == getId()) {
                Log.error("entity is self in increaseRelationship()");
                return;
            }
            if (getRelationships().ContainsKey(entity.getId())) {
                getRelationships()[entity.getId()] += amount;
            }
            else {
                getRelationships().Add(entity.getId(), amount);
            }
        }

        public void decreaseRelationship(Entity entity, int amount) {
            if (entity == null) {
                Log.error("entity is null in decreaseRelationship()");
                return;
            }
            if (entity.getId() == getId()) {
                Log.error("entity is self in decreaseRelationship()");
                return;
            }
            if (getRelationships().ContainsKey(entity.getId())) {
                getRelationships()[entity.getId()] -= amount;
            }
            else {
                getRelationships().Add(entity.getId(), -amount);
            }
        }

        public bool isCurrentlyInSettlement() {
            return getCurrentSettlementId() != null;
        }

        public EntityId getCurrentSettlementId() {
            return currentSettlementId;
        }

        public void setCurrentSettlementId(EntityId currentSettlementId) {
            this.currentSettlementId = currentSettlementId;
        }

        public void clearCurrentSettlementId() {
            setCurrentSettlementId(null);
        }

        /**
        * Eats the most nourishing food carried, if any. Which item types count as
        * food, and what each restores, belongs to FoodItems (#83) -- an apple is
        * no longer the only thing the player will eat.
        */
        private void eatIfCarryingFood() {
            energy += FoodItems.consumeMostNourishingFood(getInventory());
        }
    }
}
