using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.boxes;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * What the crafting screen, the command bar shortcuts and the inventory
    * listings say about crafting (#47). None of it needs a window.
    */
    public class TestCraftingUi {

        private static Simulation newSimulation() {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(42);
            Simulation simulation = new Simulation(gameConfig);
            simulation.getPlayer().getInventory().clear();
            return simulation;
        }

        private static List<string> labels(Simulation simulation) {
            List<string> result = new List<string>();
            foreach (WorldCommand command in WorldCommandBar.getCommands(simulation)) {
                result.Add(command.getLabel());
            }
            return result;
        }

        private static WorldCommand find(Simulation simulation, string label) {
            foreach (WorldCommand command in WorldCommandBar.getCommands(simulation)) {
                if (command.getLabel() == label) {
                    return command;
                }
            }
            return null;
        }

        [Fact]
        public void testRows_OneForEveryRecipe_SayingWhatIsMissing() {
            Simulation simulation = newSimulation();
            simulation.getPlayer().getInventory().addItem(ItemType.WOOD, 4);

            List<CraftingScreen.Row> rows = CraftingScreen.getRows(simulation);

            Assert.Equal(RecipeBook.getAll().Count, rows.Count);
            Assert.Equal("Axe: 4 wood, 2 stone", rows[0].Heading);
            Assert.False(rows[0].CanCraft);
            Assert.Equal("Need 2 more stone", rows[0].Status);
            CraftingScreen.Row planks = rows.Find(r => r.Recipe == RecipeBook.Planks);
            Assert.Equal("Planks x2: 3 wood", planks.Heading);
            Assert.Contains("(needs a Workshop)", planks.Benefit);
            Assert.Equal("Need a Workshop (inside a settlement)", planks.Status);
        }

        [Fact]
        public void testRows_ReadyRecipe_SaysHowManyAndWhatIsHeld() {
            Simulation simulation = newSimulation();
            Inventory inventory = simulation.getPlayer().getInventory();
            inventory.addItem(ItemType.WOOD, 8);
            inventory.addItem(ItemType.STONE, 4);
            inventory.addItem(ItemType.AXE, 1);
            inventory.setToolWear(ItemType.AXE, 6);

            CraftingScreen.Row axe = CraftingScreen.getRows(simulation)[0];

            Assert.True(axe.CanCraft);
            Assert.Equal("Ready: you can make 2  |  have 1 (14 uses left)", axe.Status);
        }

        [Fact]
        public void testLocationLine_ExplainsWhereWorkshopRecipesWork() {
            Simulation simulation = newSimulation();
            Assert.StartsWith("Out in the world", CraftingScreen.getLocationLine(simulation));
        }

        /** The player leads a Camp-era nation and stands in its only settlement, which has no Workshop. */
        private static Settlement leadCampEraSettlement(Simulation simulation) {
            Player player = simulation.getPlayer();
            Nation nation = new Nation("Testland", player.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            player.setNationId(nation.getId());
            Settlement settlement = new Settlement(player.getPosition(), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
            simulation.getEraTracker().applyRules(simulation);
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            return settlement;
        }

        [Fact]
        public void testWorkshopLockedByEra_SaysWhenItUnlocks_NotToBuildOne() {
            Simulation simulation = newSimulation();
            Settlement settlement = leadCampEraSettlement(simulation);
            Assert.Equal(Era.CAMP, simulation.getEraTracker().getEra());
            Assert.False(settlement.isBuildingUnlocked(BuildingType.WORKSHOP));

            string line = CraftingScreen.getLocationLine(simulation);
            Assert.Equal("In the " + settlement.getDisplayName() + ", which has no Workshop: the Workshop is unlocked in the Village era."
                + " See the era panel for what it takes.", line);
            Assert.DoesNotContain("build one", line);

            CraftingScreen.Row planks = CraftingScreen.getRows(simulation).Find(r => r.Recipe == RecipeBook.Planks);
            Assert.Equal("Locked: The Workshop is unlocked in the Village era.", planks.Status);
        }

        [Fact]
        public void testWorkshopLockedByEra_OutInTheWorld_SaysSoToo() {
            Simulation simulation = newSimulation();
            leadCampEraSettlement(simulation);
            new ExitSettlementCommand(simulation.getEntityRepository()).execute(simulation.getPlayer());

            Assert.EndsWith("need a settlement with a Workshop; the Workshop is unlocked in the Village era.", CraftingScreen.getLocationLine(simulation));
        }

        [Fact]
        public void testWorkshopUnlocked_StillSaysHowToBuildOne() {
            Simulation simulation = newSimulation();
            Settlement settlement = leadCampEraSettlement(simulation);
            settlement.setBuildingEra(Era.VILLAGE);

            Assert.Contains("which has no Workshop yet (build one", CraftingScreen.getLocationLine(simulation));
            CraftingScreen.Row planks = CraftingScreen.getRows(simulation).Find(r => r.Recipe == RecipeBook.Planks);
            Assert.StartsWith("Need ", planks.Status);
        }

        [Fact]
        public void testCommandBar_OffersCraftAxeOnceAffordable_ThenStops() {
            Simulation simulation = newSimulation();
            Player player = simulation.getPlayer();
            Assert.DoesNotContain("Craft Axe", labels(simulation));

            player.getInventory().addItem(ItemType.WOOD, 4);
            player.getInventory().addItem(ItemType.STONE, 2);
            WorldCommand craftAxe = find(simulation, "Craft Axe");
            Assert.NotNull(craftAxe);

            craftAxe.execute();

            Assert.Equal(1, player.getInventory().getNumItems(ItemType.AXE));
            Assert.DoesNotContain("Craft Axe", labels(simulation));
        }

        [Fact]
        public void testCommandBar_OffersAToolAMerchantStocked() {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(42);
            Simulation simulation = new Simulation(gameConfig);
            Settlement settlement = SettlementShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            player.getInventory().clear();

            Stall stocked = null;
            foreach (Stall stall in settlement.getMarket().getStalls()) {
                if (stall.getOwnerId() != null && stall.getOwnerId() != player.getId()) {
                    stocked = stall;
                    break;
                }
            }
            Assert.NotNull(stocked);
            stocked.getInventory().addItem(ItemType.PICKAXE, 1);
            int price = settlement.getMarket().getPriceToBuy(player, ItemType.PICKAXE);
            Assert.DoesNotContain("Buy Pickaxe (" + price + ")", labels(simulation));

            player.getInventory().addItem(ItemType.COIN, price);
            WorldCommand buy = find(simulation, "Buy Pickaxe (" + price + ")");
            Assert.NotNull(buy);
            buy.execute();
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.PICKAXE));
        }

        [Fact]
        public void testInventoryBox_ListsCraftedItemsOnlyOnceHeld() {
            Inventory inventory = new Inventory(0);
            InventoryInfoBox box = new InventoryInfoBox(inventory);
            Assert.Equal(6, box.getLines().Count);

            inventory.addItem(ItemType.PLANKS, 4);
            inventory.addItem(ItemType.AXE, 1);
            List<string> lines = box.getLines();
            Assert.Contains("Planks: 4", lines);
            Assert.Contains("Axes: 1 (" + Tools.AXE_DURABILITY + " uses)", lines);
        }

        [Fact]
        public void testInventoryScreen_NamesCraftedItems() {
            Inventory inventory = new Inventory(0);
            inventory.addItem(ItemType.COOKED_MEAT, 2);
            inventory.addItem(ItemType.PICKAXE, 1);
            List<string> lines = InventoryScreen.getLines(inventory);
            Assert.Contains("Cooked Meat: 2", lines);
            Assert.Contains("Pickaxes: 1 (" + Tools.PICKAXE_DURABILITY + " uses left on the one in hand)", lines);
        }
    }
}
