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
        /** Caravans, foreign nations and bandits within reach. */
        Trade,
        /**
        * The pawn within reach, starting a row of its own (CommandBarLayout):
        * pawns walk in and out of reach and each brings different buttons,
        * so near the end they move nothing else (playtest 0.6.0 s09: Send
        * Caravan moved between rows as pawn rows came and went), and nothing
        * before them going away slides one of them under the cursor.
        */
        People,
        /**
        * Crafting and buying tools: last of the flowing groups. A Craft Axe
        * button goes away when it is clicked, and with nothing after it
        * nothing can slide into its place under a second click (playtest
        * 0.6.2 s02 finding 17: the second click on Craft Axe landed on Send
        * Caravan; with the pawns after it, Talk would have).
        */
        Craft,
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
        private string widthLabel;
        private string tooltip;

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

        /**
        * The text the button's width is measured from: its label, unless the
        * label varies in a way that must not move its neighbours (a pawn's
        * name), when it is a stand-in of fixed length and a longer label is
        * shortened to fit (CommandBarLayout.fitLabel).
        */
        public string getWidthLabel() {
            return widthLabel ?? label;
        }

        public WorldCommand setWidthLabel(string widthLabel) {
            this.widthLabel = widthLabel;
            return this;
        }

        /** Shown when the pointer rests on the button (the full label is, when it had to be shortened); null for none. */
        public string getTooltip() {
            return tooltip;
        }

        public WorldCommand setTooltip(string tooltip) {
            this.tooltip = tooltip;
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
