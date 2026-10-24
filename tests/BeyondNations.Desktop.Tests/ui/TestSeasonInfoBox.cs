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
    }
}
