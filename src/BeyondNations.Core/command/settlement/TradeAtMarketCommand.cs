namespace beyondnations {

    /**
    * The player sells or buys a chosen quantity of one good at the market of
    * the settlement they are standing in (the market panel, #182), and the
    * status line says exactly what happened: what, how many, to or from
    * whom, for how much.
    *
    * A sale may go into a till's float (Stall.TILL_RESERVE is for settlers
    * selling surplus, not the player, #369). Each stall keeper dealt with
    * thinks a little better of the player, as with Sell Resources, and a
    * deal in another nation's market warms the two nations
    * (Diplomacy.recordTrade).
    */
    public class TradeAtMarketCommand {
        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;

        public TradeAtMarketCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public TradeReceipt sell(Player player, ItemType itemType, int quantity) {
            Settlement settlement = findMarket(player);
            if (settlement == null) {
                return new TradeReceipt(itemType, true);
            }
            if (!Market.isSoldInBulk(itemType)) {
                player.getStatus().update("Markets buy resources and food, not coins or tools.");
                return new TradeReceipt(itemType, true);
            }
            if (player.getInventory().getNumItems(itemType) == 0) {
                player.getStatus().update("You have no " + ItemNames.getSingularName(itemType) + " to sell.");
                return new TradeReceipt(itemType, true);
            }
            Market market = settlement.getMarket();
            TradeReceipt receipt = market.sellUnits(player, itemType, quantity, 0);
            if (receipt.isEmpty()) {
                player.getStatus().update(market.describeWhyNoSale(player) ?? "No one at this market can pay for " + ItemNames.getSingularName(itemType) + ".");
                return receipt;
            }
            player.recordMarketSale();
            settle(player, settlement, receipt);
            return receipt;
        }

        public TradeReceipt buy(Player player, ItemType itemType, int quantity) {
            Settlement settlement = findMarket(player);
            if (settlement == null) {
                return new TradeReceipt(itemType, false);
            }
            Market market = settlement.getMarket();
            if (market.getQuantityForSaleTo(player, itemType) == 0) {
                player.getStatus().update("No one here is selling " + ItemNames.getSingularName(itemType) + ".");
                return new TradeReceipt(itemType, false);
            }
            TradeReceipt receipt = market.buyUnits(player, itemType, quantity);
            if (receipt.isEmpty()) {
                player.getStatus().update("You need " + market.getBuyPrice(itemType) + " coins to buy " + ItemNames.describeQuantity(itemType, 1) + ".");
                return receipt;
            }
            settle(player, settlement, receipt);
            return receipt;
        }

        /**
        * Sells exactly what a quote the player was shown said -- its number
        * for its coins -- or nothing, with the status line saying the market
        * moved and what it would be now (MarketTrade.describeMovedQuote).
        * The market panel's Sell button calls this with the quote its label
        * was drawn from, so a press can never deal something different.
        */
        public TradeReceipt sellAsQuoted(Player player, TradeReceipt shown) {
            ItemType itemType = shown.getItemType();
            Settlement settlement = findMarket(player);
            if (settlement == null) {
                return new TradeReceipt(itemType, true);
            }
            TradeReceipt now = settlement.getMarket().quoteSale(player, itemType, shown.getQuantity(), 0);
            if (!MarketTrade.matchesQuote(shown, now)) {
                player.getStatus().update(MarketTrade.describeMovedQuote(shown, now));
                return new TradeReceipt(itemType, true);
            }
            return sell(player, itemType, shown.getQuantity());
        }

        /** As sellAsQuoted, for buying: exactly the number for the coins shown, or nothing. */
        public TradeReceipt buyAsQuoted(Player player, TradeReceipt shown) {
            ItemType itemType = shown.getItemType();
            Settlement settlement = findMarket(player);
            if (settlement == null) {
                return new TradeReceipt(itemType, false);
            }
            TradeReceipt now = settlement.getMarket().quotePurchase(player, itemType, shown.getQuantity());
            if (!MarketTrade.matchesQuote(shown, now)) {
                player.getStatus().update(MarketTrade.describeMovedQuote(shown, now));
                return new TradeReceipt(itemType, false);
            }
            return buy(player, itemType, shown.getQuantity());
        }

        private Settlement findMarket(Player player) {
            Settlement settlement = player.getCurrentSettlementId() == null ? null
                : entityRepository.getEntity(player.getCurrentSettlementId()) as Settlement;
            if (settlement == null) {
                player.getStatus().update("You are not inside a settlement.");
                return null;
            }
            if (settlement.getMarket() == null || settlement.getMarket().getNumStalls() == 0) {
                player.getStatus().update("This settlement does not have any stalls.");
                return null;
            }
            return settlement;
        }

        private void settle(Player player, Settlement settlement, TradeReceipt receipt) {
            foreach (EntityId keeperId in receipt.getCounterparties()) {
                Entity keeper = entityRepository.getEntity(keeperId);
                if (keeper == null) {
                    continue;
                }
                player.increaseRelationship(keeper, 1);
                if (keeper is Pawn) {
                    ((Pawn) keeper).increaseRelationship(player, 1);
                }
            }
            Diplomacy.recordTrade(nationRepository, player.getNationId(), settlement);
            player.getStatus().update(MarketTrade.describeReceipt(receipt, entityRepository));
        }
    }
}
