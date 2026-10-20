using System;

namespace beyondnations.desktop.ui {

    /**
    * What sharing needs from the platform it runs on (docs/SHARING.md). The
    * desktop copies to the system clipboard and writes files under
    * AppDataPaths.getExportsDirectory(); the browser uses the phone's share
    * sheet or the clipboard, and downloads files.
    */
    public interface ISharePlatform {
        /** The Share screen's label for handing over the link: "Copy link", or "Share link" with a share sheet. */
        string getShareLinkLabel();

        /**
        * Hands the link to the player. done is called, now or later, with
        * what to tell them, "Link copied.", and whether it is an error.
        */
        void shareLink(string url, string title, Action<string, bool> done);

        /**
        * Keeps content as a file named fileName. Returns what to tell the
        * player, "Saved to /home/...", or throws when it could not.
        */
        string saveFile(string fileName, string mimeType, string content);

        /**
        * True when typing into the game's own text field cannot work (a
        * phone's keyboard does not open for a canvas), so the New World
        * screen asks through promptText instead.
        */
        bool promptsForText();

        /** Asks for a line of text; null when the player cancels. */
        string promptText(string message, string current);
    }
}
