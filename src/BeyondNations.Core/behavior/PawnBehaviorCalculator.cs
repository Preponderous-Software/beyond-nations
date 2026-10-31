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

        /** Whether a hungry pawn would find food at this settlement: in its own stall, or for sale at a price it can pay. */
        private static bool canEatAt(Pawn pawn, Settlement settlement) {
            Market market = settlement.getMarket();
            Stall stall = market.getStall(pawn.getId());
            if (stall != null && FoodItems.hasFood(stall.getInventory())) {
                return true;
            }
            return market.canPurchaseFood(pawn);
        }

        /**
        * Whether a hungry pawn could find food out in the world: a tree in fruit
        * (any season but winter, SeasonalRules.treesBearFruit) or a chicken
        * within hunting range (SeasonalRules.STORES_HUNT_RANGE).
        */
        private bool canForage(Pawn pawn) {
            if (seasonCalendar == null || SeasonalRules.treesBearFruit(seasonCalendar.getSeason())) {
                return true;
            }
            Entity chicken = environment.getNearestEntityOfType(pawn.getPosition(), EntityType.CHICKEN);
            return chicken != null && Vector3.Distance(pawn.getPosition(), chicken.getPosition()) <= SeasonalRules.STORES_HUNT_RANGE;
        }

        /**
        * Whether the nation's leader, pawn or player, lives in this
        * settlement. Its residents build there only when not: a settlement
        * its leader lives in is the leader's to build up. The player was
        * once not counted (only a pawn leader was), so residents treated the
        * player's own town as a colony and filled its market before the
        * player could build a stall (#423, the 0.6.4 playtest).
        */
        private bool leaderLivesIn(Nation nation, EntityId settlementId) {
            Entity leader = entityRepository.getEntity(nation.getLeaderId());
            EntityId leaderHome = leader is Pawn ? ((Pawn) leader).getHomeSettlementId()
                : leader is Player ? ((Player) leader).getHomeSettlementId()
                : null;
            return settlementId.Equals(leaderHome);
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
                // Too poor to buy, but carrying what a merchant here will buy:
                // sell it, and buy food with the coins next time round. Pawns
                // used to starve at home in a town whose market held a hundred
                // food, each with twenty-odd wood in its pack and no coin.
                if (market.describeWhyNoSale(pawn) == null) {
                    return BehaviorType.SELL_RESOURCES;
                }
                if (isAtHome(pawn) && !canForage(pawn)) {
                    // nothing to buy and nothing out there (winter): stay in, where it burns less
                    return BehaviorType.NONE;
                }
                return BehaviorType.EXIT_SETTLEMENT;
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
                // only in one of its own nation's settlements: a stall bought in a
                // foreign market made a merchant whose stall was nowhere its
                // nation lives, and the merchant crashed the game back home
                if (pawn.getInventory().getNumItems(ItemType.COIN) >= Stall.COIN_COST_TO_PURCHASE && market.getNumStallsForSale() > 0
                        && nation.getId().Equals(currentSettlement.getNationId())) {
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
                        if (!leaderLivesIn(nation, currentSettlementId)) {
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
                // A merchant's stall is in one market. Anywhere else -- another of
                // its nation's settlements, or a foreign one -- it is a visitor with
                // nothing to stock or collect, and it moves on in time.
                Stall stall = market.getStall(pawn.getId());
                if (stall != null) {
                    // transfer items to stall if pawn has wood or stone
                    if (pawn.getInventory().getNumItems(ItemType.WOOD) > 0 || pawn.getInventory().getNumItems(ItemType.STONE) > 0) {
                        return BehaviorType.TRANSFER_ITEMS_TO_STALL;
                    }
                    if (stall.getInventory().getNumItems(ItemType.COIN) >= Stall.COIN_COST_TO_PURCHASE * 2) {
                        return BehaviorType.COLLECT_PROFIT_FROM_STALL;
                    }
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
            if (pawn.needsFood() && pawn.getInventory().getNumItems(ItemType.COIN) > 0 && pawn.getHomeSettlementId() != null) {
                // A hungry pawn with coins goes home to buy food. A starving one
                // goes only if home can feed it: it used to go home on a single
                // coin, find the food dearer than that or sold out, walk out and
                // back again, and starve at its own gate -- most of the deaths at
                // the end of a lean winter and into spring (docs/SYSTEMS.md,
                // "Seasons"). Now it forages instead (a chicken, or a tree in
                // fruit), unless there is nothing out there to forage (the trees
                // bare in winter, no chicken near), when it goes home all the same
                // to shelter where it burns less.
                Settlement home = entityRepository.getEntity(pawn.getHomeSettlementId()) as Settlement;
                if (pawn.isStarving() && home != null && !canEatAt(pawn, home) && canForage(pawn)) {
                    return BehaviorType.GATHER_RESOURCES;
                }
                return BehaviorType.GO_TO_HOME_SETTLEMENT;
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
                        return seekSiteOrForage(pawn);
                    }
                }
            }

            // a nation the player leads founds no more than its era allows (EraRules)
            if (pawn.isSettler() && nation.canFoundSettlement()) {
                if (pawn.getInventory().getNumItems(ItemType.WOOD) < Settlement.WOOD_COST_TO_BUILD) {
                    return BehaviorType.GATHER_RESOURCES;
                }
                return isFarEnoughToSettle(pawn) ? BehaviorType.CONSTRUCT_SETTLEMENT : seekSiteOrForage(pawn);
            }

            if (pawn.getHomeSettlementId() == null && numNationSettlements > 0) {
                if (nationHasRoomForResident(nation)) {
                    return BehaviorType.JOIN_RANDOM_SETTLEMENT;
                }
                // every settlement is full: with the wood to hand, found another for the nation
                if (pawn.getInventory().getNumItems(ItemType.WOOD) >= Settlement.WOOD_COST_TO_BUILD && nation.canFoundSettlement()) {
                    return isFarEnoughToSettle(pawn) ? BehaviorType.CONSTRUCT_SETTLEMENT : seekSiteOrForage(pawn);
                }
            }

            if (hasReasonToGoHome(pawn, role)) {
                return BehaviorType.GO_TO_HOME_SETTLEMENT;
            }
            return BehaviorType.GATHER_RESOURCES;
        }

        /**
        * A pawn out to found a settlement -- a founding leader whose nation has
        * no home yet, or a settler -- walks on to a site, unless it needs food
        * and there is food to be had out here, when it forages first -- in
        * winter, with the trees bare, the nearest chicken however far it is
        * (PawnBehaviorExecutor.selectGatherTarget). It used to walk on
        * whatever its hunger: with no home it never took the hungry pawn's way
        * home, so a founder that had to walk far, or got caught between
        * settlements, walked until it starved. In playtest 0.6.1 s02 (seed
        * 103, started in winter) two of the five hunger deaths were leaders
        * who died in the wilds before their nations "ever raised so much as a
        * hut", and each nation was disbanded with them (#395). A founder with
        * nothing at all to forage still walks on: a settlement of its own is
        * the only shelter it has.
        */
        private BehaviorType seekSiteOrForage(Pawn pawn) {
            bool foodOutHere = canForage(pawn) || environment.getNearestEntityOfType(pawn.getPosition(), EntityType.CHICKEN) != null;
            if (pawn.needsFood() && foodOutHere) {
                return BehaviorType.GATHER_RESOURCES;
            }
            return BehaviorType.SEEK_SETTLEMENT_SITE;
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