using System;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * RandomSource carries its own copy of the generator System.Random uses for
    * a seeded instance, so that its state can be saved. It has to give exactly
    * the numbers System.Random(seed) does, or every seed would grow a
    * different world from the one it always has.
    */
    public class TestRandomSource {

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(-7)]
        [InlineData(123456789)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void testMatchesSystemRandom_ForEveryKindOfDraw(int seed) {
            RandomSource ours = new RandomSource(seed);
            Random theirs = new Random(seed);
            for (int i = 0; i < 20000; i++) {
                switch (i % 5) {
                    case 0:
                        Assert.Equal(theirs.Next(0, 100), ours.range(0, 100));
                        break;
                    case 1:
                        Assert.Equal(-50f + (float) theirs.NextDouble() * 100f, ours.range(-50f, 50f));
                        break;
                    case 2:
                        Assert.Equal((float) theirs.NextDouble(), ours.value());
                        break;
                    case 3:
                        Assert.Equal(theirs.Next(int.MinValue, int.MaxValue), ours.range(int.MinValue, int.MaxValue));
                        break;
                    default:
                        Assert.Equal(theirs.Next(-1000, 7), ours.range(-1000, 7));
                        break;
                }
            }
        }

        [Fact]
        public void testRestoredState_ContinuesTheSameSequence() {
            RandomSource original = new RandomSource(42);
            for (int i = 0; i < 777; i++) {
                original.range(0, 10);
            }
            RandomSource restored = new RandomSource(42);
            restored.setState(original.getState());
            for (int i = 0; i < 1000; i++) {
                Assert.Equal(original.range(0, 1000), restored.range(0, 1000));
                Assert.Equal(original.value(), restored.value());
            }
        }

        [Fact]
        public void testState_IsACopy() {
            RandomSource random = new RandomSource(5);
            int[] state = random.getState();
            state[1] = 0;
            Assert.NotEqual(0, random.getState()[1]);
        }

        [Fact]
        public void testBadState_IsRefused() {
            RandomSource random = new RandomSource(5);
            Assert.Throws<ArgumentException>(() => random.setState(new int[3]));
            int[] state = random.getState();
            state[RandomSource.StateLength] = 99;
            Assert.Throws<ArgumentException>(() => random.setState(state));
        }
    }
}
