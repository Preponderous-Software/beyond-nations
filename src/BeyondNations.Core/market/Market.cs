using System.Text.Json;
using System.Collections.Generic;
using Enum = System.Enum;

namespace beyondnations {

    public class Market {
        private RandomSource random;
        private List<Stall> stalls = new List<Stall>();
        private int maxNumStalls;
        
        private int totalNumItemsBought = 0;
        private int totalNumItemsSold = 0;
        private MarketPrices prices = new MarketPrices();

        public Market(int maxNumStalls, RandomSource random) {
            this.random = random;
            this.maxNumStalls = maxNumStalls;
        }

        public List<Stall> getStalls() {
            return stalls;
        }

        public bool hasStall(EntityId ownerId) {
            foreach (Stall stall in stalls) {
                if (stall.getOwnerId() == ownerId) {
                    return true;
                }
            }
            return false;
        }

        public Stall getStall(EntityId ownerId) {
            foreach (Stall stall in stalls) {
                if (stall.getOwnerId() == ownerId) {
                    return stall;
                }
            }
            return null;
        }

        public bool createStall() {
            if (stalls.Count < maxNumStalls) {
                stalls.Add(new Stall());
                return true;
            }
            return false;
        }

        public void destroyStall(EntityId ownerId) {
            foreach (Stall stall in stalls) {
                if (stall.getOwnerId() == ownerId) {
                    stalls.Remove(stall);
                    return;
                }
            }
        }

        public int getNumStallsForSale() {
            int numStallsForSale = 0;
            foreach (Stall stall in stalls) {
                if (stall.getOwnerId() == null) {
                    numStallsForSale++;
                }
            }
            return numStallsForSale;
        }

        public Stall getStallForSale() {
            foreach (Stall stall in stalls) {
                if (stall.getOwnerId() == null) {
                    return stall;
                }
            }
            return null;
        }

        public int getNumStalls() {
            return stalls.Count;
        }

        public int getMaxNumStalls() {
            return maxNumStalls;
        }

        /**
        * Room for more stalls, which is what a storehouse buys a settlement.
        */
        public void increaseMaxNumStalls(int amount) {
            maxNumStalls += amount;
        }

        public int getTotalNumItemsBought() {
            return totalNumItemsBought;
        }

        public int getTotalNumItemsSold() {
            return totalNumItemsSold;
        }

        /** How this market's prices lean, item by item (MarketPricing drifts them). */
        public MarketPrices getPrices() {
            return prices;
        }

        /** What one of these costs to buy here right now. */
        public int getBuyPrice(ItemType itemType) {
            return ItemCostCalculator.calculateCostBasedOnSupply(itemType, this);
        }

        /**
        * What a stall here pays for one of these right now: the price once the
        * sold unit is on the stall, which is the next unit's buying price.
        * Buying one and selling it straight back therefore comes out even at
        * best -- selling at the price the unit was bought at, when the stall
        * had one fewer, would pay more than it cost, and a player could walk
        * a merchant's coins out of the stall a unit at a time.
        */
        public int getSellPrice(ItemType itemType) {
            return getBid(itemType, getQuantityAvailable(itemType) + 1, getRichestTill());
        }

        /**
        * A stall pays for one unit no more than this share of the coins in
        * its till (one in BID_TILL_DIVISOR), and never less than the price
        * floor: a market short of coin bids low and buys a load, rather than
        * bidding the full price and paying for one (0.8.0 playtest s05: a
        * 100-coin till bid 63 for wood, bought one, and was dry). As the till
        * fills, the bid climbs back to the supply curve. It only ever lowers
        * what a market pays, so it makes no coins.
        */
        public const int BID_TILL_DIVISOR = 6;

        /**
        * What a stall whose till holds tillCoins pays for one of these when
        * the market holds supply once it has landed: the supply curve's
        * price, held down to a share of the till, and no lower than the
        * floor (ItemCostCalculator.floorOf).
        */
        public int getBid(ItemType itemType, int supply, int tillCoins) {
            int curve = ItemCostCalculator.calculateCostAtSupply(itemType, this, supply);
            int floor = ItemCostCalculator.floorOf(ItemCostCalculator.getBaseCost(itemType));
            int share = System.Math.Max(0, tillCoins) / BID_TILL_DIVISOR;
            return System.Math.Max(floor, System.Math.Min(curve, share));
        }

        /** The most coins any owned stall's till holds here. */
        public int getRichestTill() {
            int richest = 0;
            foreach (Stall stall in stalls) {
                if (stall.getOwnerId() != null) {
                    richest = System.Math.Max(richest, stall.getInventory().getNumItems(ItemType.COIN));
                }
            }
            return richest;
        }

        /**
        * What buying quantity of these costs in all: each unit at the price
        * it costs once the ones before it have left the stalls, so buying
        * ten at once costs what buying them one at a time would.
        */
        public int getBuyCost(ItemType itemType, int quantity) {
            int supply = getQuantityAvailable(itemType);
            long total = 0;
            for (int i = 0; i < quantity; i++) {
                total += ItemCostCalculator.calculateCostAtSupply(itemType, this, supply - i);
            }
            return total > int.MaxValue ? int.MaxValue : (int) total;
        }

        /**
        * Buys one of whichever food gives the most energy per coin at current
        * prices, falling back to the next food when that one cannot be bought.
        * @return the id of the stall owner, or null if no food could be bought
        */
        public EntityId purchaseFood(Entity entity) {
            foreach (ItemType foodType in getFoodInPurchaseOrder()) {
                EntityId stallOwnerId = buyItem(entity, foodType, 1);
                if (stallOwnerId != null) {
                    return stallOwnerId;
                }
            }
            return null;
        }

        /**
        * @return whether purchaseFood would succeed for this entity right now
        */
        public bool canPurchaseFood(Entity entity) {
            foreach (ItemType foodType in FoodItems.getFoodInDescendingEnergyOrder()) {
                int cost = ItemCostCalculator.calculateCostBasedOnSupply(foodType, this);
                if (findStallsToBuyFrom(entity, foodType, 1, cost).Count > 0) {
                    return true;
                }
            }
            return false;
        }

        /**
        * Every food, cheapest per unit of energy restored first at the market's
        * current supply-based prices. Ties keep the most nourishing food first.
        */
        private List<ItemType> getFoodInPurchaseOrder() {
            List<ItemType> order = new List<ItemType>(FoodItems.getFoodInDescendingEnergyOrder());
            // insertion sort, because List.Sort is not stable and ties must keep their order
            for (int i = 1; i < order.Count; i++) {
                ItemType foodType = order[i];
                int j = i - 1;
                while (j >= 0 && costsLessPerEnergy(foodType, order[j])) {
                    order[j + 1] = order[j];
                    j--;
                }
                order[j + 1] = foodType;
            }
            return order;
        }

        private bool costsLessPerEnergy(ItemType a, ItemType b) {
            // cost(a) / energy(a) < cost(b) / energy(b), cross-multiplied to stay in integers
            long costA = ItemCostCalculator.calculateCostBasedOnSupply(a, this);
            long costB = ItemCostCalculator.calculateCostBasedOnSupply(b, this);
            return costA * FoodItems.getEnergyRestored(b) < costB * FoodItems.getEnergyRestored(a);
        }

        /**
        * @return the id of the stall owner, or null if purchase failed
        */
        public EntityId buyItem(Entity entity, ItemType itemType, int quantity) {
            if (quantity <= 0) {
                return null;
            }
            int totalCost = getBuyCost(itemType, quantity);
            List<Stall> stallsToBuyFrom = findStallsToBuyFrom(entity, itemType, quantity, totalCost);
            if (stallsToBuyFrom.Count == 0) {
                return null;
            }

            int randomStallIndex = random.range(0, stallsToBuyFrom.Count);
            Stall stallToBuyFrom = stallsToBuyFrom[randomStallIndex];

            // transfer coins
            entity.getInventory().removeItem(ItemType.COIN, totalCost);
            stallToBuyFrom.getInventory().addItem(ItemType.COIN, totalCost);

            // transfer items
            entity.getInventory().addItem(itemType, quantity);
            stallToBuyFrom.getInventory().removeItem(itemType, quantity);

            totalNumItemsBought += quantity;

            Log.debug("Entity " + entity.getName() + " bought " + quantity + " " + itemType + " at the market for " + totalCost + " coins");
            return stallToBuyFrom.getOwnerId();
        }

        private List<Stall> findStallsToBuyFrom(Entity entity, ItemType itemType, int quantity, int totalCost) {
            List<Stall> stallsToBuyFrom = new List<Stall>();
            foreach(Stall stall in stalls) {
                if (stall.getOwnerId() == null) {
                    continue;
                }
                if (stall.getOwnerId() == entity.getId()) {
                    continue;
                }
                if (!stall.getInventory().hasItem(itemType)) {
                    continue;
                }
                if (stall.getInventory().getNumItems(itemType) < quantity) {
                    continue;
                }
                if (entity.getInventory().getNumItems(ItemType.COIN) < totalCost) {
                    continue;
                }
                stallsToBuyFrom.Add(stall);
            }
            return stallsToBuyFrom;
        }

        /**
        * @return the id of the stall owner, or null if purchase failed
        */
        public EntityId sellResources(Entity entity) {
            return sellResources(entity, 0);
        }

        /**
        * Sells one of each good the entity carries to a stall that can pay.
        * priceBonusPercent raises the price the seller asks (a greedy pawn
        * drives a harder bargain); a stall that cannot pay the higher price is
        * not sold to.
        *
        * @return the id of the stall owner, or null if nothing was sold
        */
        public EntityId sellResources(Entity entity, int priceBonusPercent) {
            return sellResources(entity, priceBonusPercent, 0);
        }

        /**
        * As sellResources, but a stall only pays for a good when it still
        * holds keepInTill coins afterwards. Settlers selling surplus keep a
        * till's float (Stall.TILL_RESERVE) so the market has coin left for
        * the player (#369); the player and hungry pawns pass 0.
        *
        * @return the id of the stall owner, or null if nothing was sold
        */
        public EntityId sellResources(Entity entity, int priceBonusPercent, int keepInTill) {
            List<Stall> stallsToSellTo = findStallsToSellTo(entity, priceBonusPercent, keepInTill);
            if (stallsToSellTo.Count == 0) {
                return null;
            }

            int randomStallIndex = random.range(0, stallsToSellTo.Count);
            Stall stallToSellTo = stallsToSellTo[randomStallIndex];

            foreach(ItemType itemType in Enum.GetValues(typeof(ItemType))) {
                if (!entity.getInventory().hasItem(itemType)) {
                    continue;
                }

                if (!isSoldInBulk(itemType)) {
                    continue;
                }

                int cost = askingPrice(getSellPrice(itemType), priceBonusPercent);

                if (!canPay(stallToSellTo, cost, keepInTill)) {
                    continue;
                }

                // transfer items
                entity.getInventory().removeItem(itemType, 1);
                stallToSellTo.getInventory().addItem(itemType, 1);

                // transfer coins
                entity.getInventory().addItem(ItemType.COIN, cost);
                stallToSellTo.getInventory().removeItem(ItemType.COIN, cost);

                totalNumItemsSold++;

                Log.debug("Entity " + entity.getName() + " sold 1 " + itemType + " at the market for " + cost + " coins");
            }
            return stallToSellTo.getOwnerId();
        }

        // --- dealing in one good, by the unit, with a receipt (#182) ----------

        /**
        * Sells up to quantity of one good, a unit at a time, each at the
        * price it fetches once the units before it are on the stalls (so
        * five at once come to what five presses would). Each unit goes to
        * whichever stall can pay and still keep keepInTill coins, the one
        * with the most coins first, so a sale spreads over the market's
        * merchants instead of stopping when one till is dry. Stops when the
        * seller runs out or no stall can pay.
        */
        public TradeReceipt sellUnits(Entity entity, ItemType itemType, int quantity, int keepInTill) {
            return sellUnits(entity, itemType, quantity, keepInTill, false);
        }

        /** What sellUnits would come to right now, without dealing. */
        public TradeReceipt quoteSale(Entity entity, ItemType itemType, int quantity, int keepInTill) {
            return sellUnits(entity, itemType, quantity, keepInTill, true);
        }

        private TradeReceipt sellUnits(Entity entity, ItemType itemType, int quantity, int keepInTill, bool dryRun) {
            TradeReceipt receipt = new TradeReceipt(itemType, true);
            if (!isSoldInBulk(itemType)) {
                return receipt;
            }
            int held = entity.getInventory().getNumItems(itemType);
            int supply = getQuantityAvailable(itemType);
            int[] tills = new int[stalls.Count];
            for (int s = 0; s < stalls.Count; s++) {
                tills[s] = stalls[s].getInventory().getNumItems(ItemType.COIN);
            }
            for (int unit = 0; unit < quantity && unit < held; unit++) {
                // the richest till buys, at the bid its coins allow (getBid)
                int buyer = -1;
                for (int s = 0; s < stalls.Count; s++) {
                    Stall stall = stalls[s];
                    if (stall.getOwnerId() == null || stall.getOwnerId() == entity.getId()) {
                        continue;
                    }
                    if (buyer < 0 || tills[s] > tills[buyer]) {
                        buyer = s;
                    }
                }
                if (buyer < 0) {
                    break;
                }
                int price = getBid(itemType, supply + 1, tills[buyer]);
                if (tills[buyer] - price < System.Math.Max(0, keepInTill)) {
                    break;
                }
                tills[buyer] -= price;
                supply++;
                receipt.add(stalls[buyer].getOwnerId(), price);
                if (!dryRun) {
                    Inventory till = stalls[buyer].getInventory();
                    entity.getInventory().removeItem(itemType, 1);
                    till.addItem(itemType, 1);
                    till.removeItem(ItemType.COIN, price);
                    entity.getInventory().addItem(ItemType.COIN, price);
                    totalNumItemsSold++;
                }
            }
            if (!dryRun && !receipt.isEmpty()) {
                Log.debug("Entity " + entity.getName() + " sold " + receipt.getQuantity() + " " + itemType + " at the market for " + receipt.getCoins() + " coins");
            }
            return receipt;
        }

        /**
        * Buys up to quantity of one good, a unit at a time, each at the price
        * it costs once the units before it have left the stalls -- the same
        * total as getBuyCost. Each unit comes from whichever stall (not the
        * buyer's own) holds the most of it. Stops when the buyer cannot pay
        * for the next unit or the stalls run out.
        */
        public TradeReceipt buyUnits(Entity entity, ItemType itemType, int quantity) {
            return buyUnits(entity, itemType, quantity, false);
        }

        /** What buyUnits would come to right now, without dealing. */
        public TradeReceipt quotePurchase(Entity entity, ItemType itemType, int quantity) {
            return buyUnits(entity, itemType, quantity, true);
        }

        private TradeReceipt buyUnits(Entity entity, ItemType itemType, int quantity, bool dryRun) {
            TradeReceipt receipt = new TradeReceipt(itemType, false);
            if (itemType == ItemType.COIN) {
                return receipt;
            }
            int purse = entity.getInventory().getNumItems(ItemType.COIN);
            int supply = getQuantityAvailable(itemType);
            int[] stock = new int[stalls.Count];
            for (int s = 0; s < stalls.Count; s++) {
                stock[s] = stalls[s].getInventory().getNumItems(itemType);
            }
            for (int unit = 0; unit < quantity; unit++) {
                int price = ItemCostCalculator.calculateCostAtSupply(itemType, this, supply);
                if (price > purse) {
                    break;
                }
                int seller = -1;
                for (int s = 0; s < stalls.Count; s++) {
                    Stall stall = stalls[s];
                    if (stall.getOwnerId() == null || stall.getOwnerId() == entity.getId() || stock[s] <= 0) {
                        continue;
                    }
                    if (seller < 0 || stock[s] > stock[seller]) {
                        seller = s;
                    }
                }
                if (seller < 0) {
                    break;
                }
                stock[seller]--;
                supply--;
                purse -= price;
                receipt.add(stalls[seller].getOwnerId(), price);
                if (!dryRun) {
                    Inventory till = stalls[seller].getInventory();
                    entity.getInventory().removeItem(ItemType.COIN, price);
                    till.addItem(ItemType.COIN, price);
                    till.removeItem(itemType, 1);
                    entity.getInventory().addItem(itemType, 1);
                    totalNumItemsBought++;
                }
            }
            if (!dryRun && !receipt.isEmpty()) {
                Log.debug("Entity " + entity.getName() + " bought " + receipt.getQuantity() + " " + itemType + " at the market for " + receipt.getCoins() + " coins");
            }
            return receipt;
        }

        /** How many of this good the stalls hold that this entity could buy (not counting its own stall). */
        public int getQuantityForSaleTo(Entity entity, ItemType itemType) {
            int quantity = 0;
            foreach (Stall stall in stalls) {
                if (stall.getOwnerId() == null || stall.getOwnerId() == entity.getId()) {
                    continue;
                }
                quantity += stall.getInventory().getNumItems(itemType);
            }
            return quantity;
        }

        /**
        * The coins this market's stalls can spend on goods while each keeps
        * keepInTill: what a trader, who leaves every till its float, can
        * hope to sell for here.
        */
        public int getSpendableCoins(int keepInTill) {
            int spendable = 0;
            foreach (Stall stall in stalls) {
                if (stall.getOwnerId() == null) {
                    continue;
                }
                spendable += System.Math.Max(0, stall.getInventory().getNumItems(ItemType.COIN) - System.Math.Max(0, keepInTill));
            }
            return spendable;
        }

        /** Whether sellResources with these terms would sell this entity anything right now. */
        public bool canSellTo(Entity entity, int priceBonusPercent, int keepInTill) {
            return findStallsToSellTo(entity, priceBonusPercent, keepInTill).Count > 0;
        }

        private static bool canPay(Stall stall, int cost, int keepInTill) {
            return stall.getInventory().getNumItems(ItemType.COIN) - cost >= System.Math.Max(0, keepInTill);
        }

        private List<Stall> findStallsToSellTo(Entity entity, int priceBonusPercent, int keepInTill) {
            List<Stall> stallsToSellTo = new List<Stall>();
            foreach(Stall stall in stalls) {
                if (stall.getOwnerId() == null) {
                    continue;
                }
                if (stall.getOwnerId() == entity.getId()) {
                    continue;
                }
                foreach(ItemType itemType in Enum.GetValues(typeof(ItemType))) {
                    if (!entity.getInventory().hasItem(itemType)) {
                        continue;
                    }

                    if (!isSoldInBulk(itemType)) {
                        continue;
                    }

                    int cost = askingPrice(getSellPrice(itemType), priceBonusPercent);

                    if (!canPay(stall, cost, keepInTill)) {
                        continue;
                    }

                    stallsToSellTo.Add(stall);
                }
            }
            return stallsToSellTo;
        }

        /**
        * Why sellResources would sell nothing to this entity, in one sentence
        * for the status line (playtest 0.5.0 s05: every refusal used to read
        * the same). Null when a sale would go through.
        */
        public string describeWhyNoSale(Entity entity) {
            int cheapest = int.MaxValue;
            ItemType cheapestType = ItemType.COIN;
            foreach (ItemType itemType in Enum.GetValues(typeof(ItemType))) {
                if (!isSoldInBulk(itemType) || !entity.getInventory().hasItem(itemType)) {
                    continue;
                }
                int price = getSellPrice(itemType);
                if (price < cheapest) {
                    cheapest = price;
                    cheapestType = itemType;
                }
            }
            if (cheapest == int.MaxValue) {
                return "You have nothing to sell: markets buy resources and food, not coins or tools.";
            }
            int buyers = 0;
            bool ownsStall = false;
            int richest = 0;
            foreach (Stall stall in stalls) {
                if (stall.getOwnerId() == null) {
                    continue;
                }
                if (stall.getOwnerId() == entity.getId()) {
                    ownsStall = true;
                    continue;
                }
                buyers++;
                richest = System.Math.Max(richest, stall.getInventory().getNumItems(ItemType.COIN));
            }
            if (buyers == 0) {
                return ownsStall
                    ? "The only stall here is yours, and you can't sell to yourself."
                    : "No merchant keeps a stall here yet, so no one can buy.";
            }
            if (richest < cheapest) {
                return "No merchant here can pay: the richest has " + richest + " coins, and your cheapest good ("
                    + ItemNames.getSingularName(cheapestType) + ") sells for " + cheapest + ".";
            }
            return null;
        }

        /**
        * Whether selling resources hands this item over. Coin is the payment,
        * and tools are kept: a press of Sell Resources used to empty the
        * whole bag, which would have sold the player's axe with the wood.
        */
        public static bool isSoldInBulk(ItemType itemType) {
            return itemType != ItemType.COIN && !Tools.isTool(itemType);
        }

        /** Whether this inventory holds anything a market buys (isSoldInBulk): resources and food alike. */
        public static bool hasAnythingToSell(Inventory inventory) {
            foreach (ItemType itemType in Enum.GetValues(typeof(ItemType))) {
                if (isSoldInBulk(itemType) && inventory.hasItem(itemType)) {
                    return true;
                }
            }
            return false;
        }

        /**
        * The cheapest price one of these sells for here right now, or -1 when
        * no stall (other than the buyer's own) has one.
        */
        public int getPriceToBuy(Entity buyer, ItemType itemType) {
            foreach (Stall stall in stalls) {
                if (stall.getOwnerId() == null || stall.getOwnerId() == buyer.getId()) {
                    continue;
                }
                if (stall.getInventory().getNumItems(itemType) > 0) {
                    return ItemCostCalculator.calculateCostBasedOnSupply(itemType, this);
                }
            }
            return -1;
        }

        /** A price raised by a percentage, by at least one coin when raised at all. */
        public static int askingPrice(int cost, int priceBonusPercent) {
            if (priceBonusPercent <= 0) {
                return cost;
            }
            return cost + System.Math.Max(1, cost * priceBonusPercent / 100);
        }

        public int getQuantityAvailable(ItemType itemType) {
            int quantityAvailable = 0;
            foreach(Stall stall in stalls) {
                if (stall.getOwnerId() == null) {
                    continue;
                }
                if (!stall.getInventory().hasItem(itemType)) {
                    continue;
                }
                quantityAvailable += stall.getInventory().getNumItems(itemType);
            }
            return quantityAvailable;
        }

        public int getTotalCoins() {
            return getQuantityAvailable(ItemType.COIN);
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.writeStream("random", random);
            writer.write("maxNumStalls", maxNumStalls);
            writer.write("totalNumItemsBought", totalNumItemsBought);
            writer.write("totalNumItemsSold", totalNumItemsSold);
            writer.beginArray("stalls");
            foreach (Stall stall in stalls) {
                writer.beginObject();
                stall.save(writer);
                writer.endObject();
            }
            writer.endArray();
            writer.beginObject("prices");
            prices.save(writer);
            writer.endObject();
        }

        internal void load(JsonElement element, SaveReader reader) {
            random = reader.stream(SaveReader.property(element, "random"));
            maxNumStalls = SaveReader.getInt(element, "maxNumStalls");
            totalNumItemsBought = SaveReader.getInt(element, "totalNumItemsBought");
            totalNumItemsSold = SaveReader.getInt(element, "totalNumItemsSold");
            stalls = new List<Stall>();
            foreach (JsonElement stall in SaveReader.property(element, "stalls").EnumerateArray()) {
                Stall loaded = new Stall();
                loaded.load(stall, reader);
                stalls.Add(loaded);
            }
            prices = new MarketPrices();
            prices.load(SaveReader.property(element, "prices"));
        }
    }
}
