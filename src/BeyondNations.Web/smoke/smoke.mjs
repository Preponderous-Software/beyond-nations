// Headless Chromium smoke test for the browser build.
//
//   node smoke.mjs <url> <output-dir> [--phone | --phone-landscape]
//
// Loads the page, waits for the title screen, presses a key (main menu),
// clicks Start and then Begin on the New World screen, waits for the world to
// be drawn and checks that the canvas is not blank, opens Share from the pause
// menu and downloads the replay and the history, saves from the pause menu and
// continues from the main menu, opens a ?seed= link, then fails on any
// console error, page error or failed request.
// Writes title.png, menu.png, world.png and result.json to <output-dir>.
//
// Every run also checks the loading screen (Strata's loading shell,
// wwwroot/strata-kit): it is up while the game loads, its bar only moves
// forward, and it is gone once the title is drawn. --no-webgl2 instead loads
// the page with WebGL 2 taken away and checks that the shell shows the
// trailer with the reason, and that none of the runtime is downloaded.
//
// --phone runs the same page as a phone held upright (390x844, touch, no
// hover) and --phone-landscape as one on its side (844x390), and plays it
// with the touch controls instead (docs/WEB.md, "Touch controls"): taps
// through the menus, walks and turns with the joystick, steers with a drag,
// opens and closes every panel from the button row and scrolls one by
// dragging. Screenshots are named phone-*.png.

import { chromium, devices } from "playwright";
import fs from "node:fs";
import path from "node:path";

const url = process.argv[2];
const out = process.argv[3] || "smoke-out";
const landscape = process.argv.includes("--phone-landscape");
const phone = landscape || process.argv.includes("--phone");
const noWebgl2 = process.argv.includes("--no-webgl2");
if (!url) {
  console.error("usage: node smoke.mjs <url> <output-dir> [--phone | --phone-landscape] [--no-webgl2]");
  process.exit(2);
}
fs.mkdirSync(out, { recursive: true });

const problems = [];
const log = [];
const browser = await chromium.launch({
  headless: true,
  // CHROMIUM_PATH: use an already-installed Chromium instead of Playwright's.
  executablePath: process.env.CHROMIUM_PATH || undefined,
  // WebGL2 without a GPU: ANGLE on SwiftShader.
  args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"],
});
const context = await browser.newContext(phone
  ? {
    ...devices["iPhone 13"],
    // the whole screen, as a game added to the home screen or in full
    // screen gets it, upright or on its side
    viewport: landscape ? { width: 844, height: 390 } : { width: 390, height: 844 },
    // 2x rather than the phone's 3x: the same CSS layout, at under half the
    // pixels for the software GPU the test runs on
    deviceScaleFactor: 2,
    userAgent: devices["iPhone 13"].userAgent + " headless-monitor",
  }
  : { viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1, userAgent: "Mozilla/5.0 (X11; Linux x86_64) Chrome headless-monitor" });
const page = await context.newPage();
// Headless Chromium fakes pointer-lock movement (it warps the cursor and
// reports the warp as movement), so the test refuses the lock and checks the
// unlocked drag, which is the same code path with a visible cursor.
await page.addInitScript(() => {
  HTMLCanvasElement.prototype.requestPointerLock = function () {
    return Promise.reject(new Error("pointer lock is not used in the smoke test"));
  };
});

// The loading shell's bar, sampled from the first script on: every value of
// aria-valuenow it showed, in order, and whether the shell was ever up.
await page.addInitScript(() => {
  const seen = { up: false, values: [] };
  window.__shellSeen = seen;
  const sample = () => {
    const bar = document.querySelector(".sk-loader .sk-bar");
    if (document.querySelector(".sk-loader:not(.sk-done):not([hidden])")) seen.up = true;
    const now = bar && bar.getAttribute("aria-valuenow");
    if (now != null && seen.values[seen.values.length - 1] !== Number(now)) seen.values.push(Number(now));
  };
  const timer = setInterval(sample, 25);
  addEventListener("DOMContentLoaded", () => {
    new MutationObserver(sample).observe(document.body, { subtree: true, attributes: true, childList: true });
  });
  setTimeout(() => clearInterval(timer), 300000);
});
if (noWebgl2) {
  await page.addInitScript(() => {
    const getContext = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = function (kind, ...rest) {
      return kind === "webgl2" ? null : getContext.call(this, kind, ...rest);
    };
  });
}

page.on("console", (message) => {
  const line = `[${message.type()}] ${message.text()}`;
  log.push(line);
  if (message.type() === "error") problems.push("console error: " + message.text());
});
page.on("pageerror", (error) => problems.push("page error: " + error.message));
page.on("requestfailed", (request) => {
  // the fallback's trailer clip may be cut short by the browser; that is not the page's fault
  if (noWebgl2 && request.resourceType() === "media") return;
  problems.push("request failed: " + request.url() + " " + (request.failure()?.errorText || ""));
});
page.on("response", (response) => {
  if (response.status() >= 400) problems.push(`HTTP ${response.status()}: ${response.url()}`);
});

const status = () => page.evaluate(() => window.beyondNations?.status?.() ?? null);
async function waitFor(predicate, what, timeout = 120000) {
  const start = Date.now();
  let last = null;
  while (Date.now() - start < timeout) {
    last = await status();
    if (last && predicate(last)) return last;
    if (problems.length) break;
    await page.waitForTimeout(100);
  }
  throw new Error(`timed out waiting for ${what}; last status ${JSON.stringify(last)}; problems ${JSON.stringify(problems)}`);
}

const result = { url, phone, landscape, noWebgl2, ok: false };
const FINISHED = Symbol("finished");
try {
  const started = Date.now();
  if (noWebgl2) {
    // Without WebGL 2 the shell shows the trailer and says why, and the
    // runtime is never fetched.
    const runtimeRequests = [];
    page.on("request", (request) => { if (/\/_framework\//.test(request.url())) runtimeRequests.push(request.url()); });
    await page.goto(url, { waitUntil: "load" });
    await page.waitForSelector(".sk-loader.sk-fallback", { timeout: 30000 });
    await page.waitForTimeout(1000);
    result.fallback = await page.evaluate(() => ({
      message: document.querySelector(".sk-loader .sk-msg")?.textContent || "",
      media: document.querySelector(".sk-loader .sk-media")?.tagName || null,
      link: document.querySelector(".sk-loader .sk-box a")?.href || null,
      state: window.StrataLoader?.state,
    }));
    await page.screenshot({ path: path.join(out, "no-webgl2.png") });
    if (!/WebGL 2/.test(result.fallback.message)) throw new Error("the fallback does not say why: " + JSON.stringify(result.fallback));
    if (result.fallback.media !== "VIDEO") throw new Error("the fallback does not show the trailer: " + JSON.stringify(result.fallback));
    if (!/danielstephenson\.dev\/play/.test(result.fallback.link || "")) throw new Error("the fallback has no way back to the arcade");
    if (runtimeRequests.length) throw new Error("the runtime was fetched without WebGL 2: " + runtimeRequests.slice(0, 3).join(", "));
    if (problems.length) throw new Error(problems.join("\n"));
    result.ok = true;
    throw FINISHED;  // nothing more to check: skip the game
  }
  await page.goto(url, { waitUntil: "load" });
  result.loadEventMs = Date.now() - started;
  await waitFor((s) => s.screen === "TITLE" && s.frames >= 5, "the title screen");
  // The loading screen: up while loading, its bar only moving forward to
  // 100, then gone (hidden) so it takes no taps or clicks.
  await page.waitForSelector(".sk-loader", { state: "hidden", timeout: 5000 });
  result.shell = await page.evaluate(() => window.__shellSeen);
  const bar = result.shell.values;
  if (!result.shell.up) throw new Error("the loading screen never showed");
  if (!bar.length || bar[bar.length - 1] !== 100 || bar.some((v, i) => i && v < bar[i - 1])) {
    throw new Error("the loading bar did not move forward to 100: " + JSON.stringify(bar));
  }
  result.titleMs = Date.now() - started;
  result.transferredBytes = await page.evaluate(() =>
    performance.getEntriesByType("resource").reduce((sum, entry) => sum + (entry.transferSize || 0), 0)
    + (performance.getEntriesByType("navigation")[0]?.transferSize || 0));
  result.gl = await page.evaluate(() => window.beyondNations.gl());
  await page.screenshot({ path: path.join(out, "title.png") });

  if (phone) {
    // Touch controls (docs/WEB.md): no keyboard notice, no controls over
    // the title, and the touch layout on from the first frame.
    if (await page.isVisible("#touch")) throw new Error("the touch controls show over the title screen");
    const viewport = page.viewportSize();
    const short = Math.min(viewport.width, viewport.height);
    // A tap on the title is the "any key" a touch screen cannot press.
    await page.touchscreen.tap(viewport.width / 2, viewport.height * 0.45);
    await waitFor((s) => s.screen === "MAIN_MENU", "the main menu after a tap");
    await page.waitForTimeout(300);
    await page.screenshot({ path: path.join(out, "phone-menu.png") });
    // MainMenuScreen: Start is the first of a column of 44 px buttons whose top edge is 25 px below the centre.
    await page.touchscreen.tap(viewport.width / 2, viewport.height / 2 + 25 + 22);
    // Start opens the New World screen (docs/SHARING.md), whose Begin sits where Start was.
    await waitFor((s) => s.screen === "NEW_WORLD", "the New World screen after tapping Start", 10000);
    await page.waitForTimeout(300);
    await page.screenshot({ path: path.join(out, "phone-new-world.png") });
    if (await page.isVisible("#stick")) {
      throw new Error("the touch controls cover the New World screen");
    }
    await page.touchscreen.tap(viewport.width / 2, viewport.height / 2 + 25 + 22);
    const world = await waitFor((s) => s.screen === "WORLD" && s.world && s.instancesDrawn > 0, "the world after tapping Begin");
    result.worldMs = Date.now() - started;
    // A new world opens with the first tip (ui/feel/OnboardingHints).
    await waitFor((s) => s.hint === "move", "the first tip in a new world", 15000);
    await page.screenshot({ path: path.join(out, "phone-tip.png") });
    await waitFor((s) => s.frames >= world.frames + 30 && s.touch, "the touch layout in the world");
    for (const selector of ["#stick", '#touch [data-key="KeyE"]', '#touch [data-key="KeyI"]', '#touch [data-key="Escape"]']) {
      if (!(await page.isVisible(selector))) throw new Error(selector + " is not shown in the world on a touch screen");
    }
    result.touch = await page.evaluate(() => window.beyondNations.touch());
    if (!(result.touch.reserve > 80)) throw new Error("the controls' band was not reported to the game: " + JSON.stringify(result.touch));
    // Held upright, a hint suggests turning the phone; it is dismissed by a tap.
    if (viewport.height > viewport.width) {
      if (!(await page.isVisible("#rotate-hint"))) throw new Error("no rotate hint in portrait");
      await page.screenshot({ path: path.join(out, "phone-rotate-hint.png") });
      await page.tap("#rotate-dismiss");
      if (await page.isVisible("#rotate-hint")) throw new Error("the rotate hint did not go away");
    }
    await page.waitForTimeout(500);
    await page.screenshot({ path: path.join(out, "phone-world.png") });

    // Raw touch events, so a finger can be held and moved (page.touchscreen only taps).
    const cdp = await context.newCDPSession(page);
    const touchPoint = (x, y, id = 0) => ({ x, y, id, radiusX: 4, radiusY: 4, force: 1 });
    const finger = {
      down: (x, y, id) => cdp.send("Input.dispatchTouchEvent", { type: "touchStart", touchPoints: [touchPoint(x, y, id)] }),
      move: (x, y, id) => cdp.send("Input.dispatchTouchEvent", { type: "touchMove", touchPoints: [touchPoint(x, y, id)] }),
      up: () => cdp.send("Input.dispatchTouchEvent", { type: "touchEnd", touchPoints: [] }),
    };
    const centreOf = async (selector) => {
      const box = await (await page.$(selector)).boundingBox();
      return { x: box.x + box.width / 2, y: box.y + box.height / 2, r: box.width / 2 };
    };
    // The status is published every few frames, and a software GPU draws
    // only a few frames a second, so wait for frames rather than for time.
    const settle = async (frames = 12) => {
      const now = await status();
      return waitFor((s) => s.frames >= now.frames + frames, frames + " more frames");
    };

    // The stick: pushed up it walks the player forward, without turning.
    const stick = await centreOf("#stick");
    const beforeWalk = await status();
    await finger.down(stick.x, stick.y);
    for (let i = 1; i <= 5; i++) await finger.move(stick.x, stick.y - stick.r * i / 5);
    await page.waitForTimeout(400);
    await page.screenshot({ path: path.join(out, "phone-stick.png") });
    await page.waitForTimeout(800);
    await finger.up();
    const afterWalk = await settle();
    const walked = Math.hypot(afterWalk.playerX - beforeWalk.playerX, afterWalk.playerZ - beforeWalk.playerZ);
    result.stickWalk = { walked, turned: afterWalk.playerYaw - beforeWalk.playerYaw };
    if (!(walked > 1)) throw new Error("pushing the stick forward did not walk the player: " + JSON.stringify(result.stickWalk));
    if (Math.abs(result.stickWalk.turned) > 0.01) throw new Error("a straight push turned the player: " + JSON.stringify(result.stickWalk));
    // Pushed right it turns right (yaw falls), as D does.
    await finger.down(stick.x, stick.y);
    for (let i = 1; i <= 5; i++) await finger.move(stick.x + stick.r * i / 5, stick.y);
    await page.waitForTimeout(500);
    await finger.up();
    const afterTurn = await settle();
    result.stickTurn = afterTurn.playerYaw - afterWalk.playerYaw;
    if (!(result.stickTurn < -5)) throw new Error("pushing the stick right did not turn the player right: " + result.stickTurn);

    // A drag on the world steers like the desktop's right-drag, twice as
    // far for a thumb: +60 px turns the player -18 degrees.
    const lookStart = { x: viewport.width * 0.62, y: viewport.height * 0.42 };
    const beforeLook = await settle();
    await finger.down(lookStart.x, lookStart.y);
    // a moment between moves and before the lift, as a real thumb takes:
    // the browser merges touch moves sent faster than it delivers them
    for (let i = 1; i <= 6; i++) {
      await finger.move(lookStart.x + i * 10, lookStart.y);
      await page.waitForTimeout(50);
    }
    await page.waitForTimeout(100);
    await finger.up();
    const looked = await settle();
    result.look = { playerTurn: looked.playerYaw - beforeLook.playerYaw, pitch: looked.lookPitch };
    if (Math.abs(result.look.playerTurn + 18) > 0.5) {
      throw new Error("a 60 px drag on the world turned the player " + result.look.playerTurn + "; expected -18");
    }

    // A tap on the world with nothing in reach does nothing at all (no
    // "No entities within range"); the Use button does what E does.
    await page.touchscreen.tap(lookStart.x, lookStart.y);
    await page.waitForTimeout(400);
    const tapped = await status();
    result.tapsInteracted = tapped.tapsInteracted;
    const use = await centreOf('#touch [data-key="KeyE"]');
    await page.touchscreen.tap(use.x, use.y);
    await page.waitForTimeout(400);

    // Bag opens the inventory; the controls step aside, and the corner
    // Close (top right of the safe area) brings the world back.
    const closeAt = { x: viewport.width - 8 - 48, y: 8 + 22 };
    const openPanel = async (key, screen, shot) => {
      const button = await centreOf(`#touch [data-key="${key}"]`);
      await page.touchscreen.tap(button.x, button.y);
      const opened = await waitFor((s) => s.screen === screen, `${key} to open ${screen}`, 10000);
      await waitFor((s) => s.frames >= opened.frames + 20, `20 frames of ${screen}`);
      if (await page.isVisible("#stick")) throw new Error(`the touch controls still cover ${screen}`);
      await page.screenshot({ path: path.join(out, shot) });
    };
    const closePanel = async (screen) => {
      await page.touchscreen.tap(closeAt.x, closeAt.y);
      await waitFor((s) => s.screen === "WORLD", `Close to leave ${screen}`, 10000);
      if (!(await page.isVisible("#stick"))) throw new Error(`the touch controls did not come back after ${screen}`);
      await page.waitForTimeout(300);
    };
    await openPanel("KeyI", "INVENTORY", "phone-inventory.png");
    await closePanel("INVENTORY");
    await openPanel("KeyC", "CRAFTING", "phone-crafting.png");
    // a drag scrolls the recipe list rather than steering or crafting
    await finger.down(viewport.width / 2, viewport.height * 0.75);
    for (let i = 1; i <= 8; i++) await finger.move(viewport.width / 2, viewport.height * 0.75 - i * 0.05 * short);
    await finger.up();
    await page.waitForTimeout(400);
    await page.screenshot({ path: path.join(out, "phone-crafting-scrolled.png") });
    if ((await status()).screen !== "CRAFTING") throw new Error("a drag on the crafting list left the screen");
    await closePanel("CRAFTING");
    await openPanel("KeyO", "NATIONS", "phone-nations.png");
    // NationScreen's Laws tab: 120 px wide, right of the centre, under the corner Close (60 px)
    await page.touchscreen.tap(viewport.width / 2 + 4 + 60, 60 + 15);
    await settle();
    if ((await status()).screen !== "NATIONS") throw new Error("tapping the Laws tab left the nations screen");
    await page.screenshot({ path: path.join(out, "phone-laws.png") });
    await closePanel("NATIONS");
    // News opens the full chronicle, which a drag scrolls; News again hides it.
    const news = await centreOf('#touch [data-key="KeyY"]');
    await page.touchscreen.tap(news.x, news.y);
    await waitFor((s) => s.chronicle === "Full", "News to open the full chronicle", 10000);
    await page.waitForTimeout(400);
    await page.screenshot({ path: path.join(out, "phone-chronicle.png") });
    await page.touchscreen.tap(news.x, news.y);
    await waitFor((s) => s.chronicle === "Hidden", "News again to hide the chronicle", 10000);
    await page.waitForTimeout(300);
    await page.touchscreen.tap(news.x, news.y);
    await waitFor((s) => s.chronicle === "Recent", "News a third time for the recent strip", 10000);
    // ? opens the controls list, which leads with the touch controls.
    await openPanel("Slash", "CONTROLS", "phone-controls.png");
    await page.keyboard.press("Escape");
    await waitFor((s) => s.screen === "WORLD", "Escape to close the controls", 10000);
    await page.waitForTimeout(300);
    // Menu pauses: fingertip-sized buttons from the top, with the touch switch.
    await openPanel("Escape", "PAUSE", "phone-pause.png");
    // PauseScreen on touch: the column starts 16 + 26 px down, 44 px buttons; Resume is first.
    await page.touchscreen.tap(viewport.width / 2, 16 + 26 + 22);
    await waitFor((s) => s.screen === "WORLD", "Resume on the pause screen", 10000);

    // A world event (WorldEventSheet) rises from the bottom over the
    // controls' band: the controls step aside, and a tap on the sheet's
    // last choice answers it and brings them back.
    // On its side only, since every page load costs the software GPU a while.
    if (landscape) {
      await page.goto(url + (url.includes("?") ? "&" : "?") + "start-screen=world&stage-event&no-saves", { waitUntil: "load" });
      await waitFor((s) => s.screen === "WORLD" && s.world && s.worldEvent, "a staged world event", 120000);
      await settle();
      if (await page.isVisible("#stick")) throw new Error("the touch controls cover the world event's choices");
      await page.screenshot({ path: path.join(out, "phone-event.png") });
      // the last choice ends just above the sheet's closing note: 72 px up its middle
      await page.touchscreen.tap(viewport.width / 2, viewport.height - 72);
      await waitFor((s) => !s.worldEvent, "a tap on a choice to answer the world event", 20000);
      await settle();
      if (!(await page.isVisible("#stick"))) throw new Error("the touch controls did not come back after the world event");
    }

    // The page itself never moved: no scroll, no zoom.
    result.page = await page.evaluate(() => ({ scrollY: window.scrollY, scale: window.visualViewport ? window.visualViewport.scale : 1 }));
    if (result.page.scrollY !== 0 || result.page.scale !== 1) throw new Error("the page scrolled or zoomed: " + JSON.stringify(result.page));
    await page.waitForTimeout(500);
  } else {
    await page.keyboard.press("Enter");
    const menu = await waitFor((s) => s.screen === "MAIN_MENU", "the main menu");
    await page.waitForTimeout(300);
    await page.screenshot({ path: path.join(out, "menu.png") });
    const viewport = page.viewportSize();
    // MainMenuScreen: Start is at least 160x40, its top edge 25 px below the centre.
    const startFrames = menu.frames;
    await page.mouse.click(viewport.width / 2, viewport.height / 2 + 50);
    // Start opens the New World screen (docs/SHARING.md); Begin, where Start
    // was, builds a world with a fresh seed since the field is empty.
    await waitFor((s) => s.screen === "NEW_WORLD", "the New World screen after Start", 10000);
    await page.waitForTimeout(300);
    await page.screenshot({ path: path.join(out, "new-world.png") });
    await page.mouse.click(viewport.width / 2, viewport.height / 2 + 50);
    const world = await waitFor((s) => s.screen === "WORLD" && s.world && s.instancesDrawn > 0, "the world after Begin");
    if (!world.seed) throw new Error("the world reports no seed");
    result.worldMs = Date.now() - started;
    await waitFor((s) => s.frames >= world.frames + 60, "60 more frames in the world");
    // A new world opens with the first tip (ui/feel/OnboardingHints), and the
    // key presses so far have unlocked the page's sound (wwwroot/audio.js).
    await waitFor((s) => s.hint === "move", "the first tip in a new world", 15000);
    await page.screenshot({ path: path.join(out, "tip.png") });
    // Walk and look a little, as a player would.
    await page.keyboard.down("KeyW");
    await page.waitForTimeout(600);
    await page.keyboard.up("KeyW");
    // Steer only while the right button is held (#291): a drag of +60, -10
    // pixels at 0.15 degrees a pixel turns the player -9 degrees (a drag to
    // the right lowers yaw, which turns right) and tilts the camera 1.5;
    // the camera's own yaw offset stays 0. Moving with no button held
    // afterwards changes nothing.
    const beforeLook = await status();
    await page.mouse.move(640, 360);
    await page.mouse.down({ button: "right" });
    await page.mouse.move(700, 350, { steps: 5 });
    await page.mouse.up({ button: "right" });
    await page.mouse.move(200, 500, { steps: 5 });
    await page.waitForTimeout(400);
    const looked = await status();
    const turned = looked.playerYaw - beforeLook.playerYaw;
    result.look = { playerTurn: turned, yaw: looked.lookYaw, pitch: looked.lookPitch };
    if (Math.abs(turned + 9) > 0.01 || Math.abs(looked.lookYaw) > 0.01 || Math.abs(looked.lookPitch - 1.5) > 0.01) {
      throw new Error("right-drag steer turned the player " + turned + " (look yaw " + looked.lookYaw + ", pitch " + looked.lookPitch + "); expected -9, 0 and 1.5");
    }
    // A key tap shorter than a frame still lands (#290): V switches view.
    await page.keyboard.press("KeyV");
    await waitFor((s) => s.camera === "FirstPerson", "V to switch to first person", 10000);
    // Two taps on consecutive frames are one press to InputService, on the
    // desktop as here, so leave a moment between them.
    await page.waitForTimeout(300);
    await page.keyboard.press("KeyV");
    await waitFor((s) => s.camera === "ThirdPerson", "V to switch back", 10000);
    // C opens the crafting screen (#47) and Escape closes it; drawing it must
    // not trip the browser's ImGui bindings.
    await page.waitForTimeout(300);
    await page.keyboard.press("KeyC");
    const crafting = await waitFor((s) => s.screen === "CRAFTING", "C to open the crafting screen", 10000);
    await waitFor((s) => s.frames >= crafting.frames + 30, "30 frames of the crafting screen");
    await page.screenshot({ path: path.join(out, "crafting.png") });
    await page.keyboard.press("Escape");
    await waitFor((s) => s.screen === "WORLD", "Escape to close the crafting screen", 10000);

    // Saves (docs/SAVES.md): Escape pauses, Save writes the world to
    // localStorage, Main Menu (which saves again) offers Continue, and
    // Continue brings the same world back at the tick it was saved on.
    // PauseScreen's column starts at the centre, MainMenuScreen's 25 px
    // below it; buttons are 40 px tall with 12 px between them. The pause
    // column is Resume, Save, Share, Controls, Main Menu, Quit.
    await page.waitForTimeout(300);
    await page.keyboard.press("Escape");
    await waitFor((s) => s.screen === "PAUSE", "Escape to pause", 10000);
    await page.waitForTimeout(300);

    // Share (docs/SHARING.md): its column starts 10 px below the centre and
    // is Copy link, Export replay (USD), Export history (text), Back. Each
    // export is a download.
    await page.mouse.click(viewport.width / 2, viewport.height / 2 + 2 * 52 + 20);
    await waitFor((s) => s.screen === "SHARE", "Share on the pause screen", 10000);
    await page.waitForTimeout(300);
    await page.screenshot({ path: path.join(out, "share.png") });
    const shareRow = (i) => viewport.height / 2 + 10 + i * 52 + 20;
    result.share = { seed: world.seed };
    for (const [row, name, expect] of [[1, "replay", "usd-replay/1"], [2, "history", "# The History of"]]) {
      const [download] = await Promise.all([
        page.waitForEvent("download", { timeout: 20000 }),
        page.mouse.click(viewport.width / 2, shareRow(row)),
      ]);
      const file = path.join(out, download.suggestedFilename());
      await download.saveAs(file);
      const text = fs.readFileSync(file, "utf8");
      if (!text.includes(expect)) throw new Error(`the ${name} download (${file}) does not contain ${expect}`);
      result.share[name] = { file: download.suggestedFilename(), bytes: text.length };
      await waitFor((s) => s.shareFeedback.startsWith("Downloaded"), `the Share screen to report the ${name} download`, 10000);
      await page.waitForTimeout(200);
    }
    await page.screenshot({ path: path.join(out, "share-exported.png") });
    await page.keyboard.press("Escape");
    await waitFor((s) => s.screen === "PAUSE", "Escape from Share back to the pause screen", 10000);
    await page.waitForTimeout(300);
    await page.mouse.click(viewport.width / 2, viewport.height / 2 + 52 + 20);
    const saved = await waitFor((s) => s.savedWorlds >= 1 && s.saveFeedback.startsWith("Saved"), "Save on the pause screen", 20000);
    await page.waitForTimeout(200);
    await page.screenshot({ path: path.join(out, "pause-saved.png") });
    result.save = {
      feedback: saved.saveFeedback,
      tick: saved.tick,
      keys: await page.evaluate(() => Object.keys(localStorage).filter((k) => k.startsWith("beyond-nations/saves/"))
        .map((k) => k + " " + localStorage.getItem(k).length)),
    };
    await page.mouse.click(viewport.width / 2, viewport.height / 2 + 4 * 52 + 20);
    await waitFor((s) => s.screen === "MAIN_MENU", "Main Menu from the pause screen", 10000);
    await page.waitForTimeout(300);
    await page.screenshot({ path: path.join(out, "menu-with-saves.png") });
    await page.mouse.click(viewport.width / 2, viewport.height / 2 + 25 + 52 + 20);
    const resumed = await waitFor((s) => s.screen === "WORLD" && s.world && s.instancesDrawn > 0, "Continue to load the saved world", 20000);
    result.save.resumedAtTick = resumed.tick;
    if (resumed.tick < saved.tick || resumed.tick > saved.tick + 300) {
      throw new Error("Continue resumed at tick " + resumed.tick + ", not the saved tick " + saved.tick);
    }
    await waitFor((s) => s.frames >= resumed.frames + 30, "30 frames of the loaded world");
    result.framesSinceStart = (await status()).frames - startFrames;

    // A share link: ?seed= with text names a world (WorldSeed hashes
    // "Ironvale" to 1450162717), and the same link always builds it.
    await page.goto(new URL("?seed=Ironvale&start-screen=world&no-saves", url).href, { waitUntil: "load" });
    const linked = await waitFor((s) => s.screen === "WORLD" && s.world && s.instancesDrawn > 0, "the world from a ?seed= link");
    if (linked.seed !== 1450162717) throw new Error("?seed=Ironvale built seed " + linked.seed + ", not 1450162717");
    result.seedLink = { seed: linked.seed };
  }

  const sample = await page.evaluate(() => window.beyondNations.sampleNextFrame());
  result.sample = sample;
  result.frameStats = await page.evaluate(() => window.beyondNations.frameStats());
  result.status = await status();
  // "running" once a key or tap has unlocked WebAudio; recorded, not asserted,
  // since a headless browser may have no audio device.
  result.audio = await page.evaluate(() => window.beyondNations.audio && window.beyondNations.audio());
  await page.screenshot({ path: path.join(out, phone ? "phone-final.png" : "world.png") });
  if (!sample || sample.notClearColour < sample.samples * 0.2 || sample.distinctColours < 4) {
    throw new Error("the world looks blank: " + JSON.stringify(sample));
  }
  if (problems.length) throw new Error(problems.join("\n"));
  result.ok = true;
} catch (error) {
  if (error !== FINISHED) {
    result.error = String(error && error.message ? error.message : error);
    await page.screenshot({ path: path.join(out, "failure.png") }).catch(() => {});
  }
} finally {
  result.problems = problems;
  fs.writeFileSync(path.join(out, "result.json"), JSON.stringify(result, null, 2));
  fs.writeFileSync(path.join(out, "console.log"), log.join("\n") + "\n");
  await browser.close();
}

console.log(JSON.stringify(result, null, 2));
process.exit(result.ok ? 0 : 1);
