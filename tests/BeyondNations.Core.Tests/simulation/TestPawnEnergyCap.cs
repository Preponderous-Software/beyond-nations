using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * A pawn's energy stops at a full bar, as the player's does (#334): the
    * pawn energy step eats below 90, and a cooked meat eaten at 89 used to
    * leave the pawn at 134.
    */
    public class TestPawnEnergyCap {
        private const float FixedDeltaTime = 1f / 50f;

        [Fact]
        public void testAPawnEatingNearAFullBar_StopsAtMaxEnergy() {
            // prepare
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Pawn pawn = new Pawn(new Vector3(30, 0, 30), "Ada", simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            pawn.setEnergy(89f);
            pawn.getInventory().addItem(ItemType.COOKED_MEAT, 1);

            // run
            simulation.fixedUpdate(FixedDeltaTime);

            // check: the meal was eaten, and the bar is no fuller than full
            Assert.Equal(0, pawn.getInventory().getNumItems(ItemType.COOKED_MEAT));
            Assert.True(pawn.getEnergy() <= Pawn.MaxEnergy, "pawn energy " + pawn.getEnergy());
            Assert.True(pawn.getEnergy() > Pawn.MaxEnergy - 1, "pawn energy " + pawn.getEnergy());
        }
    }
}
