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
        // after the player dies in a nation: the heir who takes their place,
        // or, with none, the end of the line (realm/Succession)
        SUCCESSION,
        GAME_OVER,
        // the seed for a new world, between Start and the world (docs/SHARING.md)
        NEW_WORLD,
        // the world's seed, its share link and the exports, from the pause screen
        SHARE
    }
}