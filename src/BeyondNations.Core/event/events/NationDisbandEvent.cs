namespace beyondnations {

    public class NationDisbandEvent : Event {

        private Nation nation;

        public NationDisbandEvent(Nation nation) : base(EventType.NationDisband, "The nation " + nation.getName() + " has been disbanded.") {
            this.nation = nation;
        }

        public Nation getNation() {
            return nation;
        }

        public override string ToString() {
            return "NationDisbandEvent [" + "name=" + nation.getName() + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}