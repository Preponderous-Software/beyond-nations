using System.Numerics;

namespace beyondnations {
    public class PlantSaplingCommand {
        private RandomSource random;
        private EntityRepository entityRepository;

        public PlantSaplingCommand(EntityRepository entityRepository, RandomSource random) {
            this.random = random;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            if (player.getInventory().getNumItems(ItemType.SAPLING) > 0) {
                player.getInventory().removeItem(ItemType.SAPLING, 1);
                Sapling sapling = new Sapling(player.getPosition(), 3, random);
                sapling.setPosition(sapling.getPosition() + new Vector3(random.range(-5, 5), 0, random.range(-5, 5)));
                sapling.setPosition(new Vector3(sapling.getPosition().X, 2, sapling.getPosition().Z));
                entityRepository.addEntity(sapling);
            }
            else {
                player.getStatus().update("You don't have any saplings.");
            }
        }
    }
}