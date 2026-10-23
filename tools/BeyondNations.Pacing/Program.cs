using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace beyondnations.pacing {

    /**
    * dotnet run --project tools/BeyondNations.Pacing -c Release -- --check
    *
    * Runs the pacing bot on every seed in the budget file, writes a report,
    * and with --check exits 1 when any objective is outside its budget.
    * See docs/PACING.md.
    */
    public static class Program {
        private const string Usage =
            "usage: BeyondNations.Pacing [--budget FILE] [--seeds 1,2,3] [--objectives N] [--max-ticks N]\n"
            + "                            [--out DIR] [--check] [--write-budget] [--trace]\n"
            + "  --budget FILE    budget to read (default docs/pacing-budget.json)\n"
            + "  --seeds LIST     seeds to run (default: the budget's)\n"
            + "  --objectives N   stop each run after N objectives (default: the budget's)\n"
            + "  --max-ticks N    stop each run after N ticks (default: the budget's)\n"
            + "  --out DIR        write pacing-report.md and pacing-report.json there (default pacing-out)\n"
            + "  --check          exit 1 if any objective is too fast, too slow or never completes\n"
            + "  --write-budget   rewrite the budget file from these runs (review the diff!)\n"
            + "  --trace          print every status line the bot's player sees, with its time";

        public static int Main(string[] args) {
            string budgetPath = Path.Combine("docs", "pacing-budget.json");
            string outDir = "pacing-out";
            List<int> seeds = null;
            int objectiveLimit = -1;
            int maxTicks = -1;
            bool check = false;
            bool writeBudget = false;
            bool trace = false;
            try {
                for (int i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "--budget": budgetPath = next(args, ref i); break;
                        case "--seeds": seeds = parseSeeds(next(args, ref i)); break;
                        case "--objectives": objectiveLimit = int.Parse(next(args, ref i)); break;
                        case "--max-ticks": maxTicks = int.Parse(next(args, ref i)); break;
                        case "--out": outDir = next(args, ref i); break;
                        case "--check": check = true; break;
                        case "--write-budget": writeBudget = true; break;
                        case "--trace": trace = true; break;
                        case "-h":
                        case "--help":
                            Console.WriteLine(Usage);
                            return 0;
                        default:
                            throw new ArgumentException("unknown argument " + args[i]);
                    }
                }
            } catch (Exception e) when (e is ArgumentException || e is FormatException) {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(Usage);
                return 2;
            }

            PacingBudget budget = File.Exists(budgetPath) ? PacingBudget.load(budgetPath) : null;
            if (budget == null && check) {
                Console.Error.WriteLine("--check needs a budget file; " + budgetPath + " does not exist");
                return 2;
            }
            PacingBudget settings = budget ?? new PacingBudget();
            seeds ??= settings.Seeds;
            if (objectiveLimit <= 0) {
                objectiveLimit = settings.ObjectiveLimit;
            }
            if (maxTicks <= 0) {
                maxTicks = settings.MaxTicks;
            }

            // Each seed is its own Simulation with its own random streams, so
            // seeds run side by side without changing any result. A trace
            // runs them one at a time so its lines do not interleave.
            Stopwatch watch = Stopwatch.StartNew();
            PacingRunResult[] results = new PacingRunResult[seeds.Count];
            ParallelOptions parallel = new ParallelOptions { MaxDegreeOfParallelism = trace ? 1 : System.Environment.ProcessorCount };
            Parallel.For(0, seeds.Count, parallel, i => {
                results[i] = PacingRun.run(new PacingRunOptions {
                    Seed = seeds[i], ObjectiveLimit = objectiveLimit, MaxTicks = maxTicks,
                    Trace = trace ? Console.Out : null,
                });
            });
            List<PacingRunResult> runs = new List<PacingRunResult>(results);
            foreach (PacingRunResult run in runs) {
                int done = run.Objectives.FindAll(t => t.Completed).Count;
                Console.WriteLine("seed " + run.Seed + ": " + done + " objectives in " + run.TicksRun + " ticks ("
                    + PacingReport.clock((double) run.TicksRun / run.TicksPerSecond) + " of game time); raids on the player's nation " + run.RaidsOnPlayer
                    + " (" + run.raidsOnPlayerPerHour().ToString("0.0") + " an hour; " + run.RaidsOnPlayerSheltered + " while sheltered), by it " + run.RaidsByPlayer);
            }
            Console.WriteLine(seeds.Count + " seeds in " + watch.Elapsed.TotalSeconds.ToString("0.0") + " s");

            if (writeBudget) {
                budget = PacingBudget.suggest(runs, budget);
                budget.Seeds = seeds;
                budget.ObjectiveLimit = objectiveLimit;
                budget.MaxTicks = maxTicks;
                File.WriteAllText(budgetPath, budget.toJson());
                Console.WriteLine("wrote " + budgetPath + " -- review the diff before committing it");
            }

            List<PacingFinding> findings = budget == null ? new List<PacingFinding>() : PacingCheck.check(runs, budget);
            Directory.CreateDirectory(outDir);
            string markdown = PacingReport.markdown(runs, budget, findings);
            File.WriteAllText(Path.Combine(outDir, "pacing-report.md"), markdown);
            File.WriteAllText(Path.Combine(outDir, "pacing-report.json"), PacingReport.json(runs, findings));
            Console.WriteLine("wrote " + Path.Combine(outDir, "pacing-report.md") + " and pacing-report.json");

            int failures = 0;
            foreach (PacingFinding finding in findings) {
                if (finding.Verdict == PacingVerdict.Ok) {
                    continue;
                }
                string where = (finding.Seed == 0 ? "" : "seed " + finding.Seed + ", ") + finding.Title + " (" + finding.Id + ")";
                if (finding.isFailure()) {
                    failures++;
                    Console.WriteLine((check ? "::error::pacing: " : "FAIL ") + where + ": " + finding.Message);
                }
                else {
                    Console.WriteLine((check ? "::warning::pacing: " : "warning ") + where + ": " + finding.Message);
                }
            }
            if (failures == 0) {
                Console.WriteLine("pacing: every objective is within its budget on every seed");
            }
            return check && failures > 0 ? 1 : 0;
        }

        private static string next(string[] args, ref int i) {
            if (i + 1 >= args.Length) {
                throw new ArgumentException(args[i] + " needs a value");
            }
            return args[++i];
        }

        private static List<int> parseSeeds(string list) {
            List<int> seeds = new List<int>();
            foreach (string part in list.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
                int seed = int.Parse(part.Trim());
                if (seed == 0) {
                    throw new ArgumentException("seed 0 means a random world; pass fixed seeds");
                }
                seeds.Add(seed);
            }
            return seeds;
        }
    }
}
