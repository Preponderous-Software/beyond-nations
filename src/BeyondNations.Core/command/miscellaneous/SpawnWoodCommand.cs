
namespace beyondnations {

    public class SpawnWoodCommand {

        public void execute(Player player) {
            player.getInventory().addItem(ItemType.WOOD, 100);
            player.getStatus().update("Spawned 100 wood.");
        }
    }
}