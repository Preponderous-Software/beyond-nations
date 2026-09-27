
using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * #174: a death with keep-inventory off leaves the inventory in the world
    * as drops, driven through Simulation.fixedUpdate() as the host calls it.
    */
    public class TestSimulationDeathDrops {
        private const float FixedDeltaTime = 1f / 60f;

        private Simulation makeSimulation(bool keepInventoryOnDeath) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(20260816);
            config.setKeepInventoryOnDeath(keepInventoryOnDeath);
            return new Simulation(config);
        }

        [Fact]
        public void testPlayerDeath_KeepInventoryOff_InventoryDroppedWherePlayerDied() {
            // prepare
            Simulation simulation = makeSimulation(false);
            Player player = simulation.getPlayer();
            player.setPosition(new Vector3(7, 2, -3));
            int coins = player.getInventory().getNumItems(ItemType.COIN);
            Assert.True(coins > 0);
            player.setEnergy(0);

            // run
            simulation.fixedUpdate(FixedDeltaTime);

            // check
            List<Entity> drops = simulation.getEntityRepository().getEntitiesOfType(EntityType.ITEM_DROP);
            Assert.Single(drops);
            ItemDrop drop = (ItemDrop) drops[0];
            Assert.Equal(ItemType.COIN, drop.getItemStack().getItemType());
            Assert.Equal(coins, drop.getItemStack().getQuantity());
            Assert.Equal(new Vector3(7, ItemDrop.GroundHeight, -3), drop.getPosition());
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testPlayerDeath_KeepInventoryOn_NoDrops() {
            // prepare
            Simulation simulation = makeSimulation(true);
            Player player = simulation.getPlayer();
            int coins = player.getInventory().getNumItems(ItemType.COIN);
            player.setEnergy(0);

            // run
            simulation.fixedUpdate(FixedDeltaTime);

            // check
            Assert.Empty(simulation.getEntityRepository().getEntitiesOfType(EntityType.ITEM_DROP));
            Assert.Equal(coins, player.getInventory().getNumItems(ItemType.COIN));
        }
    }
}
