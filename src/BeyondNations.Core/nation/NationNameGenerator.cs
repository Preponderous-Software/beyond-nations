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
            return name;
        }
    }
}