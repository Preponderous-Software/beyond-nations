namespace beyondnations {

    public enum ScreenType {
        TITLE,
        WORLD,
        PAUSE,
        MAIN_MENU,
        CONFIG,
        INVENTORY,
        CONTROLS,
        NATIONS,
        CRAFTING,
        // the list of saved worlds, reached from the main menu (docs/SAVES.md)
        LOAD,
        // after the player dies in a nation: the heir who takes their place (realm/Succession)
        SUCCESSION,
        // the seed for a new world, between Start and the world (docs/SHARING.md)
        NEW_WORLD,
        // the world's seed, its share link and the exports, from the pause screen
        SHARE,
        // a victory or a defeat -- including a line with no heir -- : the outcome,
        // the run's numbers and its history (EraTracker)
        END_OF_RUN
    }
}