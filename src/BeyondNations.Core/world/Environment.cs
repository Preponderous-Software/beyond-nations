using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /*
    * An environment is a collection of chunks.
    * It is the world.
    */
    public class Environment {
        private RandomSource random;
        private EnvironmentId id;
        private List<Chunk> chunks = new List<Chunk>();
        private EntityRepository entityRepository;

        public Environment(int chunkSize, int locationScale, EntityRepository entityRepository, RandomSource random) {
            this.random = random;
            this.entityRepository = entityRepository;
            this.id = new EnvironmentId();
            // create initial chunk
            Chunk chunk = new Chunk(0, 0, chunkSize, locationScale, random);
            addChunk(chunk);
        }

        /**
        * The chunks making up the world, in generation order. The host reads
        * this through WorldSnapshot to draw the ground.
        */
        public List<Chunk> getChunks() {
            return chunks;
        }

        public void addChunk(Chunk chunk) {
            chunks.Add(chunk);
        }

        public void removeChunk(Chunk chunk) {
            // Removal is purely logical now. Nothing has to be destroyed,
            // because the chunk never held a graphics resource.
            chunks.Remove(chunk);
        }

        public Chunk getChunk(int xpos, int zpos) {
            foreach (Chunk chunk in chunks) {
                if (chunk.getX() == xpos && chunk.getZ() == zpos) {
                    return chunk;
                }
            }
            return null;
        }

        public int getNumChunks() {
            return chunks.Count;
        }

        public int getChunkSize() {
            return chunks[0].getSize();
        }

        public int getLocationScale() {
            return chunks[0].getLocationScale();
        }

        public AppleTree getNearestTree(Vector3 position) {
            return (AppleTree)getNearestEntityOfType(position, EntityType.TREE);
        }

        public Rock getNearestRock(Vector3 position) {
            return (Rock)getNearestEntityOfType(position, EntityType.ROCK);
        }

        public Entity getNearestEntityOfType(Vector3 position, EntityType type) {
            List<Entity> entities = entityRepository.getEntitiesOfType(type);
            if (entities.Count == 0) {
                return null;
            }

            // find nearest entity
            Entity nearestEntity = null;
            float nearestDistance = float.MaxValue;
            foreach (Entity entity in entities) {
                if (entity.getType() == EntityType.PAWN) {
                    Pawn pawn = (Pawn) entity;
                    if (pawn.isCurrentlyInSettlement()) {
                        continue;
                    }
                }
                if (entity.getType() == type) {
                    float distance = Vector3.Distance(position, entity.getPosition());
                    if (distance < nearestDistance) {
                        nearestDistance = distance;
                        nearestEntity = entity;
                    }
                }
            }
            return nearestEntity;
        }

        public Chunk getChunkAtPosition(Vector3 position) {
            int chunkSize = getChunkSize();
            int locationScale = getLocationScale();
            int xpos = (int)(position.X / (chunkSize * locationScale));
            int zpos = (int)(position.Z / (chunkSize * locationScale));
            return getChunk(xpos, zpos);
        }

        public Chunk getRandomChunk() {
            if (chunks.Count == 0) {
                return null;
            }
            int randomIndex = random.range(0, chunks.Count);
            return chunks[randomIndex];
        }
    }
}