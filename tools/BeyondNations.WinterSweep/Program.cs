using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace beyondnations.wintersweep {

    /**
    * dotnet tools/BeyondNations.WinterSweep/bin/Release/net8.0/BeyondNations.WinterSweep.dll world --seeds 41-56
    *
    * Two measurements of the first winter, each over many seeds, run in
    * parallel and printed in seed order with a summary. See README.md.
    */
    public static class Program {
        private const string Usage =
            "usage: BeyondNations.WinterSweep world [--seeds 41-56] [--csv DIR] [--fed]\n"
            + "       BeyondNations.WinterSweep camp  [--seeds 101-108] [--stock N] [--members N] [--town] [--timeline]\n"
            + "  world        an AI world (as TestSeasonalRules) from day 0 to the end of the second spring's first day\n"
            + "  camp         a player-led camp, stocked at winter's first tick, watched through the winter\n"
            + "  --seeds LIST seeds, as 41-56 or 41,42,50 (defaults above)\n"
            + "  --csv DIR    world: write DIR/<seed>.csv, a row every 300 ticks (tools/winter-sweep-chart.py draws them)\n"
            + "  --fed        world: top up every nation member to 2 apples each winter tick (what a fed winter eats)\n"
            + "  --stock N    camp: food each member is set to at winter's first tick (default WINTER_FOOD_STORE)\n"
            + "  --members N  camp: members besides the player (default 6, as the 0.8.0 winter playtest)\n"
            + "  --town       camp: stage the town from --stage-settlement instead of a bare camp\n"
            + "  --timeline   camp: also print food, members and mean energy every 3000 ticks";

        public static int Main(string[] args) {
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help") {
                Console.WriteLine(Usage);
                return args.Length == 0 ? 2 : 0;
            }
            string mode = args[0];
            List<int> seeds = null;
            string csvDir = null;
            bool fed = false;
            int stock = SeasonalRules.WINTER_FOOD_STORE;
            int members = 6;
            bool town = false;
            bool timeline = false;
            try {
                for (int i = 1; i < args.Length; i++) {
                    switch (args[i]) {
                        case "--seeds": seeds = parseSeeds(next(args, ref i)); break;
                        case "--csv": csvDir = next(args, ref i); break;
                        case "--fed": fed = true; break;
                        case "--stock": stock = int.Parse(next(args, ref i)); break;
                        case "--members": members = int.Parse(next(args, ref i)); break;
                        case "--town": town = true; break;
                        case "--timeline": timeline = true; break;
                        default: throw new ArgumentException("unknown option " + args[i]);
                    }
                }
            }
            catch (Exception exception) when (exception is ArgumentException || exception is FormatException) {
                Console.Error.WriteLine(exception.Message);
                Console.Error.WriteLine(Usage);
                return 2;
            }

            switch (mode) {
                case "world": {
                    seeds = seeds ?? parseSeeds("41-56");
                    if (csvDir != null) {
                        System.IO.Directory.CreateDirectory(csvDir);
                    }
                    WorldSweep.Result[] results = runAll(seeds, seed => WorldSweep.run(seed, csvDir, fed));
                    foreach (WorldSweep.Result result in results) {
                        Console.WriteLine(result.line);
                    }
                    Console.WriteLine(WorldSweep.summarise(results));
                    return 0;
                }
                case "camp": {
                    seeds = seeds ?? parseSeeds("101-108");
                    CampProbe.Result[] results = runAll(seeds, seed => CampProbe.run(seed, stock, members, town, timeline));
                    foreach (CampProbe.Result result in results) {
                        Console.WriteLine(result.text);
                    }
                    Console.WriteLine(CampProbe.summarise(results));
                    return 0;
                }
                default:
                    Console.Error.WriteLine("unknown mode " + mode);
                    Console.Error.WriteLine(Usage);
                    return 2;
            }
        }

        // every seed is its own Simulation, so they run side by side (as the pacing bot's do)
        private static T[] runAll<T>(List<int> seeds, Func<int, T> run) {
            T[] results = new T[seeds.Count];
            Parallel.For(0, seeds.Count, new ParallelOptions { MaxDegreeOfParallelism = System.Environment.ProcessorCount },
                i => results[i] = run(seeds[i]));
            return results;
        }

        private static string next(string[] args, ref int i) {
            if (i + 1 >= args.Length) {
                throw new ArgumentException(args[i] + " needs a value");
            }
            return args[++i];
        }

        /** "41-56", "41,42,50" or a mix: "41-44,50". */
        public static List<int> parseSeeds(string text) {
            List<int> seeds = new List<int>();
            foreach (string part in text.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
                int dash = part.IndexOf('-', 1);
                if (dash > 0) {
                    int from = int.Parse(part.Substring(0, dash));
                    int to = int.Parse(part.Substring(dash + 1));
                    for (int s = from; s <= to; s++) {
                        seeds.Add(s);
                    }
                }
                else {
                    seeds.Add(int.Parse(part));
                }
            }
            if (seeds.Count == 0) {
                throw new ArgumentException("no seeds in " + text);
            }
            return seeds;
        }

        internal static string meanMax(IEnumerable<double> values) {
            List<double> list = values.ToList();
            return list.Average().ToString("F2") + " (max " + list.Max().ToString("0.##") + ")";
        }
    }
}
