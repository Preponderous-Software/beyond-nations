using System;
using System.Collections.Generic;

namespace beyondnations {

    /**
    * Walks the player through one ordered list of objectives: the first arc,
    * then ambitions appended one at a time as each is reached, so there is
    * always a next target.
    *
    * Only the current objective is evaluated, and only once every evaluation
    * interval (a second of game time by default), never per entity per tick.
    * At most one objective completes per evaluation, so a player who is
    * already past several steps sees each one announced in turn rather than a
    * burst of toasts in one frame.
    */
    public class ObjectiveTracker {
        private readonly List<Objective> objectives;
        private readonly Func<int, Objective> ambitionFactory;
        private int currentIndex = 0;
        private int ambitionsGenerated = 0;

        private ObjectiveProgress currentProgress = ObjectiveProgress.flag(false);
        private bool evaluatedOnce = false;

        private Objective lastCompleted = null;
        private int lastCompletedTick = -1;

        /**
        * @param arc the opening objectives, in order
        * @param ambitionFactory makes the n-th (0-based) ambition that follows
        *        the arc, or returns null when there are no more
        */
        public ObjectiveTracker(IEnumerable<Objective> arc, Func<int, Objective> ambitionFactory) {
            objectives = new List<Objective>(arc ?? throw new ArgumentNullException(nameof(arc)));
            this.ambitionFactory = ambitionFactory;
            ensureCurrentExists();
        }

        /** The game's own arc and ambitions. */
        public static ObjectiveTracker createDefault() {
            return new ObjectiveTracker(ObjectiveCatalog.firstArc(), ObjectiveCatalog.ambition);
        }

        /**
        * Called by Simulation every tick; evaluates on the first call and then
        * once per second of game time.
        */
        public void update(Simulation simulation) {
            TickCounter ticks = simulation.getTickCounter();
            if (evaluatedOnce && ticks.getTotalTicks() % ticks.getTicksPerSecond() != 0) {
                return;
            }
            evaluate(simulation);
        }

        /**
        * Evaluates the current objective now and, if it is met, rewards the
        * player and moves on.
        * @return whether an objective was completed
        */
        public bool evaluate(Simulation simulation) {
            evaluatedOnce = true;
            Objective current = getCurrent();
            if (current == null) {
                return false;
            }

            currentProgress = current.evaluate(simulation);
            if (!currentProgress.isComplete()) {
                return false;
            }

            complete(current, simulation);
            Objective next = getCurrent();
            currentProgress = next != null ? next.evaluate(simulation) : ObjectiveProgress.flag(false);
            return true;
        }

        private void complete(Objective objective, Simulation simulation) {
            Player player = simulation.getPlayer();
            objective.getReward().grantTo(player.getInventory());

            string message = "Objective complete: " + objective.getTitle() + ".";
            if (!objective.getReward().isEmpty()) {
                message += " " + objective.getReward().describe() + ".";
            }
            player.getStatus().update(message);
            Log.info(message);

            lastCompleted = objective;
            lastCompletedTick = simulation.getTickCounter().getTotalTicks();
            currentIndex++;
            ensureCurrentExists();
        }

        private void ensureCurrentExists() {
            while (currentIndex >= objectives.Count && ambitionFactory != null) {
                Objective ambition = ambitionFactory(ambitionsGenerated);
                if (ambition == null) {
                    return;
                }
                ambitionsGenerated++;
                objectives.Add(ambition);
            }
        }

        /** The objective being worked on, or null once every one is done. */
        public Objective getCurrent() {
            return currentIndex < objectives.Count ? objectives[currentIndex] : null;
        }

        /** Progress as of the last evaluation; the HUD reads this, not the world. */
        public ObjectiveProgress getCurrentProgress() {
            return currentProgress;
        }

        /** Every objective so far, completed ones first, in order. */
        public IReadOnlyList<Objective> getObjectives() {
            return objectives;
        }

        public int getCompletedCount() {
            return currentIndex;
        }

        public Objective getLastCompleted() {
            return lastCompleted;
        }

        /** The total tick count when the last objective completed, or -1. */
        public int getLastCompletedTick() {
            return lastCompletedTick;
        }

        public bool isOnAmbitions() {
            Objective current = getCurrent();
            return current != null && current.isAmbition();
        }
    }
}
