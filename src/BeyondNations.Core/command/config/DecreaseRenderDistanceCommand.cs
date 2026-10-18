namespace beyondnations {

    public class DecreaseRenderDistanceCommand {

        public void execute(Player player) {
            player.decreaseRenderDistance();
            player.getStatus().update("Render distance: " + player.getRenderDistance());
        }
    }
}