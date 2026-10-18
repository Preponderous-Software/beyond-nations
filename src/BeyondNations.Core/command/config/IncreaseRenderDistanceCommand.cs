namespace beyondnations {

    public class IncreaseRenderDistanceCommand {

        public void execute(Player player) {
            player.increaseRenderDistance();
            player.getStatus().update("Render distance: " + player.getRenderDistance());
        }
    }
}