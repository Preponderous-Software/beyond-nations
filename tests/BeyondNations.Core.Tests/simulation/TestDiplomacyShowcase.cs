using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestDiplomacyShowcase {

        private static Simulation staged(out DiplomacyShowcase.Staged result) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            result = DiplomacyShowcase.stage(simulation);
            return simulation;
        }

        [Fact]
        public void testStage_PlayerLeadsANationBetweenAFriendAndAFoe() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            NationRelations relations = simulation.getNationRepository().getRelations();

            Assert.Equal(simulation.getPlayer().getId(), result.playerNation.getLeaderId());
            Assert.Equal(NationRelationState.HOSTILE, relations.getState(result.playerNation, result.hostile));
            Assert.Equal(NationRelationState.FRIENDLY, relations.getState(result.playerNation, result.friendly));
            Assert.Equal(2, simulation.getDiplomacy().getSiegeProgress(result.playerSettlement));
            Assert.Equal("Besieged by " + result.hostile.getName() + " (2/" + Diplomacy.SIEGE_ROUNDS_TO_CAPTURE + ")", result.playerSettlement.getSiegeText());
        }

        [Fact]
        public void testStage_BordersTouchUpTheRoadFromThePlayer() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);

            float sum = result.playerSettlement.getClaimRadius() + result.hostileSettlement.getClaimRadius();
            Assert.Equal(2f * DiplomacyShowcase.HalfGap, sum, 2);
            // the player waits in the wilds, where the two borders can both be seen
            Assert.Null(Territory.findClaimAt(simulation.getEntityRepository(), simulation.getPlayer().getPosition()));
        }

        [Fact]
        public void testStage_LeftToRun_TheHostileNationTakesTheVillage() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);

            for (int i = 0; i < Diplomacy.ROUND_TICKS * (Diplomacy.SIEGE_ROUNDS_TO_CAPTURE - 2); i++) {
                simulation.fixedUpdate(1f / 50f);
            }

            Assert.Equal(result.hostile.getId(), result.playerSettlement.getNationId());
            // its only settlement gone, the player's nation has fallen
            Assert.Null(simulation.getPlayer().getNationId());
            Assert.Equal(1, simulation.getEventRepository().getNumberOfEvents(EventType.SettlementCapture));
        }
    }
}
