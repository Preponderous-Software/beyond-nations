namespace beyondnations.desktop.ui.screens {

    /**
    * The main menu, ported from Assets/Scripts/screens/MainMenuScreen.cs.
    *
    * Same three buttons in the same places: Start, then Config two button
    * heights below it, then Exit two further down. Start no longer returns
    * ScreenType.WORLD directly, because the world has to be created before it
    * can be shown and only the host can do that.
    */
    public class MainMenuScreen {
        public const string Title = "Beyond Nations";

        private const float ButtonWidth = 100f;
        private const float ButtonHeight = 50f;

        public UiAction draw(float width, float height) {
            float centreX = width / 2f;
            float centreY = height / 2f;
            float buttonX = centreX - ButtonWidth / 2f;
            float buttonY = centreY + ButtonHeight / 2f;

            UiAction action = UiAction.None;

            Overlay.beginFullScreen("##main-menu-screen", width, height);
            Overlay.textCentered(Title, height / 15f, centreY / 2f, width);

            if (Overlay.buttonAt("Start", buttonX, buttonY, ButtonWidth, ButtonHeight)) {
                action = UiAction.StartGame;
            }
            if (Overlay.buttonAt("Config", buttonX, buttonY + ButtonHeight * 2f, ButtonWidth, ButtonHeight)) {
                action = UiAction.OpenConfig;
            }
            if (Overlay.buttonAt("Exit", buttonX, buttonY + ButtonHeight * 4f, ButtonWidth, ButtonHeight)) {
                action = UiAction.Quit;
            }
            Overlay.end();

            return action;
        }
    }
}
