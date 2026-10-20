using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Scripts a player through the whole first arc on seed 42 using
    * the same Core commands the keys and HUD buttons run, and checks each
    * objective completes and pays out, with the simulation ticking as the host
    * ticks it.
    */
    public class TestObjectivesPlaythrough {
        private const float FixedDeltaTime = 1f / 50f;

        private readonly Simulation simulation;
        private readonly Player player;
        private readonly ObjectiveTracker tracker;

        public TestObjectivesPlaythrough() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            simulation = new Simulation(config);
            player = simulation.getPlayer();
            tracker = simulation.getObjectiveTracker();
        }

        private void tick(int count) {
            for (int i = 0; i < count; i++) {
                simulation.fixedUpdate(FixedDeltaTime);
            }
        }

        /** One second of game time: exactly one objective evaluation. */
        private void tickOneSecond() {
            tick(simulation.getTickCounter().getTicksPerSecond());
        }

        private void assertCurrent(string id) {
            Assert.Equal(id, tracker.getCurrent().getId());
        }

        private void interact() {
            new InteractCommand(simulation.getEnvironment(), simulation.getNationRepository(),
                simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getRandom()).execute(player);
        }

        private void gatherWoodUntil(int target) {
            gatherUntil(EntityType.TREE, ItemType.WOOD, target);
        }

        private void gatherStoneUntil(int target) {
            gatherUntil(EntityType.ROCK, ItemType.STONE, target);
        }

        /**
        * Walks to the nearest tree or rock that nothing else is standing by --
        * Interact talks to a pawn or enters a settlement before it chops --
        * and presses Interact, until the player carries enough.
        */
        private void gatherUntil(EntityType source, ItemType item, int target) {
            for (int attempt = 0; attempt < 300 && player.getInventory().getNumItems(item) < target; attempt++) {
                Entity resource = nearestClearResource(source);
                Assert.NotNull(resource);
                player.setPosition(resource.getPosition());
                interact();
                if (player.isCurrentlyInSettlement()) {
                    new ExitSettlementCommand(simulation.getEntityRepository()).execute(player);
                }
                tick(1); // the gathered resource is removed at the end of the tick
            }
            Assert.True(player.getInventory().getNumItems(item) >= target, item + " short of " + target);
        }

        private Entity nearestClearResource(EntityType source) {
            Entity best = null;
            float bestDistance = float.MaxValue;
            foreach (Entity candidate in simulation.getEntityRepository().getEntitiesOfType(source)) {
                if (candidate.isMarkedForDeletion() || somethingElseWithinReach(candidate.getPosition())) {
                    continue;
                }
                float distance = Vector3.Distance(player.getPosition(), candidate.getPosition());
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }

        private bool somethingElseWithinReach(Vector3 position) {
            foreach (EntityType blocker in new[] { EntityType.SETTLEMENT, EntityType.PAWN }) {
                foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(blocker)) {
                    if (Vector3.Distance(position, entity.getPosition()) < 6) {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool tooCloseToASettlement(Vector3 position, int minDistance) {
            foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT)) {
                if (Vector3.Distance(position, entity.getPosition()) < minDistance) {
                    return true;
                }
            }
            return false;
        }

        [Fact]
        public void testSeed42_ScriptedPlayer_CompletesTheWholeFirstArc() {
            tickOneSecond();
            assertCurrent("gather-wood");
            Assert.Equal("Wood 0/" + ObjectiveCatalog.firstWoodTarget(), tracker.getCurrentProgress().describe());

            // 1. gather wood: chop trees with Interact (E)
            gatherWoodUntil(ObjectiveCatalog.firstWoodTarget());
            int applesBefore = player.getInventory().getNumItems(ItemType.APPLE);
            tickOneSecond();
            assertCurrent("eat");
            Assert.Equal(applesBefore + 2, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.StartsWith("Objective complete: Gather wood.", player.getStatus().getStatus());

            // 2. eat: carry food and get hungry
            player.setEnergy(50);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);
            tickOneSecond();
            Assert.True(player.getMealsEaten() > 0);
            assertCurrent("join-nation");
            Assert.Equal(coinsBefore + Stall.COIN_COST_TO_PURCHASE / 4, player.getInventory().getNumItems(ItemType.COIN));

            // 3. create a nation (N)
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getRandom(), simulation.getNationNameGenerator()).execute(player);
            tickOneSecond();
            assertCurrent("found-settlement");

            // 4. found a settlement (F) once the wood is in hand
            gatherWoodUntil(Settlement.WOOD_COST_TO_BUILD);
            Assert.False(tracker.getCurrentProgress().isComplete());
            player.setPosition(new Vector3(player.getPosition().X, 1f, player.getPosition().Z));
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom()).execute(player);
            // a neighbour may add a diplomacy line after it (#306)
            Assert.StartsWith("Settlement founded.", player.getStatus().getStatus());
            tickOneSecond();
            assertCurrent("enter-settlement");

            // 5. enter it (E beside it)
            Settlement settlement = (Settlement) simulation.getEntityRepository().getEntity(player.getHomeSettlementId());
            player.setPosition(settlement.getPosition());
            interact();
            Assert.True(player.isCurrentlyInSettlement());
            tickOneSecond();
            assertCurrent("build-stall");

            // 6. build a stall (B) inside it; wood and stone are gathered outside first
            new ExitSettlementCommand(simulation.getEntityRepository()).execute(player);
            gatherWoodUntil(Stall.WOOD_COST_TO_BUILD);
            gatherStoneUntil(Stall.STONE_COST_TO_BUILD);
            Assert.True(Stall.canAffordToBuild(player.getInventory()));
            player.setPosition(settlement.getPosition());
            interact();
            Assert.True(player.isCurrentlyInSettlement());
            new BuildStallCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player);
            Assert.Equal("Stall built.", player.getStatus().getStatus());
            tickOneSecond();
            assertCurrent("grow-nation");
            Assert.Equal(6, tracker.getCompletedCount());
            Assert.StartsWith("Members ", tracker.getCurrentProgress().describe());

            // 7. grow the nation: on seed 42, settlers without a nation join
            // the new settlement on their own within a minute or two.
            for (int second = 0; second < 300 && tracker.getCurrent().getId() == "grow-nation"; second++) {
                tickOneSecond();
            }
            assertCurrent("sell-resources");
            Assert.True(ObjectiveCatalog.members(simulation) >= ObjectiveCatalog.ArcNationGrowthTarget);

            // 8. sell resources: keep trying while inside, as a player would.
            // A settler buys the stall and its coins come from other settlers
            // buying food there, so this takes a little over a minute.
            player.getInventory().addItem(ItemType.STONE, 20);
            int second8 = 0;
            for (; second8 < 1200 && tracker.getCurrent().getId() == "sell-resources"; second8++) {
                if (player.isCurrentlyInSettlement()) {
                    new SellResourcesAtMarketCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player);
                }
                tickOneSecond();
            }
            assertCurrent("second-settlement");
            Assert.True(player.getMarketSales() > 0);

            // 9. a second settlement, far enough from every other one
            new ExitSettlementCommand(simulation.getEntityRepository()).execute(player);
            int minDistance = simulation.getGameConfig().getMinDistanceBetweenSettlements();
            player.setPosition(settlement.getPosition() + new Vector3(minDistance * 3, 0, minDistance * 3));
            gatherWoodUntil(Settlement.WOOD_COST_TO_BUILD);
            Vector3 site = player.getPosition();
            for (int step = 0; step < 50 && tooCloseToASettlement(site, minDistance); step++) {
                site += new Vector3(minDistance, 0, 0);
            }
            player.setPosition(new Vector3(site.X, 1f, site.Z));
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom()).execute(player);
            // a neighbour may add a diplomacy line after it (#306)
            Assert.StartsWith("Settlement founded.", player.getStatus().getStatus());
            tickOneSecond();

            // The arc is done; ambitions take over and never run out.
            Assert.Equal(ObjectiveCatalog.firstArc().Count, tracker.getCompletedCount());
            Assert.True(tracker.isOnAmbitions());
            Assert.NotNull(tracker.getCurrent());
        }
    }
}
