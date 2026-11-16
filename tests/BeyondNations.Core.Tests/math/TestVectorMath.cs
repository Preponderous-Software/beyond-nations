using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * VectorMath.normalized keeps Unity's rule that a vector too short to
    * normalise becomes zero, not NaN: a pawn standing on its target or a
    * chicken whose wander offset lands on zero would otherwise get a NaN
    * velocity and a position that never recovers.
    */
    public class TestVectorMath {

        [Fact]
        public void testNormalized_ZeroVector_IsZero_NotNaN() {
            Vector3 result = VectorMath.normalized(Vector3.Zero);

            Assert.Equal(Vector3.Zero, result);
            Assert.False(float.IsNaN(result.X) || float.IsNaN(result.Y) || float.IsNaN(result.Z));
        }

        [Fact]
        public void testNormalized_ShorterThanEpsilon_IsZero() {
            Vector3 tiny = new Vector3(VectorMath.NormalizeEpsilon / 2f, 0, 0);

            Assert.Equal(Vector3.Zero, VectorMath.normalized(tiny));
        }

        [Fact]
        public void testNormalized_JustLongerThanEpsilon_IsNormalised() {
            Vector3 result = VectorMath.normalized(new Vector3(0, 0, VectorMath.NormalizeEpsilon * 2f));

            Assert.Equal(1f, result.Length(), 4);
            Assert.Equal(1f, result.Z, 4);
        }

        [Fact]
        public void testNormalized_KeepsDirection_WithUnitLength() {
            Vector3 result = VectorMath.normalized(new Vector3(3, 0, -4));

            Assert.Equal(0.6f, result.X, 5);
            Assert.Equal(0f, result.Y, 5);
            Assert.Equal(-0.8f, result.Z, 5);
            Assert.Equal(1f, result.Length(), 5);
        }

        [Fact]
        public void testNormalized_UnitVector_IsUnchanged() {
            Assert.Equal(Vector3.UnitY, VectorMath.normalized(Vector3.UnitY));
        }

        [Fact]
        public void testNormalized_MatchesSystemNumerics_ForOrdinaryVectors() {
            RandomSource random = new RandomSource(20261108);
            for (int i = 0; i < 100; i++) {
                Vector3 vector = new Vector3(random.range(-50f, 50f), random.range(-50f, 50f), random.range(-50f, 50f));
                if (vector.Length() < 1f) {
                    continue;
                }
                Vector3 expected = Vector3.Normalize(vector);
                Vector3 actual = VectorMath.normalized(vector);
                Assert.Equal(expected.X, actual.X, 5);
                Assert.Equal(expected.Y, actual.Y, 5);
                Assert.Equal(expected.Z, actual.Z, 5);
            }
        }
    }
}
