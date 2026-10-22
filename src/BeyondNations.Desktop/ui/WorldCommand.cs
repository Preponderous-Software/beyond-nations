namespace beyondnations.desktop.ui {

    /**
    * Where on the command bar a button lives. The bar lays the groups out in
    * this order, so a button's place depends on its group and on the buttons
    * before it, never on what came into range a moment ago: the groups whose
    * buttons come and go as things pass by (people, caravans) are last, and
    * the drastic ones -- raids, denunciations, leaving the nation -- are not
    * in the flow at all but on a row of their own under it (CommandBarLayout).
    */
    public enum CommandGroup {
        /** Create, join or run the nation: changes rarely. */
        Nation,
        /** Entering and leaving a settlement, and its market. */
        Place,
        /** Spending materials on a building or a stall. */
        Build,
        /** The player's own market stall. */
        Stall,
        /** Crafting and buying tools. */
        Craft,
        /** The pawn within reach. */
        People,
        /** Caravans, foreign nations and bandits within reach. */
        Trade,
        /**
        * Hostile or irreversible (raid, denounce, declare war, leave the
        * nation): its own row, and every click is confirmed. A new such
        * action needs only this group passed to its WorldCommand.
        */
        Drastic
    }

    /**
    * One button on the world HUD: the label it shows, the command it runs,
    * where it sits (CommandGroup), whether it can be clicked right now, and
    * whether a click has to be confirmed by a second one (ConfirmGate).
    *
    * A button that is only unavailable for the moment -- Purchase Food with
    * an empty purse -- is offered disabled rather than left out, so the
    * buttons after it stay where they were.
    */
    public class WorldCommand {
        private readonly string label;
        private readonly System.Action action;
        private CommandGroup group = CommandGroup.Trade;
        private bool enabled = true;
        private bool confirm;

        public WorldCommand(string label, System.Action action) {
            this.label = label;
            this.action = action;
        }

        public WorldCommand(string label, CommandGroup group, System.Action action) : this(label, action) {
            this.group = group;
            this.confirm = group == CommandGroup.Drastic;
        }

        public string getLabel() {
            return label;
        }

        public CommandGroup getGroup() {
            return group;
        }

        public bool isEnabled() {
            return enabled;
        }

        public WorldCommand setEnabled(bool enabled) {
            this.enabled = enabled;
            return this;
        }

        /** Whether a click only arms the button and a second click runs it. */
        public bool needsConfirm() {
            return confirm;
        }

        public WorldCommand requireConfirm() {
            this.confirm = true;
            return this;
        }

        /** Runs the command at once; the bar asks ConfirmGate first when needsConfirm(). */
        public void execute() {
            action();
        }
    }
}
