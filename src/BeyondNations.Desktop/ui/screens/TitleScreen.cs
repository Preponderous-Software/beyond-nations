namespace beyondnations.desktop.ui.screens {

    /**
    * The title screen, ported from Assets/Scripts/screens/TitleScreen.cs.
    *
    * One line, bold in Unity and plain here since ImGui's built-in font has no
    * bold face, centred half a title-height above the middle of the screen at a
    * tenth of the screen height. Any key moves on, which the host handles.
    */
    public class TitleScreen {
        public const string Title = "Beyond Nations";

        public UiAction draw(float width, float height) {
            Overlay.beginFullScreen("##title-screen", width, height);
            Overlay.textCentered(Title, height / 10f, height / 2f - 50f, width);
            Overlay.end();
            return UiAction.None;
        }
    }
}
