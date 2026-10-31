// Beyond Nations' page: the panels around the canvas, the touch controls,
// and the call that starts the reusable web host (webhost/host.js).

import { startWebHost } from "./webhost/host.js";
import {
  isTouchOnly, readTouchMode, storeTouchMode, wireTouchControls, wireJoystick,
  readSafeArea, preventPageGestures,
} from "./webhost/touch.js";
import * as audio from "./audio.js";

const $ = (id) => document.getElementById(id);
const canvas = $("game");
const show = (id, visible) => { $(id).hidden = !visible; };

// --- touch controls (docs/WEB.md, "Touch controls") ---
//
// The setting is "auto", "on" or "off" (?touch= in the URL, else what the
// player last chose in the game's settings). Auto turns the touch layout on
// for a touch-first device, and on any device the moment a finger touches
// the screen; a desktop with a mouse never sees it.
const TOUCH_MODE_KEY = "beyond-nations/touch-controls";
// A finger drag turns this many times as far as the same mouse drag: a
// thumb's sweep is short, and 0.15 degrees a pixel left a phone turning
// barely a quarter of the way round in a full swipe.
const TOUCH_LOOK_SCALE = 2;
const touch = {
  mode: readTouchMode(TOUCH_MODE_KEY),
  seen: isTouchOnly(),
  active: false,
  reserve: -1,
  sentKey: "",
  rotateDismissed: false,
};

let status = null;
let host = null;
let controls = null;
let joystick = null;

function touchActive() {
  return touch.mode === "on" || (touch.mode === "auto" && touch.seen);
}

/**
 * How many CSS pixels of the safe area's bottom the controls cover, for the
 * game's HUD to keep clear. Measured while they are showing; until then a
 * guess from the layout, so the first frame is already right.
 */
function measureReserve() {
  const el = $("touch");
  const rect = el.getBoundingClientRect();
  if (rect.height > 0) {
    const safe = readSafeArea($("safe-area-probe"));
    return Math.max(0, Math.round(window.innerHeight - safe.bottom - rect.top));
  }
  return touch.reserve >= 0 ? touch.reserve : (window.innerHeight > window.innerWidth ? 220 : 150);
}

/** Tells the game (and the stylesheet) what is showing, when anything changed. */
function publishTouch() {
  touch.active = touchActive();
  document.body.dataset.touch = touch.active ? "on" : "off";
  if (touch.active) touch.reserve = measureReserve();
  const key = `${touch.mode}|${touch.active}|${touch.reserve}`;
  if (host && key !== touch.sentKey) {
    touch.sentKey = key;
    host.touchState(touch.mode, touch.active, touch.active ? touch.reserve : 0);
  }
  if (!touch.active) releaseTouchControls();
  updateRotateHint();
}

function publishSafeArea() {
  if (!host) return;
  const safe = readSafeArea($("safe-area-probe"));
  host.safeArea(safe.left, safe.top, safe.right, safe.bottom);
}

/** Lets go of the stick and the Run toggle, e.g. when a panel opens over them. */
function releaseTouchControls() {
  joystick?.reset();
  controls?.release();
}

// Held upright a phone shows the world in a narrow strip between the HUD
// and the controls; it still plays, but sideways is better, so say so once.
function updateRotateHint() {
  const portrait = window.innerHeight > window.innerWidth;
  const inWorld = status && status.screen === "WORLD";
  show("rotate-hint", touch.active && portrait && inWorld && !touch.rotateDismissed);
}

// A finger anywhere on the page is a touch screen, whatever the media query said.
window.addEventListener("pointerdown", (event) => {
  if (event.pointerType === "touch" && !touch.seen) {
    touch.seen = true;
    publishTouch();
  }
}, { capture: true });
const relayout = () => requestAnimationFrame(() => { publishSafeArea(); publishTouch(); });
window.addEventListener("resize", relayout);
window.addEventListener("orientationchange", relayout);
$("rotate-dismiss").addEventListener("click", () => {
  touch.rotateDismissed = true;
  updateRotateHint();
});
preventPageGestures();

// --- the loading screen (docs/WEB.md, "Loading screen") ---
//
// Strata's loading shell (strata-kit/strata-loader.js): the trailer's still
// as a blurred backdrop and a progress bar that this page drives from the
// .NET boot, which fetches its own files. Downloading is the first 90% of the
// bar: whichever is further of the runtime's count of files fetched and the
// bytes against the usual size; starting the runtime is the rest. Until the
// game is up, a failure shows the trailer with the reason and a way back to
// the arcade; after that, the error panel.
const TRAILER = "https://danielstephenson.dev/trailers/beyond-nations";
// The gzip-compressed runtime and assemblies (webhost/loader.js): about
// 4.8 MB in 0.6.x; a guide for the bar, not a promise (the file count
// catches up, and the bar never moves back).
const USUAL_DOWNLOAD_BYTES = 5 * 1024 * 1024;
const boot = { bytes: 0, files: 0, ofFiles: 0, up: false };
function bootProgress() {
  const byFiles = boot.ofFiles ? boot.files / boot.ofFiles : 0;
  const byBytes = Math.min(0.98, boot.bytes / USUAL_DOWNLOAD_BYTES);
  const mb = (boot.bytes / (1024 * 1024)).toFixed(1);
  StrataLoader.progress(0.9 * Math.max(byFiles, byBytes), `Downloading the game… ${mb} MB`);
}

const ui = {
  progress(bytes) {
    boot.bytes = bytes;
    bootProgress();
  },
  resources(loaded, total) {
    boot.files = loaded;
    boot.ofFiles = total;
    bootProgress();
  },
  starting() {
    StrataLoader.progress(0.95, "Starting the game…");
  },
  ready() {
    boot.up = true;
    StrataLoader.progress(1, "Ready");
    StrataLoader.done();
    canvas.focus({ preventScroll: true });
  },
  fatal(message) {
    if (!boot.up && StrataLoader.state !== "done") {
      StrataLoader.fallback("engine", message);
      return;
    }
    $("error-text").textContent = message;
    show("error", true);
  },
  quit() {
    document.body.dataset.screen = "";
    show("exited", true);
  },
  status(value) {
    const before = status ? status.screen : null;
    status = value;
    // style.css shows the on-screen controls only while the world is up:
    // every other screen has its own buttons and scrolls by dragging.
    document.body.dataset.screen = (value && value.screen) || "";
    // A world event's bottom sheet (WorldEventSheet), which pauses the world, and
    // the caravan dispatch panel (Send Caravan) both
    // reach the bottom of the screen: the controls step aside for them.
    const sheet = !!(value && (value.worldEvent || value.caravanPanel));
    if (sheet !== (document.body.dataset.sheet === "on")) {
      document.body.dataset.sheet = sheet ? "on" : "off";
      if (sheet) joystick?.reset();
    }
    if (value && value.screen !== before) {
      if (value.screen !== "WORLD") joystick?.reset();
      // the band is measured once it is laid out
      requestAnimationFrame(publishTouch);
    }
  },
  touchMode(mode) {
    // the player changed the setting in the game
    touch.mode = mode === "on" || mode === "off" ? mode : "auto";
    storeTouchMode(TOUCH_MODE_KEY, touch.mode);
    touch.sentKey = "";
    publishTouch();
  },
  inputHooks: {
    // The title screen moves on with any key. A tap is not a key, so on
    // touch screens a tap on the title counts as one.
    onTap() {
      if (status && status.screen === "TITLE" && host) {
        host.input.key("Enter", true);
        setTimeout(() => host.input.key("Enter", false), 50);
      }
    },
    // What a finger drag on the canvas does (webhost/input.js): steer in
    // the world, or scroll a panel (a tap on a slider still sets it).
    touchDrag() {
      if (!status || status.screen === "WORLD") {
        return status && (status.chronicle === "Full" || status.worldEvent || status.caravanPanel) ? "scroll" : "look";
      }
      return "scroll";
    },
    touchLookScale: TOUCH_LOOK_SCALE,
  },
};

$("play-again").addEventListener("click", () => window.location.reload());
$("error-reload").addEventListener("click", () => window.location.reload());

// false, with the trailer and the reason showing, when there is no WebGL 2
const canRun = StrataLoader.show({
  title: "Beyond Nations",
  poster: TRAILER + ".jpg",
  trailer: { webm: TRAILER + ".webm", mp4: TRAILER + ".mp4" },
  trailerLabel: "A recorded clip of Beyond Nations",
  posterAlt: "A still of Beyond Nations",
  fallback: { href: "https://danielstephenson.dev/play", text: "More games at danielstephenson.dev/play" },
});

try {
  if (canRun) host = await startWebHost({
    canvas,
    ui,
    assembly: "BeyondNations.Web",
    exportsName: "beyondnations.web.host.WebHostExports",
    // sound (audio.js), called from game/WebAudioSoundService.cs
    modules: { "bn-audio": audio },
  });
  if (host) {
    controls = wireTouchControls($("touch"), host.input);
    joystick = wireJoystick($("stick"), host.input);
    publishSafeArea();
    publishTouch();
    const letGo = () => releaseTouchControls();
    window.addEventListener("blur", letGo);
    document.addEventListener("visibilitychange", () => { if (document.hidden) letGo(); });
  }
} catch (error) {
  console.error(error);
  ui.fatal("The game failed to load: " + (error && error.message ? error.message : error));
}

// For tests and the curious: window.beyondNations.status() and
// window.beyondNations.sampleNextFrame().
window.beyondNations = {
  status: () => (host ? host.status() : null),
  frameStats: () => (host ? host.frameStats() : null),
  sampleNextFrame: () => (host ? host.sampleNextFrame() : Promise.resolve(null)),
  gl: () => (host ? host.gl : null),
  audio: () => audio.state(),
  touch: () => ({ mode: touch.mode, active: touch.active, reserve: touch.reserve }),
};
