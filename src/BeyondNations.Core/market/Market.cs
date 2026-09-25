using System.Collections.Generic;
using Enum = System.Enum;

namespace beyondnations {

    public class Market {
        private RandomSource random;
        private List<Stall> stalls = new List<Stall>();
        private int maxNumStalls;
        
        private int totalNumItemsBought = 0;
        private int totalNumItemsSold = 0;

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

        public int getTotalNumItemsBought() {
            return totalNumItemsBought;
        }

        public int getTotalNumItemsSold() {
            return totalNumItemsSold;
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
        public List<ItemType> getFoodInPurchaseOrder() {
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
            int cost = ItemCostCalculator.calculateCostBasedOnSupply(itemType, this);
            List<Stall> stallsToBuyFrom = findStallsToBuyFrom(entity, itemType, quantity, cost);
            if (stallsToBuyFrom.Count == 0) {
                return null;
            }

            int randomStallIndex = random.range(0, stallsToBuyFrom.Count);
            Stall stallToBuyFrom = stallsToBuyFrom[randomStallIndex];

            // transfer coins
            entity.getInventory().removeItem(ItemType.COIN, cost * quantity);
            stallToBuyFrom.getInventory().addItem(ItemType.COIN, cost * quantity);

            // transfer items
            entity.getInventory().addItem(itemType, quantity);
            stallToBuyFrom.getInventory().removeItem(itemType, quantity);

            totalNumItemsBought += quantity;

            Log.info("Entity " + entity.getName() + " bought " + quantity + " " + itemType + " at the market for " + cost * quantity + " coins");
            return stallToBuyFrom.getOwnerId();
        }

        private List<Stall> findStallsToBuyFrom(Entity entity, ItemType itemType, int quantity, int cost) {
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
                if (entity.getInventory().getNumItems(ItemType.COIN) < cost * quantity) {
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

                    if (itemType == ItemType.COIN) {
                        continue;
                    }

                    int cost = ItemCostCalculator.calculateCostBasedOnSupply(itemType, this);

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

                if (itemType == ItemType.COIN) {
                    continue;
                }

                int cost = ItemCostCalculator.calculateCostBasedOnSupply(itemType, this);

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

                Log.info("Entity " + entity.getName() + " sold 1 " + itemType + " at the market for " + cost + " coins");
            }
            return stallToSellTo.getOwnerId();
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
    }
}