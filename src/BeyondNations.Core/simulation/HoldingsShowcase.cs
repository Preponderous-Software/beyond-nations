using System.Numerics;

namespace beyondnations {

    /**
    * Settlements held by members, for screenshots of #175, #176 and #178:
    * the two settlements of OutpostShowcase, the newer one given to the
    * longest-sworn member, and the player standing outside it, near enough
    * to enter.
    */
    public static class HoldingsShowcase {

        public static Settlement stage(Simulation simulation) {
            OutpostShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            Settlement outpost = (Settlement) simulation.getEntityRepository().getEntity(nation.getSettlement(nation.getNumberOfSettlements() - 1));
            Pawn holder = TransferLeadershipCommand.candidates(nation, simulation.getEntityRepository())[0];
            new GrantSettlementCommand(simulation.getNationRepository(), simulation.getEntityRepository()).grant(player, outpost, holder.getId());
            Vector3 at = outpost.getPosition() - player.getForward() * SettlementShowcase.DistanceAhead * 1.3f;
            player.setPosition(new Vector3(at.X, player.getPosition().Y, at.Z));
            player.getStatus().update("The " + outpost.getDisplayName() + " is held by " + holder.getName() + ".");
            return outpost;
        }
    }
}
