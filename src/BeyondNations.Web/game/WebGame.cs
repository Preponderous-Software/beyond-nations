using System;
using System.Globalization;
using System.Text;
using Silk.NET.Input;
using beyondnations;
using beyondnations.desktop;
using beyondnations.desktop.input;
using beyondnations.desktop.render;
using beyondnations.web.host;

namespace beyondnations.web {

    /**
    * Game.cs for the browser: the same host loop with the window taken away.
    *
    * requestAnimationFrame stands in for Silk.NET's Update and Render, which
    * here run back to back in frame(): input is read, the simulation advances
    * by whole fixed steps (one tick each, clamped to MaxCatchUpSteps), the
    * snapshot is captured and drawn, then labels, then the ImGui screens.
    * The order of every step, the keys only the host owns (Escape, I, F12, V)
    * and the camera, culler and render-distance handling are Game.cs's.
    *
    * What has no browser equivalent is changed, not faked: "close the window"
    * (Exit on the main menu, Escape on the title) shows the page's exited
    * panel; F12 downloads a PNG instead of writing one under the user's data
    * directory; --width/--height are the page's size, not options.
    */
    public class WebGame : IWebGame {
        private readonly GameOptions options;
        private readonly GameConfig gameConfig;
        private readonly ScreenState screens = new ScreenState();

        private readonly BrowserInputSource inputSource = new BrowserInputSource();
        private readonly InputService inputService;
        private readonly PlayerInputController playerInputController = new PlayerInputController();

        private readonly ImGuiWebBackend imgui;
        private readonly WebUiHost ui;
        private readonly IWorldRenderer renderer;
        private readonly WebWorldLabelRenderer labels;
        private readonly RenderStatsRecorder renderStats;

        private readonly PlayerCamera camera = new PlayerCamera();
        private readonly RenderCuller culler = new RenderCuller();
        private readonly WorldSnapshot snapshot = new WorldSnapshot();
        private Simulation simulation;

        private readonly double fixedTimeStep;
        private double accumulator;

        private int framesRendered;
        private int fixedStepsRun;
        private int lastWidth;
        private int lastHeight;
        private bool screenshotRequested;
        private bool stopped;
        private int lastLoggedRenderDistance;
        private ScreenType lastPublishedScreen = (ScreenType) (-1);

        public WebGame(GameOptions options, double devicePixelRatio) {
            this.options = options;
            this.gameConfig = new GameConfig();
            if (options.Seed != 0) {
                gameConfig.setWorldSeed(options.Seed);
            }
            gameConfig.setTicksPerSecond(options.TicksPerSecond);
            this.fixedTimeStep = 1.0 / options.TicksPerSecond;

            inputService = new InputService(inputSource);

            renderer = new WebGl2WorldRenderer();
            Log.info("world renderer: " + renderer.getName() + " (WebGL2)");
            if (options.RenderStats) {
                renderStats = new RenderStatsRecorder();
            }

            if (!options.NoLabels) {
                labels = new WebWorldLabelRenderer();
            }

            imgui = new ImGuiWebBackend(devicePixelRatio);
            ui = new WebUiHost(imgui, screens, gameConfig);
            playerInputController.setDebugMode(options.DebugMode);

            if (options.FirstPerson) {
                camera.setMode(CameraMode.FirstPerson);
            }

            screens.goTo(options.StartScreen);
            if (screens.isWorldVisible()) {
                createWorld();
            }
            Log.info("host ready: " + options.TicksPerSecond + " ticks per second");
        }

        public ScreenState getScreens() { return screens; }
        public Simulation getSimulation() { return simulation; }

        private void createWorld() {
            simulation = new Simulation(gameConfig);
            simulation.getPlayer().getStatus().update("Press " + KeyBindings.CreateNewNation + " to create a nation.");
            if (options.TimeOfDay >= 0f) {
                simulation.getTimeOfDay().setStartFraction(options.TimeOfDay);
            }
            if (options.RenderDistance > 0) {
                applyStartingRenderDistance(simulation.getPlayer(), options.RenderDistance);
                Log.info("starting render distance: " + simulation.getPlayer().getRenderDistance());
            }
            culler.setEnabled(!options.NoCulling);
            Log.info("world created");
        }

        // --- IWebGame ---

        public void frame(double deltaSeconds, double cssWidth, double cssHeight, double devicePixelRatio) {
            if (stopped) {
                return;
            }
            int width = Math.Max(1, (int) Math.Round(cssWidth * devicePixelRatio));
            int height = Math.Max(1, (int) Math.Round(cssHeight * devicePixelRatio));
            if (width != lastWidth || height != lastHeight) {
                lastWidth = width;
                lastHeight = height;
                camera.setAspectRatio(width, height);
                renderer.setViewportSize(width, height);
                Log.info("framebuffer resized to " + width + "x" + height + " (" + cssWidth + "x" + cssHeight
                    + " CSS px at devicePixelRatio " + devicePixelRatio.ToString(CultureInfo.InvariantCulture) + ")");
            }

            update(deltaSeconds);
            if (stopped) {
                return;
            }
            render(deltaSeconds, cssWidth, cssHeight, devicePixelRatio);
            // Every few frames is plenty for the page and the smoke test, and
            // at once whenever the screen changes.
            ScreenType screen = screens.getCurrent();
            if (framesRendered % 5 == 0 || screen != lastPublishedScreen) {
                lastPublishedScreen = screen;
                publishStatus();
            }
        }

        public void key(string code, bool down) {
            inputSource.key(BrowserKeys.fromCode(code), down);
        }

        public void pointerMove(double x, double y) { imgui.pointerMove(x, y); }
        public void pointerButton(int button, bool down) { imgui.pointerButton(button, down); }
        public void look(double dx, double dy) { inputSource.look((float) dx, (float) dy); }
        public void wheel(double dx, double dy) { imgui.wheel(dx, dy); }
        public void character(int codepoint) { imgui.character(codepoint); }

        public void focusLost() {
            inputSource.releaseAll();
            imgui.focusLost();
        }

        // --- Game.onUpdate ---

        private void update(double deltaTime) {
            readInput();
            runHousekeeping();
            if (stopped || !screens.shouldAdvanceSimulation() || simulation == null) {
                return;
            }
            accumulator += deltaTime;
            double maxBacklog = fixedTimeStep * options.MaxCatchUpSteps;
            if (accumulator > maxBacklog) {
                accumulator = maxBacklog;
            }
            while (accumulator >= fixedTimeStep) {
                simulation.fixedUpdate((float) fixedTimeStep);
                accumulator -= fixedTimeStep;
                fixedStepsRun++;
            }
        }

        private void runHousekeeping() {
            if (options.ScreenshotAfterFrames > 0 && framesRendered == options.ScreenshotAfterFrames) {
                screenshotRequested = true;
            }
            if (options.ExitAfterFrames > 0 && framesRendered >= options.ExitAfterFrames) {
                close();
            }
        }

        private void readInput() {
            inputService.update();

            if (inputService.wasPressedThisFrame(KeyBindings.Pause)) {
                if (!screens.escapePressed()) {
                    close();
                    return;
                }
            }
            if (inputService.wasAnyKeyPressedThisFrame()) {
                screens.anyKeyPressed();
            }
            if (inputService.wasPressedThisFrame(KeyBindings.TakeScreenshot)) {
                screenshotRequested = true;
            }
            if (simulation != null && inputService.wasPressedThisFrame(KeyBindings.ToggleInventory)) {
                screens.inventoryPressed();
            }
            if (simulation == null || !screens.isWorldActive()) {
                return;
            }
            if (inputService.wasPressedThisFrame(KeyBindings.ToggleCameraView)) {
                camera.toggleMode();
                simulation.getPlayer().getStatus().update(
                    camera.getMode() == CameraMode.FirstPerson ? "First-person view." : "Third-person view.");
            }
            playerInputController.update(simulation, inputService);
        }

        // --- Game.onRender ---

        private void render(double deltaTime, double cssWidth, double cssHeight, double devicePixelRatio) {
            renderStats?.beginFrame();
            WebGpu.beginFrame(0.45, 0.65, 0.90);

            if (simulation != null && screens.isWorldVisible()) {
                Player player = simulation.getPlayer();
                snapshot.capture(
                    simulation.getEntityRepository(),
                    simulation.getEnvironment(),
                    camera.getMode() == CameraMode.FirstPerson ? player.getId() : null);

                MouseLook mouseLook = playerInputController.getMouseLook();
                camera.setRenderDistance(player.getRenderDistance());
                camera.follow(player.getPosition(), player.getYaw(), mouseLook.getYawDegrees(), mouseLook.getPitchDegrees());
                culler.beginFrame(camera);
                renderer.render(snapshot, camera.getViewMatrix(), camera.getProjectionMatrix(), culler);
                reportRenderDistance(player.getRenderDistance());

                labels?.render(snapshot.getLabels(), camera.getViewMatrix(), camera.getProjectionMatrix());
            }

            ui.render(deltaTime, cssWidth, cssHeight, devicePixelRatio, simulation, playerInputController.isDebugMode());
            if (ui.consumeStartRequest()) {
                createWorld();
            }
            WebGpu.endFrame();

            if (screenshotRequested) {
                screenshotRequested = false;
                captureScreenshotAndReport();
            }
            if (ui.consumeQuitRequest()) {
                close();
            }

            framesRendered++;
            if (framesRendered == 1) {
                WebPage.ready();
            }
            renderStats?.recordCamera(
                simulation != null ? simulation.getPlayer().getRenderDistance() : 0, renderer.getInstancesCulled());
            renderStats?.endFrame(renderer.getDrawCallCount(), renderer.getInstancesDrawn());
        }

        private void captureScreenshotAndReport() {
            string message;
            try {
                message = "Screenshot saved as " + WebPage.captureScreenshot() + ".";
            } catch (Exception exception) {
                Log.error("Screenshot capture failed: " + exception.Message);
                message = "Screenshot capture failed: " + exception.Message;
            }
            simulation?.getPlayer().getStatus().update(message);
        }

        private void reportRenderDistance(int renderDistance) {
            if (!options.RenderStats || renderDistance == lastLoggedRenderDistance) {
                return;
            }
            lastLoggedRenderDistance = renderDistance;
            Log.info("render distance: " + renderDistance);
        }

        /** The browser's "close the window": stop, and let the page say so. */
        private void close() {
            if (stopped) {
                return;
            }
            stopped = true;
            Log.info("shutting down after " + framesRendered + " frames and " + fixedStepsRun + " fixed steps");
            if (renderStats != null && renderStats.hasData()) {
                Log.info(renderStats.summarize());
            }
            publishStatus();
            WebPage.stop();
            WebPage.quit();
        }

        /**
        * A small JSON summary for the page (and the smoke test): which
        * screen, how many frames, whether a world exists and what was drawn.
        */
        private void publishStatus() {
            StringBuilder json = new StringBuilder(256);
            json.Append("{\"screen\":\"").Append(screens.getCurrent().ToString()).Append('"');
            json.Append(",\"frames\":").Append(framesRendered);
            json.Append(",\"fixedSteps\":").Append(fixedStepsRun);
            json.Append(",\"world\":").Append(simulation != null ? "true" : "false");
            json.Append(",\"renderer\":\"").Append(renderer.getName()).Append('"');
            json.Append(",\"drawCalls\":").Append(renderer.getDrawCallCount());
            json.Append(",\"instancesDrawn\":").Append(renderer.getInstancesDrawn());
            json.Append(",\"instancesCulled\":").Append(renderer.getInstancesCulled());
            json.Append(",\"labelsDrawn\":").Append(labels != null ? labels.getLabelsDrawn() : 0);
            json.Append(",\"fps\":").Append(ui.getFramesPerSecond());
            MouseLook look = playerInputController.getMouseLook();
            json.Append(",\"lookYaw\":").Append(look.getYawDegrees().ToString("0.##", CultureInfo.InvariantCulture));
            json.Append(",\"lookPitch\":").Append(look.getPitchDegrees().ToString("0.##", CultureInfo.InvariantCulture));
            json.Append(",\"camera\":\"").Append(camera.getMode().ToString()).Append('"');
            json.Append(",\"stopped\":").Append(stopped ? "true" : "false");
            json.Append('}');
            WebPage.status(json.ToString());
        }

        private static void applyStartingRenderDistance(Player player, int target) {
            int previous = -1;
            while (player.getRenderDistance() != previous) {
                previous = player.getRenderDistance();
                if (previous + 10 <= target) {
                    player.increaseRenderDistance();
                } else if (previous - 10 >= target) {
                    player.decreaseRenderDistance();
                } else {
                    return;
                }
            }
        }
    }
}
