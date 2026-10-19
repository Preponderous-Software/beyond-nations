namespace beyondnations.desktop.ui.screens {

    /**
    * The main menu, ported from Assets/Scripts/screens/MainMenuScreen.cs.
    *
    * Start, then Config, Controls and Exit, as before. Once a world has been
    * saved, Continue (the latest save) and Load (every save) sit between Start
    * and Config, with the latest save named above the column. Start stays the
    * top button whatever else is shown, so it never moves under a player's
    * hand -- or the browser smoke test's click. Start no longer returns
    * ScreenType.WORLD directly, because the world has to be created before it
    * can be shown and only the host can do that.
    */
    public class MainMenuScreen {
        public const string Title = "Beyond Nations";
        public const string ContinueLabel = "Continue";
        public const string LoadLabel = "Load";

        public static readonly string[] ButtonLabels = { "Start", "Config", "Controls", "Exit" };
        private static readonly UiAction[] ButtonActions = { UiAction.StartGame, UiAction.OpenConfig, UiAction.OpenControls, UiAction.Quit };

        public static readonly string[] ButtonLabelsWithSaves = { "Start", ContinueLabel, LoadLabel, "Config", "Controls", "Exit" };
        private static readonly UiAction[] ButtonActionsWithSaves = {
            UiAction.StartGame, UiAction.ContinueGame, UiAction.OpenLoad, UiAction.OpenConfig, UiAction.OpenControls, UiAction.Quit
        };

        public UiAction draw(float width, float height) {
            return draw(width, height, null, null);
        }

        /**
        * latest is the newest loadable save, or null when there is none;
        * feedback is a message to show (a load that failed), or null.
        */
        public UiAction draw(float width, float height, SaveSlotInfo latest, string feedback) {
            float centreY = height / 2f;
            bool hasSaves = latest != null;
            string[] labels = hasSaves ? ButtonLabelsWithSaves : ButtonLabels;
            UiAction[] actions = hasSaves ? ButtonActionsWithSaves : ButtonActions;

            Overlay.beginFullScreen("##main-menu-screen", width, height);
            Overlay.textCentered(Title, height / 15f, centreY / 2f, width);
            if (feedback != null) {
                Overlay.textCenteredColoured(PauseScreen.MenuErrorColour, feedback, Overlay.BaseFontSize, centreY - Overlay.lineHeight() * 1.5f, width);
            } else if (hasSaves) {
                Overlay.textCentered("Last played: " + SaveSession.describe(latest), Overlay.BaseFontSize, centreY - Overlay.lineHeight() * 0.5f, width);
            }
            // Start keeps the top edge it always had, 25 px below the centre,
            // so it stays where players (and the browser smoke test) click.
            int clicked = Overlay.menuButtons(labels, width, centreY + UiScale.px(25f), height - UiScale.px(16f));
            Overlay.end();

            return clicked < 0 ? UiAction.None : actions[clicked];
        }
    }
}
