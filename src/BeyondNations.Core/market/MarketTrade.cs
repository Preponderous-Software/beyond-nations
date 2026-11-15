using System.Collections.Generic;

namespace beyondnations {

    /**
    * What the market panel shows and does, worked out without a window
    * (#182: "It should be easier for the player to select what they want to
    * purchase/sell at the market").
    *
    * The panel lists every good the player carries that the market buys,
    * and every good the stalls hold, each with its buying and selling
    * price. Picking one shows what it would fetch at the best market in a
    * trader's reach (Traders.getMarkets) and the margin against here, then
    * a quantity, then Buy N and Sell N buttons that name the total. A deal
    * leaves a receipt on the status line ("Sold 5 wood to Ann Lee for 120
    * coins."), which playtest 0.5.0 s05 asked for when one press sold one
    * unit and said only "You sold resources".
    *
    * Everything here reads the market (quotes are dry runs of the very deal
    * Market.sellUnits or buyUnits would make), so what a button promises is
    * what the deal pays.
    */
    public static class MarketTrade {
        /** The most the quantity can be set to. */
        public const int MAX_QUANTITY = 99;

        /** One good in the panel's list. */
        public class Row {
            public ItemType itemType;
            public int carried;
            public int forSale;
            public int buyPrice;
            public int sellPrice;
            public int trend;
        }

        /**
        * The goods the panel lists, in the price list's order: each that the
        * player carries and the market buys, or that the stalls (not the
        * player's own) hold.
        */
        public static List<ItemType> listGoods(Market market, Entity trader) {
            List<ItemType> goods = new List<ItemType>();
            foreach (ItemType itemType in MarketPricing.PricedItems) {
                bool canSell = Market.isSoldInBulk(itemType) && trader.getInventory().getNumItems(itemType) > 0;
                bool canBuy = market.getQuantityForSaleTo(trader, itemType) > 0;
                if (canSell || canBuy) {
                    goods.Add(itemType);
                }
            }
            return goods;
        }

        public static Row describeRow(Market market, Entity trader, ItemType itemType) {
            return new Row {
                itemType = itemType,
                carried = trader.getInventory().getNumItems(itemType),
                forSale = market.getQuantityForSaleTo(trader, itemType),
                buyPrice = market.getBuyPrice(itemType),
                sellPrice = market.getSellPrice(itemType),
                trend = MarketPricing.getTrend(market, itemType),
            };
        }

        /** "Wood  54/49  you 12": the list button for a good, short enough for a phone held upright. */
        public static string describeRowLabel(Row row) {
            return ItemNames.getDisplayName(row.itemType) + "  " + row.buyPrice + "/" + row.sellPrice
                + (row.carried > 0 && Market.isSoldInBulk(row.itemType) ? "  you " + row.carried : "");
        }

        /** "Buy at 54, sell at 49. You carry 12; the stalls hold 30." */
        public static string describeDetail(Row row) {
            string sell = Market.isSoldInBulk(row.itemType) ? ", sell at " + row.sellPrice : " (the market does not buy tools)";
            return "Buy at " + row.buyPrice + sell + ". You carry " + row.carried + "; the stalls hold " + row.forSale + ".";
        }

        /** What a player would get selling quantity here now (keeping nothing back in the tills). */
        public static TradeReceipt quoteSale(Market market, Entity trader, ItemType itemType, int quantity) {
            return market.quoteSale(trader, itemType, quantity, 0);
        }

        public static TradeReceipt quotePurchase(Market market, Entity trader, ItemType itemType, int quantity) {
            return market.quotePurchase(trader, itemType, quantity);
        }

        /**
        * "Sell 5 for 245", or "Sell 3 for 147" when the tills can only pay for
        * three, or "Can't sell" when they can pay for none.
        */
        public static string describeSellButton(TradeReceipt quote) {
            return quote.isEmpty() ? "Can't sell" : "Sell " + quote.getQuantity() + " for " + quote.getCoins();
        }

        /** "Buy 5 for 270", or fewer when the stalls or the purse run short, or "Can't buy". */
        public static string describeBuyButton(TradeReceipt quote) {
            return quote.isEmpty() ? "Can't buy" : "Buy " + quote.getQuantity() + " for " + quote.getCoins();
        }

        /**
        * Why the quote is short of what was asked, or null when it is not:
        * the line under the buttons, so a greyed button never leaves the
        * player guessing.
        */
        public static string describeShortfall(Market market, Entity trader, ItemType itemType, int requested, TradeReceipt sale, TradeReceipt purchase) {
            List<string> reasons = new List<string>();
            if (Market.isSoldInBulk(itemType) && sale.getQuantity() < requested) {
                int carried = trader.getInventory().getNumItems(itemType);
                if (carried == 0) {
                    reasons.Add("you carry none to sell");
                }
                else if (sale.getQuantity() < carried) {
                    reasons.Add(sale.isEmpty() ? "no till here can pay for one" : "the tills can pay for " + sale.getQuantity());
                }
            }
            if (purchase.getQuantity() < requested) {
                int forSale = market.getQuantityForSaleTo(trader, itemType);
                if (forSale == 0) {
                    reasons.Add("no stall has any to sell");
                }
                else if (purchase.getQuantity() < forSale) {
                    reasons.Add(purchase.isEmpty() ? "you can't pay for one" : "your coins cover " + purchase.getQuantity());
                }
            }
            if (reasons.Count == 0) {
                return null;
            }
            string line = string.Join("; ", reasons) + ".";
            return char.ToUpperInvariant(line[0]) + line.Substring(1);
        }

        /** The market in reach that pays the most for one of these, or null if none does; price is what it pays. */
        public static Settlement findBestElsewhere(List<Settlement> markets, ItemType itemType, out int price) {
            Settlement best = null;
            price = 0;
            if (!Market.isSoldInBulk(itemType)) {
                return null;
            }
            foreach (Settlement settlement in markets) {
                int there = settlement.getMarket().getSellPrice(itemType);
                if (there > price) {
                    best = settlement;
                    price = there;
                }
            }
            return best;
        }

        /** "Best price in reach: 61 at the Village of Yland." Null when no market is in reach. */
        public static string describeBestElsewhere(Settlement bestElsewhere, int priceThere) {
            if (bestElsewhere == null) {
                return null;
            }
            return "Best price in reach: " + priceThere + " at the " + bestElsewhere.getDisplayName() + ".";
        }

        /**
        * The profit or loss a unit in buying here to sell at the best market
        * in reach, the trader's sum: price there less price here.
        */
        public static int getCarryMargin(Row row, int priceThere) {
            return priceThere - row.buyPrice;
        }

        /** "Buy here, sell there: +7 a unit." / "...: -2 a unit (a loss)." */
        public static string describeCarryMargin(Row row, int priceThere) {
            int margin = getCarryMargin(row, priceThere);
            return "Buy here, sell there: " + signed(margin) + " a unit" + (margin < 0 ? " (a loss)." : ".");
        }

        /** "Sell there, not here: +12 a unit." -- for what the player already carries. */
        public static string describeSellThereMargin(Row row, int priceThere) {
            return "Sell there, not here: " + signed(priceThere - row.sellPrice) + " a unit.";
        }

        /** "+7", "-3", "0". */
        public static string signed(int value) {
            return value > 0 ? "+" + value : value.ToString();
        }

        /**
        * "Sold 5 wood to Ann Lee for 120 coins." / "Bought 3 apples from Ann
        * Lee and Bob Roberts for 54 coins." Up to two stall keepers are
        * named; more read "and 2 others".
        */
        public static string describeReceipt(TradeReceipt receipt, EntityRepository entityRepository) {
            if (receipt.isEmpty()) {
                return receipt.isSale() ? "Nothing sold." : "Nothing bought.";
            }
            List<string> names = new List<string>();
            foreach (EntityId id in receipt.getCounterparties()) {
                Entity entity = entityRepository.getEntity(id);
                names.Add(entity == null ? "a merchant" : entity.getName());
            }
            string who = joinNames(names);
            string goods = ItemNames.describeQuantity(receipt.getItemType(), receipt.getQuantity());
            return (receipt.isSale() ? "Sold " + goods + (who.Length > 0 ? " to " + who : "")
                                     : "Bought " + goods + (who.Length > 0 ? " from " + who : ""))
                + " for " + receipt.getCoins() + (receipt.getCoins() == 1 ? " coin." : " coins.");
        }

        /** "Ann", "Ann and Bob", "Ann, Bob and 2 others". */
        public static string joinNames(List<string> names) {
            if (names.Count == 0) {
                return "";
            }
            if (names.Count == 1) {
                return names[0];
            }
            if (names.Count == 2) {
                return names[0] + " and " + names[1];
            }
            int others = names.Count - 2;
            return names[0] + ", " + names[1] + " and " + others + (others == 1 ? " other" : " others");
        }

        /** "This visit: spent 40, took 120, net +80." */
        public static string describeVisit(int spent, int earned) {
            return "This visit: spent " + spent + ", took " + earned + ", net " + signed(earned - spent) + ".";
        }

        /** The next quantity after a step, within 1 and MAX_QUANTITY. */
        public static int stepQuantity(int quantity, int step) {
            return System.Math.Clamp(quantity + step, 1, MAX_QUANTITY);
        }
    }
}
