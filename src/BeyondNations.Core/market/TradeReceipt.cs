using System.Collections.Generic;

namespace beyondnations {

    /**
    * What one deal at a market came to: the good, how many changed hands,
    * the coins that went the other way, and whose stalls dealt (in the order
    * they first did). A receipt for nothing has a quantity of 0.
    *
    * Market.sellUnits and Market.buyUnits make these; MarketTrade turns them
    * into the line the player reads ("Sold 5 wood to Ann Lee for 120 coins.",
    * the receipt playtest 0.5.0 s05 asked for in #182).
    */
    public class TradeReceipt {
        private readonly ItemType itemType;
        private readonly bool sale;
        private int quantity;
        private int coins;
        private readonly List<EntityId> counterparties = new List<EntityId>();

        public TradeReceipt(ItemType itemType, bool sale) {
            this.itemType = itemType;
            this.sale = sale;
        }

        public ItemType getItemType() { return itemType; }

        /** True for goods sold to the market, false for goods bought from it. */
        public bool isSale() { return sale; }

        public int getQuantity() { return quantity; }

        /** Coins received for a sale, or paid for a purchase. */
        public int getCoins() { return coins; }

        /** The owners of the stalls that dealt, each once, first dealer first. */
        public List<EntityId> getCounterparties() { return counterparties; }

        public bool isEmpty() { return quantity == 0; }

        internal void add(EntityId counterparty, int unitPrice) {
            quantity++;
            coins += unitPrice;
            if (counterparty != null && !counterparties.Contains(counterparty)) {
                counterparties.Add(counterparty);
            }
        }
    }
}
