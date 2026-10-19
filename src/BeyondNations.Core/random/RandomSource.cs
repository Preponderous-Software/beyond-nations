using System;

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
    *
    * The generator is the one System.Random uses for a seeded instance (Knuth's
    * subtractive generator, as in .NET's Net5CompatSeedImpl), written out here
    * so its state can be saved and restored (docs/SAVES.md). It produces
    * exactly the numbers System.Random(seed) does -- TestRandomSource holds it
    * to that -- so every seed generates the world it always has, and a loaded
    * world draws the same numbers the saved one would have gone on to draw.
    */
    public class RandomSource {
        /** How many ints the generator's state holds: Knuth's 55, plus the unused slot 0. */
        public const int StateLength = 56;

        private readonly int seed;
        private int[] seedArray;
        private int inext;
        private int inextp;

        public RandomSource() : this(System.Environment.TickCount) {
        }

        public RandomSource(int seed) {
            this.seed = seed;
            initialise(seed);
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
            return next(minInclusive, maxExclusive);
        }

        /**
        * Returns a float in [minInclusive, maxInclusive].
        * Matches UnityEngine.Random.Range(float, float).
        */
        public float range(float minInclusive, float maxInclusive) {
            return minInclusive + (float) sample() * (maxInclusive - minInclusive);
        }

        /**
        * Returns a float in [0, 1]. Matches UnityEngine.Random.value.
        */
        public float value() {
            return (float) sample();
        }

        // --- state, for saving (docs/SAVES.md) ---

        /** A copy of the generator's state: the 56 ints, then inext and inextp. */
        public int[] getState() {
            int[] state = new int[StateLength + 2];
            Array.Copy(seedArray, state, StateLength);
            state[StateLength] = inext;
            state[StateLength + 1] = inextp;
            return state;
        }

        /** Puts the generator back where getState() found it. */
        public void setState(int[] state) {
            if (state == null || state.Length != StateLength + 2) {
                throw new ArgumentException("a random state is " + (StateLength + 2) + " ints");
            }
            int savedINext = state[StateLength];
            int savedINextp = state[StateLength + 1];
            if (savedINext < 0 || savedINext >= StateLength || savedINextp < 0 || savedINextp >= StateLength) {
                throw new ArgumentException("random state indices out of range");
            }
            int[] copy = new int[StateLength];
            Array.Copy(state, copy, StateLength);
            seedArray = copy;
            inext = savedINext;
            inextp = savedINextp;
        }

        // --- the generator ---

        private void initialise(int seed) {
            int[] array = new int[StateLength];

            int subtraction = (seed == int.MinValue) ? int.MaxValue : Math.Abs(seed);
            int mj = 161803398 - subtraction;
            array[55] = mj;
            int mk = 1;

            int ii = 0;
            for (int i = 1; i < 55; i++) {
                if ((ii += 21) >= 55) {
                    ii -= 55;
                }
                array[ii] = mk;
                mk = mj - mk;
                if (mk < 0) {
                    mk += int.MaxValue;
                }
                mj = array[ii];
            }

            for (int k = 1; k < 5; k++) {
                for (int i = 1; i < 56; i++) {
                    int n = i + 30;
                    if (n >= 55) {
                        n -= 55;
                    }
                    array[i] -= array[1 + n];
                    if (array[i] < 0) {
                        array[i] += int.MaxValue;
                    }
                }
            }

            seedArray = array;
            inext = 0;
            inextp = 21;
        }

        private int internalSample() {
            int locINext = inext;
            if (++locINext >= 56) {
                locINext = 1;
            }
            int locINextp = inextp;
            if (++locINextp >= 56) {
                locINextp = 1;
            }

            int retVal = seedArray[locINext] - seedArray[locINextp];
            if (retVal == int.MaxValue) {
                retVal--;
            }
            if (retVal < 0) {
                retVal += int.MaxValue;
            }

            seedArray[locINext] = retVal;
            inext = locINext;
            inextp = locINextp;
            return retVal;
        }

        private double sample() {
            return internalSample() * (1.0 / int.MaxValue);
        }

        private double sampleForLargeRange() {
            int result = internalSample();
            bool negative = internalSample() % 2 == 0;
            if (negative) {
                result = -result;
            }
            double d = result;
            d += int.MaxValue - 1;
            d /= 2.0 * int.MaxValue - 1;
            return d;
        }

        private int next(int minValue, int maxValue) {
            long range = (long) maxValue - minValue;
            return range <= int.MaxValue
                ? (int) (sample() * range) + minValue
                : (int) ((long) (sampleForLargeRange() * range) + minValue);
        }
    }
}
