namespace beyondnations {

    public class NationCreationEvent : Event {
        private Nation nation;

        public NationCreationEvent(Nation nation) : base(EventType.NationCreation, "The nation " + nation.getName() + " has been created.") {
            this.nation = nation;
        }

        public Nation getNation() {
            return nation;
        }
        
        public override string ToString() {
            return "NationCreationEvent [" + "name=" + nation.getName() + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}