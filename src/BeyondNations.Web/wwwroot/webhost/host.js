// The web host: boots the .NET runtime, gives C# a WebGL2 device and page
// services (the "webhost" JSImport module), forwards input and drives the
// frame clock. Reusable by any .NET game that implements IWebGame
// (host/WebHost.cs); the page supplies a `ui` object for what it shows.
//
//   const host = await startWebHost({ canvas, ui, assembly: "BeyondNations.Web",
//                                    exportsName: "beyondnations.web.host.WebHostExports" });
//
// ui: { progress(loadedBytes), ready(), fatal(message), quit(), status(obj),
//       log(level, message), touchMode(mode), inputHooks } -- every member
//       optional. touchMode is the game asking the page to store its
//       touch-controls setting; the page answers with host.touchState().
// modules: { name: object } -- extra JSImport modules the game's own code
//       imports (Beyond Nations' "bn-audio"), registered before Main runs.

import * as gfx from "./gfx.js";
import { attachInput } from "./input.js";
import { trackCanvasSize } from "./canvas.js";
import { gzipResourceLoader } from "./loader.js";

// The share sheet is for phones and tablets; a desktop browser that has
// navigator.share too still copies, which is what a mouse user expects.
function canShareLink() {
  try {
    return typeof navigator.share === "function" && window.matchMedia("(pointer: coarse)").matches;
  } catch (_) { return false; }
}

export async function startWebHost({ canvas, ui = {}, assembly, exportsName, frameworkPath = "./_framework/", modules = {} }) {
  const size = trackCanvasSize(canvas);
  size.measure();

  let runtime = null;
  const heapView = () => runtime.localHeapViewU8();
  const gl = gfx.init(canvas, heapView);
  if (!gl) {
    ui.fatal?.("This game needs WebGL2, which this browser or device does not provide.");
    return null;
  }

  let running = true;
  let lastStatus = null;
  const loader = gzipResourceLoader((bytes) => ui.progress?.(bytes));

  // Relative to the page, not to this module.
  const { dotnet } = await import(new URL(frameworkPath + "dotnet.js", document.baseURI).href);
  runtime = await dotnet
    .withResourceLoader(loader)
    // Native code's stderr (Emscripten's printErr) is the console's error
    // channel by default. Strata writes its informational lines there too
    // ("strata info: ..."), so those go to the log instead; anything else
    // native code prints on stderr is still an error.
    .withModuleConfig({
      printErr(text) {
        if (/^strata (info|debug):/.test(text)) console.log(text);
        else console.error(text);
      },
    })
    .withConfig({ disableIntegrityCheck: true })
    .create();

  const query = window.location.search || "";
  runtime.setModuleImports("webhost", {
    gfx,
    page: {
      log(level, message) {
        if (ui.log) ui.log(level, message);
        if (level === "error") console.error(message);
        else if (level === "warn") console.warn(message);
        else console.log(message);
      },
      captureScreenshot() {
        const stamp = new Date().toISOString().replace(/[-:]/g, "").replace("T", "-").slice(0, 15);
        const name = `${assembly.split(".")[0].toLowerCase()}-${stamp}.png`;
        const link = document.createElement("a");
        link.download = name;
        link.href = canvas.toDataURL("image/png");
        link.click();
        return name;
      },
      quit() { running = false; ui.quit?.(); },
      query() { return query; },
      devicePixelRatio() { return size.measure().ratio; },
      stop() { running = false; },
      fatal(message) { running = false; ui.fatal?.(message); },
      status(json) {
        try { lastStatus = JSON.parse(json); } catch (_) { lastStatus = null; }
        ui.status?.(lastStatus);
      },
      ready() { ui.ready?.(); },
      touchMode(mode) { ui.touchMode?.(mode); },
      // Key-value storage for saves. localStorage can be missing or throw
      // (private windows, storage turned off, quota), so every call is
      // guarded and a failure comes back as a value, never an exception.
      storageKeys(prefix) {
        try {
          const keys = [];
          for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key && key.startsWith(prefix)) keys.push(key.slice(prefix.length));
          }
          return JSON.stringify(keys);
        } catch (_) { return "[]"; }
      },
      storageGet(key) {
        try { return localStorage.getItem(key); } catch (_) { return null; }
      },
      storageSet(key, value) {
        try { localStorage.setItem(key, value); return ""; }
        catch (e) { return (e && e.name === "QuotaExceededError") ? "the browser's storage for this site is full" : String(e && e.message || e); }
      },
      storageRemove(key) {
        try { localStorage.removeItem(key); } catch (_) { /* nothing to do */ }
      },
      // Sharing (docs/SHARING.md). The system share sheet on a touch device
      // that has one; the clipboard everywhere else.
      canShare() { return canShareLink(); },
      async shareLink(url, title) {
        if (canShareLink()) {
          try { await navigator.share({ title, url }); return "shared"; }
          catch (e) {
            if (e && e.name === "AbortError") return "cancelled";
            // fall through to the clipboard
          }
        }
        try {
          if (navigator.clipboard && navigator.clipboard.writeText) {
            await navigator.clipboard.writeText(url);
            return "copied";
          }
        } catch (e) { return "failed: " + String(e && e.message || e); }
        return "failed: no clipboard";
      },
      downloadText(name, mime, content) {
        try {
          const blob = new Blob([content], { type: mime || "text/plain" });
          const link = document.createElement("a");
          link.download = name;
          link.href = URL.createObjectURL(blob);
          document.body.appendChild(link);
          link.click();
          link.remove();
          setTimeout(() => URL.revokeObjectURL(link.href), 60000);
          return "";
        } catch (e) { return String(e && e.message || e); }
      },
      promptText(message, current) {
        try {
          const typed = window.prompt(message, current || "");
          return typed === null ? null : String(typed);
        } catch (_) { return null; }
      },
    },
  });

  for (const [name, module] of Object.entries(modules)) {
    runtime.setModuleImports(name, module);
  }

  const config = runtime.getConfig();
  const exports = await runtime.getAssemblyExports(config.mainAssemblyName);
  // The [JSExport] class, by its full name (namespace.Class).
  const host = exportsName.split(".").reduce((scope, key) => scope?.[key], exports);
  if (!host) throw new Error("webhost: " + exportsName + " is not exported by " + config.mainAssemblyName);
  const input = attachInput(canvas, host, ui.inputHooks || {});
  // A tab being hidden is the last moment a page can count on: closing it,
  // switching away on a phone, or the browser discarding it all pass through
  // here, so the game is told and can save.
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "hidden" && host.Hidden) {
      try { host.Hidden(); } catch (e) { console.error(e); }
    }
  });

  await runtime.runMain(config.mainAssemblyName, []);

  // Main-thread time spent in the game's frame (simulation, snapshot, UI
  // and issuing the GL calls), over the last 120 frames.
  const frameTimes = [];
  function frame(now) {
    if (!running) return;
    const s = size.measure();
    const start = performance.now();
    host.Frame(now, s.cssWidth, s.cssHeight, s.ratio);
    frameTimes.push(performance.now() - start);
    if (frameTimes.length > 120) frameTimes.shift();
    if (running) requestAnimationFrame(frame);
  }
  const frameStats = () => {
    if (!frameTimes.length) return null;
    const sorted = [...frameTimes].sort((a, b) => a - b);
    const mean = frameTimes.reduce((a, b) => a + b, 0) / frameTimes.length;
    return { frames: frameTimes.length, meanMs: +mean.toFixed(2), p95Ms: +sorted[Math.floor(sorted.length * 0.95)].toFixed(2) };
  };
  requestAnimationFrame(frame);

  return {
    input,
    status: () => lastStatus,
    frameStats,
    sampleNextFrame: () => gfx.sampleNextFrame(),
    gl: gfx.info(),
    stop() { running = false; },
    /** Insets of the canvas's safe area (notches), in CSS pixels; the game keeps its UI inside. */
    safeArea(left, top, right, bottom) { host.SafeArea?.(left, top, right, bottom); },
    /** The touch-controls setting, whether they are showing, and the CSS pixels they cover at the bottom. */
    touchState(mode, active, bottomReserve) { host.TouchState?.(mode, active, bottomReserve); },
  };
}
