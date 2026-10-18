
namespace beyondnations {

    public class SpawnMoneyCommand {

        public void execute(Player player) {
            player.getInventory().addItem(ItemType.COIN, 100);
            player.getStatus().update("Spawned 100 coins.");
        }
    }
}