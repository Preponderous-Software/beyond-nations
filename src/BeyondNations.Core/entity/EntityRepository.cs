using System.Collections.Generic;

namespace beyondnations {

    public class EntityRepository {
        private RandomSource random;
        // TODO: transition to using a dictionary of dictionaries with entity type as the key for the outer dictionary
        private Dictionary<EntityId, Entity> entities;
        private List<EntityId> entityIds;
        // the same entities in the same (insertion) order, so a walk over them
        // needs no dictionary lookup per entity -- with a populated world the
        // nearest-entity searches pawns make every few ticks walk them all
        private List<Entity> orderedEntities;

        public EntityRepository(RandomSource random) {
            this.random = random;
            entities = new Dictionary<EntityId, Entity>();
            entityIds = new List<EntityId>();
            orderedEntities = new List<Entity>();
        }

        public List<Entity> getEntities() {
            // Iterates entityIds rather than entities.Values: dictionary order
            // follows EntityId's Guid hash and therefore varies between runs,
            // which makes world generation irreproducible. Insertion order does not.
            return new List<Entity>(orderedEntities);
        }

        /**
        * How many entities exist, and the one at a given position in insertion
        * order.
        *
        * getEntities() builds a fresh list on every call, which is fine for the
        * simulation but not for something that runs once a frame: the world
        * snapshot walks every entity to draw it, and copying the whole list
        * first would be an allocation proportional to the number of objects in
        * the world, which is what #218 forbids. These two let a caller walk the
        * entities in the same order without that copy.
        */
        public int getEntityCount() {
            return entityIds.Count;
        }

        public Entity getEntityAt(int index) {
            return orderedEntities[index];
        }

        public List<Entity> getEntitiesOfType(EntityType type) {
            List<Entity> entitiesOfType = new List<Entity>();
            foreach (Entity entity in orderedEntities) {
                if (entity.getType() == type) {
                    entitiesOfType.Add(entity);
                }
            }
            return entitiesOfType;
        }

        public Entity getEntity(EntityId id) {
            try {
                return entities[id];
            } catch (KeyNotFoundException) {
                return null;
            }
        }

        public void addEntity(Entity entity) {
            entities.Add(entity.getId(), entity);
            entityIds.Add(entity.getId());
            orderedEntities.Add(entity);
        }

        public void removeEntity(Entity entity) {
            if (!entities.Remove(entity.getId())) {
                return;
            }
            int index = entityIds.IndexOf(entity.getId());
            entityIds.RemoveAt(index);
            orderedEntities.RemoveAt(index);
        }

        public int getNumEntities() {
            return entities.Count;
        }

        public int getNumEntitiesOfType(EntityType type) {
            int numEntitiesOfType = 0;
            foreach (Entity entity in orderedEntities) {
                if (entity.getType() == type) {
                    numEntitiesOfType++;
                }
            }
            return numEntitiesOfType;
        }
        
        public Entity getRandomEntity() {
            if (entities.Count == 0) {
                return null;
            }
            int randomIndex = random.range(0, entities.Count);
            return entities[entityIds[randomIndex]];
        }

        // --- saving (docs/SAVES.md) ---

        /** Replaces everything with the saved entities, in their saved order. */
        internal void restore(List<Entity> saved) {
            entities.Clear();
            entityIds.Clear();
            orderedEntities.Clear();
            foreach (Entity entity in saved) {
                addEntity(entity);
            }
        }
    }
}
