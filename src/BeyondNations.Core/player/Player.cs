using System.Text.Json;
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
        private float metabolism = DefaultMetabolism;

        /**
        * The player eats carried food only below this, so a nearly full bar
        * does not swallow a meal it has no room for.
        */
        public const float EatBelowEnergy = 90f;

        /**
        * A full bar. Nothing fills the player past it -- a meal eaten at 89
        * stops here rather than at 134 -- and a save from a build that
        * allowed more loads with the bar at full.
        */
        public const float MaxEnergy = 100f;

        /**
        * The energy a new world starts the player with: a little hungry, below
        * EatBelowEnergy, so the first food picked up is eaten at once and "Eat
        * something" is met by going and getting food rather than by a minute
        * of waiting for the bar to fall (it used to start full, at 100). An
        * apple tops it back up to full. Only a new world: loads keep the saved
        * energy, and a player who respawns or a fresh Player starts at 100.
        */
        public const float NewWorldEnergy = 85f;

        /** One full bar of energy per in-game day; GameConfig overrides it. */
        public const float DefaultMetabolism = 100f / TimeOfDay.DefaultTicksPerDay;

        // map of entity id to integer representing relationship strength
        private Dictionary<EntityId, int> relationships = new Dictionary<EntityId, int>();
        private EntityId currentSettlementId = null;

        // Lifetime counters read by the objectives (objectives/). Both are
        // cumulative, so an objective that asks for one is met even if the
        // player did it before the objective came up.
        private int mealsEaten = 0;
        private int marketSales = 0;

        public Player(int walkSpeed, int runSpeed, TickCounter tickCounter, int statusExpirationTicks, int renderDistance, RandomSource random) : base(EntityType.PLAYER, "Player") {
            // Metabolism is now a fixed config value (see setMetabolism). The
            // draw that used to pick a random one is kept, discarded, so that
            // every seed still generates the same world it always has.
            random.range(0.001f, 0.010f);
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

        /**
        * Turns the player by an angle in radians; positive turns left. Mouse
        * steering (a right-button drag) uses this, on top of the A/D turn
        * applied in fixedUpdate.
        */
        public void turn(float radians) {
            yaw += radians;
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

            if (energy < EatBelowEnergy) {
                eatIfCarryingFood();
            }

            energy -= metabolism;
        }

        public float getMetabolism() {
            return metabolism;
        }

        public void setMetabolism(float metabolism) {
            this.metabolism = metabolism;
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
            this.energy = System.Math.Min(energy, MaxEnergy);
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
            int restored = FoodItems.consumeMostNourishingFood(getInventory());
            if (restored > 0) {
                energy = System.Math.Min(energy + restored, MaxEnergy);
                mealsEaten++;
            }
        }

        public int getMealsEaten() {
            return mealsEaten;
        }

        public int getMarketSales() {
            return marketSales;
        }

        public void recordMarketSale() {
            marketSales++;
        }

        // --- saving (docs/SAVES.md) ---

        internal override void saveState(SaveWriter writer) {
            writer.write("walkSpeed", walkSpeed);
            writer.write("runSpeed", runSpeed);
            writer.write("currentSpeed", currentSpeed);
            writer.write("renderDistance", renderDistance);
            writer.write("horizontalInput", horizontalInput);
            writer.write("verticalInput", verticalInput);
            writer.write("jumpRequested", jumpRequested);
            writer.write("yaw", yaw);
            writer.write("nationId", nationId);
            writer.write("homeSettlementId", homeSettlementId);
            writer.beginObject("status");
            status.save(writer);
            writer.endObject();
            writer.write("autoWalk", autoWalk);
            writer.write("energy", energy);
            writer.write("metabolism", metabolism);
            writer.writeIdIntMap("relationships", relationships);
            writer.write("currentSettlementId", currentSettlementId);
            writer.write("mealsEaten", mealsEaten);
            writer.write("marketSales", marketSales);
        }

        internal override void loadState(JsonElement element, SaveReader reader) {
            walkSpeed = SaveReader.getInt(element, "walkSpeed");
            runSpeed = SaveReader.getInt(element, "runSpeed");
            currentSpeed = SaveReader.getInt(element, "currentSpeed");
            renderDistance = SaveReader.getInt(element, "renderDistance");
            horizontalInput = SaveReader.getFloat(element, "horizontalInput");
            verticalInput = SaveReader.getFloat(element, "verticalInput");
            jumpRequested = SaveReader.getBool(element, "jumpRequested");
            yaw = SaveReader.getFloat(element, "yaw");
            nationId = reader.getNationId(element, "nationId");
            homeSettlementId = reader.getEntityId(element, "homeSettlementId");
            status.load(SaveReader.property(element, "status"));
            autoWalk = SaveReader.getBool(element, "autoWalk");
            energy = System.Math.Min(SaveReader.getFloat(element, "energy"), MaxEnergy);
            metabolism = SaveReader.getFloat(element, "metabolism");
            reader.readIdIntMap(element, "relationships", relationships);
            currentSettlementId = reader.getEntityId(element, "currentSettlementId");
            mealsEaten = SaveReader.getInt(element, "mealsEaten");
            marketSales = SaveReader.getInt(element, "marketSales");
        }
    }
}
