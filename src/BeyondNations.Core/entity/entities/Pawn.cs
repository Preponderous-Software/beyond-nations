using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    public class Pawn : Entity {
        private RandomSource random;
        private int speed;
        private NationId nationId;
        private EntityId homeSettlementId;
        private Entity targetEntity;
        private BehaviorType currentBehaviorType = BehaviorType.NONE;
        
        private int distanceThreshold = 10;

        // Below this much energy a pawn with nothing edible to hand goes looking for food.
        public const float HUNGRY_ENERGY_THRESHOLD = 80f;

        private float energy = 100.00f;
        private float metabolism;

        // map of entity id to integer representing relationship strength
        private Dictionary<EntityId, int> relationships = new Dictionary<EntityId, int>();
        private EntityId currentSettlementId;

        public Pawn(Vector3 position, string name, RandomSource random) : base(EntityType.PAWN, name) {
            this.random = random;
            this.speed = random.range(10, 20);
            this.metabolism = random.range(0.001f, 0.010f);

            setPosition(position);
            setAppearance(new Appearance(PrimitiveKind.Capsule, Vector3.One, Rgba.Gray));
            getAppearance().setLabel(name);
            int startingGoldCoins = random.range(50, 200);
            getInventory().addItem(ItemType.COIN, startingGoldCoins);
        }

        public int getSpeed() {
            return speed;
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

        public Dictionary<EntityId, int> getRelationships() {
            return relationships;
        }

        public bool hasTargetEntity() {
            return targetEntity != null;
        }

        public Entity getTargetEntity() {
            return targetEntity;
        }

        public void setTargetEntity(Entity targetEntity) {
            this.targetEntity = targetEntity;
        }

        public void moveTowardsTargetEntity() {
            if (targetEntity == null) {
                return;
            }
            if (targetEntity.isMarkedForDeletion()) {
                setTargetEntity(null);
                return;
            }

            // Horizontal only: vertical velocity belongs to the movement
            // integrator (#220), which this and PawnBehaviorExecutor's wander
            // share with the player so both move the same way.
            Vector3 toTarget = targetEntity.getPosition() - getPosition();
            toTarget.Y = 0;
            Vector3 horizontalVelocity = VectorMath.normalized(toTarget) * getSpeed();
            setVelocity(new Vector3(horizontalVelocity.X, getVelocity().Y, horizontalVelocity.Z));
        }

        public bool isAtTargetEntity() {
            return isAtTargetEntity(distanceThreshold);
        }

        public bool isAtTargetEntity(int distanceThreshold) {
            if (targetEntity == null) {
                return false;
            }
            if (targetEntity.isMarkedForDeletion()) {
                setTargetEntity(null);
                return false;
            }
            return (targetEntity.getPosition() - getPosition()).Length() < distanceThreshold;
        }

        public void setColor(Rgba color) {
            getAppearance().setColor(color);
        }

        public float getEnergy() {
            return energy;
        }

        public void setEnergy(float energy) {
            this.energy = energy;
        }

        /**
        * Whether the pawn should be looking for something to eat. Any edible
        * item counts, not just an apple (#83) -- a pawn carrying chicken meat
        * will eat it on the next energy step, so it has no reason to go
        * looking for food. Shared by the behavior calculator, which decides
        * whether to buy food, and the executor, which decides whether to hunt
        * for it (#253).
        */
        public bool needsFood() {
            return energy < HUNGRY_ENERGY_THRESHOLD && !FoodItems.hasFood(getInventory());
        }

        public BehaviorType getCurrentBehaviorType() {
            return currentBehaviorType;
        }

        public void setCurrentBehaviorType(BehaviorType currentBehaviorType) {
            this.currentBehaviorType = currentBehaviorType;
        }

        public void setNameTag(string name) {
            getAppearance().setLabel(name);
        }

        public string getNameTag() {
            return getAppearance().getLabel();
        }

        public float getMetabolism() {
            return metabolism;
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
            return currentSettlementId != null;
        }

        public EntityId getCurrentSettlementId() {
            return currentSettlementId;
        }

        public void setCurrentSettlementId(EntityId currentSettlementId) {
            this.currentSettlementId = currentSettlementId;
        }

        public void clearCurrentSettlementId() {
            currentSettlementId = null;
        }

        public string getCurrentBehaviorDescription() {
            if (getCurrentBehaviorType() == BehaviorType.NONE) {
                return "(doing nothing)";
            }
            else if (getCurrentBehaviorType() == BehaviorType.GATHER_RESOURCES) {
                return "(gathering resources)";
            }
            else if (getCurrentBehaviorType() == BehaviorType.SELL_RESOURCES) {
                return "(selling resources)";
            }
            else if (getCurrentBehaviorType() == BehaviorType.WANDER) {
                return "(wandering)";
            }
            else if (getCurrentBehaviorType() == BehaviorType.PURCHASE_FOOD) {
                return "(purchasing food)";
            }
            else if (getCurrentBehaviorType() == BehaviorType.CONSTRUCT_SETTLEMENT) {
                return "(creating settlementing)";
            }
            else if (getCurrentBehaviorType() == BehaviorType.GO_TO_HOME_SETTLEMENT) {
                return "(going home)";
            }
            else if (getCurrentBehaviorType() == BehaviorType.PLANT_SAPLING) {
                return "(planting sapling)";
            }
            else {
                return "(?)";
            }
        }
    }
}