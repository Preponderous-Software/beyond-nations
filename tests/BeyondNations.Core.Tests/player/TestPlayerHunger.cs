using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The player's hunger (#357): food was eaten whenever energy fell below 90,
    * the most nourishing first, so a cooked meat lost most of its 45 to the
    * cap, food vanished unannounced, and inside a settlement energy never
    * fell at all.
    */
    public class TestPlayerHunger {

        private static Player makePlayer() {
            Player player = new Player(25, 50, new TickCounter(), 500, 200, new RandomSource(357));
            player.getInventory().clear();
            return player;
        }

        [Fact]
        public void testFoodThatFits_IsTheRichestThatLosesNothingToTheCap() {
            Inventory inventory = makePlayer().getInventory();
            inventory.addItem(ItemType.APPLE, 1);
            inventory.addItem(ItemType.COOKED_MEAT, 1);
            Assert.Equal(ItemType.APPLE, FoodItems.getFoodThatFits(inventory, 20f));
            Assert.Equal(ItemType.COOKED_MEAT, FoodItems.getFoodThatFits(inventory, 45f));
            Assert.Null(FoodItems.getFoodThatFits(inventory, 10f));
            Assert.Equal(ItemType.APPLE, FoodItems.getLeastNourishingFood(inventory));
        }

        [Fact]
        public void testCookedMeat_IsKeptUntilItAllFits_ThenEatenWhole() {
            Player player = makePlayer();
            player.getInventory().addItem(ItemType.COOKED_MEAT, 1);
            player.setEnergy(89f);
            player.fixedUpdate();
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.COOKED_MEAT));
            Assert.Equal(0, player.getMealsEaten());

            player.setEnergy(Player.MaxEnergy - FoodItems.getEnergyRestored(ItemType.COOKED_MEAT));
            player.fixedUpdate();
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.COOKED_MEAT));
            Assert.True(player.getEnergy() > 99f, "the whole 45 should count, not " + player.getEnergy());
            Assert.Equal("Ate cooked meat (+45 energy).", player.getStatus().getLatest());
        }

        [Fact]
        public void testAnApple_IsEatenFirst_WhileTheMeatWaits() {
            Player player = makePlayer();
            player.getInventory().addItem(ItemType.APPLE, 1);
            player.getInventory().addItem(ItemType.COOKED_MEAT, 1);
            player.setEnergy(80f);
            player.fixedUpdate();
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.COOKED_MEAT));
            Assert.Equal("Ate an apple (+15 energy).", player.getStatus().getLatest());
        }

        [Fact]
        public void testEatNow_TakesWhatFits_OrTheLeast_AndRefusesOnAFullBar() {
            Player player = makePlayer();
            player.getInventory().addItem(ItemType.APPLE, 1);
            player.getInventory().addItem(ItemType.COOKED_MEAT, 1);

            player.setEnergy(50f);
            Assert.Equal(ItemType.COOKED_MEAT, player.getFoodToEatNow());

            player.setEnergy(95f);
            Assert.Equal(ItemType.APPLE, player.getFoodToEatNow());
            Assert.True(player.eatNow());
            Assert.Equal(Player.MaxEnergy, player.getEnergy());

            Assert.Null(player.getFoodToEatNow());
            Assert.False(player.eatNow());
            Assert.Equal("You are full.", player.getStatus().getLatest());
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.COOKED_MEAT));
        }

        /** Playtest 0.5.0 s03: energy sat at 92.4 indoors for 25,000 ticks through a starving winter. */
        [Fact]
        public void testInASettlement_TheBarStillFalls() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();
            Settlement settlement = new Settlement(player.getPosition(), new NationId(), Rgba.Red, "Testland", simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            player.getInventory().clear();
            player.setCurrentSettlementId(settlement.getId());
            player.setEnergy(90f);
            for (int i = 0; i < 3000; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            float burned = 90f - player.getEnergy();
            float expected = 3000 * player.getMetabolism() * config.getSettlementMetabolismMultiplier();
            // float steps add up to a hundredth or so over 3,000 ticks
            Assert.Equal(expected, burned, 1);
        }
    }
}
