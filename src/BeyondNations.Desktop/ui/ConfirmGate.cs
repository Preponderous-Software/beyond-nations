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
    * Time is passed in (seconds, any origin) so the rule can be tested
    * without a clock.
    */
    public class ConfirmGate {
        public const double DefaultWindowSeconds = 4.0;

        private readonly double windowSeconds;
        private string armedKey;
        private double armedAt;

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
    }
}
