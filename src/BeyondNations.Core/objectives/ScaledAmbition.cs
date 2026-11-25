using System;

namespace beyondnations {

    /**
    * What a scaled ambition asks, once chosen: a counter ("Members"), its
    * target, the title that names the target, and what it pays.
    */
    public sealed class AmbitionGoal {
        public readonly string label;
        public readonly int target;
        public readonly string title;
        public readonly Func<Simulation, int> measure;
        public readonly Func<Simulation, string> hint;
        public readonly ObjectiveReward reward;

        public AmbitionGoal(string label, int target, string title, Func<Simulation, int> measure,
                            Func<Simulation, string> hint, ObjectiveReward reward) {
            this.label = label;
            this.target = target;
            this.title = title;
            this.measure = measure;
            this.hint = hint;
            this.reward = reward;
        }
    }

    /**
    * An ambition whose target is set from the nation as it stands when the
    * ambition appears: its members, funds or settlements now plus a step,
    * and never below the era's floor (ObjectiveCatalog.ambition). A target
    * from the era alone was often one the nation had long passed: "Grow your
    * nation to 16" and "to 25" were met within a second of appearing on
    * every pacing seed, and "Hold 3 settlements" on four of five
    * (docs/PACING.md, "Ambitions"). Which goal it is may depend on the
    * world too: a settlements ambition past the era's limit asks for
    * buildings, and so does a members ambition once no stranger is left.
    *
    * The goal is chosen on the first evaluation, which ObjectiveTracker makes
    * in the same tick the ambition before it completes. Nothing new is
    * saved: the save already holds the current objective's progress, whose
    * counter carries the target, and ObjectiveTracker.load gives it back with
    * restore. A completed ambition rebuilt from a save shows its era's floor.
    */
    public class ScaledAmbition : Objective {
        private readonly Func<Simulation, AmbitionGoal> choose;
        private readonly Func<string, int, AmbitionGoal> rebuild;
        private readonly AmbitionGoal floor;
        private AmbitionGoal goal;

        /**
        * @param floor the goal as the era alone sets it: shown until the goal is chosen
        * @param choose picks the goal from the world, on the first evaluation
        * @param rebuild the goal for a saved counter label and target, or null when it is not one of this ambition's
        */
        public ScaledAmbition(string id, AmbitionGoal floor, Func<Simulation, AmbitionGoal> choose,
                              Func<string, int, AmbitionGoal> rebuild)
            : base(id, floor.title, _ => ObjectiveProgress.flag(false), floor.hint, floor.reward, true) {
            this.floor = floor;
            this.choose = choose ?? throw new ArgumentNullException(nameof(choose));
            this.rebuild = rebuild;
        }

        /** The goal as chosen, or null before the first evaluation. */
        public AmbitionGoal getGoal() {
            return goal;
        }

        private AmbitionGoal shown() {
            return goal ?? floor;
        }

        public override string getTitle() { return shown().title; }
        public override ObjectiveReward getReward() { return shown().reward; }

        public override ObjectiveProgress evaluate(Simulation simulation) {
            if (goal == null) {
                goal = choose(simulation);
            }
            return ObjectiveProgress.count(goal.label, goal.measure(simulation), goal.target);
        }

        public override string getHint(Simulation simulation) {
            return shown().hint(simulation) ?? "";
        }

        /**
        * Takes back the goal a save's progress counter names. Returns whether
        * it could; if not, the goal is chosen afresh on the next evaluation.
        */
        public virtual bool restore(ObjectiveProgress saved) {
            if (!saved.hasCounter() || rebuild == null) {
                return false;
            }
            AmbitionGoal restored = rebuild(saved.getLabel(), saved.getTarget());
            if (restored == null) {
                return false;
            }
            goal = restored;
            return true;
        }
    }
}
