using beyondnations.desktop.ui;

namespace beyondnations.desktop.input {

    /**
    * The leave-nation key (KeyBindings.LeaveNation, L).
    *
    * One press used to leave the nation, or disband it when the player was
    * its only member (playtest 0.6.0 s09: "You disbanded nation Bigland."),
    * while the command bar's Leave Nation asked for a second click. Now the
    * first press says what will happen and the second, within a few seconds
    * (ConfirmGate), does it. With no nation, the command's refusal is shown
    * at once.
    */
    public class LeaveNationKey {
        private readonly ConfirmGate gate = new ConfirmGate();

        /** A press of the key at now (seconds). Returns whether the command ran. */
        public bool press(Simulation simulation, double now) {
            Player player = simulation.getPlayer();
            NationLeaveCommand command = new NationLeaveCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository());
            Nation nation = player.getNationId() == null ? null : simulation.getNationRepository().getNation(player.getNationId());
            if (nation == null) {
                gate.disarm();
                command.execute(player);
                return true;
            }
            if (gate.press("leave:" + nation.getId(), now)) {
                command.execute(player);
                return true;
            }
            gate.prompt(player.getStatus(), getConfirmPrompt(nation, player));
            return false;
        }

        /** Called every frame: takes the prompt back once its window lapses (see ConfirmGate). */
        public void update(Player player, double now) {
            gate.update(now, player.getStatus());
        }

        /** "Press L again to disband Bigland." / "Press L again to leave Bigland." */
        public static string getConfirmPrompt(Nation nation, Player player) {
            bool disbands = nation.getLeaderId() == player.getId() && nation.getNumberOfMembers() == 1;
            return "Press " + KeyNames.describe(KeyBindings.LeaveNation) + " again to "
                + (disbands ? "disband " : "leave ") + nation.getName() + ".";
        }
    }
}
