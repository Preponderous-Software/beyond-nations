using System.Collections.Generic;
using System.Text;
using beyondnations.desktop.ui.progression;

namespace beyondnations.pacing {

    /**
    * The HUD's unlocks (HudUnlocks) under the bot (docs/PACING.md,
    * "Unlocks"). A run records the tick each unlock's own condition first
    * holds, leaving out the safety net that unlocks a part once an
    * objective points at it, and the check fails when one comes after an
    * objective that needs it became the current one, or never comes in the
    * run. Watching reads the world through UnlockSnapshot, which only reads,
    * so the run steps exactly as it does without it.
    */
    public static class PacingUnlocks {
        /** An unlock's findings carry its id with this in front, so they never collide with an objective's. */
        public const string FindingPrefix = "unlock:";

        /** Looks again at every rule not yet met; call once a second and on every tick an objective completes. */
        public static void observe(Simulation simulation, int tick, Dictionary<string, int> reached) {
            if (reached.Count == HudUnlocks.Rules.Length) {
                return;
            }
            UnlockSnapshot snapshot = UnlockSnapshot.of(simulation);
            foreach (HudUnlocks.Rule rule in HudUnlocks.Rules) {
                string id = rule.id.ToString();
                if (!reached.ContainsKey(id) && rule.when(snapshot)) {
                    reached[id] = tick;
                }
            }
        }

        /** The tick an objective became the current one in this run (the previous one's completion), or -1 if it never did. */
        public static int pointedAt(PacingRunResult run, string objectiveId) {
            for (int i = 0; i < run.Objectives.Count; i++) {
                if (run.Objectives[i].Id != objectiveId) {
                    continue;
                }
                if (i == 0) {
                    return 0;
                }
                return run.Objectives[i - 1].Completed ? run.Objectives[i - 1].CompletedAtTick : -1;
            }
            return -1;
        }

        public static List<PacingFinding> check(List<PacingRunResult> runs) {
            List<PacingFinding> findings = new List<PacingFinding>();
            foreach (PacingRunResult run in runs) {
                foreach (HudUnlocks.Rule rule in HudUnlocks.Rules) {
                    string id = rule.id.ToString();
                    PacingFinding finding = new PacingFinding { Seed = run.Seed, Id = FindingPrefix + id, Title = rule.title + " unlock" };
                    int at;
                    if (!run.Unlocks.TryGetValue(id, out at)) {
                        finding.Verdict = PacingVerdict.Never;
                        finding.Message = "never unlocked in " + ((double) run.TicksRun / run.TicksPerSecond).ToString("0") + " s";
                        findings.Add(finding);
                        continue;
                    }
                    finding.Verdict = PacingVerdict.Ok;
                    finding.Message = "unlocked at " + PacingReport.clock((double) at / run.TicksPerSecond);
                    foreach (string objective in rule.neededBy) {
                        int pointed = pointedAt(run, objective);
                        if (pointed >= 0 && at > pointed) {
                            finding.Verdict = PacingVerdict.TooSlow;
                            finding.Message = "unlocked at " + PacingReport.clock((double) at / run.TicksPerSecond) + ", after objective " + objective
                                + " pointed at it (" + PacingReport.clock((double) pointed / run.TicksPerSecond) + ")";
                            break;
                        }
                    }
                    findings.Add(finding);
                }
            }
            return findings;
        }

        /** The report's Unlocks table: when each came on each seed, and the objective that needs it. */
        public static string markdown(List<PacingRunResult> runs) {
            StringBuilder md = new StringBuilder();
            md.Append("\n## Unlocks\n\nWhen each part of the HUD first became useful (HudUnlocks' own condition, without the objective safety net), from the start of the world (docs/PACING.md, \"Unlocks\").\n\n| Unlock | ");
            foreach (PacingRunResult run in runs) {
                md.Append("seed ").Append(run.Seed).Append(" | ");
            }
            md.Append("needed by |\n|---|");
            foreach (PacingRunResult run in runs) {
                md.Append("---|");
            }
            md.Append("---|\n");
            foreach (HudUnlocks.Rule rule in HudUnlocks.Rules) {
                string id = rule.id.ToString();
                md.Append("| ").Append(rule.title).Append(" `").Append(id).Append("` | ");
                foreach (PacingRunResult run in runs) {
                    int at;
                    md.Append(run.Unlocks.TryGetValue(id, out at) ? PacingReport.clock((double) at / run.TicksPerSecond) : "never").Append(" | ");
                }
                md.Append(rule.neededBy.Length == 0 ? "-" : string.Join(", ", rule.neededBy)).Append(" |\n");
            }
            return md.ToString();
        }
    }
}
