using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The ground kept for the player in the opening (territory/ReservedGround):
    * the trees and rocks by the spawn (#351), the ground around it (#433),
    * and the open ground a refused founding points to (#433).
    */
    public class TestReservedGround {
        private const float Step = 1f / 50f;
        // the seed of the #351 report: the nearest tree went from 33.6 m to 165 m in 40 seconds
        private const int StrippedSeed = 1450162717;

        private static Simulation newWorld(int seed) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            return new Simulation(config);
        }

        private static void run(Simulation simulation, int ticks) {
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(Step);
            }
        }

        /** Moves the clock on without running the world. */
        private static void advanceClock(Simulation simulation, int ticks) {
            for (int i = 0; i < ticks; i++) {
                simulation.getTickCounter().increment();
            }
        }

        private static List<Entity> inGrove(Simulation simulation, EntityType type) {
            List<Entity> found = new List<Entity>();
            foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(type)) {
                if (!entity.isMarkedForDeletion() && Territory.groundDistance(entity.getPosition(), ReservedGround.Spawn) <= ReservedGround.GROVE_RADIUS) {
                    found.Add(entity);
                }
            }
            return found;
        }

        [Fact]
        public void testTheTreesAndRocksBySpawn_SurviveTheFirstMinutes() {
            Simulation simulation = newWorld(StrippedSeed);
            simulation.fixedUpdate(Step);
            List<Entity> trees = inGrove(simulation, EntityType.TREE);
            List<Entity> rocks = inGrove(simulation, EntityType.ROCK);
            Assert.NotEmpty(trees);
            // two game minutes with the player idle at spawn
            run(simulation, 120 * 50);
            foreach (Entity tree in trees) {
                Assert.NotNull(simulation.getEntityRepository().getEntity(tree.getId()));
            }
            foreach (Entity rock in rocks) {
                Assert.NotNull(simulation.getEntityRepository().getEntity(rock.getId()));
            }
        }

        [Fact]
        public void testTheGrove_IsOnlyGuarded_InTheOpening() {
            Simulation simulation = newWorld(StrippedSeed);
            simulation.fixedUpdate(Step);
            ReservedGround reserved = simulation.getReservedGround();
            Entity tree = inGrove(simulation, EntityType.TREE)[0];
            Rock far = new Rock(new Vector3(ReservedGround.GROVE_RADIUS + 50, 1, 0));
            Assert.True(reserved.isOpening());
            Assert.True(reserved.isGuarded(tree));
            Assert.False(reserved.isGuarded(far));
            Assert.False(reserved.isGuarded(simulation.getPlayer()));

            // over once the player has a home
            Settlement home = new Settlement(new Vector3(500, 1, 500), null, Rgba.White, "Home", simulation.getRandom());
            simulation.getPlayer().setHomeSettlementId(home.getId());
            Assert.False(reserved.isOpening());
            Assert.False(reserved.isGuarded(tree));
            simulation.getPlayer().setHomeSettlementId(null);

            // or once the opening is past
            advanceClock(simulation, ReservedGround.OPENING_SECONDS * 50);
            Assert.False(reserved.isOpening());
            Assert.False(reserved.isGuarded(tree));
        }

        [Fact]
        public void testNoPawnFoundsNearAPlayerAboutToFound_SoTheirFirstFGoesThrough() {
            Simulation simulation = newWorld(102);
            Player player = simulation.getPlayer();
            simulation.fixedUpdate(Step);
            // a player who has created a nation but not yet founded, idle at the spawn
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getRandom(), simulation.getNationNameGenerator()).execute(player);
            Assert.True(simulation.getReservedGround().isFounding());
            // three game minutes: the #433 report saw nations crowd the open ground within the first day
            run(simulation, 180 * 50);
            int minDistance = simulation.getGameConfig().getMinDistanceBetweenSettlements();
            Assert.NotEmpty(simulation.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT));
            foreach (Entity settlement in simulation.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT)) {
                Assert.True(Territory.groundDistance(settlement.getPosition(), player.getPosition()) > minDistance + ReservedGround.FOUNDER_RADIUS,
                    "a settlement was founded " + Territory.groundDistance(settlement.getPosition(), player.getPosition()) + " from the player");
            }

            // anywhere within the founder's radius, the player's first F goes through
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            player.setPosition(player.getPosition() + new Vector3(ReservedGround.FOUNDER_RADIUS * 0.7f, 0, -ReservedGround.FOUNDER_RADIUS * 0.7f));
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom()).execute(player);
            Assert.StartsWith("Settlement founded.", player.getStatus().getStatus());
            Assert.False(simulation.getReservedGround().isOpening());
            Assert.False(simulation.getReservedGround().isFounding());
        }

        [Fact]
        public void testPawnFounding_KeepsClearOfAPlayerAboutToFound_AndOnlyThen() {
            Simulation simulation = newWorld(5);
            simulation.fixedUpdate(Step);
            ReservedGround reserved = simulation.getReservedGround();
            float minDistance = simulation.getGameConfig().getMinDistanceBetweenSettlements();
            Vector3 near = new Vector3(minDistance, 1, 0);
            // a player with no nation holds no ground: the world opens as it always has
            Assert.False(reserved.isFounding());
            Assert.True(reserved.allowsPawnFounding(near, minDistance));
            Assert.Equal(new Vector3(10, 1, 0), PawnBehaviorExecutor.findSettlementSite(new Vector3(10, 1, 0), new List<Entity>(), minDistance, reserved));

            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getRandom(), simulation.getNationNameGenerator()).execute(simulation.getPlayer());
            Assert.False(reserved.allowsPawnFounding(near, minDistance));
            Assert.True(reserved.allowsPawnFounding(new Vector3(minDistance + ReservedGround.FOUNDER_RADIUS + 1, 1, 0), minDistance));
            // a settler standing by the player walks out past it
            Vector3? site = PawnBehaviorExecutor.findSettlementSite(new Vector3(10, 1, 0), new List<Entity>(), minDistance, reserved);
            Assert.NotNull(site);
            Assert.True(reserved.allowsPawnFounding(site.Value, minDistance));
        }

        [Fact]
        public void testTheGroundARefusalPointsTo_IsHeldForThePlayer_ForAWhile() {
            DiplomacyFixture world = new DiplomacyFixture();
            Simulation simulation = world.simulation;
            Player player = world.player;
            ReservedGround reserved = simulation.getReservedGround();
            int minDistance = simulation.getGameConfig().getMinDistanceBetweenSettlements();

            // past the opening, so only the held site is reserved
            advanceClock(simulation, ReservedGround.OPENING_SECONDS * 50);
            // the player leads a nation far from the spawn, beside a pawn nation's camp
            world.settlement(world.pawnNation("Aland", 1), 5000, 5000);
            world.playerNation(1);
            player.setPosition(new Vector3(5000 + 100, 1, 5000));
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            new FoundSettlementCommand(world.nations, simulation.getEventProducer(),
                world.entities, simulation.getGameConfig(), simulation.getRandom()).execute(player);
            Assert.StartsWith("Too close to the", player.getStatus().getStatus());
            Assert.Null(reserved.getHeldSite());

            reserved.update();
            Vector3? held = reserved.getHeldSite();
            Assert.NotNull(held);
            Assert.True(SettlementSites.isOpen(world.entities, held.Value, minDistance, player.getNationId()));
            // a settler can no longer found where it would shut that ground
            Vector3 rival = held.Value + new Vector3(0, 0, minDistance);
            Assert.False(reserved.allowsPawnFounding(rival, minDistance));
            Assert.True(reserved.allowsPawnFounding(held.Value + new Vector3(0, 0, minDistance + ReservedGround.HELD_SITE_RADIUS + 1), minDistance));

            // for as long as it takes to walk there
            advanceClock(simulation, ReservedGround.HELD_SITE_SECONDS * 50 - 1);
            reserved.update();
            Assert.NotNull(reserved.getHeldSite());
            advanceClock(simulation, 1);
            reserved.update();
            Assert.Null(reserved.getHeldSite());
            Assert.True(reserved.allowsPawnFounding(rival, minDistance));
        }
    }
}
