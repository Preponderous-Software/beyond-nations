using System.Numerics;

namespace beyondnations.desktop.ui.screens {

    /**
    * Share, from the pause screen (docs/SHARING.md): this world's seed and
    * the link that opens it in the browser, then
    *
    *   Copy link / Share link   the link, to the clipboard or the phone's share sheet
    *   Export replay (USD)      this run as a usd-replay/1 replay (ReplayUsdaWriter)
    *   Export history (text)    the chronicle as "The History of <nation>" (ChronicleExport)
    *   Back                     to the pause screen, as Escape does
    *
    * The line under the buttons says how the last one went: "Link copied.",
    * or where the file was saved. Laid over the world, like the pause screen.
    */
    public class ShareScreen {
        public const string Title = "Share this world";
        public const string ExportReplayLabel = "Export replay (USD)";
        public const string ExportHistoryLabel = "Export history (text)";
        public const string BackLabel = "Back";

        private static readonly Vector4 DoneColour = new Vector4(0.6f, 1f, 0.6f, 1f);
        private static readonly Vector4 LinkColour = new Vector4(1f, 1f, 1f, 1f);

        public UiAction draw(float width, float height, Simulation simulation, ShareSession share) {
            float centreY = height / 2f;
            string[] labels = { share.getPlatform().getShareLinkLabel(), ExportReplayLabel, ExportHistoryLabel, BackLabel };
            UiAction[] actions = { UiAction.ShareLink, UiAction.ExportReplay, UiAction.ExportChronicle, UiAction.BackToPause };

            Overlay.beginFullScreen("##share-screen", width, height);
            Overlay.dimBackground(width, height);
            // no wider than a phone held upright
            Overlay.textCentered(Title, System.MathF.Min(height / 15f, width / 11f), centreY / 2f, width);
            int seed = ShareSession.seedOf(simulation);
            Overlay.textCentered("World seed " + WorldSeed.format(seed), Overlay.BaseFontSize * 1.25f,
                centreY - Overlay.lineHeight() * 2.75f, width);
            // Two lines on a phone held upright, split before "?seed=".
            drawWrapped(SeedLink.build(seed), LinkColour, width, centreY - Overlay.lineHeight() * 1.5f);

            float feedbackSpace = Overlay.lineHeight() * 3f;
            float buttonsBottom;
            int clicked = Overlay.menuButtons(labels, width, centreY + UiScale.px(10f), height - feedbackSpace, out buttonsBottom);
            string feedback = share.getFeedback();
            if (feedback != null) {
                float y = System.MathF.Min(buttonsBottom + Overlay.lineHeight(), height - feedbackSpace * 0.6f);
                drawWrapped(feedback, share.isFeedbackError() ? PauseScreen.ErrorColour : DoneColour, width, y);
            }
            Overlay.end();

            return clicked < 0 ? UiAction.None : actions[clicked];
        }

        /** A long path or link is split onto two centred lines when it does not fit on one. */
        private static void drawWrapped(string text, Vector4 colour, float width, float y) {
            float maxWidth = width - UiScale.px(32f);
            if (ImGuiNET.ImGui.CalcTextSize(text).X <= maxWidth) {
                Overlay.textCenteredColoured(colour, text, Overlay.BaseFontSize, y, width);
                return;
            }
            int split = text.IndexOf('?');
            if (split <= 0) {
                split = text.LastIndexOfAny(new[] { '/', '\\', ' ' }, text.Length / 2 + text.Length / 4);
            }
            if (split <= 0) {
                split = text.Length / 2;
            }
            // a split before "?" keeps it with the seed; after a slash or space, with the first line
            int cut = text[split] == '?' ? split : split + 1;
            Overlay.textCenteredColoured(colour, text.Substring(0, cut), Overlay.BaseFontSize, y, width);
            Overlay.textCenteredColoured(colour, text.Substring(cut), Overlay.BaseFontSize, y + Overlay.lineHeight(), width);
        }
    }
}
