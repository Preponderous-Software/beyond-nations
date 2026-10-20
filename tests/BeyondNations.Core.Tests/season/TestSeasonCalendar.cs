using Xunit;
using beyondnations;

namespace beyondnationstests {

    /**
    * The season API other systems read: which season, which day of the year,
    * and how long until winter, all from the in-game day.
    */
    public class TestSeasonCalendar {

        private static SeasonCalendar calendarAtDay(int day) {
            TickCounter ticks = new TickCounter();
            // start at midnight so day N begins at tick N * ticksPerDay
            TimeOfDay timeOfDay = new TimeOfDay(ticks, TimeOfDay.DefaultTicksPerDay, 0f);
            for (long i = 0; i < (long) day * TimeOfDay.DefaultTicksPerDay; i++) {
                ticks.increment();
            }
            return new SeasonCalendar(timeOfDay);
        }

        [Fact]
        public void testAYear_LastsAboutAnHourAndAHalfOfPlay() {
            double minutes = (double) SeasonCalendar.DAYS_PER_YEAR * TimeOfDay.DefaultTicksPerDay / TickCounter.DefaultTicksPerSecond / 60.0;
            Assert.InRange(minutes, 60.0, 120.0);
        }

        [Fact]
        public void testAWorld_StartsInSpring() {
            SeasonCalendar calendar = calendarAtDay(0);
            Assert.Equal(Season.SPRING, calendar.getSeason());
            Assert.Equal(0, calendar.getDayOfYear());
            Assert.Equal(0, calendar.getYear());
            Assert.False(calendar.isWinter());
        }

        [Fact]
        public void testSeasons_ComeRoundInOrder() {
            for (int day = 0; day < SeasonCalendar.DAYS_PER_YEAR * 2; day++) {
                Season expected = (Season) ((day / SeasonCalendar.DAYS_PER_SEASON) % 4);
                Assert.Equal(expected, SeasonCalendar.seasonOfDay(day));
            }
            Assert.Equal(Season.SPRING, SeasonCalendar.next(Season.WINTER));
        }

        [Fact]
        public void testTheCalendar_FollowsTheClock() {
            int winterStarts = 3 * SeasonCalendar.DAYS_PER_SEASON;
            SeasonCalendar calendar = calendarAtDay(winterStarts);
            Assert.True(calendar.isWinter());
            Assert.Equal(winterStarts, calendar.getDayOfYear());
            Assert.Equal(0, calendar.getDayOfSeason());
            Assert.Equal(0, calendar.getDaysUntilWinter());

            SeasonCalendar nextYear = calendarAtDay(SeasonCalendar.DAYS_PER_YEAR + 1);
            Assert.Equal(1, nextYear.getYear());
            Assert.Equal(1, nextYear.getDayOfYear());
        }

        [Fact]
        public void testDaysUntilWinter_CountsDown() {
            int winterStarts = 3 * SeasonCalendar.DAYS_PER_SEASON;
            Assert.Equal(winterStarts, SeasonCalendar.daysUntil(0, Season.WINTER));
            Assert.Equal(1, SeasonCalendar.daysUntil(winterStarts - 1, Season.WINTER));
            Assert.Equal(0, SeasonCalendar.daysUntil(winterStarts, Season.WINTER));
            // just after winter, a whole year less winter to go
            Assert.Equal(SeasonCalendar.DAYS_PER_YEAR - SeasonCalendar.DAYS_PER_SEASON,
                SeasonCalendar.daysUntil(SeasonCalendar.DAYS_PER_YEAR, Season.WINTER));
        }

        [Fact]
        public void testSeasonProgress_RunsFromZeroToOne() {
            Assert.Equal(0f, calendarAtDay(0).getSeasonProgress(), 3);
            float halfway = calendarAtDay(SeasonCalendar.DAYS_PER_SEASON / 2).getSeasonProgress();
            Assert.InRange(halfway, 0.49f, 0.51f);
        }

        [Fact]
        public void testTheSimulation_ExposesTheCalendar() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Assert.Equal(Season.SPRING, simulation.getSeasonCalendar().getSeason());
            Assert.Same(simulation.getTimeOfDay(), simulation.getSeasonCalendar().getTimeOfDay());
        }

        [Fact]
        public void testAnnouncements_SayWhenASeasonTurns_AndWarnOfWinter() {
            int winterStarts = 3 * SeasonCalendar.DAYS_PER_SEASON;
            Assert.StartsWith("Winter has come.", SeasonalWorld.announcementFor(winterStarts));
            Assert.StartsWith("Winter comes tomorrow.", SeasonalWorld.announcementFor(winterStarts - 1));
            Assert.StartsWith("Summer has come.", SeasonalWorld.announcementFor(SeasonCalendar.DAYS_PER_SEASON));
            Assert.Null(SeasonalWorld.announcementFor(1));
        }
    }
}
