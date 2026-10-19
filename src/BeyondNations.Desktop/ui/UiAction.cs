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
        Continue,
        StartGame,
        OpenConfig,
        Back,
        MainMenu,
        OpenControls,
        CloseControls,
        Quit,
        // close an overlay screen (crafting) and return to the world
        ResumeWorld,
        // saves (docs/SAVES.md): load the latest, open the list, load the
        // one picked from it, save now from the pause screen
        ContinueGame,
        OpenLoad,
        LoadSlot,
        SaveGame
    }
}
