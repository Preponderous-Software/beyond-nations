using System.Text.Json;
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

        /**
        * Below this much energy a hungry pawn is starving: it stops walking home
        * to a market that cannot feed it and forages instead
        * (PawnBehaviorCalculator). Above it, a hungry pawn still heads home first.
        */
        public const float STARVING_ENERGY_THRESHOLD = 40f;

        private float energy = 100.00f;
        private float metabolism;

        // map of entity id to integer representing relationship strength
        private Dictionary<EntityId, int> relationships = new Dictionary<EntityId, int>();
        private EntityId currentSettlementId;
        private Vector3 millTarget;
        private bool hasMillTargetValue;

        // map of entity id to the tick it last talked with this pawn, so a
        // conversation only warms the relationship once per cooldown
        private Dictionary<EntityId, int> lastConversationTicks = new Dictionary<EntityId, int>();

        // a leader's standing request, or null
        private NationOrder order;

        // character (PawnTraits); empty for a pawn made outside world generation
        private List<PawnTrait> traits = new List<PawnTrait>();

        // sent out by a crowded settlement to found another (PopulationGrowth)
        private bool settler;

        public Pawn(Vector3 position, string name, RandomSource random) : base(EntityType.PAWN, name) {
            this.random = random;
            this.speed = random.range(10, 20);
            // 0.002-0.005 energy per tick: a full bar lasts 20,000-50,000 ticks
            // (7-17 minutes), so a pawn has time to find food. The old
            // 0.001-0.010 starved the hungriest pawns in about three minutes.
            this.metabolism = random.range(0.002f, 0.005f);

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

        /** Whether this pawn needs food and is down to its last reserves (STARVING_ENERGY_THRESHOLD). */
        public bool isStarving() {
            return energy < STARVING_ENERGY_THRESHOLD && needsFood();
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

        /** Energy burned per tick, after traits (a glutton burns more). */
        public float getMetabolism() {
            return metabolism * PawnTraits.metabolismMultiplier(this);
        }

        public bool isSettler() {
            return settler;
        }

        public void setSettler(bool settler) {
            this.settler = settler;
        }

        public IReadOnlyList<PawnTrait> getTraits() {
            return traits;
        }

        public bool hasTrait(PawnTrait trait) {
            return traits.Contains(trait);
        }

        public void setTraits(IEnumerable<PawnTrait> traits) {
            this.traits = new List<PawnTrait>();
            foreach (PawnTrait trait in traits) {
                if (!this.traits.Contains(trait)) {
                    this.traits.Add(trait);
                }
            }
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

        /**
        * Whether a conversation at this tick would warm the relationship:
        * true the first time, then only once the cooldown has passed since the
        * last conversation that did.
        */
        public bool isConversationWarm(EntityId speakerId, int tick, int cooldownTicks) {
            int lastTick;
            if (!lastConversationTicks.TryGetValue(speakerId, out lastTick)) {
                return true;
            }
            return tick - lastTick >= cooldownTicks;
        }

        public void recordConversation(EntityId speakerId, int tick) {
            lastConversationTicks[speakerId] = tick;
        }

        public NationOrder getOrder() {
            return order;
        }

        public void setOrder(NationOrder order) {
            this.order = order;
        }

        /**
        * Where the pawn is strolling to while it mills about inside a
        * settlement (SettlementLife). Means nothing outside one.
        */
        public bool hasMillTarget() {
            return hasMillTargetValue;
        }

        public Vector3 getMillTarget() {
            return millTarget;
        }

        public void setMillTarget(Vector3 millTarget) {
            this.millTarget = millTarget;
            this.hasMillTargetValue = true;
        }

        public void clearMillTarget() {
            this.hasMillTargetValue = false;
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
                return "(founding a settlement)";
            }
            else if (getCurrentBehaviorType() == BehaviorType.GO_TO_HOME_SETTLEMENT) {
                return "(going home)";
            }
            else if (getCurrentBehaviorType() == BehaviorType.PLANT_SAPLING) {
                return "(planting sapling)";
            }
            else if (getCurrentBehaviorType() == BehaviorType.SEEK_SETTLEMENT_SITE) {
                return "(looking for land to settle)";
            }
            else {
                return "(?)";
            }
        }

        // --- saving (docs/SAVES.md) ---

        internal override void saveState(SaveWriter writer) {
            writer.writeStream("random", random);
            writer.write("speed", speed);
            writer.write("nationId", nationId);
            writer.write("homeSettlementId", homeSettlementId);
            writer.write("targetEntity", targetEntity == null ? null : targetEntity.getId());
            writer.writeEnum("behavior", currentBehaviorType);
            writer.write("distanceThreshold", distanceThreshold);
            writer.write("energy", energy);
            writer.write("metabolism", metabolism);
            writer.writeIdIntMap("relationships", relationships);
            writer.write("currentSettlementId", currentSettlementId);
            writer.write("millTarget", millTarget);
            writer.write("hasMillTarget", hasMillTargetValue);
            writer.writeIdIntMap("lastConversationTicks", lastConversationTicks);
            if (order == null) {
                writer.write("order", (string) null);
            } else {
                writer.beginObject("order");
                order.save(writer);
                writer.endObject();
            }
            writer.beginArray("traits");
            foreach (PawnTrait trait in traits) {
                writer.writeValue(trait.ToString());
            }
            writer.endArray();
            writer.write("settler", settler);
        }

        internal override void loadState(JsonElement element, SaveReader reader) {
            random = reader.stream(SaveReader.property(element, "random"));
            speed = SaveReader.getInt(element, "speed");
            nationId = reader.getNationId(element, "nationId");
            homeSettlementId = reader.getEntityId(element, "homeSettlementId");
            targetEntity = reader.entity(SaveReader.property(element, "targetEntity"));
            currentBehaviorType = SaveReader.getEnum<BehaviorType>(element, "behavior");
            distanceThreshold = SaveReader.getInt(element, "distanceThreshold");
            energy = SaveReader.getFloat(element, "energy");
            metabolism = SaveReader.getFloat(element, "metabolism");
            reader.readIdIntMap(element, "relationships", relationships);
            currentSettlementId = reader.getEntityId(element, "currentSettlementId");
            millTarget = SaveReader.getVector3(element, "millTarget");
            hasMillTargetValue = SaveReader.getBool(element, "hasMillTarget");
            reader.readIdIntMap(element, "lastConversationTicks", lastConversationTicks);
            JsonElement savedOrder = SaveReader.property(element, "order");
            order = savedOrder.ValueKind == JsonValueKind.Null ? null : NationOrder.load(savedOrder, reader);
            traits = new List<PawnTrait>();
            foreach (JsonElement trait in SaveReader.property(element, "traits").EnumerateArray()) {
                traits.Add(SaveReader.toEnum<PawnTrait>(trait));
            }
            settler = SaveReader.getBool(element, "settler");
        }
    }
}
