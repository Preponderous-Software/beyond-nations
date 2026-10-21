using System.Collections.Generic;
using beyondnations;
using beyondnations.desktop;
using beyondnations.web;
using Xunit;

namespace beyondnationstests.web {

    public class TestWebOptions {

        private static List<string> parse(string query, List<string> warnings = null) {
            return WebOptions.toArguments(query, w => warnings?.Add(w));
        }

        [Fact]
        public void anEmptyQueryIsNoArguments() {
            Assert.Empty(parse(""));
            Assert.Empty(parse("?"));
            Assert.Empty(parse(null));
        }

        /** touch= is the page's (main.js), so the game takes no flag and gives no warning. */
        [Fact]
        public void theTouchSettingIsThePagesAndNotWarnedAbout() {
            List<string> warnings = new List<string>();
            Assert.Equal(new[] { "--seed", "7" }, parse("?touch=on&seed=7", warnings));
            Assert.Empty(warnings);
        }

        [Fact]
        public void namesAndValuesBecomeTheDesktopsFlags() {
            List<string> args = parse("?ticks-per-second=20&seed=42&start-screen=world&debug-mode");
            Assert.Equal(new[] { "--ticks-per-second", "20", "--seed", "42", "--start-screen", "world", "--debug-mode" }, args);
        }

        [Fact]
        public void theDiplomacyStageAndTheNationsScreenCanBeAskedFor() {
            List<string> args = parse("?stage-diplomacy&start-screen=nations");
            Assert.Equal(new[] { "--stage-diplomacy", "--start-screen", "nations" }, args);
        }

        [Fact]
        public void theSeasonOptionIsPassedThrough() {
            GameOptions options = GameOptions.parse(parse("?season=Winter").ToArray());
            Assert.Equal(Season.WINTER, options.StartSeason);
        }

        [Fact]
        public void theParsedOptionsMatchTheQuery() {
            GameOptions options = GameOptions.parse(parse("?ticks-per-second=25&seed=7&start-screen=main-menu&first-person=1&no-labels=true&render-distance=60&time-of-day=0.25").ToArray());
            Assert.Equal(25, options.TicksPerSecond);
            Assert.Equal(7, options.Seed);
            Assert.Equal(ScreenType.MAIN_MENU, options.StartScreen);
            Assert.True(options.FirstPerson);
            Assert.True(options.NoLabels);
            Assert.Equal(60, options.RenderDistance);
            Assert.Equal(0.25f, options.TimeOfDay);
        }

        [Fact]
        public void aLeadingDoubleDashAndCaseAreAccepted() {
            Assert.Equal(new[] { "--seed", "3" }, parse("?--SEED=3"));
        }

        /**
        * A share link is ?seed=42 (docs/SHARING.md); a seed can be text too,
        * which GameOptions hashes the way the New World screen does. A
        * number too large for an int used to be dropped; it is now text like
        * any other, so a link never silently starts a different, random world.
        */
        [Theory]
        [InlineData("?seed=42", 42, "42")]
        [InlineData("?seed=Ironvale", 1450162717, "Ironvale")]
        [InlineData("?seed=a+quiet%20valley", -1, "a quiet valley")]
        [InlineData("?seed=99999999999", -1, "99999999999")]
        public void aSeedInTheUrlStartsThatWorld(string query, int expected, string text) {
            List<string> warnings = new List<string>();
            List<string> args = parse(query, warnings);
            Assert.Empty(warnings);
            Assert.Equal(new[] { "--seed", text }, args);

            GameOptions options = GameOptions.parse(args.ToArray());
            Assert.Equal(text, options.SeedText);
            Assert.Equal(expected == -1 ? WorldSeed.fromText(text) : expected, options.Seed);
            Assert.NotEqual(0, options.Seed);
        }

        [Fact]
        public void theShareLinkRoundTripsToItsSeed() {
            int seed = WorldSeed.fromText("Ironvale");
            string link = SeedLink.build(seed);
            string query = link.Substring(link.IndexOf('?'));
            GameOptions options = GameOptions.parse(parse(query).ToArray());
            Assert.Equal(seed, options.Seed);
        }

        [Fact]
        public void theNewWorldAndShareScreensCanBeAskedFor() {
            Assert.Equal(new[] { "--start-screen", "new-world" }, parse("?start-screen=new-world"));
            Assert.Equal(ScreenType.SHARE, GameOptions.parse(parse("?start-screen=share").ToArray()).StartScreen);
        }

        [Fact]
        public void zeroOrFalseLeavesAFlagOff() {
            Assert.Empty(parse("?debug-mode=0&no-culling=false"));
        }

        /**
        * GameOptions.parse ends the process on a value it cannot read; in a
        * browser that would end the runtime. Nothing malformed may reach it.
        */
        [Theory]
        [InlineData("?ticks-per-second=fast")]
        [InlineData("?ticks-per-second=0")]
        [InlineData("?ticks-per-second=-5")]
        [InlineData("?seed=")]
        [InlineData("?start-screen=credits")]
        [InlineData("?renderer=vulkan")]
        [InlineData("?time-of-day=1")]
        [InlineData("?time-of-day=NaN")]
        [InlineData("?season=monsoon")]
        [InlineData("?debug-mode=maybe")]
        [InlineData("?width=800&height=600&no-vsync&smoke-resize&no-usage-reporting&help")]
        [InlineData("?utm_source=arcade&fbclid=x")]
        public void anythingUnusableIsDroppedWithAWarning(string query) {
            List<string> warnings = new List<string>();
            List<string> args = parse(query, warnings);
            Assert.Empty(args);
            Assert.NotEmpty(warnings);
        }

        [Fact]
        public void escapedValuesAreDecoded() {
            Assert.Equal(new[] { "--start-screen", "main-menu" }, parse("?start%2Dscreen=main%2Dmenu"));
        }
    }
}
