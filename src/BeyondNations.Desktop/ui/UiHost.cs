using System;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using beyondnations;
using beyondnations.desktop.input;
using beyondnations.desktop.ui.feel;
using beyondnations.desktop.ui.screens;

namespace beyondnations.desktop.ui {

    /**
    * Owns Dear ImGui and decides which screen draws this frame.
    *
    * The Unity build had one OnGUI per screen class and a MonoBehaviour that
    * switched between them. The switch now lives in ScreenState, in core, and
    * this is the host half: it holds the ImGui context, hands each screen the
    * framebuffer size and whatever simulation state it needs, and turns the
    * intent a screen returns back into something only the host can do -- create
    * a world, change screen, or close the window.
    *
    * Screens never touch the window or the simulation lifetime themselves,
    * which is why they return a UiAction instead of acting.
    */
    public class UiHost : IDisposable {
        private readonly ImGuiController controller;
        private readonly IInputContext input;
        private readonly PressLatch<MouseButton> clicks = new PressLatch<MouseButton>();
        private readonly ScreenState screens;
        // The config screen edits this whether or not a world exists, so it
        // comes from the host rather than through the simulation.
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
        private readonly SuccessionScreen successionScreen = new SuccessionScreen();
        // sharing (docs/SHARING.md): the seed for a new world, and the Share
        // screen's link and exports
        private readonly NewWorldScreen newWorldScreen = new NewWorldScreen();
        private readonly ShareScreen shareScreen = new ShareScreen();
        private readonly ShareSession share;
        private readonly EndOfRunScreen endOfRunScreen = new EndOfRunScreen();
        private bool newWorldRequested;
        // saving (docs/SAVES.md): the host owns the world, so a screen asking
        // to save or load is a request the host picks up after the frame
        private readonly SaveSession saves;
        private string loadRequest;
        private bool saveRequested;
        private bool leaveRequested;
        private readonly WorldHud worldHud = new WorldHud();
        private readonly WorldEventSheet worldEventSheet = new WorldEventSheet();
        private readonly FrameRateCounter frameRate = new FrameRateCounter();

        private bool quitRequested;
        private bool startRequested;

        // sound, effects and tips (ui/feel); null leaves the screens as they were
        private FeelDirector feel;

        public void setFeel(FeelDirector director) {
            feel = director;
        }

        public UiHost(GL gl, IWindow window, IInputContext input, ScreenState screens, GameConfig gameConfig, SaveSession saves, ShareSession share) {
            this.controller = new ImGuiController(gl, window, input, configureForUiScale);
            this.input = input;
            foreach (IMouse mouse in input.Mice) {
                mouse.MouseDown += (_, button) => clicks.recordPress(button);
            }
            this.screens = screens;
            this.gameConfig = gameConfig;
            this.saves = saves;
            this.share = share;
        }

        /**
        * Rasterises ImGui's font at the UI scale rather than at 13 px and
        * stretching it, so text on a high-DPI display is both the right size
        * and sharp, and scales padding, rounding and spacing to match. Runs
        * inside ImGuiController's constructor, before the font atlas is
        * uploaded.
        */
        private static unsafe void configureForUiScale() {
            float scale = UiScale.get();
            if (scale == 1f) {
                return;
            }
            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.Clear();
            ImFontConfig* config = ImGuiNative.ImFontConfig_ImFontConfig();
            config->SizePixels = Overlay.DesignFontSize * scale;
            config->OversampleH = 1;
            config->OversampleV = 1;
            config->PixelSnapH = 1;
            io.Fonts.AddFontDefault(new ImFontConfigPtr(config));
            ImGuiNative.ImFontConfig_destroy(config);
            ImGui.GetStyle().ScaleAllSizes(scale);
        }

        public bool isQuitRequested() {
            return quitRequested;
        }

        /**
        * True once, when a screen has asked for a world to be created. The host
        * clears it by calling this; the world is the host's to build.
        */
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

        /** True once, when New world on the end-of-run screen was pressed: the host saves the ended world (the New World screen builds the next). */
        public bool consumeNewWorldRequest() {
            bool requested = newWorldRequested;
            newWorldRequested = false;
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

        /**
        * Draws whichever screen is active. simulation may be null, which is the
        * case on every screen except the world.
        *
        * debugMode is passed in rather than held here, because the input layer
        * already owns it: F1 gates the debug commands as well as the overlay,
        * and two copies would drift.
        */
        public void render(double deltaTime, float width, float height, Simulation simulation, bool debugMode, ChronicleView chronicleView = null) {
            frameRate.record(deltaTime);
            replayClicksShorterThanAFrame();
            controller.Update((float) deltaTime);

            UiAction action = UiAction.None;
            switch (screens.getCurrent()) {
                case ScreenType.TITLE:
                    action = titleScreen.draw(width, height);
                    break;
                case ScreenType.MAIN_MENU:
                    action = mainMenuScreen.draw(width, height, saves.latest(), saves.isFeedbackError() ? saves.getFeedback() : null);
                    break;
                case ScreenType.CONFIG:
                    action = configScreen.draw(width, height, gameConfig, feel?.getSettings());
                    break;
                case ScreenType.PAUSE:
                    action = pauseScreen.draw(width, height, saves, feel?.getSettings(),
                        simulation != null ? "World seed " + WorldSeed.format(ShareSession.seedOf(simulation)) : null);
                    break;
                case ScreenType.NEW_WORLD:
                    action = newWorldScreen.draw(width, height, share);
                    break;
                case ScreenType.SHARE:
                    if (simulation != null) {
                        action = shareScreen.draw(width, height, simulation, share);
                    }
                    break;
                case ScreenType.WORLD:
                    if (simulation != null) {
                        action = worldHud.draw(width, height, simulation, debugMode, frameRate.getFramesPerSecond(), chronicleView);
                        // a world event waiting on the player is drawn over the HUD (worldevents/)
                        worldEventSheet.draw(width, height, simulation);
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
                case ScreenType.SUCCESSION:
                    if (simulation != null) {
                        action = successionScreen.draw(width, height, simulation);
                    }
                    break;
                case ScreenType.END_OF_RUN:
                    if (simulation != null) {
                        action = endOfRunScreen.draw(width, height, simulation);
                    }
                    break;
            }

            feel?.drawOverlay(width, height, screens.getCurrent(), simulation);
            if (share.handle(action, simulation)) {
                action = UiAction.None;
            }
            apply(action);
            controller.Render();
        }

        /**
        * ImGuiController reads the mouse buttons by polling, so a click whose
        * down and up both land between two frames -- a touchpad tap-to-click,
        * or any quick click at a low frame rate -- never reached ImGui and
        * buttons such as Start ignored it. Such a click is handed to ImGui as
        * queued events instead, which it plays back as down on this frame and
        * up on the next. Called before controller.Update(), which is where
        * ImGui consumes the queue.
        */
        private void replayClicksShorterThanAFrame() {
            if (input.Mice.Count == 0) {
                clicks.clear();
                return;
            }
            IMouse mouse = input.Mice[0];
            foreach (MouseButton button in clicks.takeReleasedSinceLastFrame(b => mouse.IsButtonPressed(b))) {
                int index = (int) button;
                if (index < 0 || index > 2) {
                    continue;
                }
                ImGuiIOPtr io = ImGui.GetIO();
                io.AddMouseButtonEvent(index, true);
                io.AddMouseButtonEvent(index, false);
            }
        }

        private void apply(UiAction action) {
            switch (action) {
                case UiAction.Continue:
                    screens.anyKeyPressed();
                    break;
                case UiAction.StartGame:
                    // the New World screen asks for a seed first; its Begin builds the world
                    screens.goTo(ScreenType.NEW_WORLD);
                    break;
                case UiAction.BeginWorld:
                    startRequested = true;
                    screens.goTo(ScreenType.WORLD);
                    break;
                case UiAction.OpenShare:
                    share.clearFeedback();
                    screens.goTo(ScreenType.SHARE);
                    break;
                case UiAction.BackToPause:
                    screens.goTo(ScreenType.PAUSE);
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
                case UiAction.NewWorld:
                    // the ended world is saved, then the New World screen asks for a seed as Start does
                    newWorldRequested = true;
                    screens.goTo(ScreenType.NEW_WORLD);
                    break;
            }
        }

        public void Dispose() {
            controller?.Dispose();
        }
    }
}
