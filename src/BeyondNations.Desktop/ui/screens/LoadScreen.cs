using System.Collections.Generic;
using System.Numerics;

namespace beyondnations.desktop.ui.screens {

    /**
    * The saved worlds, newest first (docs/SAVES.md): one button per world
    * naming its day, the player's nation and when it was saved. A save that
    * cannot be loaded -- damaged with no good backup, or made by a newer
    * version -- is listed with the reason instead of a button, so it is
    * neither hidden nor clickable. Escape or Back returns to the main menu.
    */
    public class LoadScreen {
        public const string Title = "Load";
        public const int MaxRows = 8;

        private string selected;
        // the slot whose Delete was pressed once; a second press deletes it
        private string armedForDelete;

        /** The slot the player picked, once, after draw returned LoadSlot. */
        public string consumeSelection() {
            string slot = selected;
            selected = null;
            return slot;
        }

        private static readonly Vector4 UnloadableRow = new Vector4(0.12f, 0.12f, 0.2f, 0.75f);

        /** Why a save is not a button, in few enough words to fit its row. The full reason is in the log. */
        public static string shortReason(SaveSlotInfo info) {
            string problem = info.Problem ?? "";
            if (problem.Contains("newer version")) {
                return "Made by a newer version of the game";
            }
            if (problem.Contains("missing")) {
                return "Save missing";
            }
            return "Damaged, and no backup to fall back on";
        }

        public UiAction draw(float width, float height, SaveSession saves) {
            float centreY = height / 2f;
            float rowHeight = UiScale.px(32f);
            float gap = UiScale.px(6f);
            float rowWidth = System.MathF.Min(width - UiScale.px(32f), UiScale.px(460f));
            float x = (width - rowWidth) / 2f;
            float y = centreY / 2f + height / 15f;
            UiAction action = UiAction.None;

            Overlay.beginFullScreen("##load-screen", width, height);
            Overlay.textCentered(Title, height / 15f, centreY / 2f, width);

            List<SaveSlotInfo> slots = saves.list();
            if (slots.Count == 0) {
                Overlay.textCentered("No saved worlds yet.", Overlay.BaseFontSize, y + rowHeight / 2f, width);
            }
            float bottom = height - rowHeight * 2.5f;
            int shown = 0;
            for (int i = 0; i < slots.Count && shown < MaxRows; i++) {
                if (y + rowHeight > bottom) {
                    break;
                }
                SaveSlotInfo info = slots[i];
                float deleteWidth = UiScale.px(64f);
                float mainWidth = rowWidth - deleteWidth - gap;
                if (info.isLoadable()) {
                    if (Overlay.buttonAt(SaveSession.describe(info) + "##" + info.Slot, x, y, mainWidth, rowHeight)) {
                        selected = info.Slot;
                        action = UiAction.LoadSlot;
                        armedForDelete = null;
                    }
                } else {
                    // a dimmed row where the button would be, saying why it cannot be loaded
                    Overlay.fillLocalRect(new Vector2(x, y), new Vector2(mainWidth, rowHeight), UnloadableRow);
                    Overlay.textCenteredColoured(PauseScreen.ErrorColour, shortReason(info), Overlay.BaseFontSize,
                        y + rowHeight / 2f, x * 2f + mainWidth);
                }
                // Deleting takes two presses: "Delete", then "Sure?".
                bool armed = armedForDelete == info.Slot;
                if (Overlay.buttonAt((armed ? "Sure?" : "Delete") + "##delete-" + info.Slot, x + mainWidth + gap, y, deleteWidth, rowHeight)) {
                    if (armed) {
                        saves.delete(info.Slot);
                        armedForDelete = null;
                        break;
                    }
                    armedForDelete = info.Slot;
                }
                y += rowHeight + gap;
                shown++;
            }
            if (shown < slots.Count) {
                Overlay.textCentered((slots.Count - shown) + " older saves not shown.", Overlay.BaseFontSize, y + rowHeight / 2f, width);
            }
            if (saves.getFeedback() != null && saves.isFeedbackError()) {
                Overlay.textCenteredColoured(PauseScreen.MenuErrorColour, saves.getFeedback(), Overlay.BaseFontSize, height - rowHeight * 2.2f, width);
            }

            float buttonWidth = UiScale.px(100f);
            if (Overlay.buttonAt("Back", (width - buttonWidth) / 2f, height - rowHeight * 1.5f, buttonWidth, UiScale.px(24f))) {
                action = UiAction.Back;
            }
            Overlay.end();
            return action;
        }
    }
}
