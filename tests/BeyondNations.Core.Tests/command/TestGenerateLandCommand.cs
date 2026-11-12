using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The debug command that generates land in bulk (bound in
    * PlayerInputController). At the chunk cap it refuses and generates
    * nothing. Otherwise it walks a 101 x 101 grid of positions around the
    * player's chunk, then generates chunks at 100 random positions within
    * 1000 paces of the origin, and reports how many chunks were new.
    *
    * The grid steps by the chunk's size in locations (7 by default), not by
    * its width in paces (7 locations of 15 paces, 105), so it reaches about
    * 350 paces either way: chunks -4 to 3 on each axis, not 50 chunks out.
    * The cap is checked once, before generating; LagPreventer trims any
    * excess afterwards.
    */
    public class TestGenerateLandCommand {

        private class World {
            public readonly RandomSource random;
            public readonly GameConfig gameConfig;
            public readonly EntityRepository entityRepository;
            public readonly Environment environment;
            public readonly Player player;
            public readonly WorldGenerator worldGenerator;

            public World(int seed, GameConfig gameConfig) {
                random = new RandomSource(seed);
                this.gameConfig = gameConfig;
                entityRepository = new EntityRepository(random);
                environment = new Environment(gameConfig.getChunkSize(), gameConfig.getLocationScale(), entityRepository, random);
                player = new Player(5, 10, new TickCounter(), 500, 200, random);
                worldGenerator = new WorldGenerator(environment, player, new EventProducer(new EventRepository()), entityRepository, gameConfig, random, new PawnNameGenerator(random));
            }

            public void generateLand() {
                new GenerateLandCommand(environment, worldGenerator, gameConfig, random).execute(player);
            }
        }

        private static World defaultWorld(int seed) {
            return new World(seed, new GameConfig());
        }

        [Fact]
        public void testExecute_AtTheChunkCap_RefusesAndGeneratesNothing() {
            // one-location chunks, so filling the world to the cap stays cheap
            GameConfig gameConfig = new GameConfig();
            gameConfig.setChunkSize(1);
            gameConfig.setLocationScale(1);
            World world = new World(20261111, gameConfig);
            for (int i = 1; i < gameConfig.getMaxNumChunks(); i++) {
                world.environment.addChunk(new Chunk(5000 + i, 0, 1, 1, world.random));
            }
            Assert.Equal(gameConfig.getMaxNumChunks(), world.environment.getNumChunks());

            world.generateLand();

            Assert.Equal(gameConfig.getMaxNumChunks(), world.environment.getNumChunks());
            Assert.Equal(0, world.entityRepository.getEntityCount());
            Assert.Equal("Max number of chunks reached.", world.player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_FillsEveryChunkWithinFourOfThePlayersChunk() {
            World world = defaultWorld(20261111);
            world.player.setPosition(new Vector3(20, 0, 30));

            world.generateLand();

            for (int x = -4; x <= 3; x++) {
                for (int z = -4; z <= 3; z++) {
                    Assert.NotNull(world.environment.getChunk(x, z));
                }
            }
        }

        [Fact]
        public void testExecute_GeneratesNothingBeyondTheRandomScattersReach() {
            World world = defaultWorld(20261111);
            world.player.setPosition(new Vector3(20, 0, 30));

            world.generateLand();

            // random positions span [-1000, 999] paces; 105-pace chunks put
            // those in chunks -10 to 9, and the grid's -4 to 3 is inside that
            foreach (Chunk chunk in world.environment.getChunks()) {
                Assert.InRange(chunk.getX(), -10, 9);
                Assert.InRange(chunk.getZ(), -10, 9);
            }
        }

        [Fact]
        public void testExecute_ReportsHowManyChunksWereNew() {
            World world = defaultWorld(20261111);
            world.player.setPosition(new Vector3(20, 0, 30));
            int before = world.environment.getNumChunks();

            world.generateLand();

            int generated = world.environment.getNumChunks() - before;
            // 63 new grid chunks (64 less the starting one), plus up to 100 scattered
            Assert.InRange(generated, 63, 163);
            Assert.Equal("Generated " + generated + " chunks.", world.player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_OffTheGeneratedWorld_OnlyScattersAtMostOneHundredChunks() {
            World world = defaultWorld(20261111);
            // chunk (4, 4) does not exist yet, so there is no grid to walk
            world.player.setPosition(new Vector3(500, 0, 500));
            Assert.Null(world.environment.getChunkAtPosition(world.player.getPosition()));
            int before = world.environment.getNumChunks();

            world.generateLand();

            int generated = world.environment.getNumChunks() - before;
            Assert.InRange(generated, 1, 100);
            Assert.Equal("Generated " + generated + " chunks.", world.player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_SameSeed_GeneratesTheSameChunks() {
            World first = defaultWorld(7);
            World second = defaultWorld(7);
            first.player.setPosition(new Vector3(20, 0, 30));
            second.player.setPosition(new Vector3(20, 0, 30));

            first.generateLand();
            second.generateLand();

            Assert.Equal(coordinates(first.environment), coordinates(second.environment));
        }

        private static List<(int, int)> coordinates(Environment environment) {
            List<(int, int)> result = new List<(int, int)>();
            foreach (Chunk chunk in environment.getChunks()) {
                result.Add((chunk.getX(), chunk.getZ()));
            }
            return result;
        }
    }
}
