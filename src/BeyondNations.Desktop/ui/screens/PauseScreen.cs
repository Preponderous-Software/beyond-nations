namespace beyondnations.desktop.ui.screens {

    /**
    * The pause screen, ported from Assets/Scripts/screens/PauseScreen.cs.
    *
    * "PAUSED" large, the hint about Escape small below it, then Resume, Main
    * Menu and Quit. The Unity screen offered only EXIT, so the one way out of
    * a world was to close the game; Resume and Main Menu are the exits a player
    * expects from a pause menu. The game has no saves yet, so Main Menu says
    * that it ends the world rather than letting a player find out.
    * Escape itself is read by the host, since leaving this screen is a
    * transition and quitting closes a window.
    */
    public class PauseScreen {
        public const string Title = "PAUSED";
        public const string ResumeHint = "(Press ESCAPE to resume)";
        public const string ResumeLabel = "Resume";
        public const string MainMenuLabel = "Main Menu";
        public const string MainMenuWarning = "Main Menu ends this world; it is not saved.";
        public const string QuitLabel = "Quit";

        private const float ButtonWidth = 160f;
        private const float ButtonHeight = 40f;
        private const float ButtonGap = 12f;

        public UiAction draw(float width, float height) {
            float centreX = width / 2f;
            float centreY = height / 2f;
            float buttonX = centreX - ButtonWidth / 2f;
            float buttonY = centreY;

            UiAction action = UiAction.None;

            Overlay.beginFullScreen("##pause-screen", width, height);
            Overlay.dimBackground(width, height);
            Overlay.textCentered(Title, height / 10f, centreY / 2f, width);
            Overlay.textCentered(ResumeHint, height / 30f, centreY / 2f + height / 12f, width);

            if (Overlay.buttonAt(ResumeLabel, buttonX, buttonY, ButtonWidth, ButtonHeight)) {
                action = UiAction.Back;
            }
            buttonY += ButtonHeight + ButtonGap;
            if (Overlay.buttonAt(MainMenuLabel, buttonX, buttonY, ButtonWidth, ButtonHeight)) {
                action = UiAction.MainMenu;
            }
            buttonY += ButtonHeight + ButtonGap;
            if (Overlay.buttonAt(QuitLabel, buttonX, buttonY, ButtonWidth, ButtonHeight)) {
                action = UiAction.Quit;
            }
            buttonY += ButtonHeight + ButtonGap;
            Overlay.textCentered(MainMenuWarning, Overlay.BaseFontSize, buttonY + ButtonGap, width);
            Overlay.end();

            return action;
        }
    }
}
