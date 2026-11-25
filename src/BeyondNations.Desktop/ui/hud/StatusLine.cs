namespace beyondnations.desktop.ui.hud {

    /**
    * The status line as the HUD shows it: the player's feedback (what a
    * command did, or why it could not) for a few seconds of real time, then
    * gone, even while the world stands still under a panel; and a hint (a
    * standing suggestion) only while no feedback shows, so a hint never
    * overwrites feedback (Roam's status.py setHint).
    *
    * Pure: the clock is passed in (TestStatusLine).
    */
    public class StatusLine {
        public const double FeedbackSeconds = 4.0;
        /** A refusal stays a little longer: it says why something did nothing. */
        public const double RefusalSeconds = 6.0;

        private string feedback;
        private bool refusal;
        private double feedbackAt = double.NegativeInfinity;
        private string hint;

        /** The line the game says now (Player.getStatus()); a new or repeated message restarts its time. */
        public void observe(string line, bool isRefusal, double now) {
            if (string.IsNullOrEmpty(line)) {
                return;
            }
            if (line != feedback) {
                feedback = line;
                refusal = isRefusal;
                feedbackAt = now;
            }
        }

        /** A standing hint (null clears it); shown only while no feedback is. */
        public void setHint(string text) {
            hint = text;
        }

        public bool isFeedbackShowing(double now) {
            return feedback != null && now - feedbackAt < (refusal ? RefusalSeconds : FeedbackSeconds);
        }

        /** What the line shows now: live feedback, else the hint, else nothing (null). */
        public string visible(double now) {
            if (isFeedbackShowing(now)) {
                return feedback;
            }
            return string.IsNullOrEmpty(hint) ? null : hint;
        }

        /** Whether what shows now is a refusal. */
        public bool isRefusal(double now) {
            return isFeedbackShowing(now) && refusal;
        }

        /** Seconds since the feedback showing now was said, for its flash. */
        public double age(double now) {
            return now - feedbackAt;
        }
    }
}
