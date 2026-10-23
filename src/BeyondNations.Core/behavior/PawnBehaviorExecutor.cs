using System.Net;
using System.Numerics;
using System.Collections.Generic;

namespace beyondnations {

    /**
     * A class that executes the behavior of a pawn.
     */
    public class PawnBehaviorExecutor {
        private NationNameGenerator nationNameGenerator;
        private RandomSource random;
        private Environment environment;
        private NationRepository nationRepository;
        private EventProducer eventProducer;
        private EntityRepository entityRepository;
        private TickCounter tickCounter;
        private SeasonCalendar seasonCalendar;

        /** Gives pawns seasons: from autumn they lay in food (SeasonalRules.wantsStores). */
        public void setSeasonCalendar(SeasonCalendar seasonCalendar) {
            this.seasonCalendar = seasonCalendar;
        }

        public PawnBehaviorExecutor(Environment environment, NationRepository nationRepository, EventProducer eventProducer, EntityRepository entityRepository, RandomSource random, NationNameGenerator nationNameGenerator, TickCounter tickCounter) {
            this.tickCounter = tickCounter;
            this.nationNameGenerator = nationNameGenerator;
            this.random = random;
            this.environment = environment;
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
            this.entityRepository = entityRepository;
        }

        // the player's laws (realm/): conscripts gather less
        private Realm realm;

        public void setRealm(Realm realm) {
            this.realm = realm;
        }

        public void executeBehavior(Pawn pawn, BehaviorType behaviorType) {
            switch (behaviorType) {
                case BehaviorType.NONE:
                    break;
                case BehaviorType.GATHER_RESOURCES:
                    executeGatherResourcesBehavior(pawn);
                    break;
                case BehaviorType.SELL_RESOURCES:
                    executeSellResourcesBehavior(pawn);
                    break;
                case BehaviorType.WANDER:
                    executeWanderBehavior(pawn);
                    break;
                case BehaviorType.PURCHASE_FOOD:
                    executePurchaseFoodBehavior(pawn);
                    break;
                case BehaviorType.CONSTRUCT_SETTLEMENT:
                    executeConstructSettlementBehavior(pawn);
                    break;
                case BehaviorType.GO_TO_HOME_SETTLEMENT:
                    executeGoToHomeSettlementBehavior(pawn);
                    break;
                case BehaviorType.EXIT_SETTLEMENT:
                    executeExitSettlementBehavior(pawn);
                    break;
                case BehaviorType.PLANT_SAPLING:
                    executePlantSaplingBehavior(pawn);
                    break;
                case BehaviorType.CONSTRUCT_STALL:
                    executeConstructStallBehavior(pawn);
                    break;
                case BehaviorType.CONSTRUCT_BUILDING:
                    executeConstructBuildingBehavior(pawn);
                    break;
                case BehaviorType.PURCHASE_STALL:
                    executePurchaseStallBehavior(pawn);
                    break;
                case BehaviorType.TRANSFER_ITEMS_TO_STALL:
                    executeTransferItemsToStallBehavior(pawn);
                    break;
                case BehaviorType.JOIN_NATION:
                    executeJoinNationBehavior(pawn);
                    break;
                case BehaviorType.CREATE_NATION:
                    executeCreateNationBehavior(pawn);
                    break;
                case BehaviorType.JOIN_RANDOM_SETTLEMENT:
                    executeJoinRandomSettlementBehavior(pawn);
                    break;
                case BehaviorType.COLLECT_PROFIT_FROM_STALL:
                    executeCollectProfitFromStallBehavior(pawn);
                    break;
                case BehaviorType.COLLECT_FOOD_FROM_STALL:
                    executeCollectFoodFromStallBehavior(pawn);
                    break;
                case BehaviorType.WITHDRAW_SETTLEMENT_FUNDS:
                    executeWithdrawSettlementFundsBehavior(pawn);
                    break;
                case BehaviorType.CONTRIBUTE_TO_STOCKPILE:
                    executeContributeToStockpileBehavior(pawn);
                    break;
                case BehaviorType.SEEK_SETTLEMENT_SITE:
                    executeSeekSettlementSiteBehavior(pawn);
                    break;
                default:
                    Log.error("Behavior type " + behaviorType + " is not implemented.");
                    break;
            }
        }

        /** Whether the trees pawns fell carry apples now: not in winter. Without a calendar it is always spring. */
        private bool treesBearFruit() {
            return seasonCalendar == null || SeasonalRules.treesBearFruit(seasonCalendar.getSeason());
        }

        private void executeGatherResourcesBehavior(Pawn pawn) {
            if (!pawn.hasTargetEntity() || !isGatherable(pawn.getTargetEntity().getType())) {
                Entity target = selectGatherTarget(pawn);
                if (target != null) {
                    pawn.setTargetEntity(target);
                }
                else {
                    // TODO: nothing to gather, pawn should do something else
                }
            }

            Entity targetEntity = pawn.getTargetEntity();
            if (targetEntity == null) {
                Log.warning("Pawn " + pawn + " has no target entity in gather resources behavior.");
                return;
            }
            EntityType targetEntityType = targetEntity.getType();

            if (pawn.isAtTargetEntity()) {
                // gather
                if (isGatherable(targetEntityType)) {
                    targetEntity.markForDeletion();
                    // an industrious pawn works a tree or rock harder (PawnTraits)
                    int woodBonus = PawnTraits.gatherBonus(pawn, targetEntity.getInventory().getNumItems(ItemType.WOOD));
                    int stoneBonus = PawnTraits.gatherBonus(pawn, targetEntity.getInventory().getNumItems(ItemType.STONE));
                    int woodBefore = pawn.getInventory().getNumItems(ItemType.WOOD);
                    int stoneBefore = pawn.getInventory().getNumItems(ItemType.STONE);
                    // a tree felled in winter is bare: wood, but no apples (SeasonalRules.treesBearFruit)
                    Inventory haul = targetEntity.getInventory();
                    if (targetEntityType == EntityType.TREE && !treesBearFruit()) {
                        haul.removeItem(ItemType.APPLE, haul.getNumItems(ItemType.APPLE));
                    }
                    pawn.getInventory().transferContentsOfInventory(haul);
                    Tools.applyGatherBonus(pawn.getInventory(), targetEntityType);
                    // the trait stacks with any tool bonus
                    if (woodBonus > 0) {
                        pawn.getInventory().addItem(ItemType.WOOD, woodBonus);
                    }
                    if (stoneBonus > 0) {
                        pawn.getInventory().addItem(ItemType.STONE, stoneBonus);
                    }
                    // a conscript spends part of its day at drill (realm/Laws)
                    realm?.afterGather(pawn, woodBefore, stoneBefore);
                    pawn.setTargetEntity(null);

                    if (targetEntityType == EntityType.TREE && pawn.getInventory().getNumItems(ItemType.SAPLING) > 0) {
                        pawn.setCurrentBehaviorType(BehaviorType.PLANT_SAPLING);
                    }
                    else {
                        pawn.setCurrentBehaviorType(BehaviorType.NONE);
                    }
                }
                else {
                    Log.warning("Pawn " + pawn + " is at target entity " + targetEntity + " but it is not a tree, rock or chicken.");
                    pawn.setTargetEntity(null);
                }
            }
            else {
                // move towards target entity
                pawn.moveTowardsTargetEntity();
            }
        }

        /**
         * The entity types a pawn gathers from by walking up to them and taking
         * their inventory -- the same set the player's interact command harvests.
         */
        private static bool isGatherable(EntityType type) {
            return type == EntityType.TREE || type == EntityType.ROCK || type == EntityType.CHICKEN;
        }

        /**
         * Picks what a pawn should gather next. A pawn that needs food (or,
         * from autumn, is short of stores) goes for food: a chicken within
         * hunting range (#253), or else a tree carrying apples. A hungry pawn
         * with neither -- in winter the trees are bare -- walks to the nearest
         * chicken however far it is. Otherwise it takes whichever of the
         * nearest tree and the nearest rock is closer, so that a pawn after
         * wood or stone leaves the chickens alone. Returns null when there is
         * nothing to gather.
         *
         * A hungry pawn once hunted the nearest chicken at any distance, and
         * with no chicken took the nearer of a tree and a rock, so in early
         * spring, with chickens hunted out and only beginning to hatch again,
         * a hungry pawn could walk past fruit trees to a distant chicken or
         * go for stone (docs/SYSTEMS.md, "Seasons").
         */
        private Entity selectGatherTarget(Pawn pawn) {
            bool hungry = pawn.needsFood();
            bool wantsStores = seasonCalendar != null && SeasonalRules.wantsStores(pawn, seasonCalendar.getSeason());
            if (hungry || wantsStores) {
                Entity nearestChicken = environment.getNearestEntityOfType(pawn.getPosition(), EntityType.CHICKEN);
                if (nearestChicken != null && Vector3.Distance(pawn.getPosition(), nearestChicken.getPosition()) <= SeasonalRules.STORES_HUNT_RANGE) {
                    return nearestChicken;
                }
                Entity nearestFruit = environment.getNearestTree(pawn.getPosition());
                if (nearestFruit != null && treesBearFruit() && nearestFruit.getInventory().getNumItems(ItemType.APPLE) > 0) {
                    return nearestFruit;
                }
                if (hungry && nearestChicken != null) {
                    return nearestChicken;
                }
            }

            Entity nearestTree = environment.getNearestTree(pawn.getPosition());
            Entity nearestRock = environment.getNearestRock(pawn.getPosition());

            // Wood enough for a stall but short of the stone a house takes: go for a rock,
            // rocks are scarce and the nearest-wins rule rarely picks one.
            Inventory inventory = pawn.getInventory();
            if (nearestRock != null
                    && inventory.getNumItems(ItemType.WOOD) >= Stall.WOOD_COST_TO_BUILD
                    && inventory.getNumItems(ItemType.STONE) < BuildingCatalog.getStoneCost(BuildingType.HOUSE)) {
                return nearestRock;
            }
            if (nearestTree != null && nearestRock != null) {
                if (Vector3.Distance(pawn.getPosition(), nearestTree.getPosition()) < Vector3.Distance(pawn.getPosition(), nearestRock.getPosition())) {
                    return nearestTree;
                }
                return nearestRock;
            }
            if (nearestTree != null) {
                return nearestTree;
            }
            return nearestRock;
        }

        private void executeSellResourcesBehavior(Pawn pawn) {
            Settlement settlement = (Settlement) entityRepository.getEntity(pawn.getCurrentSettlementId());
            
            Market market = settlement.getMarket();
            EntityId stallOwnerId = market.sellResources(pawn, PawnTraits.sellPriceBonusPercent(pawn));
            if (stallOwnerId == null) {
                Log.warning("Pawn " + pawn + " tried to sell resources but was unable to.");
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }
            else {
                // increase relationship with stall owner
                Entity stallOwner = entityRepository.getEntity(stallOwnerId);
                if (stallOwner is Pawn) {
                    Pawn stallOwnerPawn = (Pawn) stallOwner;
                    increaseRelationship(pawn, stallOwnerPawn, 1);
                }
                else if (stallOwner is Player) {
                    Player stallOwnerPlayer = (Player) stallOwner;
                    increaseRelationship(pawn, stallOwnerPlayer, 1);
                }
                else {
                    Log.error("Stall owner " + stallOwner + " is not a pawn or player.");
                }
            }
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private void executeWanderBehavior(Pawn pawn) {
            // 80% chance to skip
            if (random.range(0, 100) < 80) {
                return;
            }

            if (pawn.isCurrentlyInSettlement()) {
                Log.error("Pawn " + pawn + " is currently in a settlement but is trying to wander.");
                return;
            }
            Vector3 currentPosition = pawn.getPosition();
            Vector3 targetPosition = currentPosition + new Vector3(random.range(-1f, 1f), 0, random.range(-1f, 1f));
            // Horizontal only -- see the comment in Pawn.moveTowardsTargetEntity().
            Vector3 horizontalVelocity = VectorMath.normalized(targetPosition - currentPosition) * pawn.getSpeed();
            pawn.setVelocity(new Vector3(horizontalVelocity.X, pawn.getVelocity().Y, horizontalVelocity.Z));
        }

        private void executePurchaseFoodBehavior(Pawn pawn) {
            // pawn is assumed to be in a settlement
            if (!pawn.isCurrentlyInSettlement()) {
                Log.error("Pawn " + pawn + " is not currently in a settlement but is trying to purchase food.");
                return;
            }

            // purchase food from settlement market
            Settlement settlement = (Settlement) entityRepository.getEntity(pawn.getCurrentSettlementId());
            if (settlement == null) {
                Log.error("Pawn " + pawn.getName() + " is trying to purchase food from settlement market " + pawn.getCurrentSettlementId() + " but it does not exist.");
                return;
            }
            Market market = settlement.getMarket();
            EntityId stallOwnerId = market.purchaseFood(pawn);
            if (stallOwnerId == null) {
                Log.warning("Pawn " + pawn.getName() + " tried to purchase food from settlement market " + settlement + " but there was not enough food.");
            }
            else {
                // increase relationship with stall owner
                Entity stallOwner = entityRepository.getEntity(stallOwnerId);
                if (stallOwner is Pawn) {
                    Pawn stallOwnerPawn = (Pawn) stallOwner;
                    increaseRelationship(pawn, stallOwnerPawn, 1);
                }
                else if (stallOwner is Player) {
                    Player stallOwnerPlayer = (Player) stallOwner;
                    increaseRelationship(pawn, stallOwnerPlayer, 1);
                }
                else {
                    Log.error("Stall owner " + stallOwner + " is not a pawn or player.");
                }
            }
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private void executeConstructSettlementBehavior(Pawn pawn) {
            Vector3 targetPosition = pawn.getPosition();

            // get nation color
            Nation nation = nationRepository.getNation(pawn.getNationId());
            Rgba nationColor = nation.getColor();

            // remove wood
            Inventory inventory = pawn.getInventory();
            if (inventory.getNumItems(ItemType.WOOD) < Settlement.WOOD_COST_TO_BUILD) {
                Log.error("Pawn " + pawn + " does not have enough wood to build a settlement but is trying to.");
                return;
            }
            // never inside another nation's land, nor past what the era allows (EraRules)
            if (!nation.canFoundSettlement() || Territory.getFoundingObjection(entityRepository, nationRepository, targetPosition, nation.getId()) != null) {
                pawn.setCurrentBehaviorType(BehaviorType.WANDER);
                return;
            }
            inventory.removeItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);

            // create settlement
            Settlement settlement = new Settlement(targetPosition, nation.getId(), nationColor, nation.getName(), random);
            nation.addSettlement(settlement.getId());
            entityRepository.addEntity(settlement);
            pawn.setHomeSettlementId(settlement.getId());
            Territory.recordFounding(settlement, entityRepository, nationRepository);
            pawn.setSettler(false);
            eventProducer.produceSettlementFoundedEvent(settlement, nation, pawn.getId());
            pawn.setCurrentBehaviorType(BehaviorType.WANDER);
        }

        private void executeGoToHomeSettlementBehavior(Pawn pawn) {
            NationId nationId = pawn.getNationId();
            if (nationId == null) {
                Log.error("Pawn " + pawn + " has no nation id.");
                return;
            }
            Nation nation = nationRepository.getNation(nationId);
            Settlement homeSettlement = null;
            if (nation != null && nation.getNumberOfSettlements() > 0) {
                EntityId homeSettlementId = pawn.getHomeSettlementId();
                if (homeSettlementId == null) {
                    Log.error("Pawn " + pawn + " has no settlement id.");
                    return;
                }
                homeSettlement = (Settlement) entityRepository.getEntity(homeSettlementId);
            }

            // Head for home, whatever it was walking to before. It used to keep
            // a leftover target (the tree it was gathering from), walk there
            // and be let into the settlement from wherever that was.
            if (homeSettlement == null) {
                Log.warning("Pawn " + pawn + " has no settlement to go to.");
                return;
            }
            if (pawn.getTargetEntity() != homeSettlement) {
                pawn.setTargetEntity(homeSettlement);
            }
            if (pawn.isAtTargetEntity(20)) {
                // Inside, but still drawn: it mills about the town (SettlementLife).
                SettlementLife.admit(pawn, homeSettlement);
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }
            else {
                // move towards target entity
                pawn.moveTowardsTargetEntity();
            }
        }

        private void executeExitSettlementBehavior(Pawn pawn) {
            // if not in settlement
            if (!pawn.isCurrentlyInSettlement()) {
                Log.error("Pawn " + pawn + " is not currently in a settlement but is trying to exit one.");
                return;
            }

            Settlement settlement = entityRepository.getEntity(pawn.getCurrentSettlementId()) as Settlement;
            settlement.removeCurrentlyPresentEntity(pawn.getId());
            pawn.clearCurrentSettlementId();
            pawn.clearMillTarget();
            pawn.setPosition(settlement.getPosition() + new Vector3(random.range(-20, 20), 0, random.range(-20, 20)));
            pawn.setVisible(true);
            pawn.setColor(settlement.getColor());
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private void executePlantSaplingBehavior(Pawn pawn) {
            // if pawn has no saplings, skip
            if (pawn.getInventory().getNumItems(ItemType.SAPLING) == 0) {
                Log.warning("Pawn " + pawn + " has no saplings to plant.");
                return;
            }

            Vector3 position = pawn.getPosition();
            position += new Vector3(random.range(-5f, 5f), 0, random.range(-5f, 5f));
            position.Y = 2;
            Sapling tree = new Sapling(position, 3, random, tickCounter);
            entityRepository.addEntity(tree);
            pawn.getInventory().removeItem(ItemType.SAPLING, 1);
            pawn.setCurrentBehaviorType(BehaviorType.WANDER);
        }

        /**
        * Builds a stall in the settlement the pawn is in, paid from what it
        * carries and then the stockpile. A leader builds; so does a member at
        * home in a settlement its leader does not live in (a colony).
        */
        private void executeConstructStallBehavior(Pawn pawn) {
            // if not currently in a settlement
            EntityId currentSettlementId = pawn.getCurrentSettlementId();
            if (currentSettlementId == null) {
                Log.error("Pawn " + pawn + " is not in a settlement but is trying to build a stall.");
                return;
            }
            Settlement currentSettlement = (Settlement) entityRepository.getEntity(currentSettlementId);

            // if not enough wood and stone
            if (!Stall.canAffordToBuild(pawn.getInventory(), currentSettlement.getInventory())) {
                Log.error("Pawn " + pawn + " does not have enough wood and stone to build a stall, but is trying to.");
                return;
            }

            // only a member of the nation that owns the settlement
            Nation nation = nationRepository.getNation(pawn.getNationId());
            if (nation == null || !nation.getId().Equals(currentSettlement.getNationId())) {
                Log.error("Pawn " + pawn + " is trying to build a stall in another nation's settlement.");
                return;
            }

            // pay wood and stone
            Stall.payBuildCost(pawn.getInventory(), currentSettlement.getInventory());

            // build stall
            Market market = currentSettlement.getMarket();

            market.createStall();
            eventProducer.produceStallBuiltEvent(currentSettlement, nation, pawn.getId());
            pawn.setCurrentBehaviorType(BehaviorType.NONE);       
        }

        private void executePurchaseStallBehavior(Pawn pawn) {
            EntityId currentSettlementId = pawn.getCurrentSettlementId();
            if (currentSettlementId == null) {
                Log.error("Pawn " + pawn + " is not in a settlement but is trying to purchase a stall.");
                return;
            }
            Settlement currentSettlement = (Settlement) entityRepository.getEntity(currentSettlementId);
            Market market = currentSettlement.getMarket();

            // only in one of its own nation's settlements, as for the player
            // (PurchaseStallCommand): a merchant whose stall is in a foreign
            // market has no stall anywhere its nation lives
            Nation nation = nationRepository.getNation(pawn.getNationId());
            if (nation == null || !nation.getId().Equals(currentSettlement.getNationId())) {
                Log.warning("Pawn " + pawn + " is trying to purchase a stall in another nation's settlement.");
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }

            // if no stalls for sale
            if (market.getNumStallsForSale() == 0) {
                Log.warning("Pawn " + pawn + " is trying to purchase a stall but there are none for sale.");
                return;
            }

            // if not enough money
            if (pawn.getInventory().getNumItems(ItemType.COIN) < Stall.COIN_COST_TO_PURCHASE) {
                Log.warning("Pawn " + pawn + " is trying to purchase a stall but does not have enough money.");
                return;
            }

            // remove money
            pawn.getInventory().removeItem(ItemType.COIN, Stall.COIN_COST_TO_PURCHASE);

            // assign ownership of stall to pawn
            Stall stall = market.getStallForSale();
            stall.setOwnerId(pawn.getId());
            nation.setRole(pawn.getId(), NationRole.MERCHANT);
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private void executeTransferItemsToStallBehavior(Pawn pawn) {
            EntityId currentSettlementId = pawn.getCurrentSettlementId();
            if (currentSettlementId == null) {
                Log.error("Pawn " + pawn + " has no home settlement id.");
                return;
            }
            Settlement currentSettlement = (Settlement) entityRepository.getEntity(currentSettlementId);
            Market market = currentSettlement.getMarket();

            // no stall of its own in this market (a merchant visiting elsewhere)
            Stall stall = market.getStall(pawn.getId());
            if (stall == null) {
                Log.warning("Pawn " + pawn + " is trying to transfer items to a stall but owns none in this settlement.");
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }

            // if no wood or stone
            if (pawn.getInventory().getNumItems(ItemType.WOOD) == 0 && pawn.getInventory().getNumItems(ItemType.STONE) == 0) {
                Log.warning("Pawn " + pawn + " is trying to transfer items to a stall but has no wood or stone.");
                return;
            }

            // make what it can from the haul first (tools, cooked meat, and
            // planks and bricks where there is a workshop), then stock it all
            int crafted = MerchantCrafting.craftGoods(pawn.getInventory(), currentSettlement, stall.getInventory());
            if (crafted > 0) {
                Log.info("Pawn " + pawn.getName() + " crafted " + crafted + " batches of goods for their stall.");
            }
            stall.transferContentsFromEntity(pawn);

            // get half of coins from stall
            int numCoins = stall.getInventory().getNumItems(ItemType.COIN);
            int numCoinsToTransfer = numCoins / 2;
            stall.getInventory().removeItem(ItemType.COIN, numCoinsToTransfer);
            pawn.getInventory().addItem(ItemType.COIN, numCoinsToTransfer);

            Log.debug("Pawn " + pawn.getName() + " transferred items to their stall.");
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private void executeJoinNationBehavior(Pawn pawn) {
            Settlement nearestSettlement = (Settlement) environment.getNearestEntityOfType(pawn.getPosition(), EntityType.SETTLEMENT);
            Nation nation = nearestSettlement == null ? null : nationRepository.getNation(nearestSettlement.getNationId());
            if (nation == null) {
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());
            pawn.setColor(nation.getColor());
            eventProducer.produceNationJoinEvent(nation, pawn.getId());
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private void executeCreateNationBehavior(Pawn pawn) {
            Nation nation = new Nation(nationNameGenerator.generate(), pawn.getId(), random);
            nationRepository.addNation(nation);
            pawn.setNationId(nation.getId());
            pawn.setColor(nation.getColor());
            eventProducer.produceNationCreationEvent(nation);
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private void executeJoinRandomSettlementBehavior(Pawn pawn) {
            // get nation
            Nation nation = nationRepository.getNation(pawn.getNationId());

            // if no settlements
            if (nation.getNumberOfSettlements() == 0) {
                Log.warning("Pawn " + pawn + " is trying to join a random settlement but their nation has no settlements.");
                return;
            }

            // get random settlement, falling back to any with room for another resident
            EntityId settlementId = nation.getRandomSettlementId();
            Settlement settlement = (Settlement) entityRepository.getEntity(settlementId);
            if (settlement == null || !settlement.hasRoomForResident()) {
                settlement = findSettlementWithRoom(nation);
            }
            if (settlement == null) {
                Log.info("Pawn " + pawn + " found no settlement of " + nation.getName() + " with room for another resident.");
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }

            // join settlement
            pawn.setHomeSettlementId(settlement.getId());
            settlement.setResidentCount(settlement.getResidentCount() + 1);
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private Settlement findSettlementWithRoom(Nation nation) {
            foreach (EntityId id in nation.getSettlements()) {
                Settlement candidate = entityRepository.getEntity(id) as Settlement;
                if (candidate != null && candidate.hasRoomForResident()) {
                    return candidate;
                }
            }
            return null;
        }

        /**
        * A leader puts up whatever its settlement needs most that it can pay
        * for (BuildingPlanner), paying in wood and stone.
        */
        private void executeConstructBuildingBehavior(Pawn pawn) {
            EntityId currentSettlementId = pawn.getCurrentSettlementId();
            if (currentSettlementId == null) {
                Log.error("Pawn " + pawn + " is not in a settlement but is trying to construct a building.");
                return;
            }
            Settlement settlement = (Settlement) entityRepository.getEntity(currentSettlementId);
            BuildingType? type = BuildingPlanner.chooseNext(settlement, pawn.getInventory());
            if (type == null) {
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }
            BuildingCatalog.deductCost(pawn.getInventory(), settlement.getInventory(), type.Value);
            settlement.addBuilding(type.Value);
            Log.info("Pawn " + pawn.getName() + " built a " + BuildingCatalog.getDisplayName(type.Value) + " in " + settlement.getName() + ".");
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private void executeCollectProfitFromStallBehavior(Pawn pawn) {
            EntityId currentSettlementId = pawn.getCurrentSettlementId();
            if (currentSettlementId == null) {
                Log.error("Pawn " + pawn + " is not in a settlement but is trying to collect profit from a stall.");
                return;
            }
            Settlement currentSettlement = (Settlement) entityRepository.getEntity(currentSettlementId);
            Market market = currentSettlement.getMarket();

            // no stall of its own in this market (a merchant visiting elsewhere)
            Stall stall = market.getStall(pawn.getId());
            if (stall == null) {
                Log.warning("Pawn " + pawn + " is trying to collect profit from a stall but owns none in this settlement.");
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }

            // collect profit
            int profit = stall.getInventory().getNumItems(ItemType.COIN);
            pawn.getInventory().addItem(ItemType.COIN, profit);
            stall.getInventory().removeItem(ItemType.COIN, profit);
            Log.debug("Pawn " + pawn.getName() + " collected " + profit + " coins from their stall.");
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private void executeCollectFoodFromStallBehavior(Pawn pawn) {
            EntityId currentSettlementId = pawn.getCurrentSettlementId();
            if (currentSettlementId == null) {
                Log.error("Pawn " + pawn + " has no home settlement id.");
                return;
            }
            Settlement currentSettlement = (Settlement) entityRepository.getEntity(currentSettlementId);
            Market market = currentSettlement.getMarket();

            // no stall of its own in this market
            Stall stall = market.getStall(pawn.getId());
            if (stall == null) {
                Log.warning("Pawn " + pawn + " is trying to collect food from a stall but owns none in this settlement.");
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }

            // collect food
            ItemType? foodType = FoodItems.getMostNourishingFood(stall.getInventory());
            if (foodType == null) {
                Log.warning("Pawn " + pawn + " is trying to collect food from their stall but it holds none.");
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }
            int food = stall.getInventory().getNumItems(foodType.Value);
            int foodToTransfer = food / 2;
            if (foodToTransfer == 0) {
                foodToTransfer = 1;
            }
            pawn.getInventory().addItem(foodType.Value, foodToTransfer);
            stall.getInventory().removeItem(foodType.Value, foodToTransfer);
            Log.debug("Pawn " + pawn.getName() + " collected " + foodToTransfer + " " + foodType.Value + " from their stall.");
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        private void executeWithdrawSettlementFundsBehavior(Pawn pawn) {
            if (!pawn.isCurrentlyInSettlement()) {
                Log.warning("Pawn " + pawn + " is trying to withdraw funds from a settlement but is not in a settlement.");
                return;
            }
            Settlement settlement = (Settlement) entityRepository.getEntity(pawn.getCurrentSettlementId());
            int funds = settlement.getFunds();
            int fundsToWithdraw = funds / 2;
            if (fundsToWithdraw == 0) {
                fundsToWithdraw = 1;
            }
            settlement.removeFunds(fundsToWithdraw);
            pawn.getInventory().addItem(ItemType.COIN, fundsToWithdraw);
            Log.debug("Pawn " + pawn.getName() + " withdrew " + fundsToWithdraw + " funds from " + settlement.getName() + ".");
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        /**
        * A member at home with wood and stone nobody can buy hands it into the
        * settlement's stockpile, where the leader builds from it. Only into its
        * own nation's settlement.
        */
        private void executeContributeToStockpileBehavior(Pawn pawn) {
            Settlement settlement = pawn.getCurrentSettlementId() == null ? null : entityRepository.getEntity(pawn.getCurrentSettlementId()) as Settlement;
            if (settlement == null || pawn.getNationId() == null || !pawn.getNationId().Equals(settlement.getNationId())) {
                pawn.setCurrentBehaviorType(BehaviorType.NONE);
                return;
            }
            foreach (ItemType itemType in new ItemType[] { ItemType.WOOD, ItemType.STONE }) {
                int quantity = pawn.getInventory().getNumItems(itemType);
                if (quantity > 0) {
                    pawn.getInventory().removeItem(itemType, quantity);
                    settlement.getInventory().addItem(itemType, quantity);
                }
            }
            Log.info("Pawn " + pawn.getName() + " contributed to the stockpile of " + settlement.getName() + ".");
            pawn.setCurrentBehaviorType(BehaviorType.NONE);
        }

        /**
        * Walks straight away from the nearest settlement, to where a new one
        * may be founded.
        */
        private void executeSeekSettlementSiteBehavior(Pawn pawn) {
            pawn.setTargetEntity(null);
            Vector3 away = computeAwayFromSettlements(pawn.getPosition(), entityRepository.getEntitiesOfType(EntityType.SETTLEMENT));
            if (away == Vector3.Zero) {
                return;
            }
            Vector3 horizontalVelocity = away * pawn.getSpeed();
            pawn.setVelocity(new Vector3(horizontalVelocity.X, pawn.getVelocity().Y, horizontalVelocity.Z));
        }

        /**
        * The unit direction out of a cluster of settlements: each settlement
        * within Territory.NEIGHBOUR_DISTANCE pushes away from itself, the
        * nearer the harder (1/d). Walking away from only the nearest one sent
        * a settler between two settlements back and forth forever, so a world
        * whose generated land was all claimed never got another colony.
        * Falls back to the nearest settlement when none is in range, and
        * returns zero when there are no settlements at all.
        */
        public static Vector3 computeAwayFromSettlements(Vector3 position, List<Entity> settlements) {
            Vector3 push = Vector3.Zero;
            Entity nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (Entity settlement in settlements) {
                Vector3 offset = position - settlement.getPosition();
                offset.Y = 0;
                float distance = offset.Length();
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearest = settlement;
                }
                if (distance > Territory.NEIGHBOUR_DISTANCE) {
                    continue;
                }
                if (distance < 0.1f) {
                    offset = new Vector3(1, 0, 0);
                    distance = 1f;
                }
                push += offset / (distance * distance);
            }
            if (push.LengthSquared() < 1e-12f) {
                if (nearest == null) {
                    return Vector3.Zero;
                }
                push = position - nearest.getPosition();
                push.Y = 0;
                if (push.LengthSquared() < 0.01f) {
                    push = new Vector3(1, 0, 0);
                }
            }
            return VectorMath.normalized(push);
        }

        // helpers -----------------------------------------------------------------------------------------------

        private void increaseRelationship(Pawn pawn, Pawn otherPawn, int amount) {
            pawn.increaseRelationship(otherPawn, amount);
            otherPawn.increaseRelationship(pawn, amount);
        }

        private void increaseRelationship(Pawn pawn, Player player, int amount) {
            pawn.increaseRelationship(player, amount);
            player.increaseRelationship(pawn, amount);
            player.getStatus().update("Relationship with " + pawn.getName() + " is now " + player.getRelationships()[pawn.getId()] + ".");
        }
    }
}