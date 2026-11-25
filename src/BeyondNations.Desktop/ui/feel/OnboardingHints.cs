using System;
using System.Numerics;
using beyondnations.desktop.input;

namespace beyondnations.desktop.ui.feel {

    /**
    * The first five minutes: one short tip at a time for a new world, in
    * order -- walk, chop a tree, gather stone, found a settlement, then the
    * goals panel. Each tip goes away by itself the moment the player has
    * done what it says, can be dismissed ("Got it"), and the whole flow can be
    * skipped ("No tips"). A tip that has been done or dismissed is recorded in
    * FeelSettings and never shown again, in this world or the next; one whose
    * goal is already met when its turn comes (a player who already carries
    * stone) is passed over.
    *
    * This is separate from the objectives: it reads the world and the
    * objective count, and changes neither.
    */
    public class OnboardingHints {
        public enum Step {
            Move,
            Chop,
            Stone,
            Found,
            Goals
        }

        public static readonly Step[] Steps = { Step.Move, Step.Chop, Step.Stone, Step.Found, Step.Goals };

        /** Seconds before the first tip, so the world is seen before it is explained. */
        public const float FirstDelaySeconds = 1.5f;
        /** Seconds between one tip being done and the next appearing. */
        public const float BetweenSeconds = 1.2f;
        /** How far, across the ground, counts as having walked. */
        public const float MoveDistance = 8f;

        private readonly FeelSettings settings;
        private bool active;
        private int index;
        private float wait;
        private bool baselineTaken;
        private Vector3 startPosition;
        private int startWood;
        private int startStone;
        private int startCompleted;

        public OnboardingHints(FeelSettings settings) {
            this.settings = settings;
        }

        public static string id(Step step) {
            switch (step) {
                case Step.Move: return "move";
                case Step.Chop: return "chop";
                case Step.Stone: return "stone";
                case Step.Found: return "found";
                default: return "goals";
            }
        }

        /** Starts the flow for a new world; a loaded world gets none. */
        public void begin(bool newWorld) {
            active = newWorld && settings.areHintsEnabled();
            index = 0;
            wait = FirstDelaySeconds;
            baselineTaken = false;
        }

        public void stop() {
            active = false;
        }

        public bool isActive() {
            return active;
        }

        /** The tip on screen now, or null. */
        public Step? getVisible() {
            if (!active || wait > 0f || index >= Steps.Length) {
                return null;
            }
            return Steps[index];
        }

        /** 1-based position of the visible tip, for "Tip 2 of 5". */
        public int getNumber() {
            int number = 1;
            for (int i = 0; i < index && i < Steps.Length; i++) {
                if (!(objectivesShown && duplicatesObjectives(Steps[i]))) {
                    number++;
                }
            }
            return number;
        }

        public void update(Simulation simulation, float deltaSeconds) {
            if (!active || simulation == null) {
                return;
            }
            if (!settings.areHintsEnabled()) {
                active = false;
                return;
            }
            skipFinished(simulation);
            if (index >= Steps.Length) {
                active = false;
                return;
            }
            if (wait > 0f) {
                wait -= deltaSeconds;
                if (wait > 0f) {
                    return;
                }
                wait = 0f;
            }
            if (!baselineTaken) {
                takeBaseline(simulation);
            }
            if (isDone(Steps[index], simulation)) {
                finish();
            }
        }

        /** "Got it": this tip is done with, for good. */
        public void dismiss() {
            if (!active || index >= Steps.Length) {
                return;
            }
            finish();
        }

        /** "No tips": none of them, now or in later worlds. */
        public void skipAll() {
            settings.setHintsEnabled(false);
            active = false;
        }

        private void finish() {
            settings.markHintDone(id(Steps[index]));
            index++;
            wait = BetweenSeconds;
            baselineTaken = false;
        }

        private void skipFinished(Simulation simulation) {
            while (index < Steps.Length && (settings.isHintDone(id(Steps[index])) || alreadyMet(Steps[index], simulation)
                    || (objectivesShown && duplicatesObjectives(Steps[index])))) {
                // one passed over only because the Next line says it is not marked done
                if (!settings.isHintDone(id(Steps[index])) && alreadyMet(Steps[index], simulation)) {
                    settings.markHintDone(id(Steps[index]));
                }
                index++;
                baselineTaken = false;
            }
        }

        private bool objectivesShown;

        /**
        * Whether the HUD shows the objectives as a Next line (the contextual
        * HUD does): then the tips that only repeat an objective, and so
        * contradict the Next line once it has moved on (gather stone, found
        * a settlement), are passed over.
        */
        public void setObjectivesShown(bool shown) {
            objectivesShown = shown;
        }

        /** The tips that say what an objective says: left out beside a Next line. */
        public static bool duplicatesObjectives(Step step) {
            return step == Step.Stone || step == Step.Found;
        }

        /** The tips that can show, for "Tip 2 of 3": all of them, or those the Next line does not repeat. */
        public int getTotal() {
            int total = 0;
            foreach (Step step in Steps) {
                if (!(objectivesShown && duplicatesObjectives(step))) {
                    total++;
                }
            }
            return total;
        }

        /** Met before the tip was ever shown: a settlement already standing. */
        private static bool alreadyMet(Step step, Simulation simulation) {
            return step == Step.Found && simulation.getPlayer().getHomeSettlementId() != null;
        }

        private void takeBaseline(Simulation simulation) {
            Player player = simulation.getPlayer();
            startPosition = player.getPosition();
            startWood = player.getInventory().getNumItems(ItemType.WOOD);
            startStone = player.getInventory().getNumItems(ItemType.STONE);
            startCompleted = simulation.getObjectiveTracker().getCompletedCount();
            baselineTaken = true;
        }

        private bool isDone(Step step, Simulation simulation) {
            Player player = simulation.getPlayer();
            switch (step) {
                case Step.Move:
                    Vector3 moved = player.getPosition() - startPosition;
                    return moved.X * moved.X + moved.Z * moved.Z >= MoveDistance * MoveDistance;
                case Step.Chop:
                    return player.getInventory().getNumItems(ItemType.WOOD) > startWood;
                case Step.Stone:
                    return player.getInventory().getNumItems(ItemType.STONE) > startStone;
                case Step.Found:
                    return player.getHomeSettlementId() != null;
                default:
                    return simulation.getObjectiveTracker().getCompletedCount() > startCompleted;
            }
        }

        /** The tip's text, worded for a keyboard or for the on-screen controls. */
        public static string text(Step step, bool touch) {
            string interact = KeyNames.describe(KeyBindings.Interact);
            switch (step) {
                case Step.Move:
                    return touch
                        ? "Walk with the stick on the left. Drag across the world to look around."
                        : "Walk with W A S D. Hold the right mouse button and move the mouse to look around.";
                case Step.Chop:
                    return touch
                        ? "Walk up to a tree and tap Use to chop it for wood."
                        : "Walk up to a tree and press " + interact + " to chop it for wood.";
                case Step.Stone:
                    return touch
                        ? "Find a grey rock and tap Use to gather stone."
                        : "Find a grey rock and press " + interact + " to gather stone.";
                case Step.Found:
                    return touch
                        ? "Tap Create Nation in the bar, gather 50 wood, then tap Found Settlement."
                        : "Press " + KeyNames.describe(KeyBindings.CreateNewNation) + " to start a nation, gather 50 wood, then press "
                          + KeyNames.describe(KeyBindings.FoundSettlement) + " to found a settlement.";
                default:
                    return "The Next line at the top says what to do now; the Journal button there shows your goals, your era and your news. Each objective you finish earns a reward.";
            }
        }

        public static string title(Step step) {
            switch (step) {
                case Step.Move: return "Walk and look";
                case Step.Chop: return "Chop a tree";
                case Step.Stone: return "Gather stone";
                case Step.Found: return "Found a settlement";
                default: return "Your goals";
            }
        }
    }
}
