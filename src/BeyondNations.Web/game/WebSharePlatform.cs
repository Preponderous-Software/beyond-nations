using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using beyondnations.desktop.ui;
using beyondnations.web.host;

namespace beyondnations.web {

    /**
    * Sharing in the browser (docs/SHARING.md), through the page
    * (wwwroot/webhost/host.js):
    *
    *   the link   navigator.share on a phone or tablet that has it (the system
    *              share sheet), the clipboard otherwise, and failing both the
    *              link is shown to be copied by hand
    *   exports    downloaded, as the screenshot key does with a PNG
    *   the seed   asked for with window.prompt, because a canvas cannot open
    *              a phone's keyboard; it works the same with a real keyboard
    */
    public partial class WebSharePlatform : ISharePlatform {

        [JSImport("page.canShare", WebPage.Module)]
        internal static partial bool canShare();

        /** Resolves to "shared", "copied", "cancelled", or "failed: <why>". */
        [JSImport("page.shareLink", WebPage.Module)]
        internal static partial Task<string> shareLinkAsync(string url, string title);

        /** Downloads content as fileName; "" on success, otherwise why not. */
        [JSImport("page.downloadText", WebPage.Module)]
        internal static partial string downloadText(string fileName, string mimeType, string content);

        /** window.prompt: the text, or null when cancelled. */
        [JSImport("page.promptText", WebPage.Module)]
        internal static partial string promptTextNative(string message, string current);

        public string getShareLinkLabel() {
            return canShare() ? "Share link" : "Copy link";
        }

        public void shareLink(string url, string title, Action<string, bool> done) {
            share(url, title, done);
        }

        private static async void share(string url, string title, Action<string, bool> done) {
            string outcome;
            try {
                outcome = await shareLinkAsync(url, title);
            } catch (Exception exception) {
                outcome = "failed: " + exception.Message;
            }
            done(describe(outcome, url), outcome == null || outcome.StartsWith("failed"));
        }

        /** What the Share screen says after the page tried to share url. */
        public static string describe(string outcome, string url) {
            switch (outcome) {
                case "shared": return "Link shared.";
                case "copied": return "Link copied: " + url;
                case "cancelled": return "Not shared. The link is " + url;
                default: return "Could not copy the link; it is " + url;
            }
        }

        public string saveFile(string fileName, string mimeType, string content) {
            string problem = downloadText(fileName, mimeType, content);
            if (!string.IsNullOrEmpty(problem)) {
                throw new InvalidOperationException(problem);
            }
            return "Downloaded " + fileName + ".";
        }

        public bool promptsForText() {
            return true;
        }

        public string promptText(string message, string current) {
            return promptTextNative(message, current ?? "");
        }
    }
}
