namespace beyondnations.desktop.ui {

    /**
    * One button on the world HUD: the label it shows and the command it runs.
    */
    public class WorldCommand {
        private readonly string label;
        private readonly System.Action action;

        public WorldCommand(string label, System.Action action) {
            this.label = label;
            this.action = action;
        }

        public string getLabel() {
            return label;
        }

        public void execute() {
            action();
        }
    }
}
