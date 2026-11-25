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
        public void testFoundSettlement_GreyedWithTheEraThatLiftsTheCap_WhileTheEraAllowsNoMore() {
            // 0.8.0 playtest s07: the button vanished at the cap, so a touch
            // player was never told why, even carrying the wood and stone
            Simulation simulation = leaderWithOneSettlement(out Nation nation, out _);
            nation.setSettlementLimit(1);
            simulation.getPlayer().getInventory().addItem(ItemType.WOOD, FoundingCost.wood(1));
            simulation.getPlayer().getInventory().addItem(ItemType.STONE, FoundingCost.stone(1));

            WorldCommand command = findFound(simulation);
            Assert.NotNull(command);
            Assert.False(command.isEnabled());
            Assert.Equal("Reach the Village era for a second settlement.", command.getTooltip());
        }

        [Fact]
        public void testSettlementCapReason_NamesTheEraThatLiftsIt() {
            Assert.Equal("Reach the Village era for a second settlement.", EraRules.describeSettlementCap(1));
            Assert.Equal("Reach the Town era for a fourth settlement.", EraRules.describeSettlementCap(3));
            Assert.Equal("Reach the Kingdom era for a seventh settlement.", EraRules.describeSettlementCap(6));
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
        public void testFoundSettlement_GreyedOnAnotherNationsLand_WithWhoseLandItIs() {
            // #503: on another nation's land it was enabled, and only pressing it said why not
            Simulation simulation = leaderWithOneSettlement(out Nation nation, out _);
            nation.setSettlementLimit(3);
            Player player = simulation.getPlayer();
            player.getInventory().addItem(ItemType.WOOD, FoundingCost.wood(1));
            player.getInventory().addItem(ItemType.STONE, FoundingCost.stone(1));
            Nation other = new Nation("Hamletwood", new EntityId(), simulation.getRandom());
            simulation.getNationRepository().addNation(other);
            simulation.getEntityRepository().addEntity(new Settlement(player.getPosition() + new Vector3(30, 0, 0),
                other.getId(), other.getColor(), other.getName(), simulation.getRandom()));

            WorldCommand command = findFound(simulation);

            Assert.False(command.isEnabled());
            Assert.Equal("This land is claimed by Hamletwood. Found your settlement outside its borders.", command.getTooltip());
        }

        [Fact]
        public void testFoundSettlement_GreyedTooNearASettlement_WithHowFarApartTheyMustBe() {
            Simulation simulation = leaderWithOneSettlement(out Nation nation, out Settlement home);
            nation.setSettlementLimit(3);
            Player player = simulation.getPlayer();
            player.getInventory().addItem(ItemType.WOOD, FoundingCost.wood(1));
            player.getInventory().addItem(ItemType.STONE, FoundingCost.stone(1));
            int minDistance = simulation.getGameConfig().getMinDistanceBetweenSettlements();
            player.setPosition(home.getPosition() + new Vector3(0, 0, minDistance - 50));

            WorldCommand command = findFound(simulation);

            Assert.False(command.isEnabled());
            Assert.Equal("Too close to the " + home.getDisplayName() + ": " + (minDistance - 50)
                + " paces away, and settlements must be " + minDistance + " paces apart.", command.getTooltip());
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
