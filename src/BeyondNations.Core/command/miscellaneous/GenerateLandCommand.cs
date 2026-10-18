using System.Numerics;

namespace beyondnations {

    public class GenerateLandCommand {
        private RandomSource random;
        private Environment environment;
        private WorldGenerator worldGenerator;
        private GameConfig gameConfig;

        public GenerateLandCommand(Environment environment, WorldGenerator worldGenerator, GameConfig gameConfig, RandomSource random) {
            this.random = random;
            this.environment = environment;
            this.worldGenerator = worldGenerator;
            this.gameConfig = gameConfig;
        }

        public void execute(Player player) {
            int maxNumChunks = gameConfig.getMaxNumChunks();
            int numChunks = environment.getNumChunks();
            if (numChunks >= maxNumChunks) {
                player.getStatus().update("Max number of chunks reached.");
                return;
            }

            int numChunksGenerated = 0;
            Vector3 playerPosition = player.getPosition();
            Chunk chunk = environment.getChunkAtPosition(playerPosition);
            if (chunk != null) {
                Vector3 chunkPosition = chunk.getPosition();
                for (int x = -50; x < 51; x++) {
                    for (int z = -50; z < 51; z++) {
                        int chunkSize = environment.getChunkSize();
                        Vector3 position = new Vector3(chunkPosition.X + x * chunkSize, 0, chunkPosition.Z + z * chunkSize);
                        bool generatedNewChunk = worldGenerator.generateChunkAtPosition(position);
                        if (generatedNewChunk) {
                            numChunksGenerated++;
                        }
                    }
                }
                
            }

            for (int i = 0; i < 100; i++) {
                Vector3 position = new Vector3(random.range(-1000, 1000), 0, random.range(-1000, 1000));
                bool generatedNewChunk = worldGenerator.generateChunkAtPosition(position);
                if (generatedNewChunk) {
                    numChunksGenerated++;
                }
            }

            player.getStatus().update("Generated " + numChunksGenerated + " chunks.");
        }
    }
}