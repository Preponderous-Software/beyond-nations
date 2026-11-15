using System.Collections.Generic;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The warning a day before winter, and stocking residents for it (#353):
    * in the 0.5.0 playtest the first sign of a starving winter was the death
    * notice, and nothing let the player lay food in for their nation.
    */
    public class TestWinterStores {

        private static Simulation inOwnSettlement(out Nation nation, out Settlement settlement) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom())
                .execute(player);
            nation = simulation.getNationRepository().getNation(player.getNationId());
            settlement = (Settlement) simulation.getEntityRepository().getEntity(player.getHomeSettlementId());
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            player.getInventory().clear();
            return simulation;
        }

        private static Pawn addMember(Simulation simulation, Nation nation, Settlement settlement, float energy, int apples, bool present) {
            Pawn pawn = new Pawn(settlement.getPosition(), "Member", simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            pawn.setNationId(nation.getId());
            nation.addMember(pawn.getId());
            pawn.getInventory().clear();
            pawn.getInventory().addItem(ItemType.APPLE, apples);
            pawn.setEnergy(energy);
            if (present) {
                pawn.setCurrentSettlementId(settlement.getId());
            }
            return pawn;
        }

        /** The clock moved on by whole days to the given day of the year, without running the world. */
        private static void jumpToDayOfYear(Simulation simulation, int dayOfYear) {
            int guard = 0;
            while (simulation.getSeasonCalendar().getDayOfYear() != dayOfYear) {
                simulation.getTickCounter().increment();
                Assert.True(++guard < 10 * TimeOfDay.DefaultTicksPerDay, "never reached day " + dayOfYear);
            }
        }

        private const int LastDayOfAutumn = 3 * SeasonCalendar.DAYS_PER_SEASON - 1;

        [Fact]
        public void testCount_AddsUpTheMembersFood_AndWhoIsShort() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            addMember(simulation, nation, settlement, 95f, 1, true);
            addMember(simulation, nation, settlement, 95f, SeasonalRules.WINTER_FOOD_STORE, false);
            WinterStores.Count stores = WinterStores.count(simulation.getEntityRepository(), nation.getId());
            Assert.Equal(2, stores.Members);
            Assert.Equal(1 + SeasonalRules.WINTER_FOOD_STORE, stores.Food);
            Assert.Equal(2 * SeasonalRules.WINTER_FOOD_STORE, stores.Wanted);
            Assert.Equal(1, stores.Short);
        }

        [Fact]
        public void testWarning_OnlyOnTheLastDayBeforeWinter_AndOnlyWhileSomeoneIsShort() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            EntityRepository entities = simulation.getEntityRepository();
            Pawn member = addMember(simulation, nation, settlement, 95f, 1, true);
            addMember(simulation, nation, settlement, 95f, 2, true);

            Assert.Null(WinterStores.describeWarning(entities, nation, simulation.getSeasonCalendar()));
            jumpToDayOfYear(simulation, LastDayOfAutumn);
            Assert.Equal("Winter comes tomorrow. " + nation.getName() + "'s 2 members carry 3 food of the 8 they want for winter; 2 are short. Bring food home and Feed Residents.",
                WinterStores.describeWarning(entities, nation, simulation.getSeasonCalendar()));

            member.getInventory().addItem(ItemType.APPLE, 10);
            Assert.Contains("1 is short", WinterStores.describeWarning(entities, nation, simulation.getSeasonCalendar()));
        }

        /** The chronicle keeps the warning, naming the nation, as the last day before winter begins. */
        [Fact]
        public void testTheDayBeforeWinter_WritesTheStoresToTheChronicle_AndTheStatusLine() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            addMember(simulation, nation, settlement, 95f, 3, true);
            jumpToDayOfYear(simulation, LastDayOfAutumn - 1);
            int guard = 0;
            while (simulation.getSeasonCalendar().getDayOfYear() != LastDayOfAutumn) {
                // the player's own hunger is not under test here
                simulation.getPlayer().setEnergy(Player.MaxEnergy);
                simulation.fixedUpdate(1f / 50f);
                Assert.True(++guard < 2 * TimeOfDay.DefaultTicksPerDay);
            }
            List<ChronicleEntry> lines = simulation.getChronicle().getRecent(50);
            ChronicleEntry warning = lines.Find(e => e.getText().Contains("Winter comes tomorrow"));
            Assert.NotNull(warning);
            Assert.Contains(nation.getName() + "'s", warning.getText());
            Assert.Contains("for winter", warning.getText());
            Assert.Contains("Winter comes tomorrow.", simulation.getPlayer().getStatus().getStatus());
        }

        [Fact]
        public void testFeed_InAutumn_StocksThoseShortOfWinterStores_AfterTheHungry() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            Player player = simulation.getPlayer();
            FeedResidentsCommand feed = new FeedResidentsCommand(simulation.getEntityRepository());
            Pawn low = addMember(simulation, nation, settlement, 95f, 1, true);
            Pawn away = addMember(simulation, nation, settlement, 95f, 0, false);
            Pawn hungry = addMember(simulation, nation, settlement, 70f, 0, true);

            // not in spring: the pawns lay in stores from autumn on
            Assert.Equal(0, feed.countLowOnStoresHere(player, Season.SPRING));
            Assert.Equal(1, feed.countLowOnStoresHere(player, Season.AUTUMN));

            player.getInventory().addItem(ItemType.APPLE, 20);
            int helped = feed.execute(player, Season.AUTUMN);
            Assert.Equal(2, helped);
            Assert.Equal(SeasonalRules.WINTER_FOOD_STORE, FoodItems.countFood(low.getInventory()));
            Assert.Equal(1, FoodItems.countFood(hungry.getInventory()));
            Assert.Equal(0, FoodItems.countFood(away.getInventory()));
            Assert.Equal(20 - 1 - (SeasonalRules.WINTER_FOOD_STORE - 1), player.getInventory().getNumItems(ItemType.APPLE));
            Assert.StartsWith("You fed 1 and stocked 1 resident for winter", player.getStatus().getStatus());
        }

        [Fact]
        public void testFeed_InSpring_LeavesTheWellFedAlone() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            Player player = simulation.getPlayer();
            Pawn low = addMember(simulation, nation, settlement, 95f, 1, true);
            player.getInventory().addItem(ItemType.APPLE, 20);
            Assert.Equal(0, new FeedResidentsCommand(simulation.getEntityRepository()).execute(player, Season.SPRING));
            Assert.Equal(1, FoodItems.countFood(low.getInventory()));
            Assert.Equal(20, player.getInventory().getNumItems(ItemType.APPLE));
        }

        [Fact]
        public void testFeed_ScarceFood_GoesRoundBeforeAnyoneGetsASecond() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            Player player = simulation.getPlayer();
            Pawn a = addMember(simulation, nation, settlement, 95f, 0, true);
            Pawn b = addMember(simulation, nation, settlement, 95f, 0, true);
            // not needsFood: above the hungry line, so these are stocked, not fed
            player.getInventory().addItem(ItemType.APPLE, 3);
            new FeedResidentsCommand(simulation.getEntityRepository()).execute(player, Season.WINTER);
            Assert.True(FoodItems.countFood(a.getInventory()) >= 1);
            Assert.True(FoodItems.countFood(b.getInventory()) >= 1);
            Assert.Contains("still short of winter stores", player.getStatus().getStatus());
        }
    }
}
