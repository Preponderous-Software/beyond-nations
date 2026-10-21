using ImGuiNET;

namespace beyondnations.desktop.ui.screens {

    /**
    * The title screen, ported from Assets/Scripts/screens/TitleScreen.cs.
    *
    * One line, bold in Unity and plain here since ImGui's built-in font has no
    * bold face, centred half a title-height above the middle of the screen at a
    * tenth of the screen height. Any key moves on, which the host handles; the
    * Unity screen never said so, and a playtest sat on it waiting, so a hint
    * under the title does. A click or tap moves on too: the browser build runs
    * on phones, which have no key to press.
    */
    public class TitleScreen {
        public const string Title = "Beyond Nations";
        public const string ContinueHint = "Press any key or click to continue";
        public const string TouchContinueHint = "Tap to continue";

        /** The hint for the controls in use: a phone has no key to press (TouchLayout). */
        public static string continueHint() {
            return TouchLayout.isActive() ? TouchContinueHint : ContinueHint;
        }

        public UiAction draw(float width, float height) {
            Overlay.beginFullScreen("##title-screen", width, height);
            Overlay.textCentered(Title, height / 10f, height / 2f - 50f, width);
            Overlay.textCentered(continueHint(), height / 30f, height / 2f + height / 10f, width);
            Overlay.end();
            return ImGui.IsMouseClicked(ImGuiMouseButton.Left) ? UiAction.Continue : UiAction.None;
        }
    }
}
