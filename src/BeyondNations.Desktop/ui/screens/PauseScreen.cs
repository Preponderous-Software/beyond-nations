using System.Collections.Generic;
using System.Numerics;
using beyondnations.desktop.ui.feel;

namespace beyondnations.desktop.ui.screens {

    /**
    * The pause screen, ported from Assets/Scripts/screens/PauseScreen.cs.
    *
    * "PAUSED" large, the hint about Escape small below it, the world's seed,
    * then Resume, Save, Share (docs/SHARING.md), Controls, Main Menu and
    * Quit. Save writes the world to its slot at once
    * and the line under the buttons says what happened -- "Saved: Day 3 -
    * Xland, 14:22." or why it could not -- so a player never has to wonder.
    * Main Menu and Quit save first (docs/SAVES.md), and the line says that
    * too until there is a save to report. Escape itself is read by the host,
    * since leaving this screen is a transition and quitting closes a window.
    */
    public class PauseScreen {
        public const string Title = "PAUSED";
        public const string ResumeHint = "(Press {Pause} to resume)";
        public const string ResumeLabel = "Resume";
        public const string SaveLabel = "Save";
        public const string MainMenuLabel = "Main Menu";
        public const string MainMenuWarning = "Main Menu ends this world; it is not saved.";
        public const string SavesOnLeaveHint = "Main Menu and Quit save this world first.";
        public const string QuitLabel = "Quit";

        public const string ControlsLabel = "Controls";
        // the world's seed, its link and the exports (docs/SHARING.md)
        public const string ShareLabel = "Share";

        public static readonly Vector4 ErrorColour = new Vector4(1f, 0.45f, 0.4f, 1f);
        /** The same, dark enough to read on the menus' light sky. */
        public static readonly Vector4 MenuErrorColour = new Vector4(0.6f, 0.05f, 0.05f, 1f);
        private static readonly Vector4 SavedColour = new Vector4(0.6f, 1f, 0.6f, 1f);

        private static readonly string[] ButtonLabels = { ResumeLabel, ShareLabel, ControlsLabel, MainMenuLabel, QuitLabel };
        private static readonly UiAction[] ButtonActions = { UiAction.Back, UiAction.OpenShare, UiAction.OpenControls, UiAction.MainMenu, UiAction.Quit };

        private static readonly string[] ButtonLabelsWithSave = { ResumeLabel, SaveLabel, ShareLabel, ControlsLabel, MainMenuLabel, QuitLabel };
        private static readonly UiAction[] ButtonActionsWithSave = { UiAction.Back, UiAction.SaveGame, UiAction.OpenShare, UiAction.OpenControls, UiAction.MainMenu, UiAction.Quit };

        public UiAction draw(float width, float height) {
            return draw(width, height, null);
        }

        public const string SoundOnLabel = "Sound: On";
        public const string SoundOffLabel = "Sound: Off";

        /** saves is null when saving is off for this run (--no-saves); the screen is then as it was before saves. */
        public UiAction draw(float width, float height, SaveSession saves) {
            return draw(width, height, saves, null);
        }

        /** The label of the mute toggle in the top-right corner. */
        public static string soundLabel(FeelSettings feel) {
            return feel.isMuted() ? SoundOffLabel : SoundOnLabel;
        }

        /**
        * feel adds a mute toggle in the top-right corner, out of the column of
        * buttons so their places do not move; the volume itself is on the
        * config screen. seedLine, "World seed 42", is shown just above the
        * buttons, so the seed can be read off without opening Share
        * (docs/SHARING.md); null leaves it out.
        */
        public UiAction draw(float width, float height, SaveSession saves, FeelSettings feel, string seedLine = null) {
            float centreY = height / 2f;
            bool canSave = saves != null && saves.isEnabled();
            string[] labels = canSave ? ButtonLabelsWithSave : ButtonLabels;
            UiAction[] actions = canSave ? ButtonActionsWithSave : ButtonActions;
            // The browser offers the touch-controls switch here too, before
            // Quit, so it can be changed mid-game (TouchLayout; the desktop
            // has no such switch and its column is unchanged).
            int touchIndex = -1;
            if (TouchLayout.isAvailable()) {
                touchIndex = labels.Length - 1;
                List<string> withTouch = new List<string>(labels);
                withTouch.Insert(touchIndex, TouchLayout.describe());
                List<UiAction> touchActions = new List<UiAction>(actions);
                touchActions.Insert(touchIndex, UiAction.None);
                labels = withTouch.ToArray();
                actions = touchActions.ToArray();
            }

            Overlay.beginFullScreen("##pause-screen", width, height);
            Overlay.dimBackground(width, height);
            float buttonsTop = centreY;
            if (TouchLayout.isActive()) {
                // A phone on its side is under 400 pixels tall: the title
                // goes to the top and the fingertip-sized column follows it,
                // rather than starting halfway down and being squeezed.
                Overlay.textCentered(Title, Overlay.BaseFontSize * 2f, UiScale.px(8f) + Overlay.BaseFontSize, width);
                buttonsTop = UiScale.px(16f) + Overlay.BaseFontSize * 2f;
                // the seed in the top-left corner, clear of the column (the
                // mute toggle has the top-right one)
                if (seedLine != null) {
                    Overlay.textAt(seedLine, UiScale.px(12f), UiScale.px(12f));
                }
            } else {
                Overlay.textCentered(Title, height / 10f, centreY / 2f, width);
                Overlay.textCentered(ObjectiveKeyNames.resolve(ResumeHint), height / 30f, centreY / 2f + height / 12f, width);
                if (seedLine != null) {
                    Overlay.textCentered(seedLine, Overlay.BaseFontSize, centreY - Overlay.lineHeight() * 0.75f, width);
                }
            }

            // The line about saving sits under the buttons, so the column
            // stops a couple of lines short of the bottom to leave it room on
            // a short window.
            float warningSpace = Overlay.lineHeight() * 2f;
            float buttonsBottom;
            int clicked = Overlay.menuButtons(labels, width, buttonsTop, height - warningSpace, out buttonsBottom);
            float warningY = System.MathF.Min(buttonsBottom + warningSpace * 0.5f, height - warningSpace * 0.5f);
            if (!canSave) {
                Overlay.textCentered(MainMenuWarning, Overlay.BaseFontSize, warningY, width);
            } else if (saves.getFeedback() != null) {
                Overlay.textCenteredColoured(saves.isFeedbackError() ? ErrorColour : SavedColour, saves.getFeedback(), Overlay.BaseFontSize, warningY, width);
            } else {
                Overlay.textCentered(SavesOnLeaveHint, Overlay.BaseFontSize, warningY, width);
            }
            // only where there is sound to mute (the browser build)
            if (feel != null && feel.isSoundAvailable()) {
                float toggleWidth = UiScale.px(110f);
                float toggleHeight = System.MathF.Max(Overlay.frameHeight(), UiScale.px(32f));
                float margin = UiScale.px(10f);
                if (Overlay.buttonAt(soundLabel(feel), width - toggleWidth - margin, margin, toggleWidth, toggleHeight)) {
                    feel.setMuted(!feel.isMuted());
                }
            }
            Overlay.end();

            if (clicked >= 0 && clicked == touchIndex) {
                TouchLayout.setMode(TouchLayout.next(TouchLayout.getMode()));
                return UiAction.None;
            }
            return clicked < 0 ? UiAction.None : actions[clicked];
        }
    }
}
