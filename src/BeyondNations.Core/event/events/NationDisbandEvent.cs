namespace beyondnations {

    public class NationDisbandEvent : Event {

        private Nation nation;
        private Nation conqueror;
        private Settlement lastSettlement;

        /**
        * conqueror and lastSettlement are set only when the nation is
        * disbanded because its last settlement was captured: by then it holds
        * none, so the chronicle cannot tell from the nation alone that it
        * ever built one (0.6.2 playtest: a conquered nation was told it
        * "never raised so much as a hut").
        */
        public NationDisbandEvent(Nation nation, Nation conqueror = null, Settlement lastSettlement = null)
                : base(EventType.NationDisband, "The nation " + nation.getName() + " has been disbanded.") {
            this.nation = nation;
            this.conqueror = conqueror;
            this.lastSettlement = lastSettlement;
        }

        public Nation getNation() {
            return nation;
        }

        /** Who took the nation's last settlement, or null if it was not disbanded by capture. */
        public Nation getConqueror() {
            return conqueror;
        }

        /** The settlement whose capture ended the nation, or null. */
        public Settlement getLastSettlement() {
            return lastSettlement;
        }

        public override string ToString() {
            return "NationDisbandEvent [" + "name=" + nation.getName() + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}