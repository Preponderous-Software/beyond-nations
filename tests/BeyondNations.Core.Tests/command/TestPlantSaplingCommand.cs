using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Planting a sapling (the Plant Sapling button) spends one from the
    * player's inventory and puts a young tree in the world a few paces
    * away; with none to plant, it only says so.
    */
    public class TestPlantSaplingCommand {
        private readonly EntityRepository entityRepository;
        private readonly Player player;
        private readonly TickCounter tickCounter = new TickCounter();
        private readonly RandomSource random = new RandomSource(20261108);

        public TestPlantSaplingCommand() {
            entityRepository = new EntityRepository(random);
            player = new Player(5, 10, tickCounter, 500, 200, random);
            player.setPosition(new Vector3(120, 0, -80));
            player.getInventory().clear();
            entityRepository.addEntity(player);
        }

        private PlantSaplingCommand command() {
            return new PlantSaplingCommand(entityRepository, random, tickCounter);
        }

        [Fact]
        public void testExecute_SpendsOneSapling_AndAddsOneToTheWorld() {
            player.getInventory().addItem(ItemType.SAPLING, 3);

            command().execute(player);

            Assert.Equal(2, player.getInventory().getNumItems(ItemType.SAPLING));
            Assert.Equal(1, entityRepository.getNumEntitiesOfType(EntityType.SAPLING));
        }

        [Fact]
        public void testExecute_PlantsAYoungTreeOfHeightThree() {
            player.getInventory().addItem(ItemType.SAPLING, 1);

            command().execute(player);

            Sapling sapling = (Sapling) entityRepository.getEntitiesOfType(EntityType.SAPLING)[0];
            Assert.Equal(3, sapling.getHeight());
            Assert.False(sapling.isGrown());
        }

        [Fact]
        public void testExecute_PlantsWithinFivePacesOfThePlayer_AtGroundHeight() {
            player.getInventory().addItem(ItemType.SAPLING, 200);

            for (int i = 0; i < 200; i++) {
                command().execute(player);
            }

            Assert.Equal(200, entityRepository.getNumEntitiesOfType(EntityType.SAPLING));
            foreach (Entity sapling in entityRepository.getEntitiesOfType(EntityType.SAPLING)) {
                Vector3 offset = sapling.getPosition() - player.getPosition();
                // RandomSource.range(int, int) excludes its upper bound, so -5 to 4
                Assert.InRange(offset.X, -5f, 4f);
                Assert.InRange(offset.Z, -5f, 4f);
                Assert.Equal(2f, sapling.getPosition().Y);
            }
        }

        [Fact]
        public void testExecute_WithNoSaplings_PlantsNothing_AndSaysSo() {
            command().execute(player);

            Assert.Equal(0, entityRepository.getNumEntitiesOfType(EntityType.SAPLING));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.SAPLING));
            Assert.Equal("You don't have any saplings.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_WithTheLastSapling_LeavesNoneToPlantAgain() {
            player.getInventory().addItem(ItemType.SAPLING, 1);

            command().execute(player);
            command().execute(player);

            Assert.Equal(1, entityRepository.getNumEntitiesOfType(EntityType.SAPLING));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.SAPLING));
            Assert.Equal("You don't have any saplings.", player.getStatus().getStatus());
        }
    }
}
