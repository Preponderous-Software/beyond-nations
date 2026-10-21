using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace beyondnations.pacing {

    /** The allowed time for one objective, in seconds of game time. */
    public sealed class ObjectiveBudget {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("minSeconds")] public double MinSeconds { get; set; }
        [JsonPropertyName("maxSeconds")] public double MaxSeconds { get; set; }
        /** Reported like any other, but never fails the check: for objectives still being balanced. */
        [JsonPropertyName("advisory")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool Advisory { get; set; }
        [JsonPropertyName("note")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Note { get; set; }
    }

    /**
    * docs/pacing-budget.json: the seeds and run length the harness uses, and
    * a min and max time per objective. Time is measured from the previous
    * objective's completion (or the start of the world) to this one's, in
    * seconds of game time at the run's ticks per second.
    */
    public sealed class PacingBudget {
        public const string Format = "beyond-nations-pacing-budget/1";

        [JsonPropertyName("format")] public string FormatName { get; set; } = Format;
        [JsonPropertyName("seeds")] public List<int> Seeds { get; set; } = new List<int> { 1, 2, 3, 4, 5 };
        [JsonPropertyName("objectiveLimit")] public int ObjectiveLimit { get; set; } = 13;
        [JsonPropertyName("maxTicks")] public int MaxTicks { get; set; } = 2 * TimeOfDay.DefaultTicksPerDay;
        [JsonPropertyName("objectives")] public List<ObjectiveBudget> Objectives { get; set; } = new List<ObjectiveBudget>();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static PacingBudget load(string path) {
            PacingBudget budget = JsonSerializer.Deserialize<PacingBudget>(File.ReadAllText(path), JsonOptions);
            if (budget == null || budget.FormatName != Format) {
                throw new InvalidDataException(path + " is not a " + Format + " file");
            }
            return budget;
        }

        public string toJson() {
            return JsonSerializer.Serialize(this, JsonOptions) + "\n";
        }

        public ObjectiveBudget find(string id) {
            foreach (ObjectiveBudget entry in Objectives) {
                if (entry.Id == id) {
                    return entry;
                }
            }
            return null;
        }

        /**
        * A budget suggested from measured runs: half the fastest time to
        * twice the slowest plus a minute. Any hand-set minimum already in
        * `previous` that is stricter is kept, because a minimum states a
        * design intent ("this should take at least ...") rather than a
        * measurement. Review the diff before committing it.
        */
        public static PacingBudget suggest(List<PacingRunResult> runs, PacingBudget previous) {
            PacingBudget budget = new PacingBudget();
            if (previous != null) {
                budget.Seeds = previous.Seeds;
                budget.ObjectiveLimit = previous.ObjectiveLimit;
                budget.MaxTicks = previous.MaxTicks;
            }
            foreach (PacingStats stats in PacingStats.collect(runs)) {
                ObjectiveBudget old = previous == null ? null : previous.find(stats.Id);
                double min = Math.Floor(stats.FastestSeconds * 0.5);
                double max = Math.Ceiling(stats.SlowestSeconds * 2 + 60);
                budget.Objectives.Add(new ObjectiveBudget {
                    Id = stats.Id,
                    Title = stats.Title,
                    MinSeconds = old != null ? Math.Min(old.MinSeconds, min) : min,
                    MaxSeconds = max,
                    Advisory = old == null ? stats.Ambition : old.Advisory,
                    Note = old == null ? null : old.Note,
                });
            }
            return budget;
        }
    }

    /** One objective across every seed. */
    public sealed class PacingStats {
        public string Id;
        public string Title;
        public bool Ambition;
        public List<double> Seconds = new List<double>(); // completed runs only
        public int Unfinished;
        public double FastestSeconds;
        public double SlowestSeconds;
        public double MedianSeconds;

        /** In the order the objectives first appear. */
        public static List<PacingStats> collect(List<PacingRunResult> runs) {
            List<PacingStats> all = new List<PacingStats>();
            Dictionary<string, PacingStats> byId = new Dictionary<string, PacingStats>();
            foreach (PacingRunResult run in runs) {
                foreach (ObjectiveTiming timing in run.Objectives) {
                    PacingStats stats;
                    if (!byId.TryGetValue(timing.Id, out stats)) {
                        stats = new PacingStats { Id = timing.Id, Title = timing.Title, Ambition = timing.Ambition };
                        byId[timing.Id] = stats;
                        all.Add(stats);
                    }
                    if (timing.Completed) {
                        stats.Seconds.Add((double) timing.DurationTicks / run.TicksPerSecond);
                    }
                    else {
                        stats.Unfinished++;
                    }
                }
            }
            foreach (PacingStats stats in all) {
                if (stats.Seconds.Count == 0) {
                    continue;
                }
                List<double> sorted = new List<double>(stats.Seconds);
                sorted.Sort();
                stats.FastestSeconds = sorted[0];
                stats.SlowestSeconds = sorted[sorted.Count - 1];
                stats.MedianSeconds = sorted.Count % 2 == 1
                    ? sorted[sorted.Count / 2]
                    : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;
            }
            return all;
        }
    }

    public enum PacingVerdict { Ok, TooFast, TooSlow, Never, Unbudgeted, NotReached }

    public sealed class PacingFinding {
        public int Seed;
        public string Id;
        public string Title;
        public PacingVerdict Verdict;
        public string Message;
        /** The budget entry is advisory: an out-of-budget time is a warning only. */
        public bool Advisory;

        public bool isOutOfBudget() {
            return Verdict == PacingVerdict.TooFast || Verdict == PacingVerdict.TooSlow || Verdict == PacingVerdict.Never;
        }

        public bool isFailure() {
            return isOutOfBudget() && !Advisory;
        }
    }

    /**
    * Compares runs against a budget. Too fast, too slow and never finished
    * are failures; an objective with no budget entry, or a budget entry the
    * runs never reached, is only a warning, so another change to the
    * objective list shows up in the report without breaking the build until
    * its budget is written.
    */
    public static class PacingCheck {

        public static List<PacingFinding> check(List<PacingRunResult> runs, PacingBudget budget) {
            List<PacingFinding> findings = new List<PacingFinding>();
            HashSet<string> seen = new HashSet<string>();
            foreach (PacingRunResult run in runs) {
                foreach (ObjectiveTiming timing in run.Objectives) {
                    seen.Add(timing.Id);
                    double seconds = (double) timing.DurationTicks / run.TicksPerSecond;
                    ObjectiveBudget entry = budget.find(timing.Id);

                    PacingFinding finding = new PacingFinding {
                        Seed = run.Seed, Id = timing.Id, Title = timing.Title, Advisory = entry != null && entry.Advisory,
                    };
                    if (entry == null) {
                        finding.Verdict = PacingVerdict.Unbudgeted;
                        finding.Message = "no budget entry (took " + seconds.ToString("0.0") + " s)";
                    }
                    else if (!timing.Completed) {
                        finding.Verdict = PacingVerdict.Never;
                        finding.Message = "never completed: still " + timing.StuckProgress + " after " + seconds.ToString("0.0")
                            + " s, bot was " + timing.StuckActivity + " (budget " + entry.MinSeconds + "-" + entry.MaxSeconds + " s)";
                    }
                    else if (seconds < entry.MinSeconds) {
                        finding.Verdict = PacingVerdict.TooFast;
                        finding.Message = "too fast: " + seconds.ToString("0.0") + " s < min " + entry.MinSeconds + " s";
                    }
                    else if (seconds > entry.MaxSeconds) {
                        finding.Verdict = PacingVerdict.TooSlow;
                        finding.Message = "too slow: " + seconds.ToString("0.0") + " s > max " + entry.MaxSeconds + " s";
                    }
                    else {
                        finding.Verdict = PacingVerdict.Ok;
                        finding.Message = seconds.ToString("0.0") + " s";
                    }
                    findings.Add(finding);
                }
            }
            foreach (ObjectiveBudget entry in budget.Objectives) {
                if (!seen.Contains(entry.Id)) {
                    findings.Add(new PacingFinding {
                        Seed = 0, Id = entry.Id, Title = entry.Title, Verdict = PacingVerdict.NotReached,
                        Message = "in the budget but no run reached it (renamed, removed, or past the objective limit)",
                    });
                }
            }
            return findings;
        }
    }
}
