namespace beyondnations {

    /**
    * A hostile nation carried off part of a neighbouring settlement's funds
    * and stockpile.
    */
    public class NationRaidEvent : Event {
        private Nation raider;
        private Nation victim;
        private Settlement settlement;
        private string loot;

        public NationRaidEvent(Nation raider, Nation victim, Settlement settlement, string loot)
                : base(EventType.NationRaid, raider.getName() + " raided " + victim.getName() + " and carried off " + loot + ".") {
            this.raider = raider;
            this.victim = victim;
            this.settlement = settlement;
            this.loot = loot;
        }

        public Nation getRaider() { return raider; }
        public Nation getVictim() { return victim; }
        public Settlement getSettlement() { return settlement; }
        public string getLoot() { return loot; }

        public override string ToString() {
            return "NationRaidEvent [" + "raider=" + raider.getName() + ", victim=" + victim.getName() + ", loot=" + loot + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}
