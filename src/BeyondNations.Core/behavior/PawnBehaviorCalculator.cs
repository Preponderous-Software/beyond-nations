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

        public PawnBehaviorCalculator(Environment environment, EntityRepository entityRepository, NationRepository nationRepository, GameConfig gameConfig, TickCounter tickCounter, RandomSource random) {
            this.random = random;
            this.environment = environment;
            this.entityRepository = entityRepository;
            this.nationRepository = nationRepository;
            this.gameConfig = gameConfig;
            this.tickCounter = tickCounter;
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
            // Percent chance per behaviour calculation (once a simulated second)
            // that an idle pawn walks back out, so a pawn with nothing to do
            // spends about ten seconds in town rather than minutes.
            int chanceToExitSettlement = 10;

            EntityId currentSettlementId = pawn.getCurrentSettlementId();
            if (currentSettlementId == null) {
                return BehaviorType.NONE;
            }
            Settlement currentSettlement = (Settlement) entityRepository.getEntity(currentSettlementId);
            Market market = currentSettlement.getMarket();
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

                    if (random.range(0, 100) < chanceToExitSettlement) {
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

                    if (random.range(0, 100) < chanceToExitSettlement) {
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
                
                if (random.range(0, 100) < chanceToExitSettlement) {
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

            if (shouldPlantSapling(pawn)) {
                return BehaviorType.PLANT_SAPLING;
            }

            if (pawn.getNationId() == null) {
                Settlement nearestSettlement = (Settlement) environment.getNearestEntityOfType(pawn.getPosition(), EntityType.SETTLEMENT);
                if (nearestSettlement != null && Vector3.Distance(pawn.getPosition(), nearestSettlement.getPosition()) < gameConfig.getSettlementJoinRange()) {
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
                        return BehaviorType.GATHER_RESOURCES;
                    }
                }
            }

            if (pawn.getHomeSettlementId() == null && numNationSettlements > 0 && nationHasRoomForResident(nation)) {
                return BehaviorType.JOIN_RANDOM_SETTLEMENT;
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
                if (market.getNumStalls() < market.getMaxNumStalls()) {
                    return Stall.canAffordToBuild(inventory);
                }
                // with the market full, put up a building once it can be paid for
                return BuildingPlanner.chooseNext(home, inventory) != null;
            }
            if (role == NationRole.MERCHANT) {
                return inventory.containsAbundanceOfResources();
            }
            if (role == NationRole.SERF) {
                if (inventory.getNumItems(ItemType.COIN) >= Stall.COIN_COST_TO_PURCHASE && market.getNumStallsForSale() > 0) {
                    return true;
                }
                return inventory.containsAbundanceOfResources() && market.getTotalCoins() > 10;
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