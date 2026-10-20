// Beyond Nations' page: the panels around the canvas, the touch gate, and
// the call that starts the reusable web host (webhost/host.js).

import { startWebHost } from "./webhost/host.js";
import { isTouchOnly, wireTouchControls } from "./webhost/touch.js";
import * as audio from "./audio.js";

const $ = (id) => document.getElementById(id);
const canvas = $("game");
const show = (id, visible) => { $(id).hidden = !visible; };

let status = null;
const touchOnly = isTouchOnly();

const ui = {
  progress(bytes) {
    const mb = bytes / (1024 * 1024);
    $("loading-text").textContent = `Loading the game… ${mb.toFixed(1)} MB`;
    // The compressed download is about 10 MB; the bar is a guide, not a promise.
    $("bar-fill").style.width = Math.min(95, (mb / 10) * 100).toFixed(0) + "%";
  },
  ready() {
    $("bar-fill").style.width = "100%";
    if (touchOnly) {
      show("loading", false);
      show("gate", true);
    } else {
      show("loading", false);
      canvas.focus({ preventScroll: true });
    }
  },
  fatal(message) {
    show("loading", false);
    show("gate", false);
    $("error-text").textContent = message;
    show("error", true);
  },
  quit() {
    show("touch", false);
    show("exited", true);
  },
  status(value) {
    status = value;
    // style.css hides the on-screen controls on screens they would cover,
    // such as crafting, whose rows are tapped directly.
    document.body.dataset.screen = (value && value.screen) || "";
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
  },
};

$("play-again").addEventListener("click", () => window.location.reload());
$("error-reload").addEventListener("click", () => window.location.reload());
$("gate-continue").addEventListener("click", () => {
  show("gate", false);
  show("touch", true);
  canvas.focus({ preventScroll: true });
});

let host = null;
try {
  host = await startWebHost({
    canvas,
    ui,
    assembly: "BeyondNations.Web",
    exportsName: "beyondnations.web.host.WebHostExports",
    // sound (audio.js), called from game/WebAudioSoundService.cs
    modules: { "bn-audio": audio },
  });
  if (host) wireTouchControls($("touch"), host.input);
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
};
