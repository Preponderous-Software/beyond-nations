using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;

namespace beyondnationstests.desktop.ui {

    /**
    * A disabled command bar button says what is missing (#444): the bar keeps
    * a button it cannot run yet in its place, and a grey button with no reason
    * left the player guessing that Found Settlement wants 50 wood (playtest
    * 0.7.0 s09).
    */
    public class TestDisabledCommandReasons {

        private static Simulation createSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(20260816);
            return new Simulation(config);
        }

        /** A town of the player's nation with the player inside it, carrying nothing, its stockpile empty. */
        private static Simulation insideOwnTown(out Settlement settlement) {
            Simulation simulation = createSimulation();
            settlement = SettlementShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            player.getInventory().clear();
            settlement.getInventory().clear();
            return simulation;
        }

        private static WorldCommand find(Simulation simulation, string labelStart) {
            WorldCommand command = WorldCommandBar.getCommands(simulation).Find(c => c.getLabel().StartsWith(labelStart));
            Assert.NotNull(command);
            return command;
        }

        private static void assertDisabledWith(Simulation simulation, string labelStart, string reason) {
            WorldCommand command = find(simulation, labelStart);
            Assert.False(command.isEnabled());
            Assert.Equal(reason, command.getTooltip());
        }

        [Fact]
        public void testFoundSettlement_SaysHowMuchWoodIsMissing() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            player.getInventory().clear();
            player.getInventory().addItem(ItemType.WOOD, 12);

            assertDisabledWith(simulation, "Found Settlement", "Needs 38 more wood.");
        }

        [Fact]
        public void testEnterSettlement_NoSettlementInTheWorld() {
            Simulation simulation = createSimulation();

            assertDisabledWith(simulation, "Enter Settlement", "No settlement has been founded yet.");
        }

        [Fact]
        public void testEnterSettlement_OutOfReach_SaysHowFarAndWhichWay() {
            Simulation simulation = createSimulation();
            Settlement settlement = SettlementShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            player.setPosition(settlement.getPosition() + new Vector3(0, 0, 400));

            string direction = Compass.describe(player.getPosition(), settlement.getPosition());
            assertDisabledWith(simulation, "Enter Settlement",
                "The " + settlement.getDisplayName() + " is about 400 paces to the " + direction + ". Walk within 50 to enter.");
        }

        [Fact]
        public void testPlantSapling_NoSapling() {
            Simulation simulation = createSimulation();
            simulation.getPlayer().getInventory().clear();

            assertDisabledWith(simulation, "Plant Sapling", "You have no saplings.");
        }

        [Fact]
        public void testMarketButtons_EmptyHanded() {
            Simulation simulation = insideOwnTown(out _);

            assertDisabledWith(simulation, "Purchase Food", "You have no coins.");
            assertDisabledWith(simulation, "Sell Resources", "You are carrying nothing the market buys.");
        }

        [Fact]
        public void testFeedResidents_NobodyHungry() {
            Simulation simulation = insideOwnTown(out _);

            assertDisabledWith(simulation, "Feed Residents", "No one here is hungry.");
        }

        [Fact]
        public void testFeedResidents_HungryButNoFood() {
            Simulation simulation = insideOwnTown(out Settlement settlement);
            EntityRepository entityRepository = simulation.getEntityRepository();
            for (int i = 0; i < entityRepository.getEntityCount(); i++) {
                Pawn pawn = entityRepository.getEntityAt(i) as Pawn;
                if (pawn != null && settlement.getId().Equals(pawn.getCurrentSettlementId())) {
                    pawn.getInventory().clear();
                    pawn.setEnergy(1);
                }
            }

            assertDisabledWith(simulation, "Feed Residents", "You have no food to give.");
        }

        [Fact]
        public void testBuildStall_NamesWhatIsMissingFromWhatThePlayerCarries() {
            Simulation simulation = insideOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();
            // the stockpile does not count towards the button or the command
            settlement.getInventory().addItem(ItemType.STONE, 100);
            player.getInventory().addItem(ItemType.WOOD, 8);

            assertDisabledWith(simulation, "Build Stall", "Needs 12 more wood and 5 more stone.");

            player.getInventory().addItem(ItemType.WOOD, 12);
            assertDisabledWith(simulation, "Build Stall", "Needs 5 more stone.");
        }

        [Fact]
        public void testBuildBuilding_CountsTheStockpileInWhatIsMissing() {
            Simulation simulation = insideOwnCamp(out Settlement settlement);
            simulation.getPlayer().getInventory().addItem(ItemType.WOOD, 5);
            settlement.getInventory().addItem(ItemType.WOOD, 10);

            assertDisabledWith(simulation, "Build House", "Needs 5 more wood and 15 more stone, counting the settlement's stockpile.");

            settlement.getInventory().addItem(ItemType.WOOD, 5);
            assertDisabledWith(simulation, "Build House", "Needs 15 more stone, counting the settlement's stockpile.");
        }

        [Fact]
        public void testCollectButtons_NothingToCollect() {
            Simulation simulation = insideOwnTown(out Settlement settlement);
            Stall stall = settlement.getMarket().getStallForSale();
            Assert.NotNull(stall);
            stall.setOwnerId(simulation.getPlayer().getId());

            assertDisabledWith(simulation, "Collect Coins", "Your stall holds no coins.");
            assertDisabledWith(simulation, "Collect Food", "Your stall holds no food.");
            assertDisabledWith(simulation, "Collect Stockpile", "The stockpile is empty.");
        }

        [Fact]
        public void testEveryDisabledButton_HasAReason() {
            // out in the world, inside a town with a stall, and in a fresh camp
            Simulation world = createSimulation();
            world.getPlayer().getInventory().clear();
            Simulation town = insideOwnTown(out Settlement settlement);
            settlement.getMarket().getStallForSale().setOwnerId(town.getPlayer().getId());
            Simulation camp = insideOwnCamp(out _);

            foreach (Simulation simulation in new[] { world, town, camp }) {
                List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
                Assert.Contains(commands, c => !c.isEnabled());
                foreach (WorldCommand command in commands) {
                    if (!command.isEnabled()) {
                        Assert.False(string.IsNullOrEmpty(command.getTooltip()), command.getLabel() + " is disabled without a reason");
                    }
                }
            }
        }

        [Fact]
        public void testEnabledButton_ShowsNoReason() {
            Simulation simulation = createSimulation();
            Settlement settlement = SettlementShowcase.stage(simulation);
            simulation.getPlayer().setPosition(settlement.getPosition());

            WorldCommand enter = find(simulation, "Enter Settlement");

            Assert.True(enter.isEnabled());
            Assert.Null(enter.getTooltip());
        }

        /** A new camp of the player's nation with the player inside it, carrying nothing, its stockpile empty. */
        private static Simulation insideOwnCamp(out Settlement settlement) {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom())
                .execute(player);
            settlement = (Settlement) simulation.getEntityRepository().getEntity(player.getHomeSettlementId());
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            player.getInventory().clear();
            settlement.getInventory().clear();
            return simulation;
        }
    }
}
