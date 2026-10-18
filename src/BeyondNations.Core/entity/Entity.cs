using System.Numerics;

namespace beyondnations {

    /**
    * Something that exists in the world.
    *
    * An entity carries state and nothing else. Before #213 it owned the
    * GameObject that drew it, which meant creating one required a graphics
    * context and destroying one required the engine. Position, velocity and
    * appearance are now plain data, and the host builds its render state by
    * reading the entity repository rather than by being handed objects.
    *
    * Deletion is purely logical: markForDeletion sets a flag, the snapshot stops
    * including the entity, and the repository sweep removes it. No graphics
    * resource is released because none was ever held.
    */
    abstract public class Entity {
        private EntityId id;
        private EntityType type;
        private string name;
        private bool markedForDeletion = false;
        private bool visible = true;
        private Inventory inventory;

        private Vector3 position;
        private Vector3 velocity;
        private Appearance appearance;

        public Entity(EntityType type, string name) {
            this.id = new EntityId();
            this.type = type;
            this.name = name;
            this.inventory = new Inventory(0);
            this.position = Vector3.Zero;
            this.velocity = Vector3.Zero;
            this.appearance = new Appearance(PrimitiveKind.Cube, Vector3.One, Rgba.White);
        }

        public EntityId getId() {
            return id;
        }

        public EntityType getType() {
            return type;
        }

        public string getName() {
            return name;
        }

        public Vector3 getPosition() {
            return position;
        }

        public void setPosition(Vector3 position) {
            this.position = position;
        }

        /**
        * Movement is expressed as a velocity the integrator applies, in place of
        * assigning to a Rigidbody. The integrator itself arrives with #220.
        */
        public Vector3 getVelocity() {
            return velocity;
        }

        public void setVelocity(Vector3 velocity) {
            this.velocity = velocity;
        }

        public Appearance getAppearance() {
            return appearance;
        }

        protected void setAppearance(Appearance appearance) {
            this.appearance = appearance;
        }

        /**
        * Whether the host should draw this entity.
        *
        * Entering a settlement used to destroy the pawn's GameObject and leaving
        * one used to build a new one. That was a rendering operation standing in
        * for a piece of simulation state, and it is now the state itself.
        */
        public bool isVisible() {
            return visible;
        }

        public void setVisible(bool visible) {
            this.visible = visible;
        }

        public void markForDeletion() {
            markedForDeletion = true;
        }

        public bool isMarkedForDeletion() {
            return markedForDeletion;
        }

        public Inventory getInventory() {
            return inventory;
        }

        public void setInventory(Inventory inventory) {
            this.inventory = inventory;
        }
    }
}
