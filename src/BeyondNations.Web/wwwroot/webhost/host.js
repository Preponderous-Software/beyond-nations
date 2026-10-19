// The web host: boots the .NET runtime, gives C# a WebGL2 device and page
// services (the "webhost" JSImport module), forwards input and drives the
// frame clock. Reusable by any .NET game that implements IWebGame
// (host/WebHost.cs); the page supplies a `ui` object for what it shows.
//
//   const host = await startWebHost({ canvas, ui, assembly: "BeyondNations.Web",
//                                    exportsName: "beyondnations.web.host.WebHostExports" });
//
// ui: { progress(loadedBytes), ready(), fatal(message), quit(), status(obj),
//       log(level, message) } -- every member optional.

import * as gfx from "./gfx.js";
import { attachInput } from "./input.js";
import { trackCanvasSize } from "./canvas.js";
import { gzipResourceLoader } from "./loader.js";

export async function startWebHost({ canvas, ui = {}, assembly, exportsName, frameworkPath = "./_framework/" }) {
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
    },
  });

  const config = runtime.getConfig();
  const exports = await runtime.getAssemblyExports(config.mainAssemblyName);
  // The [JSExport] class, by its full name (namespace.Class).
  const host = exportsName.split(".").reduce((scope, key) => scope?.[key], exports);
  if (!host) throw new Error("webhost: " + exportsName + " is not exported by " + config.mainAssemblyName);
  const input = attachInput(canvas, host, ui.inputHooks || {});

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
  };
}
