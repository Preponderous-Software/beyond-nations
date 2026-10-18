using System.Collections.Generic;

namespace beyondnations {

    public class LagPreventer {
        private GameConfig gameConfig;
        private TickCounter tickCounter;
        private EntityRepository entityRepository;
        private Environment environment;

        public LagPreventer(GameConfig gameConfig, TickCounter tickCounter, EntityRepository entityRepository, Environment environment) {
            this.gameConfig = gameConfig;
            this.tickCounter = tickCounter;
            this.entityRepository = entityRepository;
            this.environment = environment;
        }
        
        /**
        * Named for game objects when entities owned one. Deletion is logical
        * now, so the name follows: this marks entities and chunks, and releases
        * nothing.
        */
        public void markEntitiesForDeletion() {
            // delete excess entities and chunks every 500 ticks
            if (tickCounter.getTotalTicks() % 500 == 0) {
                deleteExcessEntities();
                deleteExcessChunks();
            }
        }

        private void deleteExcessEntities() {
            // if num entities is greater than max, delete some
            int maxNumEntities = gameConfig.getMaxNumEntities();
            int numEntities = entityRepository.getNumEntities();
            if (numEntities > maxNumEntities) {
                Log.info("Num entities (" + numEntities + ") is greater than max (" + maxNumEntities + "). Deleting some.");
                
                // whitelist of entity types to not delete
                List<EntityType> entityTypesToNotDelete = new List<EntityType>();
                entityTypesToNotDelete.Add(EntityType.PLAYER);
                entityTypesToNotDelete.Add(EntityType.PAWN);
                entityTypesToNotDelete.Add(EntityType.SETTLEMENT);

                // delete # of entities over max
                int numEntitiesToDelete = numEntities - maxNumEntities;
                for (int i = 0; i < numEntitiesToDelete; i++) {
                    Entity entityToDelete = entityRepository.getRandomEntity();
                    if (entityTypesToNotDelete.Contains(entityToDelete.getType())) {
                        Log.info("Skipping deletion of entity type: " + entityToDelete.getType());
                        continue;
                    }
                    entityToDelete.markForDeletion();
                    Log.info("Marked entity for deletion: " + entityToDelete.getId() + " " + entityToDelete.getType());
                }
            }
        }

        private void deleteExcessChunks() {
            // if num chunks is greater than max, delete some
            int maxNumChunks = gameConfig.getMaxNumChunks();
            int numChunks = environment.getNumChunks();
            if (numChunks > maxNumChunks) {
                Log.info("Num chunks (" + numChunks + ") is greater than max (" + maxNumChunks + "). Deleting some.");
                
                // delete # of chunks over max
                int numChunksToDelete = numChunks - maxNumChunks;
                for (int i = 0; i < numChunksToDelete; i++) {
                    Chunk chunkToDelete = environment.getRandomChunk();
                    environment.removeChunk(chunkToDelete);
                    Log.info("Deleted chunk: " + chunkToDelete.getPosition());
                }
            }
        }
    }
}