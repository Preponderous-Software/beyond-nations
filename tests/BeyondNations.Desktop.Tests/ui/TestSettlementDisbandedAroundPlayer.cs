using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;

namespace beyondnationstests {

    /**
    * The player can walk into any settlement, including one owned by a
    * nation of pawns. When the last of those pawns dies with respawn off,
    * the nation is disbanded and its settlements are deleted -- with the
    * player still inside one. The world HUD and command bar then cast the
    * player's current settlement id to a Settlement every frame, and a
    * deleted settlement made that a NullReferenceException that closed the
    * game.
    */
    public class TestSettlementDisbandedAroundPlayer {
        private const float FixedDeltaTime = 1f / 50f;

        [Fact]
        public void testPlayerInsideSettlementOfDisbandedNation_IsPutBackOutside() {
            // prepare
            GameConfig config = new GameConfig();
            config.setWorldSeed(20260816);
            config.setRespawnPawns(false);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();

            Pawn leader = new Pawn(new Vector3(0, 1.5f, 0), "Leader", simulation.getRandom());
            simulation.getEntityRepository().addEntity(leader);
            Nation nation = new Nation("Testland", leader.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            leader.setNationId(nation.getId());

            Settlement settlement = new Settlement(new Vector3(10, 0, 10), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());

            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            Assert.True(player.isCurrentlyInSettlement());
            leader.setEnergy(0);

            // run
            simulation.fixedUpdate(FixedDeltaTime);

            // check
            Assert.Null(simulation.getEntityRepository().getEntity(settlement.getId()));
            Assert.False(player.isCurrentlyInSettlement());
            Assert.True(player.isVisible());
            // what the HUD does every frame; it threw before the fix
            WorldCommandBar.getCommands(simulation);
        }
    }
}
