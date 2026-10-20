using System;

namespace beyondnations {

    /**
    * The season API (docs/SYSTEMS.md, "Seasons"): which season it is, which
    * day of the year, and how long until winter.
    *
    * Everything here is a pure function of the in-game day (TimeOfDay), which
    * is itself a pure function of the tick count, so the calendar holds no
    * state of its own: a save restores it by restoring the tick count, and the
    * same tick always falls in the same season. A world starts on the first
    * day of spring.
    *
    * Other systems (events, caravans, diplomacy) should read the season from
    * here rather than from the tick count, so the length of a season is
    * decided in one place: DAYS_PER_SEASON.
    */
    public class SeasonCalendar {

        /**
        * In-game days in each season. A day is ten minutes of play at the
        * default tick rate (TimeOfDay.DefaultTicksPerDay), so a season lasts
        * twenty minutes and a year (four seasons) about 80 minutes.
        */
        public const int DAYS_PER_SEASON = 2;

        public const int SEASONS_PER_YEAR = 4;

        public const int DAYS_PER_YEAR = DAYS_PER_SEASON * SEASONS_PER_YEAR;

        private readonly TimeOfDay timeOfDay;

        public SeasonCalendar(TimeOfDay timeOfDay) {
            if (timeOfDay == null) {
                throw new ArgumentNullException(nameof(timeOfDay));
            }
            this.timeOfDay = timeOfDay;
        }

        public TimeOfDay getTimeOfDay() { return timeOfDay; }

        /** The season it is now. */
        public Season getSeason() {
            return seasonOfDay(timeOfDay.getDay());
        }

        /** Days since the start of this year: 0 on the first day of spring, DAYS_PER_YEAR - 1 on the last of winter. */
        public int getDayOfYear() {
            return dayOfYear(timeOfDay.getDay());
        }

        /** Days since the start of this season: 0 on its first day. */
        public int getDayOfSeason() {
            return getDayOfYear() % DAYS_PER_SEASON;
        }

        /** The year: 0 for the world's first. */
        public int getYear() {
            return Math.Max(0, timeOfDay.getDay()) / DAYS_PER_YEAR;
        }

        public bool isWinter() {
            return getSeason() == Season.WINTER;
        }

        /**
        * Whole days until the given season next begins, counted from the start
        * of today: 0 while it is that season, 1 on the last day before it.
        */
        public int getDaysUntil(Season season) {
            return daysUntil(timeOfDay.getDay(), season);
        }

        /** Whole days until winter begins; 0 in winter. */
        public int getDaysUntilWinter() {
            return getDaysUntil(Season.WINTER);
        }

        /**
        * How far through the current season the world is, in [0, 1), counting
        * the time of day as well as whole days. Used to blend colours smoothly
        * from one season into the next.
        */
        public float getSeasonProgress() {
            float fraction = timeOfDay.getFraction();
            return (getDayOfSeason() + fraction) / DAYS_PER_SEASON;
        }

        /**
        * Moves the clock on by whole days to the first day of the given season
        * (of this year, or the next if it has passed; not at all if it is that
        * season already) without running the world: the
        * host's --season option, for screenshots of winter without an hour of
        * play. The tick count is all a season is, so the world simply finds
        * itself later; it is still deterministic and saves like any other.
        */
        public void advanceClockTo(TickCounter tickCounter, Season season) {
            if (tickCounter == null) {
                throw new ArgumentNullException(nameof(tickCounter));
            }
            // whole days, so the world arrives at the same time of day it left
            long ticks = (long) daysUntil(timeOfDay.getDay(), season) * timeOfDay.getTicksPerDay();
            for (long i = 0; i < ticks; i++) {
                tickCounter.increment();
            }
        }

        // pure functions, for tests and for code that asks about another day

        public static int dayOfYear(int day) {
            int d = day % DAYS_PER_YEAR;
            return d < 0 ? d + DAYS_PER_YEAR : d;
        }

        public static Season seasonOfDay(int day) {
            return (Season) (dayOfYear(day) / DAYS_PER_SEASON);
        }

        public static int daysUntil(int day, Season season) {
            if (seasonOfDay(day) == season) {
                return 0;
            }
            int start = (int) season * DAYS_PER_SEASON;
            int days = start - dayOfYear(day);
            return days < 0 ? days + DAYS_PER_YEAR : days;
        }

        public static Season next(Season season) {
            return (Season) (((int) season + 1) % SEASONS_PER_YEAR);
        }

        /** "Spring", "Summer", "Autumn" or "Winter". */
        public static string name(Season season) {
            switch (season) {
                case Season.SPRING: return "Spring";
                case Season.SUMMER: return "Summer";
                case Season.AUTUMN: return "Autumn";
                default: return "Winter";
            }
        }
    }
}
