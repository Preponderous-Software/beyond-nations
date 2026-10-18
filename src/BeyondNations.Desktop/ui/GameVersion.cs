namespace beyondnations.desktop.ui {

    /**
    * The version string the game shows in the bottom-left corner.
    *
    * It used to be a field on the BeyondNations MonoBehaviour, which is gone;
    * the label it drew is required to survive the port (#221), so the string
    * lives here and UiOverlay draws it on every screen.
    */
    public static class GameVersion {
        public const string Version = "0.3.0-alpha";

        /**
        * The label as it is drawn, "v" then the version, exactly as the Unity
        * build wrote it.
        */
        public static string getLabel() {
            return "v" + Version;
        }
    }
}
