using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The player's first village is not taken before the player has had a
    * fair chance to defend it: sheltered until the Town era, whether the
    * player founded the nation or joined it, and then given warning before
    * any siege may begin (EraRules). Played out on real worlds whose
    * player's village used to fall within minutes.
    */
    public class TestEarlySiegeGrace {
        private const float Step = 1f / 50f;

        private static Simulation newWorld(int seed) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            config.setTicksPerSecond(50);
            return new Simulation(config);
        }

        private static void run(Simulation simulation, int ticks) {
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(Step);
            }
        }

        [Fact]
        public void testAJoinedNationsVillage_IsNotBesiegedInTheCampEra() {
            // seed 1: a player who joined the first nation to hand used to see
            // it besieged 4.5 minutes later and gone a minute after that
            Simulation simulation = newWorld(1);
            while (simulation.getNationRepository().getNumberOfNations() < 3) {
                run(simulation, 1);
            }
            Player player = simulation.getPlayer();
            new NationJoinCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom()).execute(player);
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            Assert.NotEqual(player.getId(), nation.getLeaderId());

            for (int second = 0; second < 7 * 60; second++) {
                run(simulation, 50);
                Assert.Equal(Era.CAMP, simulation.getEraTracker().getEra());
                Assert.NotNull(simulation.getNationRepository().getNation(nation.getId()));
                foreach (EntityId id in nation.getSettlements()) {
                    Settlement settlement = (Settlement) simulation.getEntityRepository().getEntity(id);
                    Assert.Null(simulation.getDiplomacy().getBesieger(settlement));
                }
            }
            Assert.Equal(nation.getId(), player.getNationId());
        }

        [Fact]
        public void testAStagedTownEraVillage_StandsThroughTheWarning() {
            // TestWorldEvents' calmed diplomacy showcase: on seed 42 the
            // player's village used to be besieged 74 s in and taken at 99 s
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = DiplomacyShowcase.stage(simulation);
            Vector3 at = s.hostileSettlement.getPosition();
            Vector3 away = Vector3.Normalize(at - s.playerSettlement.getPosition());
            s.hostileSettlement.setPosition(at + away * 600f);
            simulation.getNationRepository().getRelations().adjust(s.playerNation, s.hostile, -DiplomacyShowcase.HostileScore);
            simulation.getDiplomacy().runRound();
            Assert.Null(simulation.getDiplomacy().getBesieger(s.playerSettlement));

            EraTracker eras = simulation.getEraTracker();
            while (eras.getSiegeWarningTicksLeft(simulation) > 0) {
                run(simulation, Diplomacy.ROUND_TICKS);
                Assert.True(eras.getSiegeWarningTicksLeft(simulation) <= 0 || simulation.getDiplomacy().getBesieger(s.playerSettlement) == null,
                    "besieged with " + eras.getSiegeWarningTicksLeft(simulation) + " ticks of warning left");
                Assert.Equal(s.playerNation.getId(), s.playerSettlement.getNationId());
            }
        }
    }
}
