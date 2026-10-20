using System.Collections.Generic;
using System.IO;
using Xunit;

using beyondnations;
using beyondnations.pacing;

namespace beyondnations.pacing.tests {

    /**
    * The pacing harness (docs/PACING.md): the budget check's verdicts on
    * made-up runs, the checked-in budget file, and a short real run of the
    * bot. The full five-seed run is a CI step of its own, not a unit test.
    */
    public class TestPacing {
        private const int Tps = 50;

        private static PacingRunResult runOf(int seed, params ObjectiveTiming[] timings) {
            PacingRunResult run = new PacingRunResult { Seed = seed, TicksPerSecond = Tps, TicksPerDay = 30000 };
            run.Objectives.AddRange(timings);
            return run;
        }

        private static ObjectiveTiming done(string id, double seconds) {
            return new ObjectiveTiming { Id = id, Title = id, DurationTicks = (int) (seconds * Tps), Completed = true, CompletedAtTick = 1 };
        }

        private static ObjectiveTiming unfinished(string id, double seconds) {
            return new ObjectiveTiming { Id = id, Title = id, DurationTicks = (int) (seconds * Tps), Completed = false, CompletedAtTick = -1,
                StuckActivity = "idle", StuckProgress = "Members 1/7" };
        }

        private static PacingBudget budgetOf(params ObjectiveBudget[] entries) {
            PacingBudget budget = new PacingBudget();
            budget.Objectives.AddRange(entries);
            return budget;
        }

        private static ObjectiveBudget entry(string id, double min, double max, bool advisory = false) {
            return new ObjectiveBudget { Id = id, Title = id, MinSeconds = min, MaxSeconds = max, Advisory = advisory };
        }

        private static PacingFinding only(List<PacingFinding> findings, string id) {
            return Assert.Single(findings.FindAll(f => f.Id == id));
        }

        [Fact]
        public void testCheck_FlagsTooFastTooSlowAndNever() {
            PacingBudget budget = budgetOf(entry("fast", 5, 60), entry("slow", 0, 60), entry("never", 0, 60), entry("fine", 5, 60));
            List<PacingFinding> findings = PacingCheck.check(new List<PacingRunResult> {
                runOf(1, done("fast", 1), done("slow", 61), done("fine", 30), unfinished("never", 10)),
            }, budget);

            Assert.Equal(PacingVerdict.TooFast, only(findings, "fast").Verdict);
            Assert.Equal(PacingVerdict.TooSlow, only(findings, "slow").Verdict);
            Assert.Equal(PacingVerdict.Never, only(findings, "never").Verdict);
            Assert.Equal(PacingVerdict.Ok, only(findings, "fine").Verdict);
            Assert.True(only(findings, "fast").isFailure());
            Assert.True(only(findings, "slow").isFailure());
            Assert.True(only(findings, "never").isFailure());
            Assert.False(only(findings, "fine").isFailure());
        }

        [Fact]
        public void testCheck_BoundsAreInclusive() {
            PacingBudget budget = budgetOf(entry("a", 5, 60), entry("b", 5, 60));
            List<PacingFinding> findings = PacingCheck.check(new List<PacingRunResult> { runOf(1, done("a", 5), done("b", 60)) }, budget);
            Assert.Equal(PacingVerdict.Ok, only(findings, "a").Verdict);
            Assert.Equal(PacingVerdict.Ok, only(findings, "b").Verdict);
        }

        [Fact]
        public void testCheck_AdvisoryAndUnbudgetedAndUnreachedOnlyWarn() {
            PacingBudget budget = budgetOf(entry("advisory", 30, 60, advisory: true), entry("gone", 0, 60));
            List<PacingFinding> findings = PacingCheck.check(new List<PacingRunResult> {
                runOf(1, done("advisory", 1), done("new", 3)),
            }, budget);

            PacingFinding advisory = only(findings, "advisory");
            Assert.Equal(PacingVerdict.TooFast, advisory.Verdict);
            Assert.True(advisory.isOutOfBudget());
            Assert.False(advisory.isFailure());
            Assert.Equal(PacingVerdict.Unbudgeted, only(findings, "new").Verdict);
            Assert.Equal(PacingVerdict.NotReached, only(findings, "gone").Verdict);
            Assert.DoesNotContain(findings, f => f.isFailure());
        }

        [Fact]
        public void testCheck_FailsPerSeed() {
            PacingBudget budget = budgetOf(entry("a", 5, 60));
            List<PacingFinding> findings = PacingCheck.check(new List<PacingRunResult> {
                runOf(1, done("a", 10)), runOf(2, done("a", 2)),
            }, budget);
            Assert.Equal(new[] { 2 }, findings.FindAll(f => f.isFailure()).ConvertAll(f => f.Seed));
        }

        [Fact]
        public void testSuggest_KeepsAStricterHandSetMinimumOnlyIfLower() {
            List<PacingRunResult> runs = new List<PacingRunResult> { runOf(1, done("a", 20)), runOf(2, done("a", 40)) };
            PacingBudget suggested = PacingBudget.suggest(runs, budgetOf(entry("a", 3, 999)));
            ObjectiveBudget a = suggested.find("a");
            Assert.Equal(3, a.MinSeconds);     // min(hand-set 3, half the fastest 10)
            Assert.Equal(140, a.MaxSeconds);   // twice the slowest plus a minute
        }

        [Fact]
        public void testCheckedInBudget_LoadsAndIsConsistent() {
            PacingBudget budget = PacingBudget.load(Path.Combine(repoRoot(), "docs", "pacing-budget.json"));
            Assert.NotEmpty(budget.Seeds);
            Assert.True(budget.MaxTicks > 0 && budget.ObjectiveLimit > 0);
            HashSet<string> ids = new HashSet<string>();
            foreach (ObjectiveBudget objective in budget.Objectives) {
                Assert.True(ids.Add(objective.Id), "duplicate budget entry " + objective.Id);
                Assert.True(objective.MinSeconds >= 0 && objective.MinSeconds <= objective.MaxSeconds, objective.Id);
            }
        }

        [Fact]
        public void testBot_PlaysTheOpeningAndIsDeterministic() {
            PacingRunOptions options = new PacingRunOptions { Seed = 42, ObjectiveLimit = 4, MaxTicks = 10000 };
            PacingRunResult first = PacingRun.run(options);
            PacingRunResult second = PacingRun.run(options);

            Assert.Equal(4, first.Objectives.Count);
            Assert.All(first.Objectives, t => Assert.True(t.Completed, t.Id + " did not complete"));
            Assert.Equal(ObjectiveCatalog.firstArc()[0].getId(), first.Objectives[0].Id);
            for (int i = 0; i < first.Objectives.Count; i++) {
                Assert.Equal(first.Objectives[i].Id, second.Objectives[i].Id);
                Assert.Equal(first.Objectives[i].CompletedAtTick, second.Objectives[i].CompletedAtTick);
            }
            Assert.Equal(first.Samples.Count, second.Samples.Count);
        }

        [Fact]
        public void testReport_NamesEveryObjectiveAndFinding() {
            PacingBudget budget = budgetOf(entry("a", 5, 60));
            List<PacingRunResult> runs = new List<PacingRunResult> { runOf(1, done("a", 2)) };
            List<PacingFinding> findings = PacingCheck.check(runs, budget);
            string markdown = PacingReport.markdown(runs, budget, findings);
            Assert.Contains("`a`", markdown);
            Assert.Contains("**FAIL** seed 1", markdown);
            Assert.Contains("\"verdict\": \"TooFast\"", PacingReport.json(runs, findings));
        }

        private static string repoRoot() {
            DirectoryInfo dir = new DirectoryInfo(System.AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BeyondNations.sln"))) {
                dir = dir.Parent;
            }
            Assert.NotNull(dir);
            return dir.FullName;
        }
    }
}
