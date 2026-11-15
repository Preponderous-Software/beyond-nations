using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Playtest 0.8.0 s02 and s06: by the afternoon of day 1 nearly every
    * wandering pawn led a nation of one, so a new ruler had no stranger to
    * court. Before the Village era's day a stranger declares a nation of its
    * own only while the world has few; the rest stay strangers. And the pawn
    * the player walks up to stands still for them, so it can be reached.
    */
    public class TestStrangersStayRecruitable {
        private readonly RandomSource random = new RandomSource(20261115);

        private PawnBehaviorCalculator calculatorOnDay(int day, int nations, out Pawn pawn) {
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);
            for (int i = 0; i < nations; i++) {
                // leaders far off, with no settlement near the stranger
                Pawn leader = new Pawn(new Vector3(100000 + i * 10, 0, 0), "Leader " + i, random);
                entityRepository.addEntity(leader);
                Nation nation = new Nation("Nation " + i, leader.getId(), random);
                nationRepository.addNation(nation);
                leader.setNationId(nation.getId());
            }
            pawn = new Pawn(new Vector3(0, 0, 0), "Tilly Smith", random);
            pawn.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            entityRepository.addEntity(pawn);

            TickCounter tickCounter = new TickCounter();
            TimeOfDay timeOfDay = new TimeOfDay(tickCounter);
            // from the default start (0.3 of day 0) to noon of the given day
            int ticks = (int) ((day + 0.5f - TimeOfDay.DefaultStartFraction) * TimeOfDay.DefaultTicksPerDay);
            for (int i = 0; i < ticks; i++) {
                tickCounter.increment();
            }
            Assert.Equal(day, timeOfDay.getDay());
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, new GameConfig(), tickCounter, random);
            calculator.setTimeOfDay(timeOfDay);
            return calculator;
        }

        [Fact]
        public void onTheFirstDay_AStrangerWithTheWood_KeepsGathering_OnceTheWorldHasItsEarlyNations() {
            PawnBehaviorCalculator calculator = calculatorOnDay(0, PawnBehaviorCalculator.EARLY_NATION_LIMIT, out Pawn pawn);

            Assert.False(calculator.mayDeclareNation(pawn));
            Assert.Equal(BehaviorType.GATHER_RESOURCES, calculator.computeBehaviorType(pawn));
        }

        [Fact]
        public void onTheFirstDay_AYoungWorldStillGetsItsFirstFewNations() {
            PawnBehaviorCalculator calculator = calculatorOnDay(0, PawnBehaviorCalculator.EARLY_NATION_LIMIT - 1, out Pawn pawn);

            Assert.Equal(BehaviorType.CREATE_NATION, calculator.computeBehaviorType(pawn));
        }

        [Fact]
        public void fromTheVillageErasDay_AnyPawnMayDeclareANation() {
            PawnBehaviorCalculator calculator = calculatorOnDay(EraRules.VillageDay, PawnBehaviorCalculator.EARLY_NATION_LIMIT * 3, out Pawn pawn);

            Assert.Equal(BehaviorType.CREATE_NATION, calculator.computeBehaviorType(pawn));
        }

        private static Simulation createSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(1234);
            return new Simulation(config);
        }

        private static Pawn addPawnAt(Simulation simulation, Vector3 offset) {
            Pawn pawn = new Pawn(simulation.getPlayer().getPosition() + offset, "Tilly Smith", simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            return pawn;
        }

        [Fact]
        public void thePawnThePlayerWalksUpTo_IsHeld_WithinHoldRangeOnly() {
            Simulation simulation = createSimulation();
            simulation.getPlayer().setPosition(new Vector3(50000, 2, 50000));
            Pawn pawn = addPawnAt(simulation, new Vector3(PawnRelations.HOLD_RANGE - 1f, 0, 0));

            Assert.Same(pawn, PawnRelations.findPawnToHold(simulation.getPlayer(), simulation.getEnvironment()));
            // held a little beyond reach, so it waits while the player closes in
            Assert.True(PawnRelations.HOLD_RANGE > PawnRelations.INTERACTION_RANGE);

            pawn.setPosition(simulation.getPlayer().getPosition() + new Vector3(PawnRelations.HOLD_RANGE + 1f, 0, 0));
            Assert.Null(PawnRelations.findPawnToHold(simulation.getPlayer(), simulation.getEnvironment()));
        }

        [Fact]
        public void aHeldPawnStandsStill_ThroughTheSimulationsTicks() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            Pawn pawn = addPawnAt(simulation, new Vector3(3f, 0, 0));
            pawn.setVelocity(new Vector3(pawn.getSpeed(), 0, 0));
            Vector3 before = pawn.getPosition();

            for (int i = 0; i < 100; i++) {
                // the player stays put next to it
                simulation.fixedUpdate(1f / TickCounter.DefaultTicksPerSecond);
                player.setVelocity(Vector3.Zero);
            }

            Vector3 after = pawn.getPosition();
            Assert.True(Vector2.Distance(new Vector2(before.X, before.Z), new Vector2(after.X, after.Z)) < 0.5f,
                "moved from " + before + " to " + after);
        }
    }
}
