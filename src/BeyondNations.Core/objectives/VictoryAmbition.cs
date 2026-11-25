using System;

namespace beyondnations {

    /**
    * The kingdom's lap-closing ambition. Until the nation has won, it is
    * "Win the game". Made after a victory (the run goes on), it would be met
    * within a second of appearing every lap, so it becomes a scaled goal
    * instead (ObjectiveCatalog.ambition picks which): whether it was won is
    * looked at on the first evaluation, as ScaledAmbition picks its goal.
    *
    * Nothing new is saved: a post-victory goal's saved counter brings it
    * back (restore), and "Win the game" saves a plain flag.
    */
    public class VictoryAmbition : ScaledAmbition {
        private readonly string winTitle;
        private readonly string winHint;
        private readonly ObjectiveReward winReward;
        private bool decided;
        private bool afterVictory;

        public VictoryAmbition(string id, string winTitle, string winHint, ObjectiveReward winReward,
                               AmbitionGoal floor, Func<Simulation, AmbitionGoal> choose,
                               Func<string, int, AmbitionGoal> rebuild)
            : base(id, floor, choose, rebuild) {
            this.winTitle = winTitle;
            this.winHint = winHint;
            this.winReward = winReward;
        }

        private static bool won(Simulation simulation) {
            return VictoryRules.isVictory(simulation.getEraTracker().getOutcome());
        }

        /** True once it has been decided this is a post-victory goal. */
        public bool isAfterVictory() {
            return decided && afterVictory;
        }

        public override string getTitle() { return isAfterVictory() ? base.getTitle() : winTitle; }
        public override ObjectiveReward getReward() { return isAfterVictory() ? base.getReward() : winReward; }

        public override ObjectiveProgress evaluate(Simulation simulation) {
            if (!decided) {
                decided = true;
                afterVictory = won(simulation);
            }
            return afterVictory ? base.evaluate(simulation) : ObjectiveProgress.flag(won(simulation));
        }

        public override string getHint(Simulation simulation) {
            if (!decided && won(simulation)) {
                return base.getHint(simulation);
            }
            return isAfterVictory() ? base.getHint(simulation) : winHint;
        }

        public override bool restore(ObjectiveProgress saved) {
            if (!base.restore(saved)) {
                return false;
            }
            decided = true;
            afterVictory = true;
            return true;
        }
    }
}
