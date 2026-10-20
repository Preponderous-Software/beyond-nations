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

const ui = {
  progress(bytes) {
    const mb = bytes / (1024 * 1024);
    $("loading-text").textContent = `Loading the game… ${mb.toFixed(1)} MB`;
    // The compressed download is about 10 MB; the bar is a guide, not a promise.
    $("bar-fill").style.width = Math.min(95, (mb / 10) * 100).toFixed(0) + "%";
  },
  ready() {
    $("bar-fill").style.width = "100%";
    show("loading", false);
    canvas.focus({ preventScroll: true });
  },
  fatal(message) {
    show("loading", false);
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
    // A world event's bottom sheet (WorldEventSheet) pauses the world and
    // takes the bottom of the screen: the controls step aside for it.
    const sheet = !!(value && value.worldEvent);
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
        return status && (status.chronicle === "Full" || status.worldEvent) ? "scroll" : "look";
      }
      return "scroll";
    },
    touchLookScale: TOUCH_LOOK_SCALE,
  },
};

$("play-again").addEventListener("click", () => window.location.reload());
$("error-reload").addEventListener("click", () => window.location.reload());

try {
  host = await startWebHost({
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
