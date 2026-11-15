using System.Collections.Generic;

namespace beyondnations {

    public class NationNameGenerator {
        private RandomSource random;

        public NationNameGenerator(RandomSource random) {
            this.random = random;
        }


        private static string[] prefixes = new string[] {
            "New",
            "Old",
            "Great",
            "Little",
            "Big",
            "Small",
            "Red",
            "Blue",
            "Green",
            "Yellow",
            "Black",
            "White",
            "North",
            "South",
            "East",
            "West",
            "Upper",
            "Lower",
            "Northern",
            "Southern",
            "Eastern",
            "Western",
            "Fort",
            "Fortress",
            "Castle",
            "City",
            "Town",
            "Village",
            "Hamlet",
            "Farm"
        };

        private static string[] suffixes = new string[] {
            "shire",
            "land",
            "ton",
            "ville",
            "town",
            "burg",
            "port",
            "ford",
            "ham",
            "field",
            "wood"
        };

        // list of generated
        private List<string> generated = new List<string>();

        public string generate() {
            string prefix = prefixes[random.range(0, prefixes.Length)];
            string suffix = suffixes[random.range(0, suffixes.Length)];

            // check if name already generated
            string name = prefix + suffix;
            if (generated.Contains(name)) {
                return generate();
            }
            // remembered, so two nations never share a name (borders are told apart by it)
            generated.Add(name);
            return name;
        }

        /**
        * Remembers a name a nation took for itself (RenameNationCommand), so
        * no nation founded later is given it.
        */
        public void reserve(string name) {
            if (!generated.Contains(name)) {
                generated.Add(name);
            }
        }

        /** Whether the name has been given out or taken already. */
        public bool isTaken(string name) {
            return generated.Contains(name);
        }

        /**
        * The index-th name of a fixed walk through every prefix and suffix,
        * for the rename field's Suggest button. It draws nothing from the
        * world's random stream, so asking for one changes nothing.
        */
        public static string suggest(int index) {
            int count = prefixes.Length * suffixes.Length;
            int i = ((index % count) + count) % count;
            // a stride coprime with the count, so consecutive suggestions differ in both halves
            int k = (int) (((long) i * 37) % count);
            return prefixes[k / suffixes.Length] + suffixes[k % suffixes.Length];
        }

        // --- saving (docs/SAVES.md) ---

        internal List<string> getGenerated() {
            return generated;
        }

        internal void restoreGenerated(List<string> names) {
            generated = new List<string>(names);
        }
    }
}
