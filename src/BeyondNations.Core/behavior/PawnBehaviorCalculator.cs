using System.Diagnostics;
using System.Numerics;

namespace beyondnations {

    /**
     * A class that computes the behavior type of a pawn.
     */
    public class PawnBehaviorCalculator {
        private RandomSource random;
        private Environment environment;
        private EntityRepository entityRepository;
        private NationRepository nationRepository;
        private GameConfig gameConfig;
        private TickCounter tickCounter;
        private TimeOfDay timeOfDay;
        private SeasonCalendar seasonCalendar;

        public PawnBehaviorCalculator(Environment environment, EntityRepository entityRepository, NationRepository nationRepository, GameConfig gameConfig, TickCounter tickCounter, RandomSource random) {
            this.random = random;
            this.environment = environment;
            this.entityRepository = entityRepository;
            this.nationRepository = nationRepository;
            this.gameConfig = gameConfig;
            this.tickCounter = tickCounter;
        }
        
        /**
        * Gives pawns a day: with a clock set, residents head home at nightfall
        * and stay in until morning. Without one (most tests) it is always day.
        */
        public void setTimeOfDay(TimeOfDay timeOfDay) {
            this.timeOfDay = timeOfDay;
        }

        /** Gives pawns seasons: in winter they keep to town (warmer) a while longer. Without one it is always spring. */
        public void setSeasonCalendar(SeasonCalendar seasonCalendar) {
            this.seasonCalendar = seasonCalendar;
        }

        private bool isWinter() {
            return seasonCalendar != null && seasonCalendar.isWinter();
        }

        private bool isNight() {
            return timeOfDay != null && timeOfDay.isNight();
        }

        public BehaviorType computeBehaviorType(Pawn pawn) {
            if (pawn.isMarkedForDeletion()) {
                Log.debug("[PBC] Pawn '" + pawn.getName() + " is marked for deletion. Returning NONE.");
                return BehaviorType.NONE;
            }

            if (pawn.isCurrentlyInSettlement()) {
                return computeBehaviorTypeInSettlement(pawn);
            }
            else {
                return computeBehaviorTypeOutsideSettlement(pawn);
            }
        }

        private BehaviorType computeBehaviorTypeInSettlement(Pawn pawn) {
            BehaviorType behaviorType = computeBehaviorTypeInSettlementByDay(pawn);
            // At night a pawn at home stays in unless hunger drives it out.
            if (behaviorType == BehaviorType.EXIT_SETTLEMENT && isNight() && isAtHome(pawn) && !pawn.needsFood() && !pawn.isSettler()) {
                return BehaviorType.NONE;
            }
            return behaviorType;
        }

        private static bool isAtHome(Pawn pawn) {
            return pawn.getHomeSettlementId() != null && pawn.getHomeSettlementId().Equals(pawn.getCurrentSettlementId());
        }

        /**
        * Whether an idle pawn walks back out this decision (once a simulated
        * second). A visitor leaves after about ten seconds; a pawn at home
        * lingers longer, by temperament (PawnTraits.idleExitChancePercent).
        */
        private bool rollIdleExit(Pawn pawn) {
            int chance = PawnTraits.idleExitChancePercent(pawn, isAtHome(pawn));
            if (isWinter()) {
                // indoors burns less energy, and in winter outdoors burns more
                chance = System.Math.Max(1, chance / 2);
            }
            return random.range(0, 100) < chance;
        }

        private BehaviorType computeBehaviorTypeInSettlementByDay(Pawn pawn) {
            EntityId currentSettlementId = pawn.getCurrentSettlementId();
            if (currentSettlementId == null) {
                return BehaviorType.NONE;
            }
            Settlement currentSettlement = (Settlement) entityRepository.getEntity(currentSettlementId);
            Market market = currentSettlement.getMarket();
            if (pawn.isSettler() && !pawn.needsFood()) {
                // off to found a settlement of its own
                return BehaviorType.EXIT_SETTLEMENT;
            }
            if (pawn.needsFood()) {
                Stall stall = market.getStall(pawn.getId());
                if (stall != null && FoodItems.hasFood(stall.getInventory())) {
                    return BehaviorType.COLLECT_FOOD_FROM_STALL;
                }

                if (market.canPurchaseFood(pawn)) {
                    return BehaviorType.PURCHASE_FOOD;
                }
                else {
                    return BehaviorType.EXIT_SETTLEMENT;
                }
            }

            Nation nation = nationRepository.getNation(pawn.getNationId());
            NationRole role = nation.getRole(pawn.getId());

            if (role == NationRole.LEADER) {
                if (pawn.getInventory().getNumItems(ItemType.COIN) < 100 && currentSettlement.getFunds() > 100) {
                    Log.debug("[PBC] Pawn is low on coins. Withdrawing from settlement.");
                    return BehaviorType.WITHDRAW_SETTLEMENT_FUNDS;
                }

                if (market.getNumStalls() < market.getMaxNumStalls()) {
                    // if not enough wood and stone
                    if (!Stall.canAffordToBuild(pawn.getInventory())) {
                        // members' contributions may still pay for a building
                        if (BuildingPlanner.chooseNext(currentSettlement, pawn.getInventory()) != null) {
                            return BehaviorType.CONSTRUCT_BUILDING;
                        }
                        return BehaviorType.EXIT_SETTLEMENT;
                    }
                    else {
                        // TODO: only construct stall if the settlement belongs to the pawn's nation
                        
                        return BehaviorType.CONSTRUCT_STALL;
                    }
                }
                else {
                    // with the market full, grow the settlement if the materials are to hand
                    if (BuildingPlanner.chooseNext(currentSettlement, pawn.getInventory()) != null) {
                        return BehaviorType.CONSTRUCT_BUILDING;
                    }

                    if (rollIdleExit(pawn)) {
                        return BehaviorType.EXIT_SETTLEMENT;
                    }
                    else {
                        return BehaviorType.NONE;
                    }
                }
            }
            else if (role == NationRole.SERF) {
                // if enough coins and stall for sale
                if (pawn.getInventory().getNumItems(ItemType.COIN) >= Stall.COIN_COST_TO_PURCHASE && market.getNumStallsForSale() > 0) {
                    return BehaviorType.PURCHASE_STALL;
                }
                else {

                    // if pawn has an abundance of resources, sell them
                    if (pawn.getInventory().containsAbundanceOfResources() && market.getTotalCoins() > 10) {
                        return BehaviorType.SELL_RESOURCES;
                    }

                    // nobody can buy it: put the wood and stone towards the town instead
                    if (isAtHome(pawn) && hasSurplusToContribute(pawn)) {
                        return BehaviorType.CONTRIBUTE_TO_STOCKPILE;
                    }

                    // A settlement founded by a member, away from its leader, is
                    // built up by whoever lives there, from its stockpile.
                    if (isAtHome(pawn) && nation.getLeaderId() != null) {
                        Entity leader = entityRepository.getEntity(nation.getLeaderId());
                        bool leaderLivesHere = leader is Pawn && currentSettlementId.Equals(((Pawn) leader).getHomeSettlementId());
                        if (!leaderLivesHere) {
                            if (market.getNumStalls() < market.getMaxNumStalls() && Stall.canAffordToBuild(pawn.getInventory(), currentSettlement.getInventory())) {
                                return BehaviorType.CONSTRUCT_STALL;
                            }
                            if (BuildingPlanner.chooseNext(currentSettlement, pawn.getInventory()) != null) {
                                return BehaviorType.CONSTRUCT_BUILDING;
                            }
                        }
                    }

                    if (rollIdleExit(pawn)) {
                        return BehaviorType.EXIT_SETTLEMENT;
                    }
                    else {
                        return BehaviorType.NONE;
                    }
                }
            }
            else if (role == NationRole.MERCHANT) {
                // transfer items to stall if pawn has wood or stone
                if (pawn.getInventory().getNumItems(ItemType.WOOD) > 0 || pawn.getInventory().getNumItems(ItemType.STONE) > 0) {
                    return BehaviorType.TRANSFER_ITEMS_TO_STALL;
                }
                Stall stall = market.getStall(pawn.getId());
                if (stall.getInventory().hasItem(ItemType.COIN) && stall.getInventory().getNumItems(ItemType.COIN) >= Stall.COIN_COST_TO_PURCHASE * 2) {
                    return BehaviorType.COLLECT_PROFIT_FROM_STALL;
                }
                
                if (rollIdleExit(pawn)) {
                    return BehaviorType.EXIT_SETTLEMENT;
                }
                else {
                    return BehaviorType.NONE;
                }
            }
            else {
                return BehaviorType.NONE;
            }            
        }

        private BehaviorType computeBehaviorTypeOutsideSettlement(Pawn pawn) {
            if (pawn.needsFood()) {
                int expectedFoodCost = 1;
                if (pawn.getInventory().getNumItems(ItemType.COIN) >= expectedFoodCost && pawn.getHomeSettlementId() != null) {
                    return BehaviorType.GO_TO_HOME_SETTLEMENT;
                }
            }

            // nightfall: residents head home, so towns fill up in the evening
            if (isNight() && pawn.getHomeSettlementId() != null && entityRepository.getEntity(pawn.getHomeSettlementId()) is Settlement) {
                return BehaviorType.GO_TO_HOME_SETTLEMENT;
            }

            if (shouldPlantSapling(pawn)) {
                return BehaviorType.PLANT_SAPLING;
            }

            if (pawn.getNationId() == null && pawn.isSettler()) {
                // a seceding settler: a nation of its own, not the one next door
                return pawn.getInventory().getNumItems(ItemType.WOOD) >= Settlement.WOOD_COST_TO_BUILD ? BehaviorType.CREATE_NATION : BehaviorType.GATHER_RESOURCES;
            }

            if (pawn.getNationId() == null) {
                Settlement nearestSettlement = (Settlement) environment.getNearestEntityOfType(pawn.getPosition(), EntityType.SETTLEMENT);
                // an ambitious pawn only joins when practically inside the settlement (PawnTraits)
                if (nearestSettlement != null && Vector3.Distance(pawn.getPosition(), nearestSettlement.getPosition()) < PawnTraits.joinRange(pawn, gameConfig.getSettlementJoinRange())) {
                    return BehaviorType.JOIN_NATION;
                }
                else {
                    // if enough wood to build settlement, create nation
                    if (pawn.getInventory().getNumItems(ItemType.WOOD) >= Settlement.WOOD_COST_TO_BUILD) {
                        return BehaviorType.CREATE_NATION;
                    }
                    else {
                        return BehaviorType.GATHER_RESOURCES;
                    }
                }
            }

            Nation nation = nationRepository.getNation(pawn.getNationId());
            NationRole role = nation.getRole(pawn.getId());

            int numNationSettlements = nation.getNumberOfSettlements();

            if (numNationSettlements == 0) {
                if (role == NationRole.LEADER) {

                    if (pawn.getInventory().getNumItems(ItemType.WOOD) < Settlement.WOOD_COST_TO_BUILD) {
                        return BehaviorType.GATHER_RESOURCES;
                    }

                    // if no settlements within x units, create settlement
                    Entity nearestSettlement = environment.getNearestEntityOfType(pawn.getPosition(), EntityType.SETTLEMENT);
                    int distanceToNearestSettlement = nearestSettlement == null ? int.MaxValue : (int)Vector3.Distance(nearestSettlement.getPosition(), pawn.getPosition());
                    if (nearestSettlement == null || distanceToNearestSettlement > gameConfig.getMinDistanceBetweenSettlements()) {
                        return BehaviorType.CONSTRUCT_SETTLEMENT;
                    }
                    else {
                        // it used to keep gathering here, and a founder that
                        // happened to stay close to a rival never built at all
                        return BehaviorType.SEEK_SETTLEMENT_SITE;
                    }
                }
            }

            if (pawn.isSettler()) {
                if (pawn.getInventory().getNumItems(ItemType.WOOD) < Settlement.WOOD_COST_TO_BUILD) {
                    return BehaviorType.GATHER_RESOURCES;
                }
                return isFarEnoughToSettle(pawn) ? BehaviorType.CONSTRUCT_SETTLEMENT : BehaviorType.SEEK_SETTLEMENT_SITE;
            }

            if (pawn.getHomeSettlementId() == null && numNationSettlements > 0) {
                if (nationHasRoomForResident(nation)) {
                    return BehaviorType.JOIN_RANDOM_SETTLEMENT;
                }
                // every settlement is full: with the wood to hand, found another for the nation
                if (pawn.getInventory().getNumItems(ItemType.WOOD) >= Settlement.WOOD_COST_TO_BUILD) {
                    return isFarEnoughToSettle(pawn) ? BehaviorType.CONSTRUCT_SETTLEMENT : BehaviorType.SEEK_SETTLEMENT_SITE;
                }
            }

            if (hasReasonToGoHome(pawn, role)) {
                return BehaviorType.GO_TO_HOME_SETTLEMENT;
            }
            return BehaviorType.GATHER_RESOURCES;
        }

        /**
        * Whether the trip home would achieve anything. A pawn used to head home
        * whenever it carried an abundance of goods, whatever its role, so a
        * leader (who never sells) or a serf facing a market with no coin walked
        * in, idled and walked straight back -- and spent most of its life
        * hidden inside the settlement.
        */
        /** Wood and stone a serf must carry before handing it in is worth the trip. */
        public const int CONTRIBUTION_MIN_SURPLUS = 20;

        private static bool hasSurplusToContribute(Pawn pawn) {
            return pawn.getInventory().getNumItems(ItemType.WOOD) + pawn.getInventory().getNumItems(ItemType.STONE) >= CONTRIBUTION_MIN_SURPLUS;
        }

        private bool isFarEnoughToSettle(Pawn pawn) {
            Entity nearestSettlement = environment.getNearestEntityOfType(pawn.getPosition(), EntityType.SETTLEMENT);
            return nearestSettlement == null || Vector3.Distance(nearestSettlement.getPosition(), pawn.getPosition()) > gameConfig.getMinDistanceBetweenSettlements();
        }

        private bool hasReasonToGoHome(Pawn pawn, NationRole role) {
            EntityId homeSettlementId = pawn.getHomeSettlementId();
            Settlement home = homeSettlementId == null ? null : entityRepository.getEntity(homeSettlementId) as Settlement;
            if (home == null) {
                return false;
            }
            Market market = home.getMarket();
            Inventory inventory = pawn.getInventory();

            if (role == NationRole.LEADER) {
                // build a stall once the materials are in hand and there is room
                if (market.getNumStalls() < market.getMaxNumStalls() && Stall.canAffordToBuild(inventory)) {
                    return true;
                }
                // or put up a building once it can be paid for, stockpile included
                return BuildingPlanner.chooseNext(home, inventory) != null;
            }
            if (role == NationRole.MERCHANT) {
                return inventory.containsAbundanceOfResources();
            }
            if (role == NationRole.SERF) {
                if (inventory.getNumItems(ItemType.COIN) >= Stall.COIN_COST_TO_PURCHASE && market.getNumStallsForSale() > 0) {
                    return true;
                }
                if (!inventory.containsAbundanceOfResources()) {
                    return false;
                }
                // to sell it, or, with no coin at the market, to hand it in
                return market.getTotalCoins() > 10 || hasSurplusToContribute(pawn);
            }
            return false;
        }


        // helper methods
        private bool nationHasRoomForResident(Nation nation) {
            foreach (EntityId settlementId in nation.getSettlements()) {
                Settlement settlement = entityRepository.getEntity(settlementId) as Settlement;
                if (settlement != null && settlement.hasRoomForResident()) {
                    return true;
                }
            }
            return false;
        }

        private bool shouldPlantSapling(Pawn pawn) {
            if (pawn.getInventory().getNumItems(ItemType.SAPLING) == 0) {
                return false;
            }

            if (tickCounter.getTotalTicks() % 1000 != 0) {
                return false;
            }

            // if no tree or sapling within x units, plant sapling
            int threshold = 25;
            AppleTree nearestTree = environment.getNearestTree(pawn.getPosition());
            Sapling nearestSapling = (Sapling)environment.getNearestEntityOfType(pawn.getPosition(), EntityType.SAPLING);
            int distanceToNearestTree = nearestTree == null ? int.MaxValue : (int)Vector3.Distance(nearestTree.getPosition(), pawn.getPosition());
            int distanceToNearestSapling = nearestSapling == null ? int.MaxValue : (int)Vector3.Distance(nearestSapling.getPosition(), pawn.getPosition());

            return (nearestTree == null || distanceToNearestTree > threshold) && (nearestSapling == null || distanceToNearestSapling > threshold);
        }
    }
}