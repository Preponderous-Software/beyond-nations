using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui.boxes;

namespace beyondnationstests.desktop.ui {

    /**
    * The HUD's player box says which way the player faces, so a founding
    * hint's "to the south-west" can be followed (#374).
    */
    public class TestPlayerInfoBox {

        private static Simulation newSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            return new Simulation(config);
        }

        [Fact]
        public void testFacing_FollowsThePlayersTurn() {
            Simulation simulation = newSimulation();
            Player player = simulation.getPlayer();
            PlayerInfoBox box = new PlayerInfoBox(player, simulation.getNationRepository(), simulation.getEntityRepository());
            player.turn(-player.getYaw());

            Assert.Contains("Facing: north", box.getLines());

            // a quarter turn of yaw faces +X, which is east
            player.turn(System.MathF.PI / 2f);
            Assert.Contains("Facing: east", box.getLines());

            player.turn(System.MathF.PI * 3f / 4f);
            Assert.Contains("Facing: south-west", box.getLines());
        }

        [Fact]
        public void testFacing_ComesBeforeTheLandLine() {
            Simulation simulation = newSimulation();
            List<string> lines = new PlayerInfoBox(simulation.getPlayer(), simulation.getNationRepository(), simulation.getEntityRepository()).getLines();

            Assert.StartsWith("Facing: ", lines[lines.Count - 2]);
            Assert.StartsWith("Land: ", lines[lines.Count - 1]);
        }

        [Fact]
        public void testCompact_HasNoFacingLine() {
            Simulation simulation = newSimulation();
            PlayerInfoBox box = new PlayerInfoBox(simulation.getPlayer(), simulation.getNationRepository(), simulation.getEntityRepository());
            box.setCompact(true);

            Assert.DoesNotContain(box.getLines(), line => line.StartsWith("Facing: "));
        }
    }
}
