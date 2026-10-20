using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace beyondnations {

    /**
    * Trade between settlements: living prices, and the caravans that carry
    * goods from where they are cheap to where they are dear.
    *
    * Every tick this drifts market prices when a pricing round is due
    * (MarketPricing), rolls every caravan along its road, and settles those
    * that have arrived. Every DISPATCH_TICKS each nation led by a pawn that
    * has no caravan on the road looks for a route: goods its settlements can
    * spare (TradeNeeds) that would sell for more at another nation's market
    * -- or another of its own -- within MAX_ROUTE_DISTANCE, and not at a
    * nation it is Hostile to (at war with). The best such route, if any pays,
    * gets a caravan.
    *
    * A caravan sells at its destination to the stalls that can pay, and then
    * to the settlement's treasury, for its stockpile; whatever is left rides
    * home with the takings, which go into the home treasury. Coins only ever
    * change hands: nothing here makes or destroys one, which TestTradeCaravans
    * checks by counting every coin in the world before and after.
    *
    * The player can trade with a caravan on the road, ride guard on one (a
    * share of its takings, and goodwill with its nation), or rob it (its
    * goods and purse, at the cost of that nation's goodwill -- enough to start
    * a war). A player who leads a nation can send caravans of their own from
    * a settlement with a Storehouse.
    *
    * Nothing here is random: routes, cargo and timing all follow from the
    * world's state, so a seeded world trades the same way every time.
    */
    public class TradeCaravans {
        /** How often pawn-led nations consider sending a caravan: every 30 seconds. */
        public const int DISPATCH_TICKS = 1500;
        /** How long a nation waits after sending one before it sends the next. */
        public const int DISPATCH_COOLDOWN_TICKS = 4500;
        /** The furthest a caravan will travel to market. */
        public const float MAX_ROUTE_DISTANCE = 1200f;
        /** The most goods a nation's caravan carries, and the fewest worth the trip. */
        public const int MAX_CARGO = 20;
        public const int MIN_CARGO = 5;
        /** The most goods the player can load on one caravan. */
        public const int MAX_PLAYER_CARGO = 40;
        /** Coins a nation's caravan takes from the treasury, to buy from travellers on the road. */
        public const int PURSE_COINS = 30;
        /** How many of the player's caravans can be on the road at once. */
        public const int MAX_PLAYER_CARAVANS = 2;

        /** How close the player has to be to a caravan to deal with it. */
        public const float REACH = 20f;
        /** How close an escort has to be when the caravan arrives to be paid. */
        public const float ESCORT_RANGE = 60f;
        /** How far an escort can fall behind before the escort is off. */
        public const float ESCORT_LEASH = 150f;
        /** An escort's pay: this share of the takings, at least ESCORT_MIN_REWARD (if the purse runs to it). */
        public const int ESCORT_SHARE_PERCENT = 15;
        public const int ESCORT_MIN_REWARD = 5;
        /** Goodwill with the caravan's nation for riding guard. */
        public const int ESCORT_RELATION = 8;
        /** Goodwill lost with the caravan's nation for robbing it. */
        public const int RAID_RELATION_PENALTY = 30;
        /** What each member of a robbed nation thinks less of a nationless robber. */
        public const int RAID_PAWN_RELATION_PENALTY = 10;
        /** Goodwill between two nations each time one's caravan sells at the other's market. */
        public const int DELIVERY_RELATION = 2;
        /** A caravan pays this share of what it expects to get at market for goods bought on the road. */
        public const int ROAD_BUY_PERCENT = 80;
        /**
        * A haul between two other nations worth at least this is told in the
        * chronicle -- the first one of the day, so a busy world's trade is a
        * line a day rather than a flood.
        */
        public const int NOTABLE_EARNINGS = 400;

        private readonly EntityRepository entityRepository;
        private readonly NationRepository nationRepository;
        private readonly EventProducer eventProducer;
        private readonly Player player;
        private readonly TimeOfDay timeOfDay;

        // when each nation last sent a caravan, and the day its last notable haul was told
        private Dictionary<NationId, int> lastDispatchTicks = new Dictionary<NationId, int>();
        private int lastNotableDay = -1;
        private int numDispatched = 0;
        private int numDelivered = 0;
        private int numRaided = 0;
        private int numEscorted = 0;
        private readonly TradeNeeds needs;

        public TradeCaravans(EntityRepository entityRepository, NationRepository nationRepository, EventProducer eventProducer, Player player,
                TimeOfDay timeOfDay, NationResources nationResources = null) {
            this.entityRepository = entityRepository;
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
            this.player = player;
            this.timeOfDay = timeOfDay;
            this.needs = new TradeNeeds(nationResources, nationRepository);
        }

        private Realm realm;

        /** Wars, truces and laws (realm/, #322); null leaves only Hostile relations to stop trade. */
        public void setRealm(Realm realm) { this.realm = realm; }

        /** What settlements can spare and need, by their nations' land (NationResources). */
        public TradeNeeds getNeeds() { return needs; }

        public int getNumDispatched() { return numDispatched; }
        public int getNumDelivered() { return numDelivered; }
        public int getNumRaided() { return numRaided; }
        public int getNumEscorted() { return numEscorted; }

        /** Once a tick, from Simulation.fixedUpdate. */
        public void update(int totalTicks, float deltaTime) {
            MarketPricing.update(entityRepository, totalTicks, needs);
            if (totalTicks > 0 && totalTicks % DISPATCH_TICKS == 0) {
                dispatchNationCaravans(totalTicks);
            }
            // indexed, as the snapshot walks it: a copy of every entity each
            // tick to find a handful of caravans is waste. Anything added
            // while walking (drops from an abandoned cart) is past count.
            int count = entityRepository.getEntityCount();
            for (int i = 0; i < count; i++) {
                Caravan caravan = entityRepository.getEntityAt(i) as Caravan;
                if (caravan != null && !caravan.isMarkedForDeletion()) {
                    advance(caravan, deltaTime);
                }
            }
        }

        public List<Caravan> getCaravans() {
            List<Caravan> caravans = new List<Caravan>();
            foreach (Entity entity in entityRepository.getEntitiesOfType(EntityType.CARAVAN)) {
                if (!entity.isMarkedForDeletion()) {
                    caravans.Add((Caravan) entity);
                }
            }
            return caravans;
        }

        // --- routes ---------------------------------------------------------

        /**
        * Whether two nations are at war: a war declared (Realm) or relations
        * Hostile. A truce is peace, so caravans travel under one. A nation is
        * never at war with itself.
        */
        public bool atWar(NationId a, NationId b) {
            Nation first = nationRepository.getNation(a);
            Nation second = nationRepository.getNation(b);
            if (first == null || second == null || first == second) {
                return false;
            }
            if (realm != null && realm.isAtWar(first, second)) {
                return true;
            }
            return nationRepository.getRelations().getState(first, second) == NationRelationState.HOSTILE;
        }

        /** Whether the player's nation is under a truce with the caravan's: no robbing it then. */
        public bool isUnderTruceWithPlayer(Caravan caravan) {
            Nation own = nationRepository.getNation(player.getNationId());
            Nation theirs = nationRepository.getNation(caravan.getNationId());
            return realm != null && own != null && theirs != null && realm.hasTruce(own, theirs);
        }

        /**
        * Every market a caravan from origin could sell at, nearest first: any
        * other settlement within MAX_ROUTE_DISTANCE whose nation is not at war
        * with the caravan's.
        */
        public List<Settlement> getDestinations(Settlement origin, NationId nationId) {
            List<Settlement> destinations = new List<Settlement>();
            if (origin == null) {
                return destinations;
            }
            int count = entityRepository.getEntityCount();
            for (int i = 0; i < count; i++) {
                Settlement candidate = entityRepository.getEntityAt(i) as Settlement;
                if (candidate == null || candidate == origin || candidate.isMarkedForDeletion()) {
                    continue;
                }
                if (nationRepository.getNation(candidate.getNationId()) == null) {
                    continue;
                }
                if (groundDistance(origin.getPosition(), candidate.getPosition()) > MAX_ROUTE_DISTANCE) {
                    continue;
                }
                if (atWar(nationId, candidate.getNationId())) {
                    continue;
                }
                destinations.Add(candidate);
            }
            // insertion sort by distance: stable, so ties keep repository order
            for (int i = 1; i < destinations.Count; i++) {
                Settlement s = destinations[i];
                float d = groundDistance(origin.getPosition(), s.getPosition());
                int j = i - 1;
                while (j >= 0 && groundDistance(origin.getPosition(), destinations[j].getPosition()) > d) {
                    destinations[j + 1] = destinations[j];
                    j--;
                }
                destinations[j + 1] = s;
            }
            return destinations;
        }

        /**
        * The goods a settlement can spare for a caravan, most plentiful first,
        * up to maxCargo in all.
        */
        public static List<ItemStack> pickCargo(Settlement origin, int maxCargo, TradeNeeds needs = null) {
            List<ItemStack> cargo = new List<ItemStack>();
            List<ItemType> candidates = new List<ItemType>();
            foreach (ItemType itemType in MarketPricing.PricedItems) {
                if (TradeNeeds.getSurplus(needs, origin, itemType) > 0) {
                    candidates.Add(itemType);
                }
            }
            // most surplus first; stable, so ties keep PricedItems order
            for (int i = 1; i < candidates.Count; i++) {
                ItemType t = candidates[i];
                int j = i - 1;
                while (j >= 0 && TradeNeeds.getSurplus(needs, origin, candidates[j]) < TradeNeeds.getSurplus(needs, origin, t)) {
                    candidates[j + 1] = candidates[j];
                    j--;
                }
                candidates[j + 1] = t;
            }
            int room = maxCargo;
            foreach (ItemType itemType in candidates) {
                if (room <= 0) {
                    break;
                }
                int quantity = Math.Min(room, Math.Min(TradeNeeds.getSurplus(needs, origin, itemType), origin.getInventory().getNumItems(itemType)));
                if (quantity > 0) {
                    cargo.Add(new ItemStack(itemType, quantity));
                    room -= quantity;
                }
            }
            return cargo;
        }

        /**
        * What a load would fetch at a market, less what it would fetch at
        * home: the reason to send it. Each unit is priced as it lands, so a
        * big load into a small market is worth less than its first unit
        * suggests.
        */
        public static int estimateGain(List<ItemStack> cargo, Settlement origin, Settlement destination, TradeNeeds needs = null) {
            long there = 0;
            long here = 0;
            foreach (ItemStack stack in cargo) {
                there += estimateSale(destination.getMarket(), stack.getItemType(), stack.getQuantity());
                here += estimateSale(origin.getMarket(), stack.getItemType(), stack.getQuantity());
            }
            // a market can only pay what its stalls and treasury hold, and at
            // home the goods could only be sold to its merchants
            there = Math.Min(there, getPurchasingPower(destination));
            here = Math.Min(here, origin.getMarket().getTotalCoins());
            long gain = there - here;
            // a nation whose land is rich in what the other's lacks
            // (NationResources.canSupply) favours that route by a quarter
            if (gain > 0) {
                foreach (ItemStack stack in cargo) {
                    if (TradeNeeds.canSupply(needs, origin, destination, stack.getItemType())) {
                        gain += gain / 4;
                        break;
                    }
                }
            }
            return (int) Math.Clamp(gain, int.MinValue, int.MaxValue);
        }

        /** Every coin a settlement could pay a caravan with: its merchants' stalls and its treasury. */
        public static long getPurchasingPower(Settlement settlement) {
            return (long) settlement.getMarket().getTotalCoins() + Math.Max(0, settlement.getFunds());
        }

        /** What selling quantity of an item into a market would fetch, one unit at a time. */
        public static int estimateSale(Market market, ItemType itemType, int quantity) {
            int supply = market.getQuantityAvailable(itemType);
            long total = 0;
            for (int i = 0; i < quantity; i++) {
                total += ItemCostCalculator.calculateCostAtSupply(itemType, market, supply + i + 1);
            }
            return (int) Math.Min(total, int.MaxValue);
        }

        private bool hasCaravanOnTheRoad(NationId nationId) {
            foreach (Caravan caravan in getCaravans()) {
                if (caravan.getNationId() == nationId && !caravan.isPlayerOwned()) {
                    return true;
                }
            }
            return false;
        }

        public int getNumPlayerCaravans() {
            int count = 0;
            foreach (Caravan caravan in getCaravans()) {
                if (caravan.isPlayerOwned()) {
                    count++;
                }
            }
            return count;
        }

        /**
        * Every pawn-led nation without a caravan on the road, and not too soon
        * after its last, sends one along its best paying route, if it has one.
        */
        public void dispatchNationCaravans(int totalTicks) {
            foreach (Nation nation in new List<Nation>(nationRepository.getNations())) {
                if (nation.getLeaderId() != null && nation.getLeaderId().Equals(player.getId())) {
                    continue; // the player's nation trades when the player says so
                }
                if (hasCaravanOnTheRoad(nation.getId())) {
                    continue;
                }
                int last;
                if (lastDispatchTicks.TryGetValue(nation.getId(), out last) && totalTicks - last < DISPATCH_COOLDOWN_TICKS) {
                    continue;
                }
                Settlement bestOrigin = null;
                Settlement bestDestination = null;
                List<ItemStack> bestCargo = null;
                int bestGain = 0;
                foreach (EntityId settlementId in nation.getSettlements()) {
                    Settlement origin = entityRepository.getEntity(settlementId) as Settlement;
                    if (origin == null || origin.isMarkedForDeletion()) {
                        continue;
                    }
                    List<ItemStack> cargo = pickCargo(origin, MAX_CARGO, needs);
                    if (countOf(cargo) < MIN_CARGO) {
                        continue;
                    }
                    foreach (Settlement destination in getDestinations(origin, nation.getId())) {
                        int gain = estimateGain(cargo, origin, destination, needs);
                        if (gain > bestGain) {
                            bestGain = gain;
                            bestOrigin = origin;
                            bestDestination = destination;
                            bestCargo = cargo;
                        }
                    }
                }
                if (bestOrigin == null) {
                    continue;
                }
                sendCaravan(nation, bestOrigin, bestDestination, bestCargo, totalTicks);
            }
        }

        /**
        * Sends a nation's caravan: the cargo comes off the origin's stockpile
        * (as much of it as is there), and a purse of up to PURSE_COINS out of
        * its treasury. Public for the trade showcase and tests; the nations
        * themselves come here through dispatchNationCaravans.
        */
        public Caravan sendCaravan(Nation nation, Settlement origin, Settlement destination, List<ItemStack> cargo, int totalTicks) {
            Caravan caravan = launch(nation, origin, destination, totalTicks);
            foreach (ItemStack stack in cargo) {
                int quantity = Math.Min(stack.getQuantity(), origin.getInventory().getNumItems(stack.getItemType()));
                if (quantity <= 0 || stack.getItemType() == ItemType.COIN) {
                    continue;
                }
                origin.getInventory().removeItem(stack.getItemType(), quantity);
                caravan.getInventory().addItem(stack.getItemType(), quantity);
            }
            int purse = Math.Min(PURSE_COINS, Math.Max(0, origin.getFunds()));
            if (purse > 0) {
                origin.removeFunds(purse);
                caravan.getInventory().addItem(ItemType.COIN, purse);
            }
            lastDispatchTicks[nation.getId()] = totalTicks;
            refreshLabel(caravan);
            bool ours = isPlayersNation(nation.getId()) || isPlayersNation(destination.getNationId());
            produce(CaravanEventKind.Dispatched, caravan, nation.getName() + " sent a caravan of " + caravan.describeCargo()
                + " from the " + origin.getDisplayName() + " to the " + destination.getDisplayName() + ".", ours,
                destination.getNationId(), null);
            return caravan;
        }

        private static int countOf(List<ItemStack> cargo) {
            int count = 0;
            foreach (ItemStack stack in cargo) {
                count += stack.getQuantity();
            }
            return count;
        }

        private Caravan launch(Nation nation, Settlement origin, Settlement destination, int totalTicks) {
            Vector3 start = origin.getPosition();
            Vector3 toward = destination.getPosition() - start;
            toward.Y = 0f;
            Vector3 offset = toward.LengthSquared() > 0f ? Vector3.Normalize(toward) * (Caravan.ARRIVAL_DISTANCE + 4f) : Vector3.Zero;
            Caravan caravan = new Caravan(start + offset, nation, origin.getId(), destination.getId(), totalTicks);
            entityRepository.addEntity(caravan);
            numDispatched++;
            return caravan;
        }

        // --- the road -------------------------------------------------------

        private void advance(Caravan caravan, float deltaTime) {
            Nation nation = nationRepository.getNation(caravan.getNationId());
            if (nation == null) {
                // its nation is gone: the cart is abandoned where it stands
                dissolve(caravan);
                return;
            }
            Settlement target = entityRepository.getEntity(caravan.getCurrentTargetId()) as Settlement;
            if (caravan.getState() == CaravanState.OUTBOUND
                    && (target == null || target.isMarkedForDeletion() || atWar(caravan.getNationId(), target.getNationId()))) {
                // the market is gone, or now at war with its nation: home again
                caravan.setState(CaravanState.RETURNING);
                refreshLabel(caravan);
                if (caravan.isPlayerOwned()) {
                    player.getStatus().update("Your caravan could not reach its market and is turning for home.");
                }
                target = entityRepository.getEntity(caravan.getOriginId()) as Settlement;
            }
            if (caravan.getState() == CaravanState.RETURNING
                    && (target == null || target.isMarkedForDeletion() || target.getNationId() != caravan.getNationId())) {
                Settlement home = findNearestSettlementOf(nation, caravan.getPosition());
                if (home == null) {
                    dissolve(caravan);
                    return;
                }
                caravan.redirectHome(home.getId());
                refreshLabel(caravan);
                target = home;
            }

            checkEscort(caravan);

            if (!caravan.rollToward(target.getPosition(), deltaTime)) {
                return;
            }
            if (caravan.getState() == CaravanState.OUTBOUND) {
                deliver(caravan, target);
            }
            else {
                returnHome(caravan, target);
            }
        }

        private Settlement findNearestSettlementOf(Nation nation, Vector3 position) {
            Settlement nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (EntityId settlementId in nation.getSettlements()) {
                Settlement settlement = entityRepository.getEntity(settlementId) as Settlement;
                if (settlement == null || settlement.isMarkedForDeletion()) {
                    continue;
                }
                float distance = groundDistance(position, settlement.getPosition());
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearest = settlement;
                }
            }
            return nearest;
        }

        /** Leaves what the caravan carried on the ground, as drops anyone can pick up, and removes it. */
        private void dissolve(Caravan caravan) {
            foreach (ItemDrop drop in ItemDrop.dropContentsOf(caravan.getInventory(), caravan.getPosition())) {
                entityRepository.addEntity(drop);
            }
            caravan.markForDeletion();
        }

        private void checkEscort(Caravan caravan) {
            if (caravan.getEscortId() == null || !caravan.getEscortId().Equals(player.getId())) {
                return;
            }
            if (!isPlayerNear(caravan, ESCORT_LEASH)) {
                caravan.setEscortId(null);
                refreshLabel(caravan);
                player.getStatus().update("You fell too far behind the caravan. The escort is off.");
            }
        }

        private bool isPlayerNear(Caravan caravan, float range) {
            if (player.isCurrentlyInSettlement()) {
                // inside the settlement the caravan is arriving at counts as being there
                Settlement inside = entityRepository.getEntity(player.getCurrentSettlementId()) as Settlement;
                return inside != null && groundDistance(inside.getPosition(), caravan.getPosition()) <= range;
            }
            return groundDistance(player.getPosition(), caravan.getPosition()) <= range;
        }

        /**
        * Sells the cargo at the destination, a unit at a time at the price it
        * fetches as it lands: to whichever stall there has the most coin and
        * can pay, and once no stall can, to the settlement itself for its
        * stockpile while the treasury can. What does not sell rides home.
        */
        private void deliver(Caravan caravan, Settlement destination) {
            Market market = destination.getMarket();
            int takings = 0;
            List<string> sold = new List<string>();
            foreach (ItemStack stack in caravan.getCargo()) {
                ItemType itemType = stack.getItemType();
                int unitsSold = 0;
                for (int i = 0; i < stack.getQuantity(); i++) {
                    int price = market.getSellPrice(itemType);
                    Stall buyer = findRichestStall(market, price, caravan);
                    if (buyer != null) {
                        buyer.getInventory().removeItem(ItemType.COIN, price);
                        buyer.getInventory().addItem(itemType, 1);
                    }
                    else if (destination.getFunds() >= price) {
                        destination.removeFunds(price);
                        destination.getInventory().addItem(itemType, 1);
                    }
                    else {
                        break;
                    }
                    caravan.getInventory().removeItem(itemType, 1);
                    caravan.getInventory().addItem(ItemType.COIN, price);
                    takings += price;
                    unitsSold++;
                }
                if (unitsSold > 0) {
                    sold.Add(ItemNames.describeQuantity(itemType, unitsSold));
                }
            }
            caravan.addEarnings(takings);
            numDelivered++;

            Nation nation = nationRepository.getNation(caravan.getNationId());
            Nation host = nationRepository.getNation(destination.getNationId());
            if (takings > 0 && nation != null && host != null && nation != host) {
                // a market under Open Borders (realm/Laws) welcomes traders twice as warmly
                int warmth = realm != null && realm.getLevel(host, LawType.OPEN_BORDERS) > 0 ? DELIVERY_RELATION * 2 : DELIVERY_RELATION;
                nationRepository.getRelations().adjust(nation, host, warmth);
            }

            string goods = sold.Count == 0 ? "nothing" : string.Join(", ", sold);
            bool escorted = caravan.getEscortId() != null && caravan.getEscortId().Equals(player.getId());
            if (escorted && isPlayerNear(caravan, ESCORT_RANGE)) {
                payEscort(caravan, nation, destination, goods, takings);
            }
            else {
                string story;
                bool notable;
                if (caravan.isPlayerOwned()) {
                    story = "Your caravan sold " + goods + " at the " + destination.getDisplayName() + " for " + takings + " coins.";
                    player.getStatus().update(story);
                    notable = true;
                }
                else {
                    story = (nation != null ? "A caravan from " + nation.getName() : "A caravan") + " sold " + goods + " at the "
                        + destination.getDisplayName() + " for " + takings + " coins.";
                    notable = isPlayersNation(caravan.getNationId()) || isPlayersNation(destination.getNationId()) || isNotableHaul(takings);
                }
                produce(CaravanEventKind.Delivered, caravan, story, notable, destination.getNationId(), null);
            }
            caravan.setEscortId(null);
            caravan.setState(CaravanState.RETURNING);
            refreshLabel(caravan);
        }

        /** The stall at a market with the most coin that can pay price, or null. Never the caravan's own. */
        private static Stall findRichestStall(Market market, int price, Caravan caravan) {
            Stall richest = null;
            foreach (Stall stall in market.getStalls()) {
                if (stall.getOwnerId() == null || stall.getOwnerId() == caravan.getId()) {
                    continue;
                }
                int coins = stall.getInventory().getNumItems(ItemType.COIN);
                if (coins < price) {
                    continue;
                }
                if (richest == null || coins > richest.getInventory().getNumItems(ItemType.COIN)) {
                    richest = stall;
                }
            }
            return richest;
        }

        private void payEscort(Caravan caravan, Nation nation, Settlement destination, string goods, int takings) {
            int reward = Math.Max(ESCORT_MIN_REWARD, takings * ESCORT_SHARE_PERCENT / 100);
            reward = Math.Min(reward, caravan.getPurse());
            if (reward > 0) {
                caravan.getInventory().removeItem(ItemType.COIN, reward);
                player.getInventory().addItem(ItemType.COIN, reward);
            }
            Nation own = nationRepository.getNation(player.getNationId());
            string goodwill = "";
            if (nation != null && own != null && own != nation) {
                nationRepository.getRelations().adjust(own, nation, ESCORT_RELATION);
                goodwill = " " + nation.getName() + " will remember it.";
            }
            numEscorted++;
            string whose = caravan.isPlayerOwned() ? "your caravan" : (nation != null ? "a " + nation.getName() + " caravan" : "a caravan");
            string story = "You rode guard on " + whose + " to the " + destination.getDisplayName() + ", which sold " + goods + " for "
                + takings + " coins.";
            player.getStatus().update("Escort done: " + reward + " coins for your trouble." + goodwill);
            List<EntityId> entities = new List<EntityId> { player.getId() };
            produce(CaravanEventKind.Escorted, caravan, story, true, destination.getNationId(), entities);
        }

        /** Home again: the takings go into the treasury, and anything unsold onto the stockpile. */
        private void returnHome(Caravan caravan, Settlement home) {
            int coins = 0;
            foreach (ItemSlot slot in caravan.getInventory().getSlots()) {
                if (slot.isEmpty()) {
                    continue;
                }
                ItemType itemType = slot.getItemStack().getItemType();
                if (itemType == ItemType.COIN) {
                    coins += slot.getQuantity();
                }
            }
            home.getInventory().transferContentsOfInventory(caravan.getInventory());
            caravan.getInventory().clear();
            caravan.markForDeletion();
            if (caravan.isPlayerOwned()) {
                string story = "Your caravan came home to the " + home.getDisplayName() + " with " + coins + " coins for the treasury.";
                player.getStatus().update(story);
                produce(CaravanEventKind.Returned, caravan, story, true, null, null);
            }
        }

        private bool isNotableHaul(int takings) {
            if (takings < NOTABLE_EARNINGS) {
                return false;
            }
            int today = timeOfDay != null ? timeOfDay.getDay() : 0;
            if (lastNotableDay == today) {
                return false;
            }
            lastNotableDay = today;
            return true;
        }

        private bool isPlayersNation(NationId nationId) {
            return nationId != null && player.getNationId() != null && nationId == player.getNationId();
        }

        private void produce(CaravanEventKind kind, Caravan caravan, string story, bool notable, NationId otherNationId, List<EntityId> entities) {
            List<NationId> nations = new List<NationId>();
            if (caravan.getNationId() != null) {
                nations.Add(caravan.getNationId());
            }
            if (otherNationId != null && otherNationId != caravan.getNationId()) {
                nations.Add(otherNationId);
            }
            eventProducer.produceCaravanEvent(new CaravanEvent(kind, caravan, story, notable, nations, entities));
        }

        /** "Xland caravan / to the Town of Yland / 15 wood, 5 stone", over the cart. */
        public void refreshLabel(Caravan caravan) {
            Nation nation = nationRepository.getNation(caravan.getNationId());
            string name = caravan.isPlayerOwned() ? "Your caravan" : (nation != null ? nation.getName() + " caravan" : "Caravan");
            Settlement target = entityRepository.getEntity(caravan.getCurrentTargetId()) as Settlement;
            string where = target == null ? "" : (caravan.getState() == CaravanState.OUTBOUND ? "to the " : "home to the ") + target.getDisplayName();
            string load = caravan.getState() == CaravanState.OUTBOUND || caravan.getCargoCount() > 0
                ? caravan.describeCargo() + (caravan.getPurse() > 0 ? ", " + caravan.getPurse() + " coins" : "")
                : caravan.getPurse() + " coins";
            string label = name + "\n" + where + "\n" + load;
            if (caravan.getEscortId() != null && caravan.getEscortId().Equals(player.getId())) {
                label += "\n(you ride guard)";
            }
            caravan.setLabel(label);
        }

        // --- the player -----------------------------------------------------

        /** The caravan within reach of the player, or null. Not from inside a settlement. */
        public Caravan findCaravanInReach() {
            if (player.isCurrentlyInSettlement()) {
                return null;
            }
            Caravan nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (Caravan caravan in getCaravans()) {
                float distance = groundDistance(player.getPosition(), caravan.getPosition());
                if (distance <= REACH && distance < nearestDistance) {
                    nearest = caravan;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        public string describeCaravan(Caravan caravan) {
            if (caravan.isPlayerOwned()) {
                return "your caravan";
            }
            Nation nation = nationRepository.getNation(caravan.getNationId());
            return nation == null ? "the caravan" : "the " + nation.getName() + " caravan";
        }

        /** What the caravan asks for one of its goods: what it expects to get at market, or the base cost when it has none. */
        public int getPriceToBuyFrom(Caravan caravan, ItemType itemType) {
            Settlement market = entityRepository.getEntity(caravan.getDestinationId()) as Settlement;
            if (market == null || market.isMarkedForDeletion()) {
                return ItemCostCalculator.getBaseCost(itemType);
            }
            return market.getMarket().getBuyPrice(itemType);
        }

        /** What the caravan pays for one of an item: a share of what it expects to sell it for at market. */
        public int getPriceToSellTo(Caravan caravan, ItemType itemType) {
            Settlement market = entityRepository.getEntity(caravan.getDestinationId()) as Settlement;
            int expected = market == null || market.isMarkedForDeletion()
                ? ItemCostCalculator.getBaseCost(itemType) / 2
                : market.getMarket().getSellPrice(itemType);
            return Math.Max(1, expected * ROAD_BUY_PERCENT / 100);
        }

        public bool buyFromCaravan(Caravan caravan, ItemType itemType) {
            if (caravan == null || caravan.isMarkedForDeletion() || itemType == ItemType.COIN) {
                return false;
            }
            if (caravan.getInventory().getNumItems(itemType) <= 0) {
                player.getStatus().update("The caravan has no " + ItemNames.getSingularName(itemType) + " left.");
                return false;
            }
            int price = getPriceToBuyFrom(caravan, itemType);
            if (player.getInventory().getNumItems(ItemType.COIN) < price) {
                player.getStatus().update("You need " + price + " coins to buy " + ItemNames.describeQuantity(itemType, 1) + " from " + describeCaravan(caravan) + ".");
                return false;
            }
            player.getInventory().removeItem(ItemType.COIN, price);
            caravan.getInventory().addItem(ItemType.COIN, price);
            caravan.getInventory().removeItem(itemType, 1);
            player.getInventory().addItem(itemType, 1);
            refreshLabel(caravan);
            player.getStatus().update("Bought " + ItemNames.describeQuantity(itemType, 1) + " for " + price + " coins from " + describeCaravan(caravan) + ".");
            return true;
        }

        /** The goods the player could sell to this caravan right now: what it deals in that they carry. */
        public List<ItemType> getGoodsToSellTo(Caravan caravan) {
            List<ItemType> goods = new List<ItemType>();
            if (caravan == null || caravan.getState() != CaravanState.OUTBOUND || caravan.isPlayerOwned()) {
                return goods;
            }
            foreach (ItemStack stack in caravan.getCargo()) {
                if (player.getInventory().getNumItems(stack.getItemType()) > 0) {
                    goods.Add(stack.getItemType());
                }
            }
            return goods;
        }

        /**
        * Sells the caravan one of each good it deals in that the player
        * carries, while its purse lasts. Returns the coins paid.
        */
        public int sellToCaravan(Caravan caravan) {
            List<ItemType> goods = getGoodsToSellTo(caravan);
            if (goods.Count == 0) {
                player.getStatus().update("The caravan is not buying anything you carry.");
                return 0;
            }
            int paid = 0;
            List<string> sold = new List<string>();
            foreach (ItemType itemType in goods) {
                int price = getPriceToSellTo(caravan, itemType);
                if (caravan.getPurse() < price) {
                    continue;
                }
                caravan.getInventory().removeItem(ItemType.COIN, price);
                player.getInventory().addItem(ItemType.COIN, price);
                player.getInventory().removeItem(itemType, 1);
                caravan.getInventory().addItem(itemType, 1);
                paid += price;
                sold.Add(ItemNames.describeQuantity(itemType, 1));
            }
            if (sold.Count == 0) {
                player.getStatus().update("The caravan's purse is too light to buy from you.");
                return 0;
            }
            refreshLabel(caravan);
            player.getStatus().update("Sold " + string.Join(", ", sold) + " to " + describeCaravan(caravan) + " for " + paid + " coins.");
            return paid;
        }

        public bool canEscort(Caravan caravan) {
            return caravan != null && !caravan.isMarkedForDeletion() && caravan.getState() == CaravanState.OUTBOUND
                && caravan.getEscortId() == null;
        }

        public bool escort(Caravan caravan) {
            if (!canEscort(caravan)) {
                player.getStatus().update("That caravan does not need a guard.");
                return false;
            }
            caravan.setEscortId(player.getId());
            refreshLabel(caravan);
            Settlement destination = entityRepository.getEntity(caravan.getDestinationId()) as Settlement;
            player.getStatus().update("You ride guard on " + describeCaravan(caravan)
                + (destination != null ? " to the " + destination.getDisplayName() : "") + ". Stay close until it arrives.");
            return true;
        }

        /** The player may rob any caravan but their own nation's, and not one they are guarding. */
        public bool canRaid(Caravan caravan) {
            if (caravan == null || caravan.isMarkedForDeletion() || caravan.isPlayerOwned()) {
                return false;
            }
            if (caravan.getEscortId() != null && caravan.getEscortId().Equals(player.getId())) {
                return false;
            }
            return !isPlayersNation(caravan.getNationId()) && !isUnderTruceWithPlayer(caravan);
        }

        /**
        * Robs a caravan: everything it carries goes to the player, and the cart
        * is abandoned. Its nation does not forget -- a nation the player leads
        * or belongs to loses RAID_RELATION_PENALTY with it, enough to turn a
        * neutral neighbour into a rival and a rival to war; a nationless player
        * loses standing with every one of its people instead.
        */
        public bool raid(Caravan caravan) {
            if (!canRaid(caravan)) {
                player.getStatus().update("You cannot raid that caravan.");
                return false;
            }
            Nation victim = nationRepository.getNation(caravan.getNationId());
            string loot = caravan.describeCargo();
            int coins = caravan.getPurse();
            if (coins > 0) {
                loot += (caravan.getCargoCount() > 0 ? " and " : "") + coins + " coins";
            }
            player.getInventory().transferContentsOfInventory(caravan.getInventory());
            caravan.getInventory().clear();
            caravan.markForDeletion();
            numRaided++;

            string consequence = "";
            Nation own = nationRepository.getNation(player.getNationId());
            if (victim != null && own != null && own != victim) {
                NationRelationState after = nationRepository.getRelations().adjust(own, victim, -RAID_RELATION_PENALTY);
                consequence = after == NationRelationState.HOSTILE
                    ? " " + victim.getName() + " is now at war with " + own.getName() + "."
                    : " " + victim.getName() + " will not forget it.";
            }
            else if (victim != null) {
                foreach (EntityId memberId in victim.getMemberIds()) {
                    Pawn member = entityRepository.getEntity(memberId) as Pawn;
                    if (member != null) {
                        member.decreaseRelationship(player, RAID_PAWN_RELATION_PENALTY);
                    }
                }
                consequence = " Its people will not forget it.";
            }
            string whose = victim != null ? "a " + victim.getName() + " caravan" : "a caravan";
            player.getStatus().update("You raided " + whose + " and took " + loot + "." + consequence);
            List<EntityId> entities = new List<EntityId> { player.getId() };
            produce(CaravanEventKind.Raided, caravan, "You raided " + whose + " on the road and carried off " + loot + ".", true,
                own != null ? own.getId() : null, entities);
            return true;
        }

        // --- the player's own caravans ---------------------------------------

        /**
        * Why the player cannot send a caravan from where they stand, or null
        * when they can: they must lead their nation and stand in one of its
        * settlements that has a Storehouse.
        */
        public string getDispatchRefusal() {
            Nation own = nationRepository.getNation(player.getNationId());
            if (own == null || own.getLeaderId() == null || !own.getLeaderId().Equals(player.getId())) {
                return "Only the leader of a nation can send caravans.";
            }
            Settlement origin = getPlayerSettlement();
            if (origin == null || origin.getNationId() != own.getId()) {
                return "Send caravans from one of your own settlements.";
            }
            if (origin.getNumBuildings(BuildingType.STOREHOUSE) == 0) {
                return "This settlement needs a Storehouse before it can send caravans.";
            }
            if (getNumPlayerCaravans() >= MAX_PLAYER_CARAVANS) {
                return "You already have " + MAX_PLAYER_CARAVANS + " caravans on the road.";
            }
            return null;
        }

        public Settlement getPlayerSettlement() {
            if (!player.isCurrentlyInSettlement()) {
                return null;
            }
            return entityRepository.getEntity(player.getCurrentSettlementId()) as Settlement;
        }

        /** The goods the player could load: what they carry that a market buys. */
        public List<ItemType> getLoadableGoods() {
            List<ItemType> goods = new List<ItemType>();
            foreach (ItemType itemType in MarketPricing.PricedItems) {
                if (Market.isSoldInBulk(itemType) && player.getInventory().getNumItems(itemType) > 0) {
                    goods.Add(itemType);
                }
            }
            return goods;
        }

        /**
        * Sends a caravan of the player's goods from the settlement they stand
        * in to destination. Its takings go into the home treasury when it
        * comes back. Returns the caravan, or null (with the reason on the
        * status line).
        */
        public Caravan dispatchPlayerCaravan(Settlement destination, Dictionary<ItemType, int> goods, int totalTicks) {
            string refusal = getDispatchRefusal();
            if (refusal != null) {
                player.getStatus().update(refusal);
                return null;
            }
            Settlement origin = getPlayerSettlement();
            Nation own = nationRepository.getNation(player.getNationId());
            if (destination == null || !getDestinations(origin, own.getId()).Contains(destination)) {
                player.getStatus().update("A caravan cannot go there from here.");
                return null;
            }
            int total = 0;
            foreach (KeyValuePair<ItemType, int> entry in goods) {
                if (entry.Value < 0 || !Market.isSoldInBulk(entry.Key) || Array.IndexOf(MarketPricing.PricedItems, entry.Key) < 0
                        || player.getInventory().getNumItems(entry.Key) < entry.Value) {
                    player.getStatus().update("You do not have the goods to load.");
                    return null;
                }
                total += entry.Value;
            }
            if (total <= 0) {
                player.getStatus().update("Load some goods first.");
                return null;
            }
            if (total > MAX_PLAYER_CARGO) {
                player.getStatus().update("A caravan carries at most " + MAX_PLAYER_CARGO + " goods.");
                return null;
            }
            Caravan caravan = launch(own, origin, destination, totalTicks);
            caravan.setPlayerOwned(true);
            foreach (ItemType itemType in MarketPricing.PricedItems) {
                int quantity;
                if (goods.TryGetValue(itemType, out quantity) && quantity > 0) {
                    player.getInventory().removeItem(itemType, quantity);
                    caravan.getInventory().addItem(itemType, quantity);
                }
            }
            refreshLabel(caravan);
            string story = "You sent a caravan of " + caravan.describeCargo() + " from the " + origin.getDisplayName() + " to the "
                + destination.getDisplayName() + ".";
            player.getStatus().update(story);
            produce(CaravanEventKind.Dispatched, caravan, story, true, destination.getNationId(), new List<EntityId> { player.getId() });
            return caravan;
        }

        private static float groundDistance(Vector3 a, Vector3 b) {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.write("numDispatched", numDispatched);
            writer.write("numDelivered", numDelivered);
            writer.write("numRaided", numRaided);
            writer.write("numEscorted", numEscorted);
            writeNationIntMap(writer, "lastDispatchTicks", lastDispatchTicks);
            writer.write("lastNotableDay", lastNotableDay);
        }

        internal void load(JsonElement element, SaveReader reader) {
            numDispatched = SaveReader.getInt(element, "numDispatched");
            numDelivered = SaveReader.getInt(element, "numDelivered");
            numRaided = SaveReader.getInt(element, "numRaided");
            numEscorted = SaveReader.getInt(element, "numEscorted");
            lastDispatchTicks = readNationIntMap(element, "lastDispatchTicks", reader);
            lastNotableDay = SaveReader.getInt(element, "lastNotableDay");
        }

        private static void writeNationIntMap(SaveWriter writer, string name, Dictionary<NationId, int> map) {
            writer.beginArray(name);
            foreach (KeyValuePair<NationId, int> entry in map) {
                writer.beginObject();
                writer.write("nation", entry.Key);
                writer.write("value", entry.Value);
                writer.endObject();
            }
            writer.endArray();
        }

        private static Dictionary<NationId, int> readNationIntMap(JsonElement element, string name, SaveReader reader) {
            Dictionary<NationId, int> map = new Dictionary<NationId, int>();
            foreach (JsonElement entry in SaveReader.property(element, name).EnumerateArray()) {
                map[reader.getNationId(entry, "nation")] = SaveReader.getInt(entry, "value");
            }
            return map;
        }
    }
}
