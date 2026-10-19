using System;
using System.Runtime.InteropServices.JavaScript;

namespace beyondnations.web.host {

    /**
    * The C# side of the reusable web host (wwwroot/webhost/).
    *
    * Nothing in host/ knows about Beyond Nations: it is a canvas, a frame
    * clock, browser input and a WebGL2 device that C# drives through a few
    * calls, written so that another .NET game of the Strata family can take
    * the folder as it is and supply its own IWebGame. Every call that hands
    * over geometry passes a pointer into wasm memory and a length; the
    * JavaScript side reads it in place, so no frame data is copied through
    * the interop layer.
    *
    * The JavaScript half is wwwroot/webhost/bridge.js, imported as the
    * module "webhost" before Main runs.
    */
    public static partial class WebGpu {
        public const string Module = "webhost";

        // --- frame ---

        /** Viewport = the drawing buffer; clears colour and depth. */
        [JSImport("gfx.beginFrame", Module)]
        public static partial void beginFrame(double r, double g, double b);

        [JSImport("gfx.endFrame", Module)]
        public static partial void endFrame();

        // --- textures ---

        [JSImport("gfx.createTexture", Module)]
        public static partial int createTexture(int width, int height, bool linear);

        /** Tightly packed RGBA8 into the given sub-rectangle. */
        [JSImport("gfx.updateTexture", Module)]
        public static partial void updateTexture(int texture, int x, int y, int width, int height, int pixelsPtr, int byteLength);

        // --- world-space text ---

        /**
        * Billboarded glyph quads, 9 floats a vertex (position, uv, colour) and
        * 32-bit indices, drawn without depth test over the world.
        */
        [JSImport("gfx.drawLabels", Module)]
        public static partial void drawLabels(int matricesPtr, int texture, int verticesPtr, int vertexCount, int indicesPtr, int indexCount);

        // --- Dear ImGui ---

        /**
        * One ImDrawList: 20-byte ImDrawVert, 16-bit indices, and commands of
        * 8 floats each (clip x1 y1 x2 y2, texture, vertex offset, index
        * offset, element count). displayW/H are in CSS pixels, scale is the
        * framebuffer scale.
        */
        [JSImport("gfx.drawImGuiList", Module)]
        public static partial void drawImGuiList(
            int verticesPtr, int vertexCount, int indicesPtr, int indexCount, int commandsPtr, int commandCount,
            double displayW, double displayH, double scale);
    }

    /** Page-level services: the things a desktop host would ask its window for. */
    public static partial class WebPage {
        public const string Module = "webhost";

        [JSImport("page.log", Module)]
        public static partial void log(string level, string message);

        /** Saves the canvas as it was last drawn, as a PNG download. Returns the file name. */
        [JSImport("page.captureScreenshot", Module)]
        public static partial string captureScreenshot();

        /** The game asked to quit: the page shows its "exited" panel. */
        [JSImport("page.quit", Module)]
        public static partial void quit();

        /** The page's URL query string, including the leading "?" (or ""). */
        [JSImport("page.query", Module)]
        public static partial string query();

        /** window.devicePixelRatio when the game starts. */
        [JSImport("page.devicePixelRatio", Module)]
        public static partial double devicePixelRatio();

        /** Stops the frame clock (for --exit-after-frames). */
        [JSImport("page.stop", Module)]
        public static partial void stop();

        /** Something the page should show over the canvas, e.g. a fatal error. */
        [JSImport("page.fatal", Module)]
        public static partial void fatal(string message);

        /** Hand the page the game's own status JSON (smoke tests read it). */
        [JSImport("page.status", Module)]
        public static partial void status(string json);

        /** The first frame has been drawn: the page hides its loading panel. */
        [JSImport("page.ready", Module)]
        public static partial void ready();

        // --- storage (saves) ---

        /** Every key starting with prefix, prefix removed, as a JSON array of strings. */
        [JSImport("page.storageKeys", Module)]
        public static partial string storageKeys(string prefix);

        /** The value stored under key, or null. */
        [JSImport("page.storageGet", Module)]
        public static partial string storageGet(string key);

        /** Stores a value; "" on success, otherwise why it could not. */
        [JSImport("page.storageSet", Module)]
        public static partial string storageSet(string key, string value);

        [JSImport("page.storageRemove", Module)]
        public static partial void storageRemove(string key);
    }

    /**
    * What a game supplies to the host. The host calls frame once per
    * requestAnimationFrame and forwards browser input as it arrives.
    */
    public interface IWebGame {
        /**
        * deltaSeconds since the previous frame; the canvas is cssWidth x
        * cssHeight CSS pixels and devicePixelRatio times that in device pixels.
        */
        void frame(double deltaSeconds, double cssWidth, double cssHeight, double devicePixelRatio);

        /** KeyboardEvent.code ("KeyW", "ArrowUp", "F1" ...). */
        void key(string code, bool down);

        /** Pointer position in CSS pixels, relative to the canvas. */
        void pointerMove(double x, double y);

        /** 0 left, 1 right, 2 middle (ImGui's order). */
        void pointerButton(int button, bool down);

        /** Relative movement while the look gesture is held (pointer lock). */
        void look(double dx, double dy);

        void wheel(double dx, double dy);

        void character(int codepoint);

        /** The page lost focus: every key and button is up. */
        void focusLost();

        /** The page is being hidden (tab closed, switched away, phone locked). */
        void pageHidden();
    }

    /**
    * The [JSExport]s the page calls. One game per page, registered by Main.
    */
    public static partial class WebHostExports {
        private static IWebGame game;
        private static double lastTime = -1;

        public static void register(IWebGame instance) {
            game = instance;
        }

        [JSExport]
        public static void Frame(double nowMilliseconds, double cssWidth, double cssHeight, double devicePixelRatio) {
            if (game == null) {
                return;
            }
            double delta = lastTime < 0 ? 1.0 / 60.0 : (nowMilliseconds - lastTime) / 1000.0;
            lastTime = nowMilliseconds;
            // A background tab stops requestAnimationFrame; the first frame
            // back should not be treated as one enormous step.
            if (delta < 0 || delta > 0.25) {
                delta = 0.25;
            }
            try {
                game.frame(delta, cssWidth, cssHeight, devicePixelRatio);
            } catch (Exception e) {
                WebPage.fatal("The game crashed: " + e.Message);
                WebPage.log("error", e.ToString());
                WebPage.stop();
            }
        }

        [JSExport]
        public static void Key(string code, bool down) { game?.key(code, down); }

        [JSExport]
        public static void PointerMove(double x, double y) { game?.pointerMove(x, y); }

        [JSExport]
        public static void PointerButton(int button, bool down) { game?.pointerButton(button, down); }

        [JSExport]
        public static void Look(double dx, double dy) { game?.look(dx, dy); }

        [JSExport]
        public static void Wheel(double dx, double dy) { game?.wheel(dx, dy); }

        [JSExport]
        public static void Character(int codepoint) { game?.character(codepoint); }

        [JSExport]
        public static void FocusLost() { game?.focusLost(); }

        [JSExport]
        public static void Hidden() { game?.pageHidden(); }
    }
}
