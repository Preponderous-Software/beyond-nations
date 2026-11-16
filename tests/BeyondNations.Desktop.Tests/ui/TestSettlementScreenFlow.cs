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
