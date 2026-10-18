using System.Collections.Generic;

namespace beyondnations {

    public class PawnNameGenerator {
        private RandomSource random;

        public PawnNameGenerator(RandomSource random) {
            this.random = random;
        }


        private static string[] names = new string[] {
            "Bob",
            "Alice",
            "Charlie",
            "Dave",
            "Eve",
            "Frank",
            "Grace",
            "Hank",
            "Irene",
            "Judy",
            "Karl",
            "Linda",
            "Mike",
            "Nancy",
            "Oscar",
            "Peggy",
            "Quinn",
            "Ruth",
            "Stan",
            "Tina",
            "Ursula",
            "Victor",
            "Wendy",
            "Xavier",
            "Yvonne",
            "Zach",
            "Abigail",
            "Bella",
            "Charlotte",
            "Daisy",
            "Ella",
            "Fiona",
            "Gemma",
            "Harriet",
            "Isabella",
            "Jasmine",
            "Katie",
            "Lily",
            "Mia",
            "Natalie",
            "Olivia",
            "Phoebe",
            "Rachel",
            "Samantha",
            "Tilly",
            "Una",
        };
        
        private static string[] familyNames = new string[] {
            "Smith",
            "Jones",
            "Williams",
            "Brown",
            "Taylor",
            "Davies",
            "Evans",
            "Wilson",
            "Thomas",
            "Roberts",
            "Johnson",
            "Lewis",
            "Walker",
            "Robinson",
            "Wood",
            "Thompson",
            "White",
            "Watson"
        };

        // list of generated
        private List<string> generated = new List<string>();

        public string generate() {
            string name = names[random.range(0, names.Length)];
            string familyName = familyNames[random.range(0, familyNames.Length)];
            string fullName = name + " " + familyName;
            if (generated.Contains(fullName)) {
                return generate();
            }
            generated.Add(fullName);

            if (generated.Count == names.Length * familyNames.Length) {
                generated.Clear();
            }

            return fullName;
        }
    }
}