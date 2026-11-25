using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui.boxes;

namespace beyondnationstests.desktop.ui {

    /**
    * The HUD's season box: the season and day, the "Winter in N days"
    * warning, and the region underfoot.
    */
    public class TestSeasonInfoBox {

        private static SeasonCalendar calendarAtDay(int day) {
            TickCounter ticks = new TickCounter();
            TimeOfDay timeOfDay = new TimeOfDay(ticks, TimeOfDay.DefaultTicksPerDay, 0f);
            for (long i = 0; i < (long) day * TimeOfDay.DefaultTicksPerDay; i++) {
                ticks.increment();
            }
            return new SeasonCalendar(timeOfDay);
        }

        [Fact]
        public void testANewWorld_ShowsSpring_TheRegion_AndNoWarning() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            List<string> lines = new SeasonInfoBox(simulation).getLines();

            Assert.Equal("Spring - day 1/" + SeasonCalendar.DAYS_PER_SEASON + ", year 1", lines[0]);
            Region region = simulation.getRegionMap().regionAt(simulation.getPlayer().getPosition());
            Assert.Equal("Region: " + RegionCatalog.name(region), lines[1]);
            Assert.Equal(RegionCatalog.describe(region), lines[2]);
            Assert.Equal(3, lines.Count);
        }

        [Fact]
        public void testCompact_KeepsTheSeasonAndDropsTheRegion() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            SeasonInfoBox box = new SeasonInfoBox(simulation);
            box.setCompact(true);

            Assert.Equal(new List<string> { "Spring - day 1/" + SeasonCalendar.DAYS_PER_SEASON + ", year 1" }, box.getLines());
        }

        [Fact]
        public void testCompact_TheMarketTotalsTakeOneLine_AndThePlayerBoxJustEnergy() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Market market = new Market(4, simulation.getRandom());
            market.createStall();
            MarketInfoBox full = new MarketInfoBox(market, simulation.getEntityRepository());
            MarketInfoBox compact = new MarketInfoBox(market, simulation.getEntityRepository());
            compact.setCompact(true);

            Assert.Equal("1/4 stalls, 0 coins", compact.getLines()[0]);
            Assert.Equal(full.getLines().Count - 4, compact.getLines().Count);
            Assert.Equal(MarketInfoBox.PricesHeading, compact.getLines()[1]);

            // the player box keeps whose the player is (playtest 0.6.1 s04)
            PlayerInfoBox player = new PlayerInfoBox(simulation.getPlayer(), simulation.getNationRepository(), simulation.getEntityRepository());
            player.setCompact(true);
            Assert.Equal(new List<string> { "Energy: " + (int) simulation.getPlayer().getEnergy(), "Nation: none" }, player.getLines());
        }

        [Fact]
        public void testCompact_MerchantsTakeOneLine() {
            Assert.Equal("Merchants: Ann", MarketInfoBox.describeMerchantsCompact(new List<string> { "Ann" }));
            Assert.Equal("Merchants: Ann, Bob", MarketInfoBox.describeMerchantsCompact(new List<string> { "Ann", "Bob" }));
            Assert.Equal("Merchants: Ann, Bob +4",
                MarketInfoBox.describeMerchantsCompact(new List<string> { "Ann", "Bob", "Cy", "Di", "Ed", "Flo" }));
        }

        [Fact]
        public void testTheWarning_CountsDownToWinter() {
            int winterStarts = 3 * SeasonCalendar.DAYS_PER_SEASON;
            Assert.Null(SeasonInfoBox.getWinterLine(calendarAtDay(0)));
            Assert.Equal("Winter in 2 days", SeasonInfoBox.getWinterLine(calendarAtDay(winterStarts - 2)));
            Assert.Equal("Winter in 1 day!", SeasonInfoBox.getWinterLine(calendarAtDay(winterStarts - 1)));
            Assert.Equal("Winter: no harvests", SeasonInfoBox.getWinterLine(calendarAtDay(winterStarts)));
            Assert.StartsWith("Autumn - day 2/", SeasonInfoBox.getSeasonLine(calendarAtDay(winterStarts - 1)));
        }

        private static SeasonCalendar calendarAtTick(long totalTicks) {
            TickCounter ticks = new TickCounter();
            TimeOfDay timeOfDay = new TimeOfDay(ticks, TimeOfDay.DefaultTicksPerDay, 0f);
            for (long i = 0; i < totalTicks; i++) {
                ticks.increment();
            }
            return new SeasonCalendar(timeOfDay);
        }

        /** #501: through the last day before winter the warning counts hours, not "1 day!". */
        [Fact]
        public void testTheLastDayBeforeWinter_CountsDownInHours() {
            long lastDay = (3L * SeasonCalendar.DAYS_PER_SEASON - 1) * TimeOfDay.DefaultTicksPerDay;
            long hour = TimeOfDay.DefaultTicksPerDay / 24;

            Assert.Equal("Winter in 1 day!", SeasonInfoBox.getWinterLine(calendarAtTick(lastDay)));
            // half-hour offsets keep clear of whole-hour float rounding
            Assert.Equal("Winter in 23 hours!", SeasonInfoBox.getWinterLine(calendarAtTick(lastDay + hour + hour / 2)));
            Assert.Equal("Winter in 12 hours!", SeasonInfoBox.getWinterLine(calendarAtTick(lastDay + 12 * hour + hour / 2)));
            // the playtest's moment: 0.8 of the last autumn day
            Assert.Equal("Winter in 5 hours!", SeasonInfoBox.getWinterLine(calendarAtTick(lastDay + TimeOfDay.DefaultTicksPerDay * 4 / 5)));
            Assert.Equal("Winter in 1 hour!", SeasonInfoBox.getWinterLine(calendarAtTick(lastDay + TimeOfDay.DefaultTicksPerDay - 1)));
            Assert.Equal("Winter: no harvests", SeasonInfoBox.getWinterLine(calendarAtTick(lastDay + TimeOfDay.DefaultTicksPerDay)));
            // two days out still counts days, however late in the day
            Assert.Equal("Winter in 2 days", SeasonInfoBox.getWinterLine(calendarAtTick(lastDay - 1)));
        }

        [Fact]
        public void testHoursLeftInDay_RoundsUpAndStaysInRange() {
            Assert.Equal(24, SeasonInfoBox.hoursLeftInDay(0f));
            Assert.Equal(12, SeasonInfoBox.hoursLeftInDay(0.51f));
            Assert.Equal(5, SeasonInfoBox.hoursLeftInDay(0.8f));
            Assert.Equal(1, SeasonInfoBox.hoursLeftInDay(0.99f));
            Assert.Equal(1, SeasonInfoBox.hoursLeftInDay(1f));
        }

        /** #489: the box says why no one is joining while the nation cannot feed its members. */
        [Fact]
        public void testTheFamineLine_ShowsWhileTheNationCannotFeedItsMembers() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            Pawn member = new Pawn(player.getPosition(), "Member", simulation.getRandom());
            simulation.getEntityRepository().addEntity(member);
            member.setNationId(nation.getId());
            nation.addMember(member.getId());
            member.getInventory().clear();
            SeasonInfoBox box = new SeasonInfoBox(simulation);

            Assert.Null(SeasonInfoBox.getFamineLine(simulation.getSeasonCalendar(), simulation.getEntityRepository(), player));
            Assert.DoesNotContain(SeasonInfoBox.FamineLine, box.getLines());

            member.setEnergy(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);
            Assert.Equal(SeasonInfoBox.FamineLine, SeasonInfoBox.getFamineLine(simulation.getSeasonCalendar(), simulation.getEntityRepository(), player));
            Assert.Contains(SeasonInfoBox.FamineLine, box.getLines());
        }
    
        /** #500: a ruler whose nation has no members yet still sees a food line, saying who needs food and how to get them. */
        [Fact]
        public void testTheStoresLine_ForANationWithNoMembersYet_SaysSo_InAutumnAndWinter() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            Assert.Equal(0, WinterStores.count(simulation.getEntityRepository(), player.getNationId()).Members);
            int winterStarts = 3 * SeasonCalendar.DAYS_PER_SEASON;

            Assert.Equal(SeasonInfoBox.NoMembersLine,
                SeasonInfoBox.getStoresLine(calendarAtDay(winterStarts - 1), simulation.getEntityRepository(), player));
            Assert.Equal(SeasonInfoBox.NoMembersLine,
                SeasonInfoBox.getStoresLine(calendarAtDay(winterStarts), simulation.getEntityRepository(), player));
            // not in spring, nor without a nation
            Assert.Null(SeasonInfoBox.getStoresLine(calendarAtDay(0), simulation.getEntityRepository(), player));
            Assert.Null(SeasonInfoBox.getFamineLine(calendarAtDay(winterStarts), simulation.getEntityRepository(), player));
            // it names no key, so it reads the same by touch
            Assert.Null(TestTouchText.namedKey(SeasonInfoBox.NoMembersLine));

            // once a member joins, the count takes over
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            Pawn member = new Pawn(player.getPosition(), "Member", simulation.getRandom());
            simulation.getEntityRepository().addEntity(member);
            member.setNationId(nation.getId());
            nation.addMember(member.getId());
            member.getInventory().clear();
            Assert.Equal("Food: 0 of " + SeasonalRules.WINTER_FOOD_STORE + " for winter",
                SeasonInfoBox.getStoresLine(calendarAtDay(winterStarts - 1), simulation.getEntityRepository(), player));
        }
    
        private static SeasonCalendar calendarAtDayAndFraction(int day, float fraction) {
            TickCounter ticks = new TickCounter();
            TimeOfDay timeOfDay = new TimeOfDay(ticks, TimeOfDay.DefaultTicksPerDay, 0f);
            long target = (long) day * TimeOfDay.DefaultTicksPerDay + (long) (fraction * TimeOfDay.DefaultTicksPerDay);
            for (long i = 0; i < target; i++) {
                ticks.increment();
            }
            return new SeasonCalendar(timeOfDay);
        }

        private static Pawn memberOf(Simulation simulation, Player player, int apples) {
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            Pawn member = new Pawn(player.getPosition(), "Member", simulation.getRandom());
            simulation.getEntityRepository().addEntity(member);
            member.setNationId(nation.getId());
            nation.addMember(member.getId());
            member.getInventory().clear();
            member.getInventory().addItem(ItemType.APPLE, apples);
            member.setEnergy(95f);
            return member;
        }

        private static Simulation withNation(out Player player) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            return simulation;
        }

        /**
        * Playtest w2f: four members on track showed "Too little food" by
        * midday of winter's first day, the target still the whole winter's
        * 56. Mid-winter the line and the famine warning measure the rest of it.
        */
        [Fact]
        public void testMidWinter_AnOnTrackNation_ShowsNoFamineLine_AndAShortOneDoes() {
            Simulation simulation = withNation(out Player player);
            int winterStarts = 3 * SeasonCalendar.DAYS_PER_SEASON;
            Pawn[] members = new Pawn[4];
            for (int i = 0; i < members.Length; i++) {
                // half way through winter, each has half its store left
                members[i] = memberOf(simulation, player, SeasonalRules.WINTER_FOOD_STORE / 2);
            }
            SeasonCalendar midWinter = calendarAtDay(winterStarts + SeasonCalendar.DAYS_PER_SEASON / 2);
            int rest = 4 * SeasonalRules.WINTER_FOOD_STORE / 2;
            Assert.Equal("Food: " + rest + " of " + rest + " to spring",
                SeasonInfoBox.getStoresLine(midWinter, simulation.getEntityRepository(), player));
            Assert.Null(SeasonInfoBox.getFamineLine(midWinter, simulation.getEntityRepository(), player));

            // one member ate ahead and is now short: the nation is short of the rest of winter
            members[0].getInventory().removeItem(ItemType.APPLE, 3);
            Assert.Equal(SeasonInfoBox.FamineLine, SeasonInfoBox.getFamineLine(midWinter, simulation.getEntityRepository(), player));

            // the autumn target is the whole winter's, as before
            Assert.Equal("Food: " + (rest - 3) + " of " + (4 * SeasonalRules.WINTER_FOOD_STORE) + " for winter",
                SeasonInfoBox.getStoresLine(calendarAtDay(winterStarts - 1), simulation.getEntityRepository(), player));
            Assert.Null(SeasonInfoBox.getFamineLine(calendarAtDay(winterStarts - 1), simulation.getEntityRepository(), player));
        }

        /** By midday of winter's first day, a nation that has eaten an eighth of its store is still on track. */
        [Fact]
        public void testMiddayOfWintersFirstDay_AnOnTrackNation_ShowsNoFamineLine() {
            Simulation simulation = withNation(out Player player);
            int winterStarts = 3 * SeasonCalendar.DAYS_PER_SEASON;
            SeasonCalendar midday = calendarAtDayAndFraction(winterStarts, 0.5f);
            float progress = midday.getSeasonProgress();
            int left = (int) System.MathF.Ceiling(SeasonalRules.WINTER_FOOD_STORE * (1f - progress));
            for (int i = 0; i < 4; i++) {
                memberOf(simulation, player, left);
            }
            Assert.Null(SeasonInfoBox.getFamineLine(midday, simulation.getEntityRepository(), player));
        }

        /** Playtest w2: "17 of 14, 5 rotting" read as more rotting than the 3 over; the rotting apples are on top of the count. */
        [Fact]
        public void testTheStoresLine_SaysTheRottingApplesAreOnTopOfTheCount() {
            Simulation simulation = withNation(out Player player);
            Pawn member = memberOf(simulation, player, SeasonalRules.PAWN_APPLES_KEPT + 5);
            member.getInventory().addItem(ItemType.COOKED_MEAT, 3);
            int winterStarts = 3 * SeasonCalendar.DAYS_PER_SEASON;
            Assert.Equal("Food: " + (SeasonalRules.WINTER_FOOD_STORE + 3) + " of " + SeasonalRules.WINTER_FOOD_STORE + " for winter, +5 rotting",
                SeasonInfoBox.getStoresLine(calendarAtDay(winterStarts - 1), simulation.getEntityRepository(), player));
        }
}
}
