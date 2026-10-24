namespace beyondnations.desktop.ui {

    /**
    * Two presses to do something that cannot be taken back: the first arms
    * it, the second -- of the same thing, within a few seconds -- does it.
    *
    * Used by the command bar for raids, denunciations, leaving the nation and
    * building, and by the build key (K), so a click that lands on a button
    * that has just appeared, or a key pressed to see what it does, costs
    * nothing. Pressing anything else, or waiting, disarms it.
    *
    * The gate also owns the "click again" prompt it put on the status line
    * (prompt), so that when the window lapses (update, called every frame
    * the gate's owner is drawn or polled) the prompt is taken back instead
    * of still asking for a click that would now only re-arm it (0.6.1/0.6.2
    * playtests: "Click War again..." stayed up after the button reverted).
    * It is taken back only while it is still the whole status line, so a
    * newer message is never wiped.
    *
    * Time is passed in (seconds, any origin) so the rule can be tested
    * without a clock.
    */
    public class ConfirmGate {
        // 8 rather than 4: a second click that came 5 s later re-armed instead of
        // confirming, for players who read the prompt first (playtest 0.6.0, #345)
        public const double DefaultWindowSeconds = 8.0;

        /** What replaces a prompt whose window lapsed: says plainly that nothing happened. */
        public const string LapsedMessage = "Not confirmed in time, so nothing was done.";

        private readonly double windowSeconds;
        private string armedKey;
        private double armedAt;
        // the prompt shown for the armed press, taken back when the window lapses
        private string shownPrompt;

        public ConfirmGate() : this(DefaultWindowSeconds) {
        }

        public ConfirmGate(double windowSeconds) {
            this.windowSeconds = windowSeconds;
        }

        /**
        * A press of key at now: true when it was armed by the press before
        * and should run now (the gate disarms), false when this press only
        * armed it.
        */
        public bool press(string key, double now) {
            if (isArmed(key, now)) {
                armedKey = null;
                // the action runs and says what it did; its prompt is spent
                shownPrompt = null;
                return true;
            }
            armedKey = key;
            armedAt = now;
            return false;
        }

        public bool isArmed(string key, double now) {
            return armedKey != null && armedKey == key && now - armedAt <= windowSeconds && now >= armedAt;
        }

        public void disarm() {
            armedKey = null;
        }

        /**
        * Shows the prompt for the press that just armed the gate. It is shown
        * as a prompt (Status.prompt), so asking again after a lapse never
        * reads "(x2)", which looked like "click twice" (0.6.2 playtest).
        */
        public void prompt(Status status, string text) {
            shownPrompt = text;
            status.prompt(text);
        }

        /**
        * Takes the prompt back once the gate is no longer armed for it --
        * the window lapsed, or the owner disarmed it -- replacing it with
        * LapsedMessage if it is still what the status line says.
        */
        public void update(double now, Status status) {
            if (shownPrompt == null) {
                return;
            }
            if (armedKey != null && isArmed(armedKey, now)) {
                return;
            }
            armedKey = null;
            status.withdraw(shownPrompt, LapsedMessage);
            shownPrompt = null;
        }
    }
}
