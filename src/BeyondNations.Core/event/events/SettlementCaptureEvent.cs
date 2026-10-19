namespace beyondnations {

    /**
    * A settlement changed hands: a hostile nation laid siege to it long
    * enough to take it.
    */
    public class SettlementCaptureEvent : Event {
        private Settlement settlement;
        private Nation from;
        private Nation to;

        public SettlementCaptureEvent(Settlement settlement, Nation from, Nation to)
                : base(EventType.SettlementCapture, to.getName() + " captured a " + SettlementTiers.getDisplayName(settlement.getTier()).ToLowerInvariant() + " from " + from.getName() + ".") {
            this.settlement = settlement;
            this.from = from;
            this.to = to;
        }

        public Settlement getSettlement() { return settlement; }
        public Nation getFrom() { return from; }
        public Nation getTo() { return to; }

        public override string ToString() {
            return "SettlementCaptureEvent [" + "from=" + from.getName() + ", to=" + to.getName() + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}
