using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * The WorldGenerator class is responsible for generating the land.
    */
    public class WorldGenerator {
        private PawnNameGenerator pawnNameGenerator;
        private RandomSource random;
        private Environment environment;
        private Player player;
        private EventProducer eventProducer;
        private int chunkSize = 5;
        private int locationScale = 3;
        private int currentChunkX = 0;
        private int currentChunkZ = 0;
        private EntityRepository entityRepository;
        private GameConfig gameConfig;
        // Traits come from their own stream (PawnTraits), so they never shift the rest of a seeded world.
        private RandomSource traitRandom;

        public WorldGenerator(Environment environment, Player player, EventProducer eventProducer, EntityRepository entityRepository, GameConfig gameConfig, RandomSource random, PawnNameGenerator pawnNameGenerator) {
            this.pawnNameGenerator = pawnNameGenerator;
            this.random = random;
            this.environment = environment;
            this.player = player;
            this.eventProducer = eventProducer;
            this.chunkSize = environment.getChunkSize();
            this.locationScale = environment.getLocationScale();
            this.entityRepository = entityRepository;
            this.gameConfig = gameConfig;
        }

        public void setTraitRandom(RandomSource traitRandom) {
            this.traitRandom = traitRandom;
        }

        public void update() {
            calculateCurrentChunk();
            generateCurrentChunk();
            generateSurroundingChunksAt(currentChunkX, currentChunkZ);
        }

        public int getCurrentChunkX() {
            return currentChunkX;
        }

        public int getCurrentChunkZ() {
            return currentChunkZ;
        }

        private void generateCurrentChunk() {
            generateChunkIfNotExistent(currentChunkX, currentChunkZ);
        }

        public void generateSurroundingChunksAt(int chunkX, int chunkZ) {
            generateChunkIfNotExistent(chunkX - 1, chunkZ - 1);
            generateChunkIfNotExistent(chunkX - 1, chunkZ);
            generateChunkIfNotExistent(chunkX - 1, chunkZ + 1);
            generateChunkIfNotExistent(chunkX, chunkZ - 1);
            // generateChunkIfNotExistent(chunkX, chunkZ);
            generateChunkIfNotExistent(chunkX, chunkZ + 1);
            generateChunkIfNotExistent(chunkX + 1, chunkZ - 1);
            generateChunkIfNotExistent(chunkX + 1, chunkZ);
            generateChunkIfNotExistent(chunkX + 1, chunkZ + 1);
        }

        private bool generateChunkIfNotExistent(int chunkX, int chunkZ) {
            Chunk chunk = environment.getChunk(chunkX, chunkZ);
            if (chunk == null) {
                createNewChunkAt(chunkX, chunkZ);
                return true;
            }
            return false;
        }

        /**
        * Calculates the current chunk based on the player position.
        */
        private void calculateCurrentChunk() {
            Vector3 playerPosition = player.getPosition();
            int lengthOfChunk = chunkSize * locationScale;

            if (playerPosition.X >= 0) {
                currentChunkX = (int) (playerPosition.X / lengthOfChunk);
            } else {
                currentChunkX = (int) (playerPosition.X / lengthOfChunk) - 1;
            }

            if (playerPosition.Z >= 0) {
                currentChunkZ = (int) (playerPosition.Z / lengthOfChunk);
            } else {
                currentChunkZ = (int) (playerPosition.Z / lengthOfChunk) - 1;
            }
        }

        private void createNewChunkAt(int chunkX, int chunkZ) {
            // produce event
            eventProducer.produceChunkGenerateEvent(chunkX, chunkZ);

            // create new chunk
            Chunk chunk = new Chunk(chunkX, chunkZ, chunkSize, locationScale, random);
            environment.addChunk(chunk);
            spawnTreeEntities(chunk);
            spawnRockEntities(chunk);
            spawnPawns(chunk);
            spawnChickens(chunk);
        }

        private void spawnTreeEntities(Chunk chunk) {
            int numberOfTrees = random.range(chunk.getSize(), chunk.getSize() * 2);
            for (int i = 0; i < numberOfTrees; i++) {
                Location randomLocation = chunk.getRandomLocation();
                if (randomLocation.getNumberOfEntities() > 0) {
                    continue;
                }

                Vector3 locationPosition = randomLocation.getPosition();

                // create tree
                Vector3 position = new Vector3(locationPosition.X, locationPosition.Y + 1, locationPosition.Z);
                AppleTree tree = new AppleTree(position, 5, random);
                entityRepository.addEntity(tree);
            }
        }

        /** Ticks between rock regrowth attempts: 30 seconds at 50 ticks a second. */
        public const int ROCK_REGROW_INTERVAL_TICKS = 1500;

        /** Regrowth stops once the world holds this many rocks per generated chunk. */
        public const int TARGET_ROCKS_PER_CHUNK = 3;

        private void spawnRockEntities(Chunk chunk) {
            // Three to six attempts a chunk (a 7x7 chunk), was one or two.
            int numberOfRocks = random.range(chunk.getSize()/2, chunk.getSize());
            for (int i = 0; i < numberOfRocks; i++) {
                trySpawnRock(chunk);
            }
        }

        private bool trySpawnRock(Chunk chunk) {
            Location randomLocation = chunk.getRandomLocation();
            if (randomLocation.getNumberOfEntities() > 0) {
                return false;
            }

            Vector3 locationPosition = randomLocation.getPosition();
            Vector3 position = new Vector3(locationPosition.X, locationPosition.Y + 1, locationPosition.Z);
            entityRepository.addEntity(new Rock(position));
            return true;
        }

        /**
        * Rocks used to be gone for good once mined, while trees come back from
        * saplings, so a busy world ran out of stone. Called every tick: every
        * ROCK_REGROW_INTERVAL_TICKS it surfaces one rock in a random generated
        * chunk, until the world holds TARGET_ROCKS_PER_CHUNK per chunk.
        * Returns whether a rock was added.
        */
        public bool regrowRocks(int totalTicks) {
            if (totalTicks % ROCK_REGROW_INTERVAL_TICKS != 0) {
                return false;
            }
            Chunk chunk = environment.getRandomChunk();
            if (chunk == null) {
                return false;
            }
            int numRocks = entityRepository.getEntitiesOfType(EntityType.ROCK).Count;
            if (numRocks >= environment.getNumChunks() * TARGET_ROCKS_PER_CHUNK) {
                return false;
            }
            return trySpawnRock(chunk);
        }

        private void spawnPawns(Chunk chunk) {
            int numPawns = rollNumberOfPawns(random.range(0, 100));
            for (int i = 0; i < numPawns; i++) {
                if (gameConfig.getLagPreventionEnabled() && entityRepository.getNumEntitiesOfType(EntityType.PAWN) >= gameConfig.getMaxNumPawns()) {
                    return;
                }
                Location randomLocation = chunk.getRandomLocation();
                Vector3 locationPosition = randomLocation.getPosition();

                // create pawn
                Vector3 position = new Vector3(locationPosition.X, (float)(locationPosition.Y + 1.5), locationPosition.Z);
                Pawn pawn = new Pawn(position, pawnNameGenerator.generate(), random);
                if (traitRandom != null) {
                    pawn.setTraits(PawnTraits.roll(traitRandom));
                }
                eventProducer.producePawnSpawnEvent(position, pawn);
                entityRepository.addEntity(pawn);
            }
        }

        /** Percent chance that a new chunk has at least one pawn, and that it has two. */
        public const int ONE_PAWN_CHANCE_PERCENT = 40;
        public const int TWO_PAWNS_CHANCE_PERCENT = 15;

        /**
        * How many pawns a new chunk starts with, from a roll in [0, 100). It
        * used to be a 10% chance of one, which left a fresh world of nine
        * chunks with a single pawn -- too few for a market, let alone two
        * nations. Now about half of all chunks are inhabited.
        */
        public static int rollNumberOfPawns(int roll) {
            if (roll < TWO_PAWNS_CHANCE_PERCENT) {
                return 2;
            }
            return roll < ONE_PAWN_CHANCE_PERCENT ? 1 : 0;
        }

        private void spawnChickens(Chunk chunk) {
            if (gameConfig.getLagPreventionEnabled()) {
                int maxNumberOfChickens = 50;
                int numChickens = entityRepository.getNumEntitiesOfType(EntityType.CHICKEN);
                if (numChickens >= maxNumberOfChickens) {
                    return;
                }
            }
            
            int chickenSpawnProbability = 20;
            bool spawnChickens = random.range(0, 100) < chickenSpawnProbability;
            if (spawnChickens) {
                int numberOfChickens = random.range(1, 4); // 1-3 chickens per chunk
                for (int i = 0; i < numberOfChickens; i++) {
                    Location randomLocation = chunk.getRandomLocation();
                    Vector3 locationPosition = randomLocation.getPosition();

                    // create chicken
                    Vector3 position = new Vector3(locationPosition.X, (float)(locationPosition.Y + 0.5), locationPosition.Z);
                    Chicken chicken = new Chicken(position, random);
                    entityRepository.addEntity(chicken);
                }
            }
        }

        public bool generateChunkAtPosition(Vector3 position) {
            int lengthOfChunk = chunkSize * locationScale;

            int chunkX = 0;
            if (position.X >= 0) {
                chunkX = (int) (position.X / lengthOfChunk);
            } else {
                chunkX = (int) (position.X / lengthOfChunk) - 1;
            }

            int chunkZ = 0;
            if (position.Z >= 0) {
                chunkZ = (int) (position.Z / lengthOfChunk);
            } else {
                chunkZ = (int) (position.Z / lengthOfChunk) - 1;
            }

            return generateChunkIfNotExistent(chunkX, chunkZ);
        }

        public void generateSurroundingChunksAtPosition(Vector3 position) {
            int lengthOfChunk = chunkSize * locationScale;

            int chunkX = 0;
            if (position.X >= 0) {
                chunkX = (int) (position.X / lengthOfChunk);
            } else {
                chunkX = (int) (position.X / lengthOfChunk) - 1;
            }

            int chunkZ = 0;
            if (position.Z >= 0) {
                chunkZ = (int) (position.Z / lengthOfChunk);
            } else {
                chunkZ = (int) (position.Z / lengthOfChunk) - 1;
            }

            generateSurroundingChunksAt(chunkX, chunkZ);
        }

        // --- saving (docs/SAVES.md) ---

        internal void restoreCurrentChunk(int x, int z) {
            currentChunkX = x;
            currentChunkZ = z;
        }
    }
}
