using beyondnations.desktop.ui;

namespace beyondnations.desktop.input {

    /**
    * The build key (KeyBindings.BuildBuilding, K).
    *
    * It builds what the settlement box says it will -- "Next (K): Farm" --
    * which is BuildingPlanner.suggestNext. It used to build whatever came
    * first in the planner's order that the player could pay for, so with the
    * box saying Farm and the player able to afford only a House, K built a
    * House at once and spent wood saved for something else. Now the first
    * press names the building and its cost and the second, within a few
    * seconds (ConfirmGate), builds it. When the player cannot pay, the first
    * press says how much is missing and nothing is armed.
    */
    public class BuildKey {
        private readonly ConfirmGate gate = new ConfirmGate();

        /** The building K builds here: the settlement box's "Next", or null when K has nothing to offer. */
        public static BuildingType? getTarget(Simulation simulation) {
            Settlement settlement = getOwnSettlement(simulation);
            return settlement == null ? null : BuildingPlanner.suggestNext(settlement);
        }

        /** A press of the build key at now (seconds). Returns whether something was built. */
        public bool press(Simulation simulation, double now) {
            Player player = simulation.getPlayer();
            BuildBuildingCommand command = new BuildBuildingCommand(simulation.getNationRepository(), simulation.getEntityRepository());
            Settlement settlement = getOwnSettlement(simulation);
            BuildingType? target = settlement == null ? null : BuildingPlanner.suggestNext(settlement);
            if (target == null) {
                // not in one of the nation's settlements, or no room: the
                // command says which
                gate.disarm();
                return command.execute(player, null);
            }
            BuildingType type = target.Value;
            if (!BuildingCatalog.canAfford(player.getInventory(), settlement.getInventory(), type)) {
                // refused with what is missing; nothing is spent
                gate.disarm();
                return command.execute(player, type);
            }
            if (gate.press("build:" + type, now)) {
                return command.execute(player, type);
            }
            player.getStatus().update(getConfirmPrompt(type));
            return false;
        }

        /** "Press K again to build a Farm (25 wood, 10 stone)." */
        public static string getConfirmPrompt(BuildingType type) {
            return "Press " + KeyNames.describe(KeyBindings.BuildBuilding) + " again to build a "
                + BuildingCatalog.getDisplayName(type) + " (" + BuildingCatalog.describeCost(type) + ").";
        }

        private static Settlement getOwnSettlement(Simulation simulation) {
            Player player = simulation.getPlayer();
            if (player.getNationId() == null || player.getCurrentSettlementId() == null) {
                return null;
            }
            Settlement settlement = simulation.getEntityRepository().getEntity(player.getCurrentSettlementId()) as Settlement;
            if (settlement == null || !settlement.getNationId().Equals(player.getNationId())) {
                return null;
            }
            return settlement;
        }
    }
}
