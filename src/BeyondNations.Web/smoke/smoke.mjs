// Headless Chromium smoke test for the browser build.
//
//   node smoke.mjs <url> <output-dir> [--phone]
//
// Loads the page, waits for the title screen, presses a key (main menu),
// clicks Start and then Begin on the New World screen, waits for the world to
// be drawn and checks that the canvas is not blank, opens Share from the pause
// menu and downloads the replay and the history, saves from the pause menu and
// continues from the main menu, opens a ?seed= link, then fails on any
// console error, page error or failed request.
// Writes title.png, menu.png, world.png and result.json to <output-dir>.
//
// --phone runs the same page as an iPhone 13 (touch, no hover) and checks
// the keyboard notice and the on-screen controls instead of playing.

import { chromium, devices } from "playwright";
import fs from "node:fs";
import path from "node:path";

const url = process.argv[2];
const out = process.argv[3] || "smoke-out";
const phone = process.argv.includes("--phone");
if (!url) {
  console.error("usage: node smoke.mjs <url> <output-dir> [--phone]");
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
  ? { ...devices["iPhone 13"], userAgent: devices["iPhone 13"].userAgent + " headless-monitor" }
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

page.on("console", (message) => {
  const line = `[${message.type()}] ${message.text()}`;
  log.push(line);
  if (message.type() === "error") problems.push("console error: " + message.text());
});
page.on("pageerror", (error) => problems.push("page error: " + error.message));
page.on("requestfailed", (request) => problems.push("request failed: " + request.url() + " " + (request.failure()?.errorText || "")));
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

const result = { url, phone, ok: false };
try {
  const started = Date.now();
  await page.goto(url, { waitUntil: "load" });
  result.loadEventMs = Date.now() - started;
  await waitFor((s) => s.screen === "TITLE" && s.frames >= 5, "the title screen");
  result.titleMs = Date.now() - started;
  result.transferredBytes = await page.evaluate(() =>
    performance.getEntriesByType("resource").reduce((sum, entry) => sum + (entry.transferSize || 0), 0)
    + (performance.getEntriesByType("navigation")[0]?.transferSize || 0));
  result.gl = await page.evaluate(() => window.beyondNations.gl());
  await page.screenshot({ path: path.join(out, "title.png") });

  if (phone) {
    const gateVisible = await page.isVisible("#gate");
    if (!gateVisible) throw new Error("the keyboard notice (#gate) is not shown on a touch-only device");
    await page.screenshot({ path: path.join(out, "phone-gate.png") });
    await page.tap("#gate-continue");
    if (!(await page.isVisible("#touch"))) throw new Error("the on-screen controls did not appear");
    // A tap on the title is the "any key" a touch screen cannot press.
    await page.touchscreen.tap(195, 300);
    await waitFor((s) => s.screen === "MAIN_MENU", "the main menu after a tap");
    const viewport = page.viewportSize();
    await page.touchscreen.tap(viewport.width / 2, viewport.height / 2 + 50);
    // Start opens the New World screen (docs/SHARING.md), whose Begin sits where Start was.
    await waitFor((s) => s.screen === "NEW_WORLD", "the New World screen after tapping Start", 10000);
    await page.waitForTimeout(300);
    await page.screenshot({ path: path.join(out, "phone-new-world.png") });
    if (await page.isVisible('#touch [data-key="KeyW"]')) {
      throw new Error("the touch controls cover the New World screen");
    }
    await page.touchscreen.tap(viewport.width / 2, viewport.height / 2 + 50);
    await waitFor((s) => s.screen === "WORLD" && s.world && s.instancesDrawn > 0, "the world after tapping Begin");
    // A new world opens with the first tip (ui/feel/OnboardingHints).
    await waitFor((s) => s.hint === "move", "the first tip in a new world", 15000);
    await page.screenshot({ path: path.join(out, "phone-tip.png") });
    // Hold the forward pad button for a moment; the game must keep running.
    const forward = await page.$('#touch [data-key="KeyW"]');
    const box = await forward.boundingBox();
    await page.touchscreen.tap(box.x + box.width / 2, box.y + box.height / 2);
    await page.waitForTimeout(500);
    await page.screenshot({ path: path.join(out, "phone-world.png") });
    // The Craft button opens the crafting screen (#47); its Close button (or Escape) closes it.
    const craft = await page.$('#touch [data-key="KeyC"]');
    const craftBox = await craft.boundingBox();
    await page.touchscreen.tap(craftBox.x + craftBox.width / 2, craftBox.y + craftBox.height / 2);
    const crafting = await waitFor((s) => s.screen === "CRAFTING", "the crafting screen after tapping Craft", 10000);
    await waitFor((s) => s.frames >= crafting.frames + 30, "30 frames of the crafting screen");
    await page.screenshot({ path: path.join(out, "phone-crafting.png") });
    // The on-screen controls step aside so they do not cover the recipe rows.
    if (await page.isVisible('#touch [data-key="KeyW"]')) {
      throw new Error("the touch controls still cover the crafting screen");
    }
    await page.keyboard.press("Escape");
    await waitFor((s) => s.screen === "WORLD", "the world after closing crafting", 10000);
    if (!(await page.isVisible('#touch [data-key="KeyW"]'))) {
      throw new Error("the touch controls did not come back after crafting");
    }
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
  result.error = String(error && error.message ? error.message : error);
  await page.screenshot({ path: path.join(out, "failure.png") }).catch(() => {});
} finally {
  result.problems = problems;
  fs.writeFileSync(path.join(out, "result.json"), JSON.stringify(result, null, 2));
  fs.writeFileSync(path.join(out, "console.log"), log.join("\n") + "\n");
  await browser.close();
}

console.log(JSON.stringify(result, null, 2));
process.exit(result.ok ? 0 : 1);
