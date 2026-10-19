using System;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using beyondnations;
using beyondnations.desktop.input;
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
        private readonly WorldHud worldHud = new WorldHud();
        private readonly FrameRateCounter frameRate = new FrameRateCounter();

        private bool quitRequested;
        private bool startRequested;

        public UiHost(GL gl, IWindow window, IInputContext input, ScreenState screens, GameConfig gameConfig) {
            this.controller = new ImGuiController(gl, window, input, configureForUiScale);
            this.input = input;
            foreach (IMouse mouse in input.Mice) {
                mouse.MouseDown += (_, button) => clicks.recordPress(button);
            }
            this.screens = screens;
            this.gameConfig = gameConfig;
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
        public void render(double deltaTime, float width, float height, Simulation simulation, bool debugMode) {
            frameRate.record(deltaTime);
            replayClicksShorterThanAFrame();
            controller.Update((float) deltaTime);

            UiAction action = UiAction.None;
            switch (screens.getCurrent()) {
                case ScreenType.TITLE:
                    action = titleScreen.draw(width, height);
                    break;
                case ScreenType.MAIN_MENU:
                    action = mainMenuScreen.draw(width, height);
                    break;
                case ScreenType.CONFIG:
                    action = configScreen.draw(width, height, gameConfig);
                    break;
                case ScreenType.PAUSE:
                    action = pauseScreen.draw(width, height);
                    break;
                case ScreenType.WORLD:
                    if (simulation != null) {
                        action = worldHud.draw(width, height, simulation, debugMode, frameRate.getFramesPerSecond());
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
                    // No saves yet, so leaving for the menu ends this world;
                    // Start builds a fresh one.
                    screens.goTo(ScreenType.MAIN_MENU);
                    break;
                case UiAction.OpenControls:
                case UiAction.CloseControls:
                    screens.controlsPressed();
                    break;
                case UiAction.Quit:
                    quitRequested = true;
                    break;
            }
        }

        public void Dispose() {
            controller?.Dispose();
        }
    }
}
