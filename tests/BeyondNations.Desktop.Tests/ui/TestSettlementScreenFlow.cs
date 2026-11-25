using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * The new player's first settlement, through the settlement screen that
    * opens on entering it (#459), replaying the 0.8.0 playtest's repros:
    *
    * - s01, s07, s08: entering and leaving by the screen's Exit Settlement
    *   left "Enter your settlement" open, since the world waits while the
    *   screen is open and no tick saw the player inside;
    * - s01: "press B inside your settlement" did nothing on the screen,
    *   which had no Build button;
    * - s02, s08: after Escape (or Back) the player stood inside, unable to
    *   walk, with nothing saying why;
    * - s01, s07: "0 here now" beside "You are inside it", and the first camp
    *   opening on "Give to: you".
    *
    * The objectives are evaluated as the world would evaluate them each
    * second (ObjectiveTracker.evaluate), with no window.
    */
    [Collection("TouchLayout")]
    public class TestSettlementScreenFlow {

        /** A fresh world at objective 5: wood gathered, a meal eaten, a nation of one with one camp, the player outside it. */
        private static Simulation atEnterSettlement(out Settlement camp, out ScreenState screens) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(101);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();

            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD + ObjectiveCatalog.firstWoodTarget());
            player.getInventory().addItem(ItemType.APPLE, 1);
            player.setEnergy(50);
            player.eatNow();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            // well away from the world's own settlements, so the founding is allowed
            player.setPosition(new Vector3(4000f, player.getPosition().Y, 4000f));
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom())
                .execute(player);
            camp = (Settlement) simulation.getEntityRepository().getEntity(player.getHomeSettlementId());
            Assert.NotNull(camp);

            ObjectiveTracker tracker = simulation.getObjectiveTracker();
            for (int i = 0; i < 4; i++) {
                Assert.True(tracker.evaluate(simulation), "objective " + (i + 1) + " should be done: " + tracker.getCurrent().getTitle());
            }
            Assert.Equal("enter-settlement", tracker.getCurrent().getId());

            screens = new ScreenState();
            screens.goTo(ScreenType.WORLD);
            SettlementScreen.consumeOpenRequest();
            SettlementScreens.beforeDraw(null, screens);
            // the host's first frame with this world
            Assert.False(SettlementScreens.beforeDraw(simulation, screens));
            return simulation;
        }

        /** E at the camp (or Enter Settlement on the bar), and the frame after: the screen opens. */
        private static void enter(Simulation simulation, Settlement camp, ScreenState screens) {
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(simulation.getPlayer(), camp);
            Assert.True(SettlementScreens.beforeDraw(simulation, screens));
            Assert.Equal(ScreenType.SETTLEMENT, screens.getCurrent());
        }

        private static string currentId(Simulation simulation) {
            return simulation.getObjectiveTracker().getCurrent().getId();
        }

        [Fact]
        public void testFirstEntry_LeavingByExitSettlementOnTheScreen_CompletesEnterYourSettlement() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            enter(simulation, camp, screens);

            // Exit Settlement on the screen, then the world runs again
            SettlementScreen.exit(simulation);
            screens.closeSettlement();
            SettlementScreens.beforeDraw(simulation, screens);
            simulation.getObjectiveTracker().evaluate(simulation);

            Assert.False(simulation.getPlayer().isCurrentlyInSettlement());
            Assert.Equal("build-stall", currentId(simulation));
            Assert.Equal(5, simulation.getObjectiveTracker().getCompletedCount());
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testFirstEntry_LeavingByEscape_CompletesIt_AndSaysHowToWalkOut() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            enter(simulation, camp, screens);
            Assert.Equal("build-stall", currentId(simulation));

            Assert.True(screens.escapePressed());
            Assert.Equal(ScreenType.WORLD, screens.getCurrent());
            Assert.False(SettlementScreens.beforeDraw(simulation, screens));

            Player player = simulation.getPlayer();
            Assert.True(player.isCurrentlyInSettlement());
            Assert.Equal(SettlementScreens.InsideMessage, player.getStatus().getStatus());

            player.setEnergy(100f);
            // trying to walk says why nothing moves, once per push
            player.setMovementInput(0f, 1f);
            player.fixedUpdate(1f);
            Assert.Equal(Player.CannotWalkInsideMessage, player.getStatus().getStatus());
            Vector3 stood = player.getPosition();
            player.fixedUpdate(1f);
            Assert.Equal(Player.CannotWalkInsideMessage, player.getStatus().getStatus());
            Assert.Equal(stood, player.getPosition());
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testFirstEntry_StayInside_CompletesIt() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            enter(simulation, camp, screens);

            // Stay Inside (Back) is UiAction.CloseSettlement
            screens.closeSettlement();
            SettlementScreens.beforeDraw(simulation, screens);
            simulation.getObjectiveTracker().evaluate(simulation);

            Assert.Equal("build-stall", currentId(simulation));
            Assert.Equal(SettlementScreens.InsideMessage, simulation.getPlayer().getStatus().getStatus());
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testBuildStall_IsOnTheScreen_AndBuildingItThereCompletesTheObjective() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            Player player = simulation.getPlayer();
            enter(simulation, camp, screens);

            List<WorldCommand> build = SettlementScreen.getBuildCommands(simulation, camp);
            WorldCommand stall = build.Find(c => c.getLabel() == "Build Stall");
            Assert.NotNull(stall);
            // nothing to build with yet: greyed, saying what is missing
            Assert.False(stall.isEnabled());
            Assert.Contains("more stone", stall.getDisabledReason());

            player.getInventory().addItem(ItemType.WOOD, Stall.WOOD_COST_TO_BUILD);
            player.getInventory().addItem(ItemType.STONE, Stall.STONE_COST_TO_BUILD);
            stall = SettlementScreen.getBuildCommands(simulation, camp).Find(c => c.getLabel() == "Build Stall");
            Assert.True(stall.isEnabled());
            stall.execute();

            Assert.Equal(1, camp.getMarket().getNumStalls());
            simulation.getObjectiveTracker().evaluate(simulation);
            Assert.Equal("grow-nation", currentId(simulation));
            SettlementScreens.beforeDraw(null, screens);
        }

        /**
        * Winter2 playtest: the banner said to Feed Residents, and the ruler
        * inside the camp with 22 apples and a starving member found only
        * Build and Exit on this screen. Feed Residents is on it, and works.
        */
        [Fact]
        public void testFeedResidents_IsOnTheScreen_AndFeedsTheHungryHere() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            Player player = simulation.getPlayer();
            enter(simulation, camp, screens);
            Pawn member = new Pawn(camp.getPosition(), "Katie White", simulation.getRandom());
            simulation.getEntityRepository().addEntity(member);
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            nation.addMember(member.getId());
            member.setNationId(nation.getId());
            member.getInventory().clear();
            member.setEnergy(Pawn.STARVING_ENERGY_THRESHOLD - 1);
            member.setCurrentSettlementId(camp.getId());
            player.getInventory().clear();

            WorldCommand feed = SettlementScreen.getBuildCommands(simulation, camp).Find(SettlementScreen.isFeedResidents);
            Assert.NotNull(feed);
            Assert.Equal(WorldCommandBar.feedResidentsLabel(1), feed.getLabel());
            // nothing to give yet: greyed, saying why (a tap on the screen says so too)
            Assert.False(feed.isEnabled());
            Assert.Equal("You have no food to give.", feed.getDisabledReason());

            player.getInventory().addItem(ItemType.APPLE, 22);
            feed = SettlementScreen.getBuildCommands(simulation, camp).Find(SettlementScreen.isFeedResidents);
            Assert.True(feed.isEnabled());
            feed.execute();
            Assert.True(FoodItems.countFood(member.getInventory()) > 0);
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testTheBuildStallKey_WorksOnTheScreen() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            Player player = simulation.getPlayer();
            enter(simulation, camp, screens);
            player.getInventory().addItem(ItemType.WOOD, Stall.WOOD_COST_TO_BUILD);
            player.getInventory().addItem(ItemType.STONE, Stall.STONE_COST_TO_BUILD);

            SettlementScreen.buildStallKey(simulation);

            Assert.Equal(1, camp.getMarket().getNumStalls());
            SettlementScreens.beforeDraw(null, screens);
        }

        /**
        * The coin sink (fun pass): a purse of thousands with nothing to buy.
        * Pay with Coins turns the building buttons into Buy buttons, greyed
        * with the reason while the purse falls short, and Buy pays coins for
        * the wood and stone the player lacks.
        */
        [Fact]
        public void testPayWithCoins_TurnsBuildIntoBuy_AndBuysTheBuilding() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            Player player = simulation.getPlayer();
            enter(simulation, camp, screens);
            player.getInventory().clear();
            camp.getInventory().clear();

            List<WorldCommand> build = SettlementScreen.getBuildCommands(simulation, camp, false);
            Assert.Contains(build, c => c.getLabel() == "Build House");
            WorldCommand toggle = build.Find(SettlementScreen.isPayToggle);
            Assert.NotNull(toggle);
            Assert.Equal(SettlementScreen.PayWithCoinsLabel, toggle.getLabel());

            int price = BuildingPurchase.getFullCoinPrice(camp, BuildingType.HOUSE);
            build = SettlementScreen.getBuildCommands(simulation, camp, true);
            Assert.DoesNotContain(build, c => c.getLabel() == "Build House");
            Assert.Equal(SettlementScreen.PayWithMaterialsLabel, build.Find(SettlementScreen.isPayToggle).getLabel());
            WorldCommand buy = build.Find(c => c.getLabel() == SettlementScreen.buyLabel(BuildingType.HOUSE, price));
            Assert.NotNull(buy);
            Assert.True(SettlementScreen.isBuy(buy));
            // an empty purse: greyed, saying how many coins it needs
            Assert.False(buy.isEnabled());
            Assert.Equal("Needs " + BuildingPurchase.formatCoins(price) + " coins; you have 0.", buy.getDisabledReason());
            // Build Stall and Feed Residents stay as they were
            Assert.Contains(build, c => c.getLabel() == "Build Stall");
            Assert.Contains(build, SettlementScreen.isFeedResidents);

            player.getInventory().addItem(ItemType.COIN, price + 10);
            buy = SettlementScreen.getBuildCommands(simulation, camp, true).Find(c => c.getLabel() == SettlementScreen.buyLabel(BuildingType.HOUSE, price));
            Assert.True(buy.isEnabled());
            Assert.Equal("Confirm: " + BuildingPurchase.formatCoins(price) + " coins", SettlementScreen.confirmLabel(buy.getLabel()));
            buy.execute();

            Assert.Equal(1, camp.getNumBuildings(BuildingType.HOUSE));
            Assert.Equal(10, player.getInventory().getNumItems(ItemType.COIN));
            Assert.StartsWith("House bought", player.getStatus().getStatus());
            SettlementScreens.beforeDraw(null, screens);
        }

        /**
        * The coin sink once the slots are full (building upgrades): with
        * nothing left to build, the Upgrade buttons stand on their own,
        * greyed with the reason (no coins; level 2 waits for the Kingdom),
        * and Upgrade raises a building a level for its price.
        */
        [Fact]
        public void testAFullSettlement_OffersUpgrades_GreyedWithTheReason() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            Player player = simulation.getPlayer();
            enter(simulation, camp, screens);
            player.getInventory().clear();
            camp.setBuildingEra(Era.VILLAGE);
            foreach (BuildingType type in new[] { BuildingType.WORKSHOP, BuildingType.STOREHOUSE, BuildingType.STOREHOUSE,
                    BuildingType.FARM, BuildingType.FARM, BuildingType.FARM, BuildingType.FARM }) {
                Assert.True(camp.addBuilding(type));
            }
            while (camp.addBuilding(BuildingType.HOUSE)) {
            }
            Assert.Null(BuildingPlanner.suggestNext(camp));

            // the Village era: no upgrades yet, and the screen says when they come
            Assert.DoesNotContain(SettlementScreen.getBuildCommands(simulation, camp, false), SettlementScreen.isUpgrade);
            Assert.Contains("Upgrades come with the Town era.", SettlementScreen.describeBuildings(camp));

            camp.setBuildingEra(Era.TOWN);
            List<WorldCommand> build = SettlementScreen.getBuildCommands(simulation, camp, false);
            // nothing to build, so no Pay toggle: the upgrades stand on their own
            Assert.DoesNotContain(build, SettlementScreen.isPayToggle);
            WorldCommand house = build.Find(c => c.getLabel() == SettlementScreen.upgradeLabel(BuildingType.HOUSE, 1500));
            Assert.NotNull(house);
            Assert.True(SettlementScreen.isPurchase(house));
            Assert.False(house.isEnabled());
            Assert.Equal("Needs 1,500 coins; you have 0.", house.getDisabledReason());
            Assert.Equal("Needs 1,500 coins; you have 0. Level 1: room for 6 residents (was 4).", house.getTooltip());
            Assert.Contains(build, c => c.getLabel() == SettlementScreen.upgradeLabel(BuildingType.FARM, 1200));
            Assert.Contains(build, c => c.getLabel() == SettlementScreen.upgradeLabel(BuildingType.STOREHOUSE, 2000));
            Assert.DoesNotContain(build, c => c.getLabel().StartsWith("Upgrade Workshop"));
            Assert.Contains("Upgrade a House to level 1: room for 6 residents (was 4), 1,500 coins.", SettlementScreen.describeBuildings(camp));

            int cap = camp.getResidentCap();
            player.getInventory().addItem(ItemType.COIN, 1500);
            house = SettlementScreen.getBuildCommands(simulation, camp, false).Find(c => c.getLabel() == SettlementScreen.upgradeLabel(BuildingType.HOUSE, 1500));
            Assert.True(house.isEnabled());
            Assert.Equal("Confirm: 1,500 coins", SettlementScreen.confirmLabel(house.getLabel()));
            house.execute();
            Assert.Equal(cap + 2, camp.getResidentCap());
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.COIN));
            Assert.StartsWith("House upgraded to level 1", player.getStatus().getStatus());

            // every house at level 1: the next is level 2, which waits for the Kingdom era
            player.getInventory().addItem(ItemType.COIN, 100000);
            while (BuildingUpgrades.findNext(camp, BuildingType.HOUSE).getLevel() == 0) {
                Assert.True(new UpgradeBuildingCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player, BuildingType.HOUSE));
            }
            house = SettlementScreen.getBuildCommands(simulation, camp, false).Find(c => c.getLabel() == SettlementScreen.upgradeLabel(BuildingType.HOUSE, 3000));
            Assert.NotNull(house);
            Assert.False(house.isEnabled());
            Assert.Equal("Level 2 comes with the Kingdom era.", house.getDisabledReason());
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testPayWithCoins_AlsoOffersUpgrades_WhileThereIsRoomToBuild() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            enter(simulation, camp, screens);
            camp.setBuildingEra(Era.TOWN);
            Assert.True(camp.addBuilding(BuildingType.FARM));

            Assert.DoesNotContain(SettlementScreen.getBuildCommands(simulation, camp, false), SettlementScreen.isUpgrade);
            List<WorldCommand> coins = SettlementScreen.getBuildCommands(simulation, camp, true);
            Assert.Contains(coins, c => c.getLabel() == SettlementScreen.upgradeLabel(BuildingType.FARM, 1200));
            // only the types the settlement has
            Assert.DoesNotContain(coins, c => c.getLabel().StartsWith("Upgrade House"));
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testThePayToggle_FlipsTheScreensMode() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            enter(simulation, camp, screens);
            bool was = SettlementScreen.isPayingWithCoins();
            try {
                SettlementScreen.setPayingWithCoins(false);
                SettlementScreen.getBuildCommands(simulation, camp, false).Find(SettlementScreen.isPayToggle).execute();
                Assert.True(SettlementScreen.isPayingWithCoins());
                SettlementScreen.getBuildCommands(simulation, camp, true).Find(SettlementScreen.isPayToggle).execute();
                Assert.False(SettlementScreen.isPayingWithCoins());
            } finally {
                SettlementScreen.setPayingWithCoins(was);
            }
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testTheBuildingsSection_GivesTheNextBuildingsCoinPrice() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            List<string> lines = SettlementScreen.describeBuildings(camp);
            BuildingType next = BuildingPlanner.suggestNext(camp).Value;
            Assert.Contains(lines, l => l.EndsWith("or " + BuildingPurchase.formatCoins(BuildingPurchase.getFullCoinPrice(camp, next)) + " coins."));
            SettlementScreens.beforeDraw(null, screens);
        }

        /** Playtest w1: the text said "or 2,160 coins" and the button "Buy Farm (1,764)"; the text now gives both and says why. */
        [Fact]
        public void testTheBuildingsSection_GivesThePriceWithWhatYouHave_WhenItDiffers() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            BuildingType next = BuildingPlanner.suggestNext(camp).Value;
            Inventory carried = new Inventory(1000);
            carried.addItem(ItemType.WOOD, 1);
            int full = BuildingPurchase.getFullCoinPrice(camp, next);
            int yours = BuildingPurchase.getCoinPrice(camp, carried, next);
            List<string> lines = SettlementScreen.describeBuildings(camp, carried);
            // one wood of its own makes the price for what is short less than the full price
            Assert.True(yours > 0 && yours < full);
            Assert.Contains(lines, l => l.EndsWith("or " + BuildingPurchase.formatCoins(full) + " coins ("
                + BuildingPurchase.formatCoins(yours) + " with what you have)."));
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testOutside_TheScreenOffersNoBuilding() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            Assert.Empty(SettlementScreen.getBuildCommands(simulation, camp));
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testTheFirstCamp_CountsThePlayerHere_AndOffersNoGive() {
            Simulation simulation = atEnterSettlement(out Settlement camp, out ScreenState screens);
            enter(simulation, camp, screens);

            List<string> people = SettlementScreen.describePeople(simulation, camp);
            Assert.DoesNotContain("0 here now", people[0]);
            Assert.EndsWith("here now, you alone.", people[0]);
            Assert.Equal("You are inside it. You live here: it is your home.", people[1]);

            SettlementScreen.Actions actions = SettlementScreen.getActions(simulation, camp);
            Assert.False(actions.canGive);
            Assert.False(actions.canTakeBack);
            Assert.True(actions.canExit);
            Assert.StartsWith("You hold it as leader.", actions.note);
            SettlementScreens.beforeDraw(null, screens);
        }

        [Fact]
        public void testHomeGivenAway_SaysWhoHoldsIt_AndKeepingItYourselfReadsApartFromTakeBack() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Settlement outpost = HoldingsShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            Settlement home = (Settlement) simulation.getEntityRepository().getEntity(player.getHomeSettlementId());
            GrantSettlementCommand grants = new GrantSettlementCommand(simulation.getNationRepository(), simulation.getEntityRepository());

            grants.grant(player, home, outpost.getOwnerId());
            Assert.EndsWith(". You live here, though " + home.getOwnerName() + " holds it.", SettlementScreen.describePeople(simulation, home)[1]);
            Assert.Equal(SettlementScreen.TakeBackLabel, SettlementScreen.takeBackLabel(simulation, home));

            grants.grant(player, home, player.getId());
            Assert.Equal(SettlementScreen.HoldAsLeaderLabel, SettlementScreen.takeBackLabel(simulation, home));
            Assert.Equal("Keep it yourself", SettlementScreen.describePick(player, player));
        }
    }
}
