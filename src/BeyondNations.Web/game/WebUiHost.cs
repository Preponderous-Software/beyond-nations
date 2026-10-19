using System;
using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;
using beyondnations.web.host;

namespace beyondnations.web {

    /**
    * UiHost (#221) for the browser: the same screens, switched the same way,
    * returning the same UiActions. Only the ImGui backend differs -- the
    * desktop's ImGuiController needs a Silk.NET window and GL, this one is
    * ImGuiWebBackend -- which is why this is a sibling of UiHost rather than
    * UiHost itself.
    *
    * Width and height handed to the screens are CSS pixels. The desktop hands
    * them framebuffer pixels, which is the same thing on a 1x display; on a
    * high-DPI one the browser keeps the layout at its intended size and draws
    * it with devicePixelRatio times the pixels, instead of shrinking it.
    */
    public class WebUiHost : IDisposable {
        private readonly ImGuiWebBackend backend;
        private readonly ScreenState screens;
        private readonly GameConfig gameConfig;

        private readonly TitleScreen titleScreen = new TitleScreen();
        private readonly MainMenuScreen mainMenuScreen = new MainMenuScreen();
        private readonly PauseScreen pauseScreen = new PauseScreen();
        private readonly ConfigScreen configScreen = new ConfigScreen();
        private readonly InventoryScreen inventoryScreen = new InventoryScreen();
        private readonly ControlsScreen controlsScreen = new ControlsScreen();
        private readonly NationScreen nationScreen = new NationScreen();
        private readonly CraftingScreen craftingScreen = new CraftingScreen();
        private readonly LoadScreen loadScreen = new LoadScreen();
        // saving (docs/SAVES.md): the host owns the world, so a screen asking
        // to save or load is a request the host picks up after the frame
        private readonly SaveSession saves;
        private string loadRequest;
        private bool saveRequested;
        private bool leaveRequested;
        private readonly WorldHud worldHud = new WorldHud();
        private readonly FrameRateCounter frameRate = new FrameRateCounter();

        private bool quitRequested;
        private bool startRequested;

        public WebUiHost(ImGuiWebBackend backend, ScreenState screens, GameConfig gameConfig, SaveSession saves) {
            this.backend = backend;
            this.screens = screens;
            this.gameConfig = gameConfig;
            this.saves = saves;
        }

        public bool consumeQuitRequest() {
            bool requested = quitRequested;
            quitRequested = false;
            return requested;
        }

        /** The slot a screen asked to load (Continue, or a pick from the Load list), once; null otherwise. */
        public string consumeLoadRequest() {
            string slot = loadRequest;
            loadRequest = null;
            return slot;
        }

        /** True once, when Save on the pause screen was pressed. */
        public bool consumeSaveRequest() {
            bool requested = saveRequested;
            saveRequested = false;
            return requested;
        }

        /** True once, when Main Menu on the pause screen was pressed: the host saves the world. */
        public bool consumeLeaveRequest() {
            bool requested = leaveRequested;
            leaveRequested = false;
            return requested;
        }

        public bool consumeStartRequest() {
            bool requested = startRequested;
            startRequested = false;
            return requested;
        }

        public int getFramesPerSecond() {
            return frameRate.getFramesPerSecond();
        }

        public void render(double deltaTime, double cssWidth, double cssHeight, double devicePixelRatio,
                Simulation simulation, bool debugMode, ChronicleView chronicleView = null) {
            frameRate.record(deltaTime);
            backend.newFrame(deltaTime, cssWidth, cssHeight, devicePixelRatio);

            float width = (float) cssWidth;
            float height = (float) cssHeight;
            UiAction action = UiAction.None;
            switch (screens.getCurrent()) {
                case ScreenType.TITLE:
                    action = titleScreen.draw(width, height);
                    break;
                case ScreenType.MAIN_MENU:
                    action = mainMenuScreen.draw(width, height, saves.latest(), saves.isFeedbackError() ? saves.getFeedback() : null);
                    break;
                case ScreenType.CONFIG:
                    action = configScreen.draw(width, height, gameConfig);
                    break;
                case ScreenType.PAUSE:
                    action = pauseScreen.draw(width, height, saves);
                    break;
                case ScreenType.WORLD:
                    if (simulation != null) {
                        action = worldHud.draw(width, height, simulation, debugMode, frameRate.getFramesPerSecond(), chronicleView);
                    }
                    break;
                case ScreenType.CONTROLS:
                    action = controlsScreen.draw(width, height);
                    break;
                case ScreenType.INVENTORY:
                    if (simulation != null) {
                        action = inventoryScreen.draw(width, height, simulation.getPlayer().getInventory());
                    }
                    break;
                case ScreenType.NATIONS:
                    if (simulation != null) {
                        action = nationScreen.draw(width, height, simulation);
                    }
                    break;
                case ScreenType.CRAFTING:
                    if (simulation != null) {
                        action = craftingScreen.draw(width, height, simulation);
                    }
                    break;
                case ScreenType.LOAD:
                    action = loadScreen.draw(width, height, saves);
                    break;
            }

            apply(action);
            backend.render();
        }

        // UiHost.apply, unchanged.
        private void apply(UiAction action) {
            switch (action) {
                case UiAction.Continue:
                    screens.anyKeyPressed();
                    break;
                case UiAction.StartGame:
                    startRequested = true;
                    screens.goTo(ScreenType.WORLD);
                    break;
                case UiAction.OpenConfig:
                    screens.goTo(ScreenType.CONFIG);
                    break;
                case UiAction.Back:
                    screens.goTo(screens.getCurrent() == ScreenType.PAUSE ? ScreenType.WORLD : ScreenType.MAIN_MENU);
                    break;
                case UiAction.MainMenu:
                    // the host saves the world before the menu is shown
                    leaveRequested = true;
                    screens.goTo(ScreenType.MAIN_MENU);
                    break;
                case UiAction.OpenControls:
                case UiAction.CloseControls:
                    screens.controlsPressed();
                    break;
                case UiAction.Quit:
                    quitRequested = true;
                    break;
                case UiAction.ResumeWorld:
                    screens.goTo(ScreenType.WORLD);
                    break;
                case UiAction.ContinueGame:
                    SaveSlotInfo latest = saves.latest();
                    loadRequest = latest != null ? latest.Slot : null;
                    break;
                case UiAction.OpenLoad:
                    saves.refresh();
                    screens.goTo(ScreenType.LOAD);
                    break;
                case UiAction.LoadSlot:
                    loadRequest = loadScreen.consumeSelection();
                    break;
                case UiAction.SaveGame:
                    saveRequested = true;
                    break;
            }
        }

        public void Dispose() {
            backend.Dispose();
        }
    }
}
