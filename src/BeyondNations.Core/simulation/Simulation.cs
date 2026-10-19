using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * The running world: everything the simulation is made of, wired together,
    * and advanced one tick at a time.
    *
    * This is the composition root that WorldScreen used to be. It was extracted
    * here so that the host owns a window and a renderer and nothing else, and so
    * that the simulation can be advanced with no window at all -- which is how
    * it is tested.
    *
    * The Update and FixedUpdate split that Unity provided is preserved
    * deliberately: fixedUpdate advances the world at a fixed rate independent of
    * frame rate, and the host decides how often to call it.
    */
    public class Simulation {
        private GameConfig gameConfig;
        private RandomSource random;
        private TickCounter tickCounter;
        private TimeOfDay timeOfDay;
        private Player player;
        private EventRepository eventRepository;
        private EventProducer eventProducer;
        private EntityRepository entityRepository;
        private Environment environment;
        private WorldGenerator worldGenerator;
        private NationRepository nationRepository;
        private PawnBehaviorCalculator pawnBehaviorCalculator;
        private PawnBehaviorExecutor pawnBehaviorExecutor;
        private LagPreventer lagPreventer;
        private PawnNameGenerator pawnNameGenerator;
        private NationNameGenerator nationNameGenerator;
        private SettlementLife settlementLife;
        private Diplomacy diplomacy;
        private string playerTerritory = null;
        private bool playerTerritoryKnown = false;

        private int numPawnDeaths = 0;
        private int numPlayerDeaths = 0;

        // How high off the flat tile plane (y = 0) each mover's collider used
        // to rest in Unity. The ground has no slope (#220), so a constant per
        // entity type is all a clamp needs. Pawn and chicken values match the
        // height WorldGenerator already spawns them at; the player's matches
        // isGrounded()'s 0 < y < 2 band.
        private const float PlayerGroundHeight = 1f;
        private const float PawnGroundHeight = 1.5f;
        private const float ChickenGroundHeight = 0.5f;

        // How often each settlement recounts who calls it home.
        private const int ResidentRecountTicks = 50;

        // How often the player's position is checked against the borders.
        private const int TerritoryCheckTicks = 10;

        public Simulation(GameConfig gameConfig) {
            this.gameConfig = gameConfig;

            // One source of randomness for the whole world, seeded from config.
            // A seed of 0 means pick a fresh one, which is then reported so the
            // world can be reproduced later.
            int configuredSeed = gameConfig.getWorldSeed();
            random = configuredSeed == 0 ? new RandomSource() : new RandomSource(configuredSeed);
            Log.info("world seed: " + random.getSeed());

            pawnNameGenerator = new PawnNameGenerator(random);
            nationNameGenerator = new NationNameGenerator(random);

            tickCounter = new TickCounter(gameConfig.getTicksPerSecond());
            timeOfDay = new TimeOfDay(tickCounter); // #279: read by renderers only
            player = new Player(gameConfig.getPlayerWalkSpeed(), gameConfig.getPlayerRunSpeed(), tickCounter, gameConfig.getStatusExpirationTicks(), gameConfig.getRenderDistance(), random);
            eventRepository = new EventRepository();
            eventProducer = new EventProducer(eventRepository);
            entityRepository = new EntityRepository(random);
            environment = new Environment(gameConfig.getChunkSize(), gameConfig.getLocationScale(), entityRepository, random);
            worldGenerator = new WorldGenerator(environment, player, eventProducer, entityRepository, gameConfig, random, pawnNameGenerator);
            nationRepository = new NationRepository(random);
            nationRepository.getRelations().setEventProducer(eventProducer);
            pawnBehaviorCalculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);
            pawnBehaviorExecutor = new PawnBehaviorExecutor(environment, nationRepository, eventProducer, entityRepository, random, nationNameGenerator, tickCounter);
            player.setMetabolism(gameConfig.getPlayerMetabolism());
            entityRepository.addEntity(player);
            diplomacy = new Diplomacy(nationRepository, entityRepository, eventProducer, player);
            lagPreventer = new LagPreventer(gameConfig, tickCounter, entityRepository, environment);
            // Its own stream, so where pawns stroll in town never shifts the rest of a seeded world.
            settlementLife = new SettlementLife(new RandomSource(random.getSeed() ^ 0x5E771E));
            // The opening hint names a key, and key bindings belong to the host
            // input layer rather than to the simulation, so the host sets it.
        }

        public GameConfig getGameConfig() { return gameConfig; }
        public RandomSource getRandom() { return random; }
        public TickCounter getTickCounter() { return tickCounter; }
        public TimeOfDay getTimeOfDay() { return timeOfDay; }
        public Player getPlayer() { return player; }
        public EventRepository getEventRepository() { return eventRepository; }
        public EventProducer getEventProducer() { return eventProducer; }
        public EntityRepository getEntityRepository() { return entityRepository; }
        public Environment getEnvironment() { return environment; }
        public WorldGenerator getWorldGenerator() { return worldGenerator; }
        public NationRepository getNationRepository() { return nationRepository; }
        public Diplomacy getDiplomacy() { return diplomacy; }
        public PawnNameGenerator getPawnNameGenerator() { return pawnNameGenerator; }
        public NationNameGenerator getNationNameGenerator() { return nationNameGenerator; }
        public int getNumPawnDeaths() { return numPawnDeaths; }
        public int getNumPlayerDeaths() { return numPlayerDeaths; }

        public void fixedUpdate(float fixedDeltaTime) {
                    tickCounter.increment();
                    worldGenerator.update();
                    checkIfPlayerIsFallingIntoVoid();
                    player.getStatus().clearStatusIfExpired();

                    // list of positions to generate chunks at
                    List<Vector3> positionsToGenerateChunksAt = new List<Vector3>();

                    foreach (Entity entity in entityRepository.getEntities()) {
                        if (entity.getType() == EntityType.PAWN) {
                            Pawn pawn = (Pawn) entity;

                            // update energy
                            if (pawn.getEnergy() < 90) {
                                pawn.setEnergy(pawn.getEnergy() + FoodItems.consumeMostNourishingFood(pawn.getInventory()));
                            }

                            if (pawn.isCurrentlyInSettlement()) {
                                pawn.setEnergy(pawn.getEnergy() - pawn.getMetabolism() * gameConfig.getSettlementMetabolismMultiplier());
                            }
                            else {
                                pawn.setEnergy(pawn.getEnergy() - pawn.getMetabolism());
                            }

                            // A pawn under a leader's order hands it in before it can sell it.
                            NationOrderDelivery.deliver(pawn, entityRepository, nationRepository, player, eventProducer);

                            // Cadences count total ticks. getTick() is the tick within
                            // the current second (0..ticksPerSecond-1), so comparing
                            // against it made any value above ticksPerSecond mean
                            // "once per second".
                            int ticksBetweenBehaviorCalculations = gameConfig.getTicksBetweenBehaviorCalculations();
                            if (tickCounter.getTotalTicks() % ticksBetweenBehaviorCalculations == 0) {
                                pawn.setCurrentBehaviorType(pawnBehaviorCalculator.computeBehaviorType(pawn));
                            }

                            int ticksBetweenBehaviorExecutions = gameConfig.getTicksBetweenBehaviorExecutions();
                            if (tickCounter.getTotalTicks() % ticksBetweenBehaviorExecutions == 0) {
                                pawnBehaviorExecutor.executeBehavior(pawn, pawn.getCurrentBehaviorType());
                            }

                            if (!pawn.isCurrentlyInSettlement()) {
                                // Pawns the player has met show how they feel about the player.
                                string nameLine = PawnRelations.hasMet(pawn, player) ? PawnRelations.describe(pawn, player) : pawn.getName();
                                string nameTagText = nameLine + "\n" + pawn.getCurrentBehaviorDescription();
                                pawn.setNameTag(nameTagText);
                            }

                            if (!pawn.isCurrentlyInSettlement()) {
                                // check if pawn is falling into void
                                float ypos = pawn.getPosition().Y;
                                if (ypos < -10) {
                                    Log.debug("Entity " + pawn.getId() + " fell into void. Teleporting.");
                                    EntityId homeSettlementId = pawn.getHomeSettlementId();
                                    if (homeSettlementId != null) {
                                        // pawn has home settlement, so respawn at settlement
                                        Settlement settlement = (Settlement)entityRepository.getEntity(homeSettlementId);
                                        Vector3 newPosition = settlement.getPosition();
                                        newPosition = new Vector3(newPosition.X, newPosition.Y + 1, newPosition.Z);
                                        pawn.setPosition(newPosition);
                                    } else {
                                        // pawn is not in a settlement, so respawn at spawn
                                        pawn.setPosition(new Vector3(random.range(-100, 100), 100, random.range(-100, 100)));
                                    }
                                }

                                // check if pawn is in a new chunk
                                Chunk retrievedChunk = environment.getChunkAtPosition(pawn.getPosition());
                                if (retrievedChunk == null) {
                                    positionsToGenerateChunksAt.Add(pawn.getPosition());
                                }
                            }

                            // check if pawn is dead
                            if (pawn.getEnergy() <= 0) {
                                eventProducer.producePawnDeathEvent(pawn);
                                numPawnDeaths++;
                                player.getStatus().update(pawn.getName() + " has died.");
                                if (gameConfig.getRespawnPawns()) {
                                    pawn.setEnergy(100);
                                    if (gameConfig.getKeepInventoryOnDeath() == false) {
                                        dropInventory(pawn);
                                    }

                                    if (!pawn.isCurrentlyInSettlement()) {
                                        // teleport pawn's game object
                                        EntityId homeSettlementId = pawn.getHomeSettlementId();
                                        if (homeSettlementId != null) {
                                            // pawn has home settlement, so respawn at settlement
                                            Settlement settlement = (Settlement)entityRepository.getEntity(homeSettlementId);
                                            Vector3 newPosition = settlement.getPosition();
                                            newPosition = new Vector3(newPosition.X + random.range(-20, 20), newPosition.Y, newPosition.Z + random.range(-20, 20));
                                            pawn.setPosition(newPosition);
                                        }
                                        else {
                                            // pawn is not in a settlement, so respawn at spawn
                                            pawn.setPosition(new Vector3(0, 10, 0));
                                        }
                                    }                            
                                }
                                else {
                                    pawn.markForDeletion();

                                    if (pawn.getNationId() != null) {
                                        Nation nation = nationRepository.getNation(pawn.getNationId());
                                        NationRole role = nation.getRole(pawn.getId());

                                        // --- #267 ---
                                        // remove the dying pawn first, so it can neither inherit its own
                                        // leadership nor keep a sole-member nation from being disbanded
                                        nation.removeMember(pawn.getId());

                                        if (role == NationRole.LEADER) {
                                            // transfer leadership to another member
                                            if (nation.getNumberOfMembers() > 0) {
                                                nation.setLeaderId(nation.getOldestMemberId());
                                                Entity newLeader = entityRepository.getEntity(nation.getLeaderId());
                                                nation.setRole(newLeader.getId(), NationRole.LEADER);
                                                if (newLeader.getType() == EntityType.PAWN) {
                                                    player.getStatus().update(newLeader.getName() + " is now the leader of " + nation.getName() + ".");
                                                }
                                                else if (newLeader.getType() == EntityType.PLAYER) {
                                                    player.getStatus().update("You are now the leader of " + nation.getName() + ".");
                                                }
                                                else {
                                                    Log.info("ERROR: Oldest member of nation " + nation.getName() + " is not a pawn or player.");
                                                }
                                            }
                                            else {
                                                nationRepository.removeNation(nation);
                                                // --- #269 ---
                                                eventProducer.produceNationDisbandEvent(nation);

                                                // remove settlements
                                                foreach (EntityId settlementId in nation.getSettlements()) {
                                                    Settlement settlement = (Settlement) entityRepository.getEntity(settlementId);
                                                    settlement.markForDeletion();
                                                }

                                                // clear settlements
                                                nation.getSettlements().Clear();

                                                player.getStatus().update(nation.getName() + " has been disbanded.");
                                            }
                                        }
                                        else if (role == NationRole.MERCHANT) {
                                            // remove stall ownership
                                            foreach (EntityId settlementId in nation.getSettlements()) {
                                                Settlement settlement = (Settlement)entityRepository.getEntity(settlementId);
                                                foreach (Stall stall in settlement.getMarket().getStalls()) {
                                                    if (stall.getOwnerId() == pawn.getId()) {
                                                        stall.setOwnerId(null);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (entity.getType() == EntityType.SAPLING) {
                            Sapling sapling = (Sapling)entity;
                            if (sapling.isGrown()) {
                                // replace with tree
                                AppleTree tree = new AppleTree(sapling.getPosition(), 5, random);
                                entityRepository.addEntity(tree);
                                sapling.markForDeletion();
                            }
                        }
                        else if (entity.getType() == EntityType.CHICKEN) {
                            Chicken chicken = (Chicken)entity;
                            chicken.wander(fixedDeltaTime);
                        }
                        else if (entity.getType() == EntityType.SETTLEMENT) {
                            Settlement settlement = (Settlement)entity;

                            int totalTicks = tickCounter.getTotalTicks();
                            if (totalTicks % gameConfig.getTicksBetweenSettlementIncome() == 0) {
                                int numCoinsToGenerate = settlement.getMarket().getNumStalls();
                                if (numCoinsToGenerate > 0) {
                                    settlement.getInventory().addItem(ItemType.COIN, numCoinsToGenerate);
                                }
                            }

                            // buildings: farms stock the market, residents are recounted, and the
                            // settlement redraws itself if a stall or building has changed
                            if (totalTicks % BuildingCatalog.FARM_TICKS_PER_HARVEST == 0) {
                                settlement.harvestFarms();
                            }
                            if (totalTicks % ResidentRecountTicks == 0) {
                                settlement.setResidentCount(SettlementLife.countResidents(entityRepository, settlement.getId()));
                            }
                            settlement.refreshAppearance();
                        }
                    }

                    // nations: relations drift, pawn-led nations choose, hostility bites
                    diplomacy.update(tickCounter.getTotalTicks());
                    if (tickCounter.getTotalTicks() % TerritoryCheckTicks == 0) {
                        announceBorderCrossing();
                    }

                    int maxChunks = gameConfig.getMaxNumChunks();
                    int numChunks = environment.getNumChunks();

                    if (numChunks < maxChunks) {
                        foreach (Vector3 position in positionsToGenerateChunksAt) {
                            worldGenerator.generateChunkAtPosition(position);
                            worldGenerator.generateSurroundingChunksAtPosition(position);
                        }
                    }
            
                    player.fixedUpdate();
                    if (player.getEnergy() <= 0) {
                        eventProducer.producePlayerDeathEvent(player);
                        numPlayerDeaths++;
                        player.setEnergy(100);
                        if (gameConfig.getKeepInventoryOnDeath() == false) {
                            dropInventory(player);
                        }
                        player.getStatus().update("You died.");
                
                        if (player.getHomeSettlementId() != null) {
                            // player has home settlement, so respawn at settlement
                            Settlement homeSettlement = (Settlement)entityRepository.getEntity(player.getHomeSettlementId());
                            Vector3 newPosition = homeSettlement.getPosition();
                            newPosition = new Vector3(newPosition.X + random.range(-20, 20), newPosition.Y, newPosition.Z + random.range(-20, 20));
                            player.setPosition(newPosition);
                        }
                        else {
                            player.setPosition(new Vector3(random.range(-100, 100), 10, random.range(-100, 100)));
                        }
                    }

                    integrateMovement(fixedDeltaTime);

                    if (gameConfig.getLagPreventionEnabled()) {
                        lagPreventer.markEntitiesForDeletion();
                    }

                    deleteEntitiesMarkedForDeletion();
                }

        /**
        * Says so on the status line when the player walks from one nation's
        * land into another's, or out into the wilds.
        */
        private void announceBorderCrossing() {
            if (player.isCurrentlyInSettlement()) {
                return;
            }
            Settlement claim = Territory.findClaimAt(entityRepository, player.getPosition());
            string owner = claim == null ? null : claim.getNationName();
            if (owner == playerTerritory && playerTerritoryKnown) {
                return;
            }
            string left = playerTerritory;
            playerTerritory = owner;
            if (!playerTerritoryKnown) {
                // where the player starts is not a crossing
                playerTerritoryKnown = true;
                return;
            }
            if (owner != null && player.getNationId() != null && claim.getNationId() == player.getNationId()) {
                player.getStatus().update("You are back on your own land.");
            }
            else if (owner != null) {
                player.getStatus().update("You enter the land of " + Territory.describeFor(player, entityRepository, nationRepository) + ".");
            }
            else if (left != null) {
                player.getStatus().update("You leave the land of " + left + " for the wilds.");
            }
        }

        /**
        * Replaces Rigidbody (#220): gravity, the ground clamp and the jump
        * impulse for the player, and the same integrator for pawns and
        * chickens so every mover in the game falls and lands the same way.
        * Horizontal velocity was already decided above, by player.fixedUpdate(),
        * PawnBehaviorExecutor and Chicken.wander(); this only ever adds the
        * vertical component and moves positions.
        */
        private void integrateMovement(float fixedDeltaTime) {
            if (!player.isCurrentlyInSettlement()) {
                if (player.consumeJumpRequest() && player.isGrounded()) {
                    MovementIntegrator.jump(player);
                }
                MovementIntegrator.step(player, fixedDeltaTime, PlayerGroundHeight);
            }

            foreach (Entity entity in entityRepository.getEntities()) {
                if (entity.getType() == EntityType.PAWN) {
                    Pawn pawn = (Pawn) entity;
                    if (!pawn.isCurrentlyInSettlement()) {
                        MovementIntegrator.step(pawn, fixedDeltaTime, PawnGroundHeight);
                    }
                    else if (entityRepository.getEntity(pawn.getCurrentSettlementId()) is Settlement settlement) {
                        // inside, the pawn strolls about the town rather than falling under physics
                        settlementLife.mill(pawn, settlement, fixedDeltaTime, PawnGroundHeight);
                    }
                }
                else if (entity.getType() == EntityType.CHICKEN) {
                    MovementIntegrator.step(entity, fixedDeltaTime, ChickenGroundHeight);
                }
            }
        }

        /**
        * A death with keep-inventory off leaves the inventory where the entity
        * died, as drops that can be picked up again (#174), rather than
        * deleting it. Called before the respawn teleport, so the drops land at
        * the place of death. getEntities() returns a copy, so adding here
        * during the fixedUpdate walk is safe.
        */
        private void dropInventory(Entity entity) {
            foreach (ItemDrop drop in ItemDrop.dropContentsOf(entity.getInventory(), entity.getPosition())) {
                entityRepository.addEntity(drop);
            }
        }

        private void checkIfPlayerIsFallingIntoVoid() {
                    float ypos = player.getPosition().Y;
                    if (ypos < -10) {
                        eventProducer.producePlayerFallingIntoVoidEvent(player.getPosition());
                        if (player.getHomeSettlementId() != null) {
                            // player has home settlement, so respawn at settlement
                            Settlement homeSettlement = (Settlement)entityRepository.getEntity(player.getHomeSettlementId());
                            Vector3 newPosition = homeSettlement.getPosition();
                            newPosition = new Vector3(newPosition.X + random.range(-20, 20), newPosition.Y, newPosition.Z + random.range(-20, 20));
                            player.setPosition(newPosition);
                        }
                        else {
                            player.setPosition(new Vector3(random.range(-100, 100), 10, random.range(-100, 100)));
                        }
                        player.getStatus().update("You fell into the void. You have been teleported to the surface.");
                    }
                }

        private void deleteEntitiesMarkedForDeletion() {
                    List<Entity> entitiesToDelete = new List<Entity>();
                    foreach (Entity entity in entityRepository.getEntities()) {
                        if (entity.isMarkedForDeletion()) {
                            entitiesToDelete.Add(entity);
                        }
                    }
                    foreach (Entity entity in entitiesToDelete) {
                        // Deletion is purely logical: removing the entity from the
                        // repository removes it from the next snapshot, and there is no
                        // graphics resource to release.
                        entityRepository.removeEntity(entity);
                    }

                    // The player can be inside any settlement, including one
                    // whose nation has just been disbanded. Once it is gone the
                    // world HUD has nothing to show for "current settlement" and
                    // threw every frame, so the player is put back outside.
                    EntityId currentSettlementId = player.getCurrentSettlementId();
                    if (currentSettlementId != null && entityRepository.getEntity(currentSettlementId) == null) {
                        player.setCurrentSettlementId(null);
                        player.setVisible(true);
                    }
                }
    }
}
