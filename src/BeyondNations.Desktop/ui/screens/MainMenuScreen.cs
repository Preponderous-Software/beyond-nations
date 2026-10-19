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

        public static readonly string[] ButtonLabels = { "Start", "Config", "Controls", "Exit" };
        private static readonly UiAction[] ButtonActions = { UiAction.StartGame, UiAction.OpenConfig, UiAction.OpenControls, UiAction.Quit };

        public UiAction draw(float width, float height) {
            float centreY = height / 2f;

            Overlay.beginFullScreen("##main-menu-screen", width, height);
            Overlay.textCentered(Title, height / 15f, centreY / 2f, width);
            int clicked = Overlay.menuButtons(ButtonLabels, width, centreY, height - UiScale.px(16f));
            Overlay.end();

            return clicked < 0 ? UiAction.None : ButtonActions[clicked];
        }
    }
}
