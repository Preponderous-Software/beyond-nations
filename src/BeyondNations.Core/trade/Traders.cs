using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * The trader, a nation role (#167): a pawn who buys a load of one good
    * where it is cheap, carries it on foot to another settlement's market
    * where it fetches more, sells it there, and looks for the next run from
    * wherever it stands.
    *
    * How a pawn becomes one:
    * - a serf at home, with TRADER_STAKE coins and no stall to buy, takes up
    *   trade on its own when a run from its home market would pay, while its
    *   nation has fewer traders than settlements;
    * - or the leader appoints it (AppointTraderCommand), handing over the
    *   stake.
    *
    * How it sits beside the rest of the trade:
    * - Merchants keep stalls; traders keep none. A trader buys from
    *   merchants' stalls in one market and sells to merchants' stalls in
    *   another, so it carries coin and goods between markets the way a
    *   caravan does, on a pawn's scale and with its own purse. It sells
    *   only above each till's float (Stall.TILL_RESERVE, #369), as a settler
    *   selling surplus does, so a trader never leaves a market unable to pay
    *   the player.
    * - Caravans are a nation's (treasury, Storehouse stock, Town era); a
    *   trader is any era and spends its own coins. Both buy low and sell
    *   high, so both pull prices between markets together.
    * - Selling at another nation's market warms the two nations a little
    *   (Diplomacy.recordTrade), as a player's sale there does; a trader never
    *   goes to a nation its own is at war with.
    *
    * Everything here is a pure function of the world's state -- no random
    * draws and no saved fields -- so a trader's next step is worked out
    * afresh at every decision and a loaded world carries on the same way.
    */
    public static class Traders {
        /** Coins a serf needs to take up trade, and what a leader hands over when appointing one. */
        public const int TRADER_STAKE = 40;
        /** The most goods a trader carries on one run. */
        public const int MAX_LOAD = 10;
        /** A run must clear at least this many coins to be worth the walk. */
        public const int MIN_PROFIT = 8;
        /** How far a trader walks to market. Shorter than a caravan's road: it walks, and alone. */
        public const float MAX_ROUTE_DISTANCE = 800f;

        /** One planned run: buy quantity of good here for cost, sell at destination for takings. */
        public class TradeRun {
            public ItemType good;
            public int quantity;
            public Settlement destination;
            public int cost;
            public int takings;
            public int getProfit() { return takings - cost; }
        }

        /** Whether this member is a trader of its nation. */
        public static bool isTrader(Nation nation, EntityId memberId) {
            return nation != null && nation.getRole(memberId) == NationRole.TRADER;
        }

        public static int countTraders(Nation nation) {
            int count = 0;
            foreach (EntityId memberId in nation.getMemberIds()) {
                if (nation.getRole(memberId) == NationRole.TRADER) {
                    count++;
                }
            }
            return count;
        }

        /** A nation keeps at most one trader for each settlement it holds. */
        public static bool hasRoomForTrader(Nation nation) {
            return nation != null && countTraders(nation) < nation.getNumberOfSettlements();
        }

        /**
        * Every market a trader of this nation could carry goods to from
        * origin, nearest first: any other living settlement within
        * MAX_ROUTE_DISTANCE whose nation is not Hostile to the trader's.
        */
        public static List<Settlement> getMarkets(EntityRepository entityRepository, NationRepository nationRepository, Settlement origin, NationId nationId) {
            if (origin == null) {
                return new List<Settlement>();
            }
            return getMarketsNear(entityRepository, nationRepository, origin.getPosition(), origin, nationId, MAX_ROUTE_DISTANCE);
        }

        /**
        * As getMarkets, measured from a trader out on the road: every market
        * within range of where it stands. The range has a little slack, so a
        * trader that has just stepped out of one market still counts the
        * markets that were in reach of it.
        */
        public static List<Settlement> getMarketsNear(EntityRepository entityRepository, NationRepository nationRepository, Vector3 position, NationId nationId) {
            return getMarketsNear(entityRepository, nationRepository, position, null, nationId, MAX_ROUTE_DISTANCE + ROAD_SLACK);
        }

        /** The slack getMarketsNear allows a trader on the road. */
        public const float ROAD_SLACK = 60f;

        private static List<Settlement> getMarketsNear(EntityRepository entityRepository, NationRepository nationRepository, Vector3 originPosition,
                Settlement origin, NationId nationId, float range) {
            List<Settlement> markets = new List<Settlement>();
            Nation own = nationId == null ? null : nationRepository.getNation(nationId);
            int count = entityRepository.getEntityCount();
            for (int i = 0; i < count; i++) {
                Settlement candidate = entityRepository.getEntityAt(i) as Settlement;
                if (candidate == null || candidate == origin || candidate.isMarkedForDeletion()) {
                    continue;
                }
                Nation theirs = nationRepository.getNation(candidate.getNationId());
                if (theirs == null) {
                    continue;
                }
                if (groundDistance(originPosition, candidate.getPosition()) > range) {
                    continue;
                }
                if (own != null && own != theirs && nationRepository.getRelations().getState(own, theirs) == NationRelationState.HOSTILE) {
                    continue;
                }
                markets.Add(candidate);
            }
            // insertion sort by distance: stable, so ties keep repository order
            for (int i = 1; i < markets.Count; i++) {
                Settlement s = markets[i];
                float d = groundDistance(originPosition, s.getPosition());
                int j = i - 1;
                while (j >= 0 && groundDistance(originPosition, markets[j].getPosition()) > d) {
                    markets[j + 1] = markets[j];
                    j--;
                }
                markets[j + 1] = s;
            }
            return markets;
        }

        /**
        * What quantity of a good would fetch at a market from a trader, unit
        * by unit as each lands on the stalls, and no more than the market can
        * spend above its tills' floats.
        */
        public static int estimateTakings(Market market, ItemType itemType, int quantity) {
            int sale = TradeCaravans.estimateSale(market, itemType, quantity);
            return System.Math.Min(sale, market.getSpendableCoins(Stall.TILL_RESERVE));
        }

        /**
        * The best paying run from here: a good the stalls here sell, the
        * market to carry it to, and how many to buy (up to MAX_LOAD and what
        * the purse covers), whichever clears the most coins. Null when no run
        * clears MIN_PROFIT.
        */
        public static TradeRun planRun(Entity trader, Settlement here, List<Settlement> markets) {
            TradeRun best = null;
            Market market = here.getMarket();
            int purse = trader.getInventory().getNumItems(ItemType.COIN);
            foreach (ItemType itemType in MarketPricing.PricedItems) {
                if (Tools.isTool(itemType) || !Market.isSoldInBulk(itemType)) {
                    continue;
                }
                int forSale = System.Math.Min(MAX_LOAD, market.getQuantityForSaleTo(trader, itemType));
                if (forSale <= 0) {
                    continue;
                }
                foreach (Settlement destination in markets) {
                    for (int quantity = 1; quantity <= forSale; quantity++) {
                        int cost = market.getBuyCost(itemType, quantity);
                        if (cost > purse) {
                            break;
                        }
                        int takings = estimateTakings(destination.getMarket(), itemType, quantity);
                        int profit = takings - cost;
                        if (profit >= MIN_PROFIT && (best == null || profit > best.getProfit())) {
                            best = new TradeRun {
                                good = itemType, quantity = quantity, destination = destination, cost = cost, takings = takings,
                            };
                        }
                    }
                }
            }
            return best;
        }

        /** Whether a trader carries anything a market buys (Market.isSoldInBulk), and so has a load to sell. */
        public static bool hasGoodsToSell(Inventory inventory) {
            return Market.hasAnythingToSell(inventory);
        }

        /** What a trader's whole load would fetch at a market, good by good. */
        public static int estimateLoadTakings(Market market, Inventory carried) {
            long total = 0;
            foreach (ItemType itemType in MarketPricing.PricedItems) {
                int quantity = carried.getNumItems(itemType);
                if (quantity > 0 && Market.isSoldInBulk(itemType)) {
                    total += TradeCaravans.estimateSale(market, itemType, quantity);
                }
            }
            return (int) System.Math.Min(total, market.getSpendableCoins(Stall.TILL_RESERVE));
        }

        /**
        * Where the goods a trader carries fetch the most, among here (when
        * the trader stands in a settlement) and the markets it can reach.
        * Null when no market would pay anything for them.
        */
        public static Settlement findBestMarketFor(Inventory carried, Settlement here, List<Settlement> markets) {
            Settlement best = null;
            int bestTakings = 0;
            if (here != null) {
                bestTakings = estimateLoadTakings(here.getMarket(), carried);
                best = bestTakings > 0 ? here : null;
            }
            foreach (Settlement market in markets) {
                int takings = estimateLoadTakings(market.getMarket(), carried);
                if (takings > bestTakings) {
                    best = market;
                    bestTakings = takings;
                }
            }
            return best;
        }

        /** Where a trader on the road takes the load it carries; null when no market in reach would pay for it. */
        public static Settlement findDestinationOnTheRoad(EntityRepository entityRepository, NationRepository nationRepository, Pawn trader) {
            List<Settlement> markets = getMarketsNear(entityRepository, nationRepository, trader.getPosition(), trader.getNationId());
            return findBestMarketFor(trader.getInventory(), null, markets);
        }

        /** "buying wood to sell at the Village of Yland", for a trader's name tag and the members panel. */
        public static string describeRun(TradeRun run) {
            return "buying " + ItemNames.getSingularName(run.good) + " to sell at the " + run.destination.getDisplayName();
        }

        private static float groundDistance(Vector3 a, Vector3 b) {
            return Vector3.Distance(new Vector3(a.X, 0f, a.Z), new Vector3(b.X, 0f, b.Z));
        }
    }
}
