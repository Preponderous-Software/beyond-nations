using Xunit;

using beyondnations;
using beyondnations.desktop.ui;

namespace beyondnationstests.desktop.ui {

    /**
    * The stall buttons the command bar offers a player who owns a stall in
    * the settlement they are standing in. Collect Food counts every kind of
    * food, since the command collects the most nourishing first (#419).
    */
    public class TestStallCommands {

        private static Simulation insideOwnStall(out Stall stall) {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(42);
            Simulation simulation = new Simulation(gameConfig);
            Settlement settlement = SettlementShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);

            stall = settlement.getMarket().getStallForSale();
            Assert.NotNull(stall);
            stall.setOwnerId(player.getId());
            return simulation;
        }

        private static WorldCommand collectFood(Simulation simulation) {
            return WorldCommandBar.getCommands(simulation).Find(c => c.getLabel().StartsWith("Collect Food ("));
        }

        [Fact]
        public void testCollectFood_MeatOnly_IsCountedAndEnabled() {
            // prepare
            Stall stall;
            Simulation simulation = insideOwnStall(out stall);
            stall.getInventory().addItem(ItemType.CHICKEN_MEAT, 4);

            // run
            WorldCommand command = collectFood(simulation);

            // check: the command would collect this meat, so the button offers it
            Assert.NotNull(command);
            Assert.Equal("Collect Food (4)", command.getLabel());
            Assert.True(command.isEnabled());
        }

        [Fact]
        public void testCollectFood_MeatAndApples_CountsBoth() {
            // prepare
            Stall stall;
            Simulation simulation = insideOwnStall(out stall);
            stall.getInventory().addItem(ItemType.APPLE, 6);
            stall.getInventory().addItem(ItemType.CHICKEN_MEAT, 4);

            // run / check
            Assert.Equal("Collect Food (10)", collectFood(simulation).getLabel());
        }

        [Fact]
        public void testCollectFood_NoFood_IsDisabled() {
            // prepare
            Stall stall;
            Simulation simulation = insideOwnStall(out stall);
            stall.getInventory().addItem(ItemType.WOOD, 5);

            // run
            WorldCommand command = collectFood(simulation);

            // check
            Assert.Equal("Collect Food (0)", command.getLabel());
            Assert.False(command.isEnabled());
        }
    }
}
