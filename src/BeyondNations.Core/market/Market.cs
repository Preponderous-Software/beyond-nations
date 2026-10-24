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
            return ItemCostCalculator.calculateCostAtSupply(itemType, this, getQuantityAvailable(itemType) + 1);
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

                    if (stall.getInventory().getNumItems(ItemType.COIN) < cost) {
                        continue;
                    }

                    stallsToSellTo.Add(stall);
                }
            }
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

                if (stallToSellTo.getInventory().getNumItems(ItemType.COIN) < cost) {
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
