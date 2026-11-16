namespace beyondnations {

    public enum TraderEventKind {
        /** A serf took up trade on its own (Traders). */
        TookUpTrade,
        /** The leader appointed it (AppointTraderCommand). */
        Appointed,
        /** Bought a load to carry to another market. */
        Bought,
        /** Sold its load at a market. */
        Sold,
    }

    /**
    * Something a nation's trader did (#487: in the 0.8.0 re-run playtest a
    * member became Trader and traded for a whole session without a word on
    * screen). The story is written by whoever produced the event; the
    * chronicle tells it when the trader belongs to the player's nation, so a
    * busy world's traders do not bury the player's news.
    */
    public class TraderEvent : Event {
        private readonly TraderEventKind kind;
        private readonly Pawn trader;
        private readonly NationId nationId;
        private readonly string story;

        public TraderEvent(TraderEventKind kind, Pawn trader, NationId nationId, string story) : base(EventType.Trader, story) {
            this.kind = kind;
            this.trader = trader;
            this.nationId = nationId;
            this.story = story;
        }

        public TraderEventKind getKind() { return kind; }
        public Pawn getTrader() { return trader; }
        public NationId getNationId() { return nationId; }
        public string getStory() { return story; }

        public override string ToString() {
            return "TraderEvent [kind=" + kind + ", trader=" + (trader == null ? "?" : trader.getName()) + ", description=" + getDescription()
                + ", date=" + getDate() + "]";
        }
    }
}
