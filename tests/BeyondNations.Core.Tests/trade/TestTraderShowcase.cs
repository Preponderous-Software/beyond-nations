using System.Linq;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    /** The --stage-trader world (#487, #478): what the screenshots rely on. */
    public class TestTraderShowcase {

        private static Simulation staged(out TraderShowcase.Staged result) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            result = TraderShowcase.stage(simulation);
            return simulation;
        }

        [Fact]
        public void testStage_TheTraderHasTradedAndHoldsTheOneSlot() {
            TraderShowcase.Staged result;
            Simulation simulation = staged(out result);
            Nation own = result.diplomacy.playerNation;

            Assert.Equal(NationRole.TRADER, own.getRole(result.trader.getId()));
            Assert.False(Traders.hasRoomForTrader(own));
            Assert.True(Traders.hasGoodsToSell(result.trader.getInventory()));
            string name = result.trader.getName();
            var lines = simulation.getChronicle().getEntries().Select(e => e.getText()).ToList();
            Assert.Contains(lines, l => l.StartsWith(name + " took up trade for " + own.getName()));
            Assert.Contains(lines, l => l.Contains("trader " + name + " sold "));
            Assert.True(lines.Count(l => l.Contains("trader " + name + " bought ")) >= 2);
        }

        [Fact]
        public void testStage_TheOtherSerfIsInReach_AndAppointingIsRefusedOnlyForTheSlot() {
            TraderShowcase.Staged result;
            Simulation simulation = staged(out result);
            Player player = simulation.getPlayer();

            Assert.Same(result.serf, PawnRelations.findPawnInRange(player, simulation.getEnvironment()));
            Assert.StartsWith("One trader per settlement: " + result.trader.getName(),
                LeaderPawnCommands.whyNotAppointTrader(player, result.serf, simulation.getNationRepository(), simulation.getEntityRepository()));
        }

        [Fact]
        public void testStage_TheNeighbourIsTwiceAsStrong_AndAgreesToAnAllianceAtPlus35() {
            TraderShowcase.Staged result;
            Simulation simulation = staged(out result);
            Nation own = result.diplomacy.playerNation;
            Nation friendly = result.diplomacy.friendly;
            Diplomacy diplomacy = simulation.getDiplomacy();

            Assert.True(diplomacy.getStrength(friendly) >= 2 * diplomacy.getStrength(own));
            new ProposeAllianceCommand(simulation.getNationRepository(), simulation.getRealm()).execute(simulation.getPlayer(), friendly);
            Assert.True(simulation.getNationRepository().getRelations().isAllied(own, friendly));
        }
    }
}
