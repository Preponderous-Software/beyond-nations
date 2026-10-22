using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;

namespace beyondnationstests.desktop.ui {

    /**
    * The command bar offers a button per building the settlement has room
    * for, enabled when the player can afford it, and
    * the --stage-settlement showcase stands up a town the player can build in.
    */
    public class TestBuildingCommands {

        private static Simulation stagedSimulation(out Settlement settlement) {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(42);
            Simulation simulation = new Simulation(gameConfig);
            settlement = SettlementShowcase.stage(simulation);
            return simulation;
        }

        private static List<string> labels(Simulation simulation) {
            List<string> result = new List<string>();
            foreach (WorldCommand command in WorldCommandBar.getCommands(simulation)) {
                result.Add(command.getLabel());
            }
            return result;
        }

        [Fact]
        public void testShowcase_StandsUpATownWithResidentsInside() {
            Settlement settlement;
            Simulation simulation = stagedSimulation(out settlement);

            Assert.Equal(SettlementTier.TOWN, settlement.getTier());
            Assert.Equal(SettlementShowcase.NumResidents, settlement.getCurrentlyPresentEntitiesCount());
            Assert.Equal(settlement.getMarket().getMaxNumStalls(), settlement.getMarket().getNumStalls());
            Assert.Equal(settlement.getId(), simulation.getPlayer().getHomeSettlementId());
        }

        [Fact]
        public void testInsideOwnSettlement_OffersAffordableBuildings() {
            Settlement settlement;
            Simulation simulation = stagedSimulation(out settlement);
            Player player = simulation.getPlayer();
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            settlement.getInventory().removeItem(ItemType.WOOD, settlement.getInventory().getNumItems(ItemType.WOOD));
            settlement.getInventory().removeItem(ItemType.STONE, settlement.getInventory().getNumItems(ItemType.STONE));

            // offered, but disabled, so the buttons after it do not move when it can be paid for
            Assert.False(WorldCommandBar.getCommands(simulation).Find(c => c.getLabel() == "Build House").isEnabled());

            player.getInventory().addItem(ItemType.WOOD, 100);
            player.getInventory().addItem(ItemType.STONE, 100);

            List<string> offered = labels(simulation);
            Assert.Contains("Build House", offered);
            Assert.Contains("Build Farm", offered);
            Assert.Contains("Build Storehouse", offered);
            Assert.True(player.isVisible());
        }
    }
}
