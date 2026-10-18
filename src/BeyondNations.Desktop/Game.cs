using System;
using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using beyondnations;
using beyondnations.desktop.input;
using beyondnations.desktop.render;
using beyondnations.desktop.text;
using beyondnations.desktop.ui;

namespace beyondnations.desktop {

    /**
    * The host: a window, a GL context and the loop that drives the simulation.
    *
    * Unity gave the project Update() and FixedUpdate() for free, and that split
    * is worth keeping: behaviour, metabolism and world generation are all
    * tick-driven and should not run faster on a faster machine. The loop below
    * accumulates real elapsed time and advances the simulation a whole number of
    * fixed steps, so the tick rate is stable and independent of frame rate.
    *
    * Drawing is delegated to PrimitiveRenderer, which turns the world snapshot
    * into a few instanced draw calls (#218), viewed through the camera and
    * filtered by the culler that came with #219; the UI arrives with #221.
    */
    public class Game : IDisposable {
        private readonly GameOptions options;
        private readonly GameConfig gameConfig;
        private readonly ScreenState screens = new ScreenState();

        private IWindow window;
        private GL gl;
        private IInputContext input;
        private InputService inputService;
        private readonly PlayerInputController playerInputController = new PlayerInputController();

        private Simulation simulation;
        private readonly WorldSnapshot snapshot = new WorldSnapshot();

        // --- #222 world-space text ---
        private WorldLabelRenderer labels;
        // --- end #222 ---

        // --- #221 imgui user interface ---
        private UiHost ui;
        // --- end #221 ---

        // --- #218 instanced renderer ---
        private PrimitiveRenderer renderer;
        private RenderStatsRecorder renderStats;
        // --- end #218 ---

        // --- #219 camera and culling ---
        private readonly PlayerCamera camera = new PlayerCamera();
        private readonly RenderCuller culler = new RenderCuller();
        private int lastLoggedRenderDistance;
        // --- end #219 ---

        // Fixed-step accumulator
        private readonly double fixedTimeStep;
        private double accumulator;

        private int framesRendered;
        private bool smokeResizeDone;
        private int fixedStepsRun;
        private readonly Stopwatch clock = new Stopwatch();

        // Screenshot capture (#224). The capture path (glReadPixels -> PNG)
        // lives here so it can be exercised headlessly via
        // --screenshot-after-frames as well as from the F12 binding (#246).
        private bool screenshotAfterFramesDone;
        private string lastScreenshotPath;

        public Game(GameOptions options) {
            this.options = options;
            this.gameConfig = new GameConfig();
            if (options.Seed != 0) {
                gameConfig.setWorldSeed(options.Seed);
            }
            this.fixedTimeStep = 1.0 / options.TicksPerSecond;
        }

        public int getFramesRendered() {
            return framesRendered;
        }

        public int getFixedStepsRun() {
            return fixedStepsRun;
        }

        public ScreenState getScreens() {
            return screens;
        }

        public Simulation getSimulation() {
            return simulation;
        }

        public string getLastScreenshotPath() {
            return lastScreenshotPath;
        }

        /**
        * Reads the current framebuffer and writes it as a PNG under
        * AppDataPaths.getScreenshotsDirectory(). Public so it can be
        * invoked both from the F12 binding below and from
        * --screenshot-after-frames for headless verification.
        */
        public string takeScreenshot() {
            Vector2D<int> size = window.FramebufferSize;
            string path = ScreenshotCapture.capture(gl, size.X, size.Y, AppDataPaths.getScreenshotsDirectory());
            lastScreenshotPath = path;
            return path;
        }

        public void run() {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Size = new Vector2D<int>(options.Width, options.Height);
            windowOptions.Title = "Beyond Nations";
            windowOptions.VSync = options.VSync;

            window = Window.Create(windowOptions);
            window.Load += onLoad;
            window.Update += onUpdate;
            window.Render += onRender;
            window.FramebufferResize += onFramebufferResize;
            window.Closing += onClosing;

            window.Run();
        }

        private void onLoad() {
            gl = GL.GetApi(window);
            input = window.CreateInput();
            inputService = new InputService(new SilkInputSource(input));

            Log.info("GL vendor:   " + gl.GetStringS(StringName.Vendor));
            Log.info("GL renderer: " + gl.GetStringS(StringName.Renderer));
            Log.info("GL version:  " + gl.GetStringS(StringName.Version));

            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.CullFace);
            gl.CullFace(TriangleFace.Back);
            gl.ClearColor(0.45f, 0.65f, 0.90f, 1.0f);

            // --- #218 instanced renderer ---
            renderer = new PrimitiveRenderer(gl);
            if (options.RenderStats) {
                renderStats = new RenderStatsRecorder();
            }
            // --- end #218 ---

            // --- #222 world-space text ---
            // Nametags are billboarded quads out of one glyph atlas,
            // packed once here rather than a Canvas per label.
            if (!options.NoLabels) {
                labels = new WorldLabelRenderer(gl);
                if (labels.isReady()) {
                    Log.info("label atlas: " + labels.getAtlasWidth() + "x" + labels.getAtlasHeight());
                }
            }
            // --- end #222 ---

            // --- #221 imgui user interface ---
            ui = new UiHost(gl, window, input, screens, gameConfig);
            playerInputController.setDebugMode(options.DebugMode);
            // --- end #221 ---

            // --- #173 ---
            // The view is a mode on the one camera, so opening in first person
            // is the same call V makes and nothing more.
            if (options.FirstPerson) {
                camera.setMode(CameraMode.FirstPerson);
            }
            // --- end #173 ---

            onFramebufferResize(window.FramebufferSize);

            // The Unity build opened on the title screen, and now that the
            // screens exist again so does this. The world is not built until a
            // screen asks for one, except when --start-screen names it directly --
            // or names the inventory screen, which is drawn over a world (#181).
            screens.goTo(options.StartScreen);
            if (screens.isWorldVisible()) {
                createWorld();
            }

            clock.Start();
            Log.info("host ready: " + options.TicksPerSecond + " ticks per second");
        }

        // --- #221 imgui user interface ---
        /**
        * Builds the world. Called either because --start-screen named the world
        * or because a screen asked for a new game. Creating a world is the
        * host's to do, which is why a screen returns an intent rather than
        * doing it.
        */
        private void createWorld() {
            simulation = new Simulation(gameConfig);
            simulation.getPlayer().getStatus().update("Press " + KeyBindings.CreateNewNation + " to create a nation.");

            // --- #219 camera and culling ---
            if (options.RenderDistance > 0) {
                applyStartingRenderDistance(simulation.getPlayer(), options.RenderDistance);
                Log.info("starting render distance: " + simulation.getPlayer().getRenderDistance());
            }
            culler.setEnabled(!options.NoCulling);
            // --- end #219 ---
        }
        // --- end #221 ---

        /**
        * Variable rate. Runs once per frame, then advances the simulation by as
        * many whole fixed steps as the elapsed time allows.
        */
        private void onUpdate(double deltaTime) {
            readInput();

            // Housekeeping runs on every screen, not just the world. Before
            // #221 the host always started in the world, so leaving these below
            // the guard below was harmless; now that it can open on the title
            // screen, a run started there would never take its screenshot and
            // never exit.
            runHousekeeping();

            if (!screens.shouldAdvanceSimulation() || simulation == null) {
                return;
            }

            accumulator += deltaTime;

            // Clamp the backlog so that a long stall -- a breakpoint, a resize,
            // a paused window -- cannot make the next frame run thousands of
            // steps trying to catch up.
            double maxBacklog = fixedTimeStep * options.MaxCatchUpSteps;
            if (accumulator > maxBacklog) {
                accumulator = maxBacklog;
            }

            while (accumulator >= fixedTimeStep) {
                simulation.getTickCounter().increment();
                simulation.fixedUpdate((float) fixedTimeStep);
                accumulator -= fixedTimeStep;
                fixedStepsRun++;
            }

        }

        /**
        * Resize, screenshot and exit, none of which depend on a world existing.
        */
        private void runHousekeeping() {
            if (options.SmokeResize && !smokeResizeDone && framesRendered > 10) {
                smokeResizeDone = true;
                Vector2D<int> resized = new Vector2D<int>(options.Width / 2, options.Height / 2);
                Log.info("smoke: resizing window to " + resized.X + "x" + resized.Y);
                window.Size = resized;
            }

            if (options.ScreenshotAfterFrames > 0 && !screenshotAfterFramesDone && framesRendered >= options.ScreenshotAfterFrames) {
                screenshotAfterFramesDone = true;
                takeScreenshot();
            }

            if (options.ExitAfterFrames > 0 && framesRendered >= options.ExitAfterFrames) {
                window.Close();
            }
        }

        /**
        * The binding table lives in KeyBindings and is applied by
        * PlayerInputController (src/BeyondNations.Desktop/input); see #217.
        * Four bindings stay here because none of them acts on the simulation:
        * Escape, since toggling screens can close the window; I, since it
        * changes screen too (#181); F12, since capture reads the framebuffer;
        * and V, since the view is a mode on the host's camera. All four are
        * things only the host owns. Escape, I and F12 sit above the world
        * check because each means something off the world screen; V sits below
        * it, because the view only matters in the world.
        */
        private void readInput() {
            if (inputService == null) {
                return;
            }

            inputService.update();

            if (inputService.wasPressedThisFrame(KeyBindings.Pause)) {
                if (!screens.escapePressed()) {
                    window.Close();
                }
            }

            // --- #246 ---
            // Capture reads the framebuffer, so the screenshot key stays with
            // the host rather than moving into PlayerInputController along with
            // the rest of the bindings. It is read from the binding table and
            // edge-detected by InputService like every other key, and it sits
            // above the world check so a screenshot can be taken on any screen.
            if (inputService.wasPressedThisFrame(KeyBindings.TakeScreenshot)) {
                captureScreenshotAndReport();
            }
            // --- end #246 ---

            // --- #181 ---
            // I opens the inventory screen from the world and closes it again,
            // so it is read above the world check, which the inventory screen
            // does not pass. Leaving the world this way also stops the player
            // input controller running, so nothing moves while it is open.
            if (simulation != null && inputService.wasPressedThisFrame(KeyBindings.ToggleInventory)) {
                screens.inventoryPressed();
            }
            // --- end #181 ---

            if (simulation == null || !screens.isWorldActive()) {
                return;
            }

            // --- #173 ---
            // The camera belongs to the host, not to the simulation, so the
            // view key is read here rather than in PlayerInputController along
            // with the keys that issue commands. Below the world check, since
            // there is nothing to look at from the menus.
            if (inputService.wasPressedThisFrame(KeyBindings.ToggleCameraView)) {
                camera.toggleMode();
                simulation.getPlayer().getStatus().update(
                    camera.getMode() == CameraMode.FirstPerson
                        ? "First-person view."
                        : "Third-person view.");
            }
            // --- end #173 ---

            playerInputController.update(simulation, inputService);
        }

        /**
        * Takes a screenshot and reports the outcome on the player's status
        * line, which is the game's only in-world feedback channel. A capture
        * that succeeds silently reads exactly like a dead key, and one that
        * throws would otherwise take the window down with it.
        */
        private void captureScreenshotAndReport() {
            string message;
            try {
                message = "Screenshot saved to " + takeScreenshot() + ".";
            }
            catch (Exception exception) {
                Log.error("Screenshot capture failed: " + exception.Message);
                message = "Screenshot capture failed: " + exception.Message;
            }
            if (simulation != null) {
                simulation.getPlayer().getStatus().update(message);
            }
        }

        /**
        * Variable rate. Draws whatever the simulation currently is; it never
        * changes it.
        */
        private void onRender(double deltaTime) {
            renderStats?.beginFrame();

            gl.Clear((uint) (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

            // The inventory screen (#181) is drawn over the world, so the world
            // is drawn whenever it is visible, not only while it is active.
            if (simulation != null && screens.isWorldVisible()) {
                Player player = simulation.getPlayer();

                // The snapshot is rebuilt into the same buffers every frame and
                // is what the renderer draws from. Capturing it here keeps the
                // seam honest: the host reads the simulation, the renderer reads
                // the snapshot, and neither reaches past the other.
                //
                // --- #173 ---
                // In first person the eye sits just above the player's own
                // capsule, close enough that any downward pitch looks straight
                // at it, so the player is left out of the snapshot rather than
                // drawn and then looked through. Nothing on the entity changes:
                // the host names what its camera is attached to and the
                // simulation omits it for this frame only.
                // --- end #173 ---
                snapshot.capture(
                    simulation.getEntityRepository(),
                    simulation.getEnvironment(),
                    camera.getMode() == CameraMode.FirstPerson ? player.getId() : null);

                // --- #219 camera and culling ---
                // The camera trails the player exactly as the parented Unity
                // one did, its far plane is the render distance Page Up and
                // Page Down move, and the culler drops everything out of the
                // view volume or past that distance before an instance buffer
                // is touched.
                MouseLook mouseLook = playerInputController.getMouseLook();
                camera.setRenderDistance(player.getRenderDistance());
                camera.follow(player.getPosition(), player.getYaw(), mouseLook.getYawDegrees(), mouseLook.getPitchDegrees());
                culler.beginFrame(camera);
                renderer.render(snapshot, camera.getViewMatrix(), camera.getProjectionMatrix(), culler);
                reportRenderDistance(player.getRenderDistance());
                // --- end #219 ---

                // --- #222 world-space text ---
                labels?.render(snapshot.getLabels(), camera.getViewMatrix(), camera.getProjectionMatrix());
                // --- end #222 ---
            }

            // --- #221 imgui user interface ---
            // Drawn last, so it sits over the world rather than under it.
            ui?.render(deltaTime, window.FramebufferSize.X, window.FramebufferSize.Y, simulation,
                      playerInputController.isDebugMode());
            if (ui != null) {
                if (ui.consumeStartRequest()) {
                    createWorld();
                }
                if (ui.isQuitRequested()) {
                    window.Close();
                }
            }
            // --- end #221 ---

            framesRendered++;
            // --- #219 camera and culling ---
            renderStats?.recordCamera(
                simulation != null ? simulation.getPlayer().getRenderDistance() : 0,
                renderer != null ? renderer.getInstancesCulled() : 0);
            // --- end #219 ---
            renderStats?.endFrame(
                renderer != null ? renderer.getDrawCallCount() : 0,
                renderer != null ? renderer.getInstancesDrawn() : 0);
        }

        // --- #219 camera and culling ---
        /**
        * The debug overlay is #221's; until it exists, this is where the render
        * distance can be seen changing under Page Up and Page Down. Only
        * changes are logged, so a run is not drowned in one line per frame.
        */
        private void reportRenderDistance(int renderDistance) {
            if (!options.RenderStats || renderDistance == lastLoggedRenderDistance) {
                return;
            }
            lastLoggedRenderDistance = renderDistance;
            Log.info("render distance: " + renderDistance);
        }

        public PlayerCamera getCamera() {
            return camera;
        }

        public RenderCuller getCuller() {
            return culler;
        }

        /**
        * Steps the render distance towards a target using the same increase and
        * decrease the Page Up and Page Down bindings call, so a value this flag
        * cannot be reached by a player is not reachable here either. Stops as
        * soon as stepping stops changing anything, which is what the clamp on
        * Player does at either bound.
        */
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
        // --- end #219 ---

        private void onFramebufferResize(Vector2D<int> size) {
            if (gl == null) {
                return;
            }
            // A minimised window reports zero, which is not a valid viewport.
            int width = Math.Max(1, size.X);
            int height = Math.Max(1, size.Y);
            gl.Viewport(0, 0, (uint) width, (uint) height);
            camera.setAspectRatio(width, height);
            Log.info("framebuffer resized to " + size.X + "x" + size.Y + ", viewport set to " + width + "x" + height);
        }

        private void onClosing() {
            clock.Stop();
            double seconds = clock.Elapsed.TotalSeconds;
            Log.info(string.Format(
                "shutting down after {0} frames and {1} fixed steps in {2:F2}s ({3:F1} fps, {4:F1} tps)",
                framesRendered, fixedStepsRun, seconds,
                seconds > 0 ? framesRendered / seconds : 0,
                seconds > 0 ? fixedStepsRun / seconds : 0));

            // --- #218 instanced renderer ---
            if (renderStats != null && renderStats.hasData()) {
                Log.info(renderStats.summarize());
            }
            // Buffers and programs have to be deleted while the context that
            // owns them still exists, which by Dispose() time it does not.
            renderer?.Dispose();
            renderer = null;
            // --- end #218 ---

            // --- #222 world-space text ---
            // Same reason: the atlas texture, buffers and program belong to the
            // context, so they go here rather than in Dispose().
            if (labels != null) {
                Log.info(string.Format(
                    "labels: {0} drawn, {1} culled, {2} quads in {3} draw call(s), atlas {4}x{5} with {6} glyphs",
                    labels.getLabelsDrawn(), labels.getLabelsCulled(), labels.getQuadsDrawn(),
                    labels.getDrawCallCount(), labels.getAtlasWidth(), labels.getAtlasHeight(),
                    labels.getPackedGlyphCount()));
                labels.Dispose();
                labels = null;
            }
            // --- end #222 ---

            // --- #221 imgui user interface ---
            ui?.Dispose();
            ui = null;
            // --- end #221 ---
        }

        public WorldSnapshot getSnapshot() {
            return snapshot;
        }

        public PrimitiveRenderer getRenderer() {
            return renderer;
        }

        public void Dispose() {
            renderer?.Dispose();
            input?.Dispose();
            gl?.Dispose();
            window?.Dispose();
        }
    }
}
