
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * #267: when a pawn that leads a nation dies with respawn off, leadership
    * passes to a living member, and a nation left with no members is
    * disbanded. Driven through Simulation.fixedUpdate() as the host calls it.
    */
    public class TestSimulationLeaderDeath {
        private const float FixedDeltaTime = 1f / 60f;

        private Simulation makeSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(20260816);
            config.setRespawnPawns(false);
            return new Simulation(config);
        }

        private Pawn addPawn(Simulation simulation, string name) {
            Pawn pawn = new Pawn(new Vector3(0, 1.5f, 0), name, simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            return pawn;
        }

        private Nation foundNation(Simulation simulation, Pawn leader) {
            Nation nation = new Nation("Testland", leader.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            leader.setNationId(nation.getId());
            return nation;
        }

        [Fact]
        public void testLeaderDeath_OtherPawnRemains_LeadershipPassesToSurvivor() {
            // prepare
            Simulation simulation = makeSimulation();
            Pawn leader = addPawn(simulation, "Leader");
            Pawn survivor = addPawn(simulation, "Survivor");
            Nation nation = foundNation(simulation, leader);
            nation.addMember(survivor.getId());
            survivor.setNationId(nation.getId());
            leader.setEnergy(0);

            // run
            simulation.fixedUpdate(FixedDeltaTime);

            // check
            Assert.True(leader.isMarkedForDeletion());
            Assert.Equal(survivor.getId(), nation.getLeaderId());
            Assert.Equal(NationRole.LEADER, nation.getRole(survivor.getId()));
            Assert.False(nation.isMember(leader.getId()));
            Assert.Equal(1, nation.getNumberOfMembers());
            Assert.Equal(1, simulation.getNationRepository().getNumberOfNations());
            Assert.Equal("Survivor is now the leader of Testland.", simulation.getPlayer().getStatus().getStatus());
        }

        [Fact]
        public void testLeaderDeath_PlayerRemains_PlayerBecomesLeader() {
            // prepare
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            Pawn leader = addPawn(simulation, "Leader");
            Nation nation = foundNation(simulation, leader);
            nation.addMember(player.getId());
            player.setNationId(nation.getId());
            leader.setEnergy(0);

            // run
            simulation.fixedUpdate(FixedDeltaTime);

            // check
            Assert.Equal(player.getId(), nation.getLeaderId());
            Assert.Equal(NationRole.LEADER, nation.getRole(player.getId()));
            Assert.False(nation.isMember(leader.getId()));
            Assert.Equal("You are now the leader of Testland.", player.getStatus().getStatus());
        }

        [Fact]
        public void testLeaderDeath_SoleMember_NationDisbanded() {
            // prepare
            Simulation simulation = makeSimulation();
            Pawn leader = addPawn(simulation, "Leader");
            Nation nation = foundNation(simulation, leader);
            Settlement settlement = new Settlement(new Vector3(500, 0, 500), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
            leader.setEnergy(0);

            // run
            simulation.fixedUpdate(FixedDeltaTime);

            // check
            Assert.Equal(0, simulation.getNationRepository().getNumberOfNations());
            Assert.True(settlement.isMarkedForDeletion());
            Assert.Empty(nation.getSettlements());
            Assert.Equal("Testland has been disbanded.", simulation.getPlayer().getStatus().getStatus());
        }
    }
}
