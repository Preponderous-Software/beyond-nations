using ImGuiNET;

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

        public const string ControlsLabel = "Controls";

        private static readonly string[] ButtonLabels = { ResumeLabel, ControlsLabel, MainMenuLabel, QuitLabel };
        private static readonly UiAction[] ButtonActions = { UiAction.Back, UiAction.OpenControls, UiAction.MainMenu, UiAction.Quit };

        public UiAction draw(float width, float height) {
            float centreY = height / 2f;

            Overlay.beginFullScreen("##pause-screen", width, height);
            Overlay.dimBackground(width, height);
            Overlay.textCentered(Title, height / 10f, centreY / 2f, width);
            Overlay.textCentered(ResumeHint, height / 30f, centreY / 2f + height / 12f, width);

            // The warning about Main Menu sits under the buttons, so the
            // column stops a couple of lines short of the bottom to leave it
            // room on a short window.
            float warningSpace = ImGui.GetTextLineHeightWithSpacing() * 2f;
            int clicked = Overlay.menuButtons(ButtonLabels, width, centreY, height - warningSpace);
            float warningY = System.MathF.Min(ImGui.GetCursorPosY() + warningSpace * 0.25f, height - warningSpace * 0.5f);
            Overlay.textCentered(MainMenuWarning, Overlay.BaseFontSize, warningY, width);
            Overlay.end();

            return clicked < 0 ? UiAction.None : ButtonActions[clicked];
        }
    }
}
