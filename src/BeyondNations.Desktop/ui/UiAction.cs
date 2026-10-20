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
        SaveGame,
        // sharing (docs/SHARING.md): build the world the New World screen
        // names, open the Share screen from the pause screen and go back to
        // it, and the Share screen's three buttons
        BeginWorld,
        RollSeed,
        OpenShare,
        BackToPause,
        ShareLink,
        ExportReplay,
        ExportChronicle,
        // the end-of-run screen: save this world and start a new one
        NewWorld
    }
}
