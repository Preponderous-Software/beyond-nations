using System;
using System.Globalization;
using System.Text;

namespace beyondnations {

    /**
    * Turns what a player types as a seed into the number a world is built
    * from, so a world can be named, shared and rebuilt.
    *
    * A whole number is used as it is: "42" is seed 42, the same world
    * `--seed 42` and `?seed=42` have always built. Anything else is text and
    * is hashed (FNV-1a, 32-bit, over the trimmed text's UTF-8 bytes, case
    * kept) to a positive seed, so "Ironvale" is the same world on every
    * machine, every run and every build. Empty text, or "0", means "pick one",
    * which is what a seed of 0 already means to GameConfig and Simulation.
    *
    * A hash never gives 0 (that would be "pick one"); it gives 1 instead.
    */
    public static class WorldSeed {
        /** The seed that means "pick a fresh one" (GameConfig.getWorldSeed()). */
        public const int Random = 0;

        private const uint FnvOffset = 2166136261;
        private const uint FnvPrime = 16777619;

        /**
        * The seed for text a player typed: the number itself for a whole
        * number, a hash of the text otherwise, Random (0) for empty text.
        */
        public static int fromText(string text) {
            if (text == null) {
                return Random;
            }
            string trimmed = text.Trim();
            if (trimmed.Length == 0) {
                return Random;
            }
            int number;
            if (int.TryParse(trimmed, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number)) {
                return number;
            }
            return hash(trimmed);
        }

        /** True when text is a whole number rather than a word to be hashed. */
        public static bool isNumber(string text) {
            int ignored;
            return text != null
                && int.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out ignored);
        }

        /** FNV-1a over the UTF-8 bytes, folded into 1..int.MaxValue. */
        public static int hash(string text) {
            uint h = FnvOffset;
            foreach (byte b in Encoding.UTF8.GetBytes(text ?? "")) {
                h ^= b;
                h *= FnvPrime;
            }
            int folded = (int) (h & 0x7FFFFFFF);
            return folded == 0 ? 1 : folded;
        }

        /**
        * A fresh positive seed for the New World screen's Random button. The
        * source is the caller's, not the world's: rolling a seed must not
        * draw from any world's RandomSource.
        */
        public static int roll(System.Random source) {
            return source.Next(1, int.MaxValue);
        }

        /** The seed as text, as a share link and the pause screen write it. */
        public static string format(int seed) {
            return seed.ToString(CultureInfo.InvariantCulture);
        }
    }
}
