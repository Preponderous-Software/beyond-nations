using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * A nation may hold more than one settlement (#145): the command bar
    * offers Found Settlement again once the era allows another, so a touch
    * player can found it too, and the nation screen lists what the nation
    * holds.
    */
    public class TestMultipleSettlementsUi {

        /** The player leads a nation with one settlement, and stands well away from it. */
        private static Simulation leaderWithOneSettlement(out Nation nation, out Settlement home) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(20260816);
            Simulation simulation = new Simulation(config);
            home = SettlementShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            nation = simulation.getNationRepository().getNation(player.getNationId());
            player.getInventory().clear();
            player.setPosition(home.getPosition() + new Vector3(0, 0, 400));
            return simulation;
        }

        private static WorldCommand findFound(Simulation simulation) {
            return WorldCommandBar.getCommands(simulation).Find(c => c.getLabel().StartsWith("Found Settlement"));
        }

        [Fact]
        public void testFoundSettlement_NotOffered_WhileTheEraAllowsNoMore() {
            Simulation simulation = leaderWithOneSettlement(out Nation nation, out _);
            nation.setSettlementLimit(1);

            Assert.Null(findFound(simulation));
        }

        [Fact]
        public void testFoundSettlement_OfferedAgain_WhenTheEraAllowsAnother_WithItsHigherCost() {
            Simulation simulation = leaderWithOneSettlement(out Nation nation, out _);
            nation.setSettlementLimit(3);

            WorldCommand command = findFound(simulation);
            Assert.NotNull(command);
            Assert.False(command.isEnabled());
            Assert.Equal("Needs " + FoundingCost.wood(1) + " more wood and " + FoundingCost.stone(1) + " more stone.", command.getTooltip());

            simulation.getPlayer().getInventory().addItem(ItemType.WOOD, FoundingCost.wood(1));
            simulation.getPlayer().getInventory().addItem(ItemType.STONE, FoundingCost.stone(1));
            Assert.True(findFound(simulation).isEnabled());
        }

        [Fact]
        public void testFoundSettlement_FromTheBar_FoundsASecond_AndKeepsTheHome() {
            Simulation simulation = leaderWithOneSettlement(out Nation nation, out Settlement home);
            nation.setSettlementLimit(3);
            Player player = simulation.getPlayer();
            player.setPosition(home.getPosition() + new Vector3(0, 0, simulation.getGameConfig().getMinDistanceBetweenSettlements() * 3));
            player.getInventory().addItem(ItemType.WOOD, FoundingCost.wood(1));
            player.getInventory().addItem(ItemType.STONE, FoundingCost.stone(1));

            findFound(simulation).execute();

            Assert.Equal(2, nation.getNumberOfSettlements());
            Assert.Equal(home.getId(), player.getHomeSettlementId());
        }

        [Fact]
        public void testNationScreen_ListsTheNationsSettlements_AndWhatAnotherCosts() {
            Simulation simulation = leaderWithOneSettlement(out Nation nation, out Settlement home);
            nation.setSettlementLimit(3);

            List<string> lines = NationScreen.describeOwnSettlements(simulation, nation, true);

            Assert.Equal("Settlements: 1 of the 3 this era allows:", lines[0]);
            Assert.StartsWith("  " + home.getDisplayName() + ": ", lines[1]);
            Assert.EndsWith(" residents, 400 paces " + Compass.describe(simulation.getPlayer().getPosition(), home.getPosition()) + " (your home).", lines[1]);
            Assert.StartsWith("Another settlement costs " + FoundingCost.describe(nation) + ":", lines[2]);
            Assert.Contains(NationScreen.getHeaderLines(simulation), line => line == lines[0]);
        }

        [Fact]
        public void testNationScreen_AtTheEraLimit_SaysTheNextEraBringsMore() {
            Simulation simulation = leaderWithOneSettlement(out Nation nation, out _);
            nation.setSettlementLimit(1);

            List<string> lines = NationScreen.describeOwnSettlements(simulation, nation, true);

            Assert.Equal("Reach the next era to found another settlement.", lines[lines.Count - 1]);
        }
    }
}
