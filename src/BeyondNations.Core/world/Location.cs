using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * A location is a single point in the world.
    * It is a part of a chunk.
    *
    * A location used to own the cube that drew it. It now carries the colour and
    * size as data, and the host draws the ground from the world snapshot.
    */
    public class Location {
        private LocationId id;
        private Vector3 position;
        private int scale;
        private string name;
        private Rgba color;
        private List<EntityId> entityIds = new List<EntityId>();

        public Location(int xpos, int zpos, int scale, RandomSource random) {
            this.id = new LocationId();
            this.position = new Vector3(xpos * scale, 0, zpos * scale);
            this.scale = scale;
            this.name = "Location_" + xpos + "_" + zpos;
            // random green, as before
            this.color = new Rgba(0, random.value(), 0);
        }

        public LocationId getId() {
            return id;
        }

        public string getName() {
            return name;
        }

        public Vector3 getPosition() {
            return position;
        }

        public int getScale() {
            return scale;
        }

        /**
        * The tile's size as the host needs it: wide and deep by its scale, one
        * unit tall.
        */
        public Vector3 getScaleVector() {
            return new Vector3(scale, 1, scale);
        }

        public Rgba getColor() {
            return color;
        }

        public void setColor(Rgba color) {
            this.color = color;
        }

        public void addEntityId(EntityId entityId) {
            entityIds.Add(entityId);
        }

        public void removeEntityId(EntityId entityId) {
            entityIds.Remove(entityId);
        }

        public bool isEntityPresent(Entity entity) {
            return entityIds.Contains(entity.getId());
        }

        public int getNumberOfEntities() {
            return entityIds.Count;
        }
    }
}
