using System.Numerics;
using Xunit;
using beyondnations;

namespace beyondnationstests {

    /**
    * Seasons and regions show: the snapshot recolours ground and leaves
    * through the palette, and leaves everything as stored without one.
    */
    public class TestWorldPalette {

        private static SeasonCalendar calendarAtDay(int day, out TickCounter ticks) {
            ticks = new TickCounter();
            TimeOfDay timeOfDay = new TimeOfDay(ticks, TimeOfDay.DefaultTicksPerDay, 0f);
            for (long i = 0; i < (long) day * TimeOfDay.DefaultTicksPerDay; i++) {
                ticks.increment();
            }
            return new SeasonCalendar(timeOfDay);
        }

        [Fact]
        public void testWinterGround_IsNearlyWhite_AndSummerGroundGreen() {
            RegionMap map = new RegionMap(42, 105f);
            Rgba stored = new Rgba(0f, 0.5f, 0f);
            WorldPalette summer = new WorldPalette(calendarAtDay(SeasonCalendar.DAYS_PER_SEASON, out _), map);
            Rgba green = summer.groundColour(Region.PLAINS, stored);
            Assert.True(green.g > green.r && green.g > green.b);

            WorldPalette winter = new WorldPalette(calendarAtDay(3 * SeasonCalendar.DAYS_PER_SEASON, out _), map);
            Rgba snow = winter.groundColour(Region.PLAINS, stored);
            Assert.True(snow.r > 0.6f && snow.g > 0.6f && snow.b > 0.6f, snow.ToString());
        }

        [Fact]
        public void testLeaves_TurnInAutumn_AndOtherPartsKeepTheirColour() {
            WorldPalette autumn = new WorldPalette(calendarAtDay(2 * SeasonCalendar.DAYS_PER_SEASON, out _), new RegionMap(1, 105f));
            Rgba leaf = autumn.partColour(EntityType.TREE, Rgba.Green);
            Assert.True(leaf.r > leaf.g, leaf.ToString());
            Rgba trunk = new Rgba(0.5f, 0.25f, 0f);
            Assert.Equal(trunk, autumn.partColour(EntityType.TREE, trunk));
            Assert.Equal(Rgba.Green, autumn.partColour(EntityType.PAWN, Rgba.Green));
        }

        [Fact]
        public void testRegions_TintTheGroundDifferently() {
            WorldPalette summer = new WorldPalette(calendarAtDay(SeasonCalendar.DAYS_PER_SEASON, out _), new RegionMap(1, 105f));
            Rgba stored = new Rgba(0f, 0.5f, 0f);
            Assert.NotEqual(summer.groundColour(Region.FOREST, stored), summer.groundColour(Region.MOOR, stored));
        }

        [Fact]
        public void testTheSnapshot_UsesThePalette_OnlyWhenGivenOne() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            simulation.fixedUpdate(1f / 50f);

            WorldSnapshot plain = new WorldSnapshot();
            plain.capture(simulation.getEntityRepository(), simulation.getEnvironment());
            Location first = simulation.getEnvironment().getChunks()[0].getLocations()[0, 0];
            Assert.Equal(first.getColor(), plain.getGroundItems()[0].color);

            WorldSnapshot seasonal = new WorldSnapshot();
            seasonal.setPalette(simulation.getWorldPalette());
            seasonal.capture(simulation.getEntityRepository(), simulation.getEnvironment());
            Assert.Equal(plain.getGroundItems().Count, seasonal.getGroundItems().Count);
            Region region = simulation.getRegionMap().regionOf(simulation.getEnvironment().getChunks()[0]);
            Assert.Equal(simulation.getWorldPalette().groundColour(region, first.getColor()), seasonal.getGroundItems()[0].color);
        }
    }
}
