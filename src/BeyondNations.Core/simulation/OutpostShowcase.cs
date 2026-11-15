using System.Numerics;

namespace beyondnations {

    /**
    * A nation with two settlements, for screenshots of #145: the lived-in
    * settlement of SettlementShowcase, the Village era (which allows three),
    * a second settlement founded well away from it, and the player standing
    * between them carrying the price of a third.
    */
    public static class OutpostShowcase {

        public static Settlement stage(Simulation simulation) {
            Settlement home = SettlementShowcase.stage(simulation);
            simulation.getEraTracker().stageEnding(simulation, Era.VILLAGE, RunOutcome.NONE);
            simulation.getEraTracker().applyRules(simulation);

            Player player = simulation.getPlayer();
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            Vector3 start = player.getPosition();
            int minDistance = simulation.getGameConfig().getMinDistanceBetweenSettlements();
            Vector3 site = home.getPosition() + new Vector3(minDistance * 1.2f, 0, 0);
            player.setPosition(new Vector3(site.X, start.Y, site.Z));
            player.getInventory().addItem(ItemType.WOOD, FoundingCost.wood(nation));
            player.getInventory().addItem(ItemType.STONE, FoundingCost.stone(nation));
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom()).execute(player);

            // between the two, with what a third would cost
            Vector3 between = (home.getPosition() + site) / 2f;
            player.setPosition(new Vector3(between.X, start.Y, between.Z - minDistance * 0.4f));
            player.getInventory().addItem(ItemType.WOOD, FoundingCost.wood(nation));
            player.getInventory().addItem(ItemType.STONE, FoundingCost.stone(nation));
            return home;
        }
    }
}
