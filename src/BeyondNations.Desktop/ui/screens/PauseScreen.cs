namespace beyondnations.desktop.ui.screens {

    /**
    * The pause screen, ported from Assets/Scripts/screens/PauseScreen.cs.
    *
    * "PAUSED" large, the hint about Escape small below it, and an EXIT button.
    * Escape itself is read by the host, since leaving this screen is a
    * transition and quitting closes a window.
    */
    public class PauseScreen {
        public const string Title = "PAUSED";
        public const string ResumeHint = "(Press ESCAPE to resume)";

        private const float ButtonWidth = 100f;
        private const float ButtonHeight = 50f;

        public UiAction draw(float width, float height) {
            float centreX = width / 2f;
            float centreY = height / 2f;
            float buttonX = centreX - ButtonWidth / 2f;
            float buttonY = centreY + ButtonHeight / 2f;

            UiAction action = UiAction.None;

            Overlay.beginFullScreen("##pause-screen", width, height);
            Overlay.textCentered(Title, height / 10f, centreY / 2f, width);
            Overlay.textCentered(ResumeHint, height / 30f, buttonY + 50f, width);

            if (Overlay.buttonAt("EXIT", buttonX, buttonY + ButtonHeight * 4f, ButtonWidth, ButtonHeight)) {
                action = UiAction.Quit;
            }
            Overlay.end();

            return action;
        }
    }
}
