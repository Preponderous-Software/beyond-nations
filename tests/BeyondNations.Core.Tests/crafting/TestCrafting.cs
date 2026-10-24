using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Recipes, the craft itself, tools wearing out, and the market's handling
    * of crafted goods (#47).
    */
    public class TestCrafting {
        private readonly RandomSource random = new RandomSource(20261018);

        private Settlement makeSettlement() {
            return new Settlement(new Vector3(0, 1, 0), new NationId(), Rgba.Red, "Testland", random);
        }

        private static Inventory inventoryWith(params object[] typeQuantityPairs) {
            Inventory inventory = new Inventory(0);
            for (int i = 0; i + 1 < typeQuantityPairs.Length; i += 2) {
                inventory.addItem((ItemType) typeQuantityPairs[i], (int) typeQuantityPairs[i + 1]);
            }
            return inventory;
        }

        // --- the recipe book ---

        [Fact]
        public void testRecipeBook_HasTheToolsFoodAndTradeGoods() {
            List<ItemType> outputs = new List<ItemType>();
            foreach (Recipe recipe in RecipeBook.getAll()) {
                outputs.Add(recipe.getOutput());
                Assert.NotEmpty(recipe.getInputs());
                Assert.False(string.IsNullOrEmpty(recipe.getBenefit()));
            }
            Assert.Equal(new List<ItemType> { ItemType.AXE, ItemType.PICKAXE, ItemType.COOKED_MEAT, ItemType.PLANKS, ItemType.BRICKS }, outputs);
            Assert.Same(RecipeBook.Planks, RecipeBook.forOutput(ItemType.PLANKS));
            Assert.Null(RecipeBook.forOutput(ItemType.WOOD));
        }

        [Fact]
        public void testEveryItemExceptCoin_HasAMarketPrice() {
            foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType))) {
                if (itemType == ItemType.COIN) {
                    continue;
                }
                Assert.True(ItemCostCalculator.getBaseCost(itemType) > 0, itemType + " has no price");
                Assert.False(string.IsNullOrEmpty(ItemNames.getDisplayName(itemType)));
            }
        }

        [Theory]
        [InlineData(ItemType.AXE)]
        [InlineData(ItemType.PICKAXE)]
        [InlineData(ItemType.PLANKS)]
        [InlineData(ItemType.BRICKS)]
        public void testCraftedGoods_AreWorthMoreThanTheirInputs(ItemType output) {
            Recipe recipe = RecipeBook.forOutput(output);
            int inputValue = 0;
            foreach (ItemStack input in recipe.getInputs()) {
                inputValue += ItemCostCalculator.getBaseCost(input.getItemType()) * input.getQuantity();
            }
            int outputValue = ItemCostCalculator.getBaseCost(output) * recipe.getOutputQuantity();
            Assert.True(outputValue > inputValue, output + " sells for " + outputValue + ", its inputs for " + inputValue);
        }

        [Fact]
        public void testCookedMeat_RestoresMoreThanRawAndIsEatenFirst() {
            Assert.True(FoodItems.getEnergyRestored(ItemType.COOKED_MEAT) > FoodItems.getEnergyRestored(ItemType.CHICKEN_MEAT));
            Inventory inventory = inventoryWith(ItemType.CHICKEN_MEAT, 1, ItemType.COOKED_MEAT, 1, ItemType.APPLE, 1);
            Assert.Equal(ItemType.COOKED_MEAT, FoodItems.getMostNourishingFood(inventory));
            Assert.Equal(FoodItems.getEnergyRestored(ItemType.COOKED_MEAT), FoodItems.consumeMostNourishingFood(inventory));
        }

        [Fact]
        public void testDescribeInputs_ReadsLikeASentence() {
            Assert.Equal("4 wood, 2 stone", RecipeBook.Axe.describeInputs());
            Assert.Equal("2 planks", RecipeBook.Planks.describeOutput());
            Assert.Equal("1 axe", RecipeBook.Axe.describeOutput());
        }

        // --- crafting ---

        [Fact]
        public void testCraft_TakesInputsAndGivesOutput() {
            Inventory inventory = inventoryWith(ItemType.WOOD, 5, ItemType.STONE, 3);
            Assert.True(Crafting.craft(RecipeBook.Axe, inventory, null));
            Assert.Equal(1, inventory.getNumItems(ItemType.AXE));
            Assert.Equal(1, inventory.getNumItems(ItemType.WOOD));
            Assert.Equal(1, inventory.getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testCraft_ShortOfMaterials_ChangesNothingAndSaysWhatIsMissing() {
            Inventory inventory = inventoryWith(ItemType.WOOD, 4);
            Assert.False(Crafting.canCraft(RecipeBook.Axe, inventory, null));
            Assert.Equal(new List<string> { "2 more stone" }, Crafting.getMissing(RecipeBook.Axe, inventory, null));
            Assert.False(Crafting.craft(RecipeBook.Axe, inventory, null));
            Assert.Equal(4, inventory.getNumItems(ItemType.WOOD));
            Assert.Equal(0, inventory.getNumItems(ItemType.AXE));
        }

        [Fact]
        public void testWorkshopRecipe_NeedsASettlementWithAWorkshop() {
            Inventory inventory = inventoryWith(ItemType.WOOD, 9);
            Settlement settlement = makeSettlement();

            Assert.False(Crafting.canCraft(RecipeBook.Planks, inventory, null));
            Assert.Contains("a Workshop (inside a settlement)", Crafting.getMissing(RecipeBook.Planks, inventory, null));
            Assert.Contains("a Workshop in this settlement", Crafting.getMissing(RecipeBook.Planks, inventory, settlement));
            Assert.Equal(0, Crafting.getMaxCrafts(RecipeBook.Planks, inventory, settlement));

            Assert.True(settlement.addBuilding(BuildingType.WORKSHOP));
            Assert.True(Crafting.canCraft(RecipeBook.Planks, inventory, settlement));
            Assert.Equal(3, Crafting.getMaxCrafts(RecipeBook.Planks, inventory, settlement));
            Assert.True(Crafting.craft(RecipeBook.Planks, inventory, settlement));
            Assert.Equal(2, inventory.getNumItems(ItemType.PLANKS));
            Assert.Equal(6, inventory.getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testWorkshop_IsALimitOneBuildingANewCampPutsLast() {
            Settlement settlement = makeSettlement();
            Assert.Equal(1, BuildingCatalog.getLimit(BuildingType.WORKSHOP));
            List<BuildingType> order = BuildingPlanner.getPriorityOrder(settlement);
            Assert.Equal(BuildingType.WORKSHOP, order[order.Count - 1]);
            settlement.addBuilding(BuildingType.WORKSHOP);
            Assert.False(settlement.canAddBuilding(BuildingType.WORKSHOP));
        }

        [Fact]
        public void testWorkshop_IsAVillagesNextBuildingOnceNeedsAreMet() {
            Settlement settlement = makeSettlement();
            settlement.addBuilding(BuildingType.HOUSE);
            settlement.addBuilding(BuildingType.FARM);
            // no pressing need (room for residents, farms keeping up, stalls
            // free), so the village's workshop comes before more of the same
            Assert.Equal(BuildingType.WORKSHOP, BuildingPlanner.getPriorityOrder(settlement)[0]);
            Assert.Equal(BuildingType.WORKSHOP, BuildingPlanner.suggestNext(settlement));
        }

        [Fact]
        public void testCraftItemCommand_ReportsTheResult() {
            Player player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().clear();
            EntityRepository entityRepository = new EntityRepository(random);
            CraftItemCommand command = new CraftItemCommand(entityRepository);

            Assert.False(command.execute(player, RecipeBook.Pickaxe));
            Assert.Equal("Can't craft Pickaxe yet: need 3 more wood, 3 more stone.", player.getStatus().getStatus());

            player.getInventory().addItem(ItemType.WOOD, 3);
            player.getInventory().addItem(ItemType.STONE, 3);
            Assert.True(command.execute(player, RecipeBook.Pickaxe));
            Assert.Equal("Crafted 1 pickaxe (+2 stone per rock, lasts 15 rocks).", player.getStatus().getStatus());
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.PICKAXE));
        }

        // --- tools ---

        [Fact]
        public void testAxe_AddsWoodPerTreeAndWearsOut() {
            Inventory inventory = inventoryWith(ItemType.AXE, 1);
            Assert.Equal(Tools.AXE_DURABILITY, Tools.getUsesLeft(inventory, ItemType.AXE));

            for (int i = 1; i < Tools.AXE_DURABILITY; i++) {
                GatherBonus bonus = Tools.applyGatherBonus(inventory, EntityType.TREE);
                Assert.Equal(Tools.AXE_BONUS_WOOD, bonus.getAmount());
                Assert.False(bonus.didBreak());
            }
            Assert.Equal(1, Tools.getUsesLeft(inventory, ItemType.AXE));

            GatherBonus last = Tools.applyGatherBonus(inventory, EntityType.TREE);
            Assert.True(last.didBreak());
            Assert.Equal(0, inventory.getNumItems(ItemType.AXE));
            Assert.Equal(Tools.AXE_BONUS_WOOD * Tools.AXE_DURABILITY, inventory.getNumItems(ItemType.WOOD));
            Assert.Equal(0, Tools.applyGatherBonus(inventory, EntityType.TREE).getAmount());
        }

        [Fact]
        public void testSpareTool_StartsFreshWhenTheFirstWearsOut() {
            Inventory inventory = inventoryWith(ItemType.PICKAXE, 2);
            for (int i = 0; i < Tools.PICKAXE_DURABILITY; i++) {
                Tools.applyGatherBonus(inventory, EntityType.ROCK);
            }
            Assert.Equal(1, inventory.getNumItems(ItemType.PICKAXE));
            Assert.Equal(Tools.PICKAXE_DURABILITY, Tools.getUsesLeft(inventory, ItemType.PICKAXE));
        }

        [Fact]
        public void testToolOnlyHelpsWithItsOwnNode() {
            Inventory inventory = inventoryWith(ItemType.AXE, 1);
            Assert.Same(GatherBonus.None, Tools.applyGatherBonus(inventory, EntityType.ROCK));
            Assert.Same(GatherBonus.None, Tools.applyGatherBonus(inventory, EntityType.CHICKEN));
            Assert.Equal(Tools.AXE_DURABILITY, Tools.getUsesLeft(inventory, ItemType.AXE));
        }

        [Fact]
        public void testWear_IsForgottenWhenTheLastToolLeaves() {
            Inventory inventory = inventoryWith(ItemType.AXE, 1);
            Tools.applyGatherBonus(inventory, EntityType.TREE);
            Assert.Equal(1, inventory.getToolWear(ItemType.AXE));
            inventory.removeItem(ItemType.AXE, 1);
            inventory.addItem(ItemType.AXE, 1);
            Assert.Equal(Tools.AXE_DURABILITY, Tools.getUsesLeft(inventory, ItemType.AXE));
        }

        [Fact]
        public void testInteract_WithPickaxe_GathersExtraStone() {
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            InteractCommand command = new InteractCommand(environment, new NationRepository(random),
                new EventProducer(new EventRepository()), entityRepository, random);
            Player player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.setPosition(Vector3.Zero);
            player.getInventory().clear();
            player.getInventory().addItem(ItemType.PICKAXE, 1);
            entityRepository.addEntity(player);
            entityRepository.addEntity(new Rock(new Vector3(2, 0, 0)));

            command.execute(player);

            Assert.Equal(Rock.STONE_YIELD + Tools.PICKAXE_BONUS_STONE, player.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal("Gathered 7 stone from rock (+2 from your pickaxe), 14 uses left.", player.getStatus().getStatus());
        }

        [Fact]
        public void testInteract_WithoutTool_GathersTheRockAlone() {
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            InteractCommand command = new InteractCommand(environment, new NationRepository(random),
                new EventProducer(new EventRepository()), entityRepository, random);
            Player player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.setPosition(Vector3.Zero);
            player.getInventory().clear();
            entityRepository.addEntity(player);
            entityRepository.addEntity(new Rock(new Vector3(2, 0, 0)));

            command.execute(player);

            Assert.Equal(Rock.STONE_YIELD, player.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal("Gathered 5 stone from rock.", player.getStatus().getStatus());
        }

        // --- the market ---

        private static Pawn addMerchant(Market market, RandomSource random) {
            Pawn merchant = new Pawn(Vector3.Zero, "Merchant", random);
            market.createStall();
            market.getStallForSale().setOwnerId(merchant.getId());
            return merchant;
        }

        [Fact]
        public void testWhyNoSale_NamesTheReason() {
            Market market = new Market(4, random);
            Player player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().clear();
            player.getInventory().addItem(ItemType.AXE, 1);
            Assert.StartsWith("You have nothing to sell", market.describeWhyNoSale(player));

            player.getInventory().addItem(ItemType.WOOD, 3);
            market.createStall();
            Assert.StartsWith("No merchant keeps a stall here", market.describeWhyNoSale(player));

            market.getStallForSale().setOwnerId(player.getId());
            Assert.Equal("The only stall here is yours, and you can't sell to yourself.", market.describeWhyNoSale(player));

            Pawn merchant = addMerchant(market, random);
            Assert.Null(market.sellResources(player));
            Assert.StartsWith("No merchant here can pay: the richest has 0 coins, and your cheapest good (wood) sells for ", market.describeWhyNoSale(player));

            market.getStall(merchant.getId()).getInventory().addItem(ItemType.COIN, 5000);
            Assert.Null(market.describeWhyNoSale(player));
            Assert.Equal(merchant.getId(), market.sellResources(player));
        }

        [Fact]
        public void testSellResources_KeepsTools() {
            Market market = new Market(4, random);
            Pawn merchant = addMerchant(market, random);
            market.getStall(merchant.getId()).getInventory().addItem(ItemType.COIN, 5000);
            Player player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().clear();
            player.getInventory().addItem(ItemType.AXE, 1);
            player.getInventory().addItem(ItemType.PLANKS, 1);

            Assert.NotNull(market.sellResources(player));
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.AXE));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.PLANKS));
            // a sale is priced as the unit lands on the stall (supply 1), so
            // buying it straight back can never cost less than it fetched
            Assert.Equal(ItemCostCalculator.calculateCost(ItemCostCalculator.getBaseCost(ItemType.PLANKS), 1), player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testBuyItemCommand_BuysAToolFromAStall() {
            EntityRepository entityRepository = new EntityRepository(random);
            Settlement settlement = makeSettlement();
            entityRepository.addEntity(settlement);
            Pawn merchant = addMerchant(settlement.getMarket(), random);
            entityRepository.addEntity(merchant);
            settlement.getMarket().getStall(merchant.getId()).getInventory().addItem(ItemType.AXE, 1);

            Player player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().clear();
            player.setCurrentSettlementId(settlement.getId());
            BuyItemFromMarketCommand command = new BuyItemFromMarketCommand(entityRepository);
            int price = settlement.getMarket().getPriceToBuy(player, ItemType.AXE);
            Assert.True(price > 0);

            Assert.False(command.execute(player, ItemType.AXE));
            Assert.Equal("You need " + price + " coins to buy 1 axe.", player.getStatus().getStatus());

            player.getInventory().addItem(ItemType.COIN, price);
            Assert.True(command.execute(player, ItemType.AXE));
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.AXE));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(-1, settlement.getMarket().getPriceToBuy(player, ItemType.AXE));
        }

        // --- merchants ---

        [Fact]
        public void testMerchant_MakesOneOfEachToolWhileTheStallHasNone() {
            Settlement settlement = makeSettlement();
            Inventory merchant = inventoryWith(ItemType.WOOD, 20, ItemType.STONE, 20);
            Inventory stall = new Inventory(0);

            MerchantCrafting.craftGoods(merchant, settlement, stall);
            Assert.Equal(1, merchant.getNumItems(ItemType.AXE));
            Assert.Equal(1, merchant.getNumItems(ItemType.PICKAXE));
            // no workshop, so the rest stays raw
            Assert.Equal(0, merchant.getNumItems(ItemType.PLANKS));

            stall.transferContentsOfInventory(merchant);
            merchant.addItem(ItemType.WOOD, 20);
            merchant.addItem(ItemType.STONE, 20);
            MerchantCrafting.craftGoods(merchant, settlement, stall);
            Assert.Equal(0, merchant.getNumItems(ItemType.AXE));
            Assert.Equal(0, merchant.getNumItems(ItemType.PICKAXE));
        }

        [Fact]
        public void testMerchant_WithAWorkshop_MakesPlanksBricksAndCooks() {
            Settlement settlement = makeSettlement();
            settlement.addBuilding(BuildingType.WORKSHOP);
            Inventory stall = new Inventory(0);
            stall.addItem(ItemType.AXE, 1);
            stall.addItem(ItemType.PICKAXE, 1);
            Inventory merchant = inventoryWith(ItemType.WOOD, 20, ItemType.STONE, 20, ItemType.CHICKEN_MEAT, 3);

            int crafted = MerchantCrafting.craftGoods(merchant, settlement, stall);

            Assert.Equal(3 * MerchantCrafting.MAX_BATCHES, crafted);
            Assert.Equal(MerchantCrafting.MAX_BATCHES, merchant.getNumItems(ItemType.COOKED_MEAT));
            Assert.Equal(1, merchant.getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Equal(2 * MerchantCrafting.MAX_BATCHES, merchant.getNumItems(ItemType.PLANKS));
            Assert.Equal(2 * MerchantCrafting.MAX_BATCHES, merchant.getNumItems(ItemType.BRICKS));
        }
    }
}
