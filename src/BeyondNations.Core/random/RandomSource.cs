namespace beyondnations {

    /**
    * A seedable source of randomness for the simulation.
    *
    * This replaces UnityEngine.Random, which is static and process-wide and
    * therefore cannot be seeded per world. Instances are injected, so world
    * generation is reproducible: the same seed produces the same world.
    *
    * The two range methods deliberately keep UnityEngine.Random's asymmetry,
    * so that call sites port across without a change in behaviour:
    *   - the integer overload treats max as exclusive
    *   - the floating point overload treats max as inclusive
    */
    public class RandomSource {
        private readonly System.Random random;
        private readonly int seed;

        public RandomSource() : this(System.Environment.TickCount) {
        }

        public RandomSource(int seed) {
            this.seed = seed;
            this.random = new System.Random(seed);
        }

        public int getSeed() {
            return seed;
        }

        /**
        * Returns an integer in [minInclusive, maxExclusive).
        * Matches UnityEngine.Random.Range(int, int).
        */
        public int range(int minInclusive, int maxExclusive) {
            if (maxExclusive <= minInclusive) {
                return minInclusive;
            }
            return random.Next(minInclusive, maxExclusive);
        }

        /**
        * Returns a float in [minInclusive, maxInclusive].
        * Matches UnityEngine.Random.Range(float, float).
        */
        public float range(float minInclusive, float maxInclusive) {
            return minInclusive + (float) random.NextDouble() * (maxInclusive - minInclusive);
        }

        /**
        * Returns a float in [0, 1]. Matches UnityEngine.Random.value.
        */
        public float value() {
            return (float) random.NextDouble();
        }
    }
}
