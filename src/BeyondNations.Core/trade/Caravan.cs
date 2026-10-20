using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace beyondnations {

    public enum CaravanState {
        /** On the road to the market it means to sell at. */
        OUTBOUND,
        /** Sold up (or turned away), heading home with the takings. */
        RETURNING,
    }

    /**
    * A trade caravan: a cart and its drover, in a nation's colours, carrying
    * goods from one of the nation's settlements to another settlement's
    * market and coming home with the takings.
    *
    * What it carries is its inventory: the goods it set out with and the
    * coins it has made (its purse). Where it goes and what it does when it
    * gets there is TradeCaravans' business; the caravan only knows its route,
    * walks it and draws itself.
    */
    public class Caravan : Entity {
        /** How fast a caravan rolls, in units a second: a third of the player's walk. */
        public const float SPEED = 8f;

        /** Close enough to a settlement's centre to have arrived. */
        public const float ARRIVAL_DISTANCE = 6f;

        private static readonly Rgba CartBrown = new Rgba(0.45f, 0.3f, 0.15f);
        private static readonly Rgba WheelBrown = new Rgba(0.25f, 0.17f, 0.1f);
        private static readonly Rgba Canvas = new Rgba(0.88f, 0.84f, 0.72f);

        private NationId nationId;
        private Rgba color;
        private EntityId originId;
        private EntityId destinationId;
        private CaravanState state = CaravanState.OUTBOUND;
        private EntityId escortId;
        private bool playerOwned;
        private int earnings;
        private int departedTick;
        private bool facingX;

        public Caravan(Vector3 position, Nation nation, EntityId originId, EntityId destinationId, int departedTick)
                : base(EntityType.CARAVAN, "Caravan") {
            this.nationId = nation == null ? null : nation.getId();
            this.color = nation == null ? Rgba.Gray : nation.getColor();
            this.originId = originId;
            this.destinationId = destinationId;
            this.departedTick = departedTick;
            setPosition(new Vector3(position.X, 0f, position.Z));
            setAppearance(new Appearance());
            buildAppearance();
        }

        public NationId getNationId() { return nationId; }
        public Rgba getColor() { return color; }
        public EntityId getOriginId() { return originId; }
        public EntityId getDestinationId() { return destinationId; }
        public CaravanState getState() { return state; }
        public EntityId getEscortId() { return escortId; }
        public bool isPlayerOwned() { return playerOwned; }
        public int getEarnings() { return earnings; }
        public int getDepartedTick() { return departedTick; }

        public void setPlayerOwned(bool playerOwned) { this.playerOwned = playerOwned; }
        public void setEscortId(EntityId escortId) { this.escortId = escortId; }
        public void addEarnings(int coins) { this.earnings += coins; }

        public void setState(CaravanState state) {
            this.state = state;
        }

        /** Where it is going now: the market on the way out, home on the way back. */
        public EntityId getCurrentTargetId() {
            return state == CaravanState.OUTBOUND ? destinationId : originId;
        }

        /** Sends it somewhere else: home when its market is gone, or to another of its towns when home is. */
        public void redirectHome(EntityId newOriginId) {
            this.originId = newOriginId;
            this.state = CaravanState.RETURNING;
        }

        /** The goods it carries, every slot but coin, in inventory order. */
        public List<ItemStack> getCargo() {
            List<ItemStack> cargo = new List<ItemStack>();
            foreach (ItemSlot slot in getInventory().getSlots()) {
                if (slot.isEmpty() || slot.getItemStack().getItemType() == ItemType.COIN) {
                    continue;
                }
                cargo.Add(new ItemStack(slot.getItemStack().getItemType(), slot.getQuantity()));
            }
            return cargo;
        }

        public int getCargoCount() {
            int count = 0;
            foreach (ItemStack stack in getCargo()) {
                count += stack.getQuantity();
            }
            return count;
        }

        public int getPurse() {
            return getInventory().getNumItems(ItemType.COIN);
        }

        /** "15 wood, 5 stone", or "nothing" for an empty cart. */
        public string describeCargo() {
            List<string> parts = new List<string>();
            foreach (ItemStack stack in getCargo()) {
                parts.Add(ItemNames.describeQuantity(stack.getItemType(), stack.getQuantity()));
            }
            return parts.Count == 0 ? "nothing" : string.Join(", ", parts);
        }

        /**
        * Rolls toward a point across the ground for one tick. Returns whether
        * it has arrived. Caravans keep to the road rather than to physics: the
        * ground is flat (#220), so they never fall and need no integrator.
        */
        public bool rollToward(Vector3 target, float deltaTime) {
            Vector3 here = getPosition();
            Vector3 toTarget = new Vector3(target.X - here.X, 0f, target.Z - here.Z);
            float distance = toTarget.Length();
            if (distance <= ARRIVAL_DISTANCE) {
                setVelocity(Vector3.Zero);
                return true;
            }
            float stepLength = Math.Min(distance, SPEED * deltaTime);
            Vector3 direction = toTarget / distance;
            setVelocity(direction * SPEED);
            setPosition(new Vector3(here.X + direction.X * stepLength, 0f, here.Z + direction.Z * stepLength));
            bool alongX = Math.Abs(direction.X) > Math.Abs(direction.Z);
            if (alongX != facingX) {
                facingX = alongX;
                buildAppearance();
            }
            return Vector3.Distance(new Vector3(getPosition().X, 0f, getPosition().Z), new Vector3(target.X, 0f, target.Z)) <= ARRIVAL_DISTANCE;
        }

        public void setLabel(string label) {
            getAppearance().setLabel(label);
        }

        /**
        * A cart with four wheels, a canvas bale and a pack in the nation's
        * colour, and its drover walking alongside -- built only from the
        * primitives everything else is, along whichever axis it is travelling.
        */
        private void buildAppearance() {
            Appearance a = getAppearance();
            a.clearParts();
            part(a, PrimitiveKind.Cube, 0f, 0.95f, 0f, 1.6f, 0.5f, 2.6f, CartBrown);       // bed
            part(a, PrimitiveKind.Cube, 0f, 1.45f, -0.5f, 1.3f, 0.6f, 1.2f, color);       // pack
            part(a, PrimitiveKind.Sphere, 0f, 1.5f, 0.6f, 1.1f, 0.8f, 0.9f, Canvas);      // bale
            part(a, PrimitiveKind.Cube, 0f, 0.85f, 2.0f, 0.15f, 0.15f, 1.6f, CartBrown);   // shaft
            foreach (float side in new[] { -0.9f, 0.9f }) {
                foreach (float end in new[] { -0.85f, 0.85f }) {
                    part(a, PrimitiveKind.Sphere, side, 0.45f, end, 0.2f, 0.9f, 0.9f, WheelBrown);
                }
            }
            part(a, PrimitiveKind.Capsule, 1.3f, 1.0f, 2.4f, 0.8f, 1f, 0.8f, color);       // drover
            part(a, PrimitiveKind.Sphere, 1.3f, 2.05f, 2.4f, 0.5f, 0.5f, 0.5f, Canvas);   // drover's hat
            a.setLabelOffset(new Vector3(0f, 3.6f, 0f));
        }

        /** One part, laid along z, or turned onto x when the caravan travels that way. */
        private void part(Appearance a, PrimitiveKind kind, float x, float y, float z, float sx, float sy, float sz, Rgba c) {
            if (facingX) {
                a.addPart(kind, new Vector3(z, y, x), new Vector3(sz, sy, sx), c);
            }
            else {
                a.addPart(kind, new Vector3(x, y, z), new Vector3(sx, sy, sz), c);
            }
        }

        // --- saving (docs/SAVES.md) ---

        internal override void saveState(SaveWriter writer) {
            writer.write("nationId", nationId);
            writer.write("color", color);
            writer.write("originId", originId);
            writer.write("destinationId", destinationId);
            writer.writeEnum("state", state);
            writer.write("escortId", escortId);
            writer.write("playerOwned", playerOwned);
            writer.write("earnings", earnings);
            writer.write("departedTick", departedTick);
            writer.write("facingX", facingX);
        }

        internal override void loadState(JsonElement element, SaveReader reader) {
            nationId = reader.getNationId(element, "nationId");
            color = SaveReader.getRgba(element, "color");
            originId = reader.getEntityId(element, "originId");
            destinationId = reader.getEntityId(element, "destinationId");
            state = SaveReader.getEnum<CaravanState>(element, "state");
            escortId = reader.getEntityId(element, "escortId");
            playerOwned = SaveReader.getBool(element, "playerOwned");
            earnings = SaveReader.getInt(element, "earnings");
            departedTick = SaveReader.getInt(element, "departedTick");
            facingX = SaveReader.getBool(element, "facingX");
        }
    }
}
