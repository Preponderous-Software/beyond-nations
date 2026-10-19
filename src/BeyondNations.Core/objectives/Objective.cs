using System;

namespace beyondnations {

    /**
    * One goal for the player. It is data plus two small functions over the
    * running Simulation: one says how far along the player is, the other what
    * to do next. Neither changes the world; only ObjectiveTracker does, and
    * only to hand out the reward.
    */
    public class Objective {
        private readonly string id;
        private readonly string title;
        private readonly Func<Simulation, ObjectiveProgress> progress;
        private readonly Func<Simulation, string> hint;
        private readonly ObjectiveReward reward;
        private readonly bool ambition;

        public Objective(string id, string title, Func<Simulation, ObjectiveProgress> progress,
                         Func<Simulation, string> hint, ObjectiveReward reward, bool ambition = false) {
            this.id = id ?? throw new ArgumentNullException(nameof(id));
            this.title = title ?? throw new ArgumentNullException(nameof(title));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.hint = hint ?? (_ => "");
            this.reward = reward ?? ObjectiveReward.None;
            this.ambition = ambition;
        }

        public Objective(string id, string title, Func<Simulation, ObjectiveProgress> progress,
                         string hint, ObjectiveReward reward, bool ambition = false)
            : this(id, title, progress, _ => hint, reward, ambition) {
        }

        public string getId() { return id; }
        public string getTitle() { return title; }
        public ObjectiveReward getReward() { return reward; }

        /** True for the endless goals that follow the first arc. */
        public bool isAmbition() { return ambition; }

        public ObjectiveProgress evaluate(Simulation simulation) {
            return progress(simulation);
        }

        /**
        * The hint, still carrying "{HintKey}" tokens; the host resolves them
        * with ObjectiveHint.resolve.
        */
        public string getHint(Simulation simulation) {
            return hint(simulation) ?? "";
        }
    }
}
