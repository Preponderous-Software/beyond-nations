namespace beyondnations.desktop.ui {

    /**
    * What a screen is asking for after a frame of drawing.
    *
    * The Unity screens returned a ScreenType, or called Application.Quit()
    * directly from inside OnGUI. Returning an intent instead keeps the screens
    * free of anything host-owned: only the host can create a world or close a
    * window, so only the host does.
    */
    public enum UiAction {
        None,
        StartGame,
        OpenConfig,
        Back,
        Quit
    }
}
