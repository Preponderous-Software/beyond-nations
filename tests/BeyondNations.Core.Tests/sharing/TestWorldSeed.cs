using System;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * A seed typed on the New World screen, passed as --seed or carried in a
    * share link has to name the same world everywhere, forever. These pin
    * the text-to-seed rule (docs/SHARING.md) so it cannot drift.
    */
    public class TestWorldSeed {

        [Theory]
        [InlineData("42", 42)]
        [InlineData(" 42 ", 42)]
        [InlineData("-7", -7)]
        [InlineData("007", 7)]
        [InlineData("2147483647", 2147483647)]
        public void testAWholeNumberIsThatSeed(string text, int seed) {
            Assert.Equal(seed, WorldSeed.fromText(text));
            Assert.True(WorldSeed.isNumber(text));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("0")]
        public void testEmptyTextOrZeroMeansAFreshWorld(string text) {
            Assert.Equal(WorldSeed.Random, WorldSeed.fromText(text));
        }

        [Fact]
        public void testTextHashesToAPinnedSeed() {
            // FNV-1a/32 of the UTF-8 bytes, top bit cleared. Pinned: a change
            // here would send every shared word-seed to a different world.
            Assert.Equal(1450162717, WorldSeed.fromText("Ironvale"));
            Assert.Equal(WorldSeed.fromText("Ironvale"), WorldSeed.fromText("  Ironvale\t"));
        }

        [Fact]
        public void testTextSeedsAreDeterministicAndDistinct() {
            Assert.Equal(WorldSeed.fromText("a quiet valley"), WorldSeed.fromText("a quiet valley"));
            Assert.NotEqual(WorldSeed.fromText("Ironvale"), WorldSeed.fromText("ironvale"));
            Assert.NotEqual(WorldSeed.fromText("Ironvale"), WorldSeed.fromText("Ironvale2"));
            // a number too large for an int is text, not an error
            Assert.False(WorldSeed.isNumber("99999999999"));
            Assert.True(WorldSeed.fromText("99999999999") > 0);
        }

        [Theory]
        [InlineData("Ironvale")]
        [InlineData("x")]
        [InlineData("Ünïcödé seed")]
        [InlineData("99999999999")]
        public void testAHashedSeedIsPositive(string text) {
            int seed = WorldSeed.fromText(text);
            Assert.True(seed > 0, text + " -> " + seed);
        }

        [Fact]
        public void testTheSameTextBuildsTheSameWorld() {
            GameConfig a = new GameConfig();
            a.setWorldSeed(WorldSeed.fromText("Ironvale"));
            GameConfig b = new GameConfig();
            b.setWorldSeed(WorldSeed.fromText("Ironvale"));
            Simulation first = new Simulation(a);
            Simulation second = new Simulation(b);
            Assert.Equal(first.getRandom().getSeed(), second.getRandom().getSeed());
            Assert.Equal(first.getEntityRepository().getEntityCount(), second.getEntityRepository().getEntityCount());
            Assert.Equal(first.getRandom().range(0, 1000000), second.getRandom().range(0, 1000000));
        }

        [Fact]
        public void testRolledSeedsArePositiveAndComeFromTheGivenSource() {
            Random a = new Random(5);
            Random b = new Random(5);
            for (int i = 0; i < 100; i++) {
                int seed = WorldSeed.roll(a);
                Assert.True(seed > 0);
                Assert.Equal(seed, WorldSeed.roll(b));
            }
        }

        [Fact]
        public void testTheShareLinkCarriesTheNumber() {
            Assert.Equal("https://beyond-nations.play.danielstephenson.dev/?seed=42", SeedLink.build(42));
            Assert.Equal("https://beyond-nations.play.danielstephenson.dev/?seed=-7", SeedLink.build(-7));
            Assert.Equal(SeedLink.BaseUrl + "?seed=1450162717", SeedLink.build(WorldSeed.fromText("Ironvale")));
        }
    }
}
