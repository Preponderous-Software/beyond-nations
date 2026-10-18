using System;
using System.Collections.Generic;
using System.IO;

namespace beyondnations.historyexport {

    /**
    * dotnet run --project tools/BeyondNations.HistoryExport -- \
    *     --seed 42 --ticks 30000 --every 25 --out history.jsonl
    *
    * Runs a seeded world with no window and writes its history as JSON Lines
    * (format described in HistoryExporter). tools/history-to-usd.py turns the
    * result into a time-sampled USD stage.
    */
    public static class Program {
        private const string Usage =
            "usage: BeyondNations.HistoryExport [--seed N] [--ticks N] [--every N]\n"
            + "                                  [--ticks-per-second N] [--kinds pawn,settlement,...]\n"
            + "                                  [--out FILE]   (default: stdout)";

        public static int Main(string[] args) {
            ExportOptions options = new ExportOptions();
            string outPath = null;
            try {
                for (int i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "--seed": options.Seed = int.Parse(next(args, ref i)); break;
                        case "--ticks": options.Ticks = int.Parse(next(args, ref i)); break;
                        case "--every": options.Every = int.Parse(next(args, ref i)); break;
                        case "--ticks-per-second": options.TicksPerSecond = int.Parse(next(args, ref i)); break;
                        case "--kinds": options.Kinds = parseKinds(next(args, ref i)); break;
                        case "--out": outPath = next(args, ref i); break;
                        case "-h":
                        case "--help":
                            Console.WriteLine(Usage);
                            return 0;
                        default:
                            throw new ArgumentException("unknown argument " + args[i]);
                    }
                }
                if (options.Seed == 0) {
                    throw new ArgumentException("--seed 0 means a random world in the game; pass a fixed seed");
                }
                if (options.Ticks <= 0 || options.Every <= 0 || options.TicksPerSecond <= 0) {
                    throw new ArgumentException("--ticks, --every and --ticks-per-second must be positive");
                }
            } catch (Exception e) when (e is ArgumentException || e is FormatException) {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(Usage);
                return 2;
            }

            using (Stream output = outPath == null ? Console.OpenStandardOutput() : File.Create(outPath)) {
                using (BufferedStream buffered = new BufferedStream(output, 1 << 16)) {
                    new HistoryExporter(options, buffered).run();
                }
            }
            if (outPath != null) {
                Console.Error.WriteLine("wrote " + outPath + " (seed " + options.Seed + ", " + options.Ticks + " ticks)");
            }
            return 0;
        }

        private static string next(string[] args, ref int i) {
            if (i + 1 >= args.Length) {
                throw new ArgumentException(args[i] + " needs a value");
            }
            return args[++i];
        }

        private static HashSet<EntityType> parseKinds(string list) {
            HashSet<EntityType> kinds = new HashSet<EntityType>();
            foreach (string name in list.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
                EntityType type;
                if (!Enum.TryParse(name.Trim().Replace("-", "_"), true, out type)) {
                    throw new ArgumentException("unknown entity kind " + name);
                }
                kinds.Add(type);
            }
            return kinds;
        }
    }
}
