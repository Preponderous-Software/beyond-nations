using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace beyondnations.pacing {

    /** Writes the pacing report as Markdown (to read) and JSON (to diff or chart). */
    public static class PacingReport {

        /** Curve rows in the Markdown report: one a minute of game time. */
        private const int MarkdownCurveEverySeconds = 60;

        private static string secs(double seconds) {
            return seconds.ToString("0.0", CultureInfo.InvariantCulture);
        }

        /** "1m 23s" or "45.0 s". */
        public static string clock(double seconds) {
            if (seconds < 60) {
                return secs(seconds) + " s";
            }
            int whole = (int) Math.Round(seconds);
            return (whole / 60) + "m " + (whole % 60).ToString("00") + "s";
        }

        public static string markdown(List<PacingRunResult> runs, PacingBudget budget, List<PacingFinding> findings) {
            StringBuilder md = new StringBuilder();
            md.Append("# Pacing report\n\n");
            if (runs.Count == 0) {
                return md.Append("No runs.\n").ToString();
            }
            int tps = runs[0].TicksPerSecond;
            int ticksPerDay = runs[0].TicksPerDay;
            List<int> seeds = new List<int>();
            foreach (PacingRunResult run in runs) {
                seeds.Add(run.Seed);
            }
            md.Append("Seeds ").Append(string.Join(", ", seeds)).Append("; ").Append(tps).Append(" ticks a second, ")
              .Append(ticksPerDay).Append(" ticks (").Append(clock((double) ticksPerDay / tps)).Append(") a day. ")
              .Append("Times are seconds of game time spent on each objective, from the previous one's completion. ")
              .Append("See docs/PACING.md.\n\n");

            md.Append("## Objectives\n\n");
            md.Append("| # | Objective | ");
            foreach (int seed in seeds) {
                md.Append("seed ").Append(seed).Append(" | ");
            }
            md.Append("median | budget | verdict |\n|---|---|");
            foreach (int seed in seeds) {
                md.Append("---|");
            }
            md.Append("---|---|---|\n");

            List<PacingStats> stats = PacingStats.collect(runs);
            for (int i = 0; i < stats.Count; i++) {
                PacingStats s = stats[i];
                md.Append("| ").Append(i + 1).Append(" | ").Append(s.Title).Append(" `").Append(s.Id).Append("` | ");
                foreach (PacingRunResult run in runs) {
                    ObjectiveTiming timing = run.Objectives.Find(t => t.Id == s.Id);
                    if (timing == null) {
                        md.Append("– | ");
                    }
                    else if (!timing.Completed) {
                        md.Append("never (").Append(clock((double) timing.DurationTicks / tps)).Append("+) | ");
                    }
                    else {
                        md.Append(clock((double) timing.DurationTicks / tps)).Append(" | ");
                    }
                }
                md.Append(s.Seconds.Count > 0 ? clock(s.MedianSeconds) : "–").Append(" | ");
                ObjectiveBudget entry = budget == null ? null : budget.find(s.Id);
                md.Append(entry == null ? "none" : clock(entry.MinSeconds) + " – " + clock(entry.MaxSeconds) + (entry.Advisory ? " (advisory)" : "")).Append(" | ");
                md.Append(verdictFor(findings, s.Id)).Append(" |\n");
            }

            md.Append("\n## Completion time (cumulative)\n\n| Objective | ");
            foreach (int seed in seeds) {
                md.Append("seed ").Append(seed).Append(" | ");
            }
            md.Append("\n|---|");
            foreach (int seed in seeds) {
                md.Append("---|");
            }
            md.Append("\n");
            foreach (PacingStats s in stats) {
                md.Append("| ").Append(s.Title).Append(" | ");
                foreach (PacingRunResult run in runs) {
                    ObjectiveTiming timing = run.Objectives.Find(t => t.Id == s.Id);
                    if (timing == null || !timing.Completed) {
                        md.Append("– | ");
                    }
                    else {
                        md.Append(clock((double) timing.CompletedAtTick / tps)).Append(" (day ")
                          .Append(((double) timing.CompletedAtTick / ticksPerDay).ToString("0.00", CultureInfo.InvariantCulture)).Append(") | ");
                    }
                }
                md.Append("\n");
            }

            List<PacingFinding> problems = findings == null ? new List<PacingFinding>() : findings.FindAll(f => f.Verdict != PacingVerdict.Ok);
            md.Append("\n## Findings\n\n");
            if (problems.Count == 0) {
                md.Append("Every objective on every seed is within its budget.\n");
            }
            foreach (PacingFinding f in problems) {
                md.Append("- ").Append(f.isFailure() ? "**FAIL** " : f.isOutOfBudget() ? "advisory " : "warning ")
                  .Append(f.Seed == 0 ? "" : "seed " + f.Seed + ", ").Append(f.Title).Append(" (`").Append(f.Id).Append("`): ")
                  .Append(f.Message).Append("\n");
            }

            md.Append("\n## Curves\n\nNations alive, pawns alive, and the player's nation and resources, once a minute.\n");
            foreach (PacingRunResult run in runs) {
                md.Append("\n### Seed ").Append(run.Seed).Append("\n\n")
                  .Append("| time | day | nations | pawns | members | settlements | coins | funds | wood | stone | objectives | bot |\n")
                  .Append("|---|---|---|---|---|---|---|---|---|---|---|---|\n");
                int every = MarkdownCurveEverySeconds * tps;
                for (int i = 0; i < run.Samples.Count; i++) {
                    PacingSample p = run.Samples[i];
                    if (p.Tick % every != 0 && i != run.Samples.Count - 1) {
                        continue;
                    }
                    md.Append("| ").Append(clock((double) p.Tick / tps)).Append(" | ")
                      .Append(((double) p.Tick / ticksPerDay).ToString("0.00", CultureInfo.InvariantCulture)).Append(" | ")
                      .Append(p.Nations).Append(" | ").Append(p.Pawns).Append(" | ").Append(p.PlayerNationMembers).Append(" | ")
                      .Append(p.PlayerSettlements).Append(" | ").Append(p.Coins).Append(" | ").Append(p.Funds).Append(" | ")
                      .Append(p.Wood).Append(" | ").Append(p.Stone).Append(" | ").Append(p.ObjectivesCompleted).Append(" | ").Append(p.Activity).Append(" |\n");
                }
            }
            return md.ToString();
        }

        private static string verdictFor(List<PacingFinding> findings, string id) {
            if (findings == null) {
                return "–";
            }
            List<string> bad = new List<string>();
            bool any = false;
            foreach (PacingFinding f in findings) {
                if (f.Id != id) {
                    continue;
                }
                any = true;
                if (f.Verdict != PacingVerdict.Ok) {
                    string word = f.Verdict switch {
                        PacingVerdict.TooFast => "too fast",
                        PacingVerdict.TooSlow => "too slow",
                        PacingVerdict.Never => "never",
                        PacingVerdict.Unbudgeted => "no budget",
                        _ => "not reached",
                    };
                    string entry = (f.Seed == 0 ? "" : "seed " + f.Seed + " ") + word;
                    if (!bad.Contains(entry)) {
                        bad.Add(entry);
                    }
                }
            }
            if (!any) {
                return "–";
            }
            return bad.Count == 0 ? "ok" : (findings.Exists(f => f.Id == id && f.isFailure()) ? "**" : "") + string.Join(", ", bad)
                + (findings.Exists(f => f.Id == id && f.isFailure()) ? "**" : "");
        }

        public static string json(List<PacingRunResult> runs, List<PacingFinding> findings) {
            var document = new {
                format = "beyond-nations-pacing-report/1",
                objectives = PacingStats.collect(runs).ConvertAll(s => new {
                    id = s.Id,
                    title = s.Title,
                    ambition = s.Ambition,
                    seconds = s.Seconds,
                    unfinished = s.Unfinished,
                    fastestSeconds = s.FastestSeconds,
                    medianSeconds = s.MedianSeconds,
                    slowestSeconds = s.SlowestSeconds,
                }),
                runs = runs.ConvertAll(r => new {
                    seed = r.Seed,
                    ticksPerSecond = r.TicksPerSecond,
                    ticksPerDay = r.TicksPerDay,
                    ticksRun = r.TicksRun,
                    objectives = r.Objectives.ConvertAll(t => new {
                        index = t.Index,
                        id = t.Id,
                        title = t.Title,
                        ambition = t.Ambition,
                        completed = t.Completed,
                        durationTicks = t.DurationTicks,
                        durationSeconds = (double) t.DurationTicks / r.TicksPerSecond,
                        completedAtTick = t.CompletedAtTick,
                        completedAtDay = t.Completed ? (double) t.CompletedAtTick / r.TicksPerDay : -1,
                        stuckActivity = t.StuckActivity,
                        stuckProgress = t.StuckProgress,
                    }),
                    samples = r.Samples.ConvertAll(p => new {
                        tick = p.Tick,
                        nations = p.Nations,
                        pawns = p.Pawns,
                        members = p.PlayerNationMembers,
                        settlements = p.PlayerSettlements,
                        coins = p.Coins,
                        funds = p.Funds,
                        wood = p.Wood,
                        stone = p.Stone,
                        objectivesCompleted = p.ObjectivesCompleted,
                        activity = p.Activity,
                    }),
                }),
                findings = (findings ?? new List<PacingFinding>()).ConvertAll(f => new {
                    seed = f.Seed,
                    id = f.Id,
                    verdict = f.Verdict.ToString(),
                    failure = f.isFailure(),
                    advisory = f.Advisory,
                    message = f.Message,
                }),
            };
            return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }) + "\n";
        }
    }
}
