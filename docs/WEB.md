# Beyond Nations in the browser

`src/BeyondNations.Web` is the game compiled for the browser: .NET 8
`browser-wasm`, ahead-of-time compiled, drawing with WebGL2. It is published on
the arcade at **https://beyond-nations.play.danielstephenson.dev** and listed at
<https://danielstephenson.dev/play>.

The simulation is `BeyondNations.Core`, unchanged. The desktop host's
window-free code is compiled in from `src/BeyondNations.Desktop` as linked
source, not copied: the input layer (`InputService`, `KeyBindings`,
`PlayerInputController`, `MouseLook`, `PressLatch`), the camera and culler, the
primitive meshes, the label maths, `GameOptions` and every ImGui screen and info
box. Only what touches a window or OpenGL has a browser counterpart.

| Desktop (`BeyondNations.Desktop`) | Browser (`BeyondNations.Web`) |
| --- | --- |
| `Game` (Silk.NET window, Update/Render) | `game/WebGame.cs` (`requestAnimationFrame`) |
| `SilkInputSource` | `game/BrowserInputSource.cs` (`KeyboardEvent.code` via `game/BrowserKeys.cs`) |
| `SilkWorldRenderer`, `PrimitiveBatch`, `InstancedPrimitiveShader` | `game/WebGl2WorldRenderer.cs` + `wwwroot/webhost/gfx.js` |
| `WorldLabelRenderer` (FontStashSharp + GL) | `game/WebWorldLabelRenderer.cs` (FontStashSharp + WebGL2) |
| `UiHost` + Silk's `ImGuiController` | `game/WebUiHost.cs` + `host/ImGuiWebBackend.cs` |
| Command-line options | the URL query (`game/WebOptions.cs`) |

The shaders are the desktop's with `#version 330 core` replaced by
`#version 300 es` and a precision line; the meshes, instance layout, light,
ambient and clear colour are the same. A seed-42 world drawn by both differs in
about 1% of pixels, the edges antialiasing touches.

## Playing

- Keyboard and mouse, as on the desktop. Hold the **right mouse button** and
  move the mouse to look around (the page takes pointer lock for the drag, so
  the cursor does not wander).
- Text and the user interface are laid out in CSS pixels and drawn at the
  screen's `devicePixelRatio`, so they are the intended size on a high-DPI
  screen rather than shrunk, and the ImGui font is rasterised at that ratio.
- **Touch-only devices** (phones, tablets without a keyboard) get a notice that
  the game is played with a keyboard and mouse, and can then carry on with
  basic on-screen controls: a pad for W/A/S/D, buttons for interact, jump, run,
  inventory, view and the menu, and a drag on the world to look around. A tap
  on the title screen counts as the "any key" it asks for. The HUD's command
  buttons are tappable. This is a first pass, not a designed touch scheme; the
  command bar is cramped on a narrow portrait screen.
- What a browser cannot do is changed rather than faked: Exit on the main menu
  shows an "exited" panel (play again, or back to the arcade); F12 downloads a
  PNG; the window size is the page's.

### URL options

The desktop's command-line options work as URL query parameters with the same
names, so `?ticks-per-second=20&seed=42&start-screen=world&debug-mode` is
`--ticks-per-second 20 --seed 42 --start-screen world --debug-mode`.

| Parameter | Meaning |
| --- | --- |
| `ticks-per-second=N` | simulation rate (default 50) |
| `seed=N` | world seed |
| `start-screen=title\|main-menu\|config\|world\|pause\|inventory` | first screen |
| `render-distance=N` | starting render distance |
| `time-of-day=F` | starting time of day, 0 to 1 |
| `debug-mode`, `first-person`, `no-labels`, `no-culling`, `render-stats` | switches (`=1`/`=true` also work; `=0`/`=false` leave them off) |
| `exit-after-frames=N`, `screenshot-after-frames=N` | for automated runs |
| `renderer=silk\|native` | `native` is not available in the browser yet (see below); it logs a warning and uses silk |

Anything unknown or malformed is dropped with a console warning. (On the
desktop a bad option ends the process; in a page that would take the runtime
down with it, so nothing unusable reaches `GameOptions.parse`.)

## Usage reporting: off

The browser build sends **no usage report**. The desktop build's one `startup`
event (`telemetry/UsageReporting.cs`, see "Usage reporting" in the README) is
not compiled into it, and nothing else in the page contacts trace or any other
service: the page only fetches its own files.

Why off rather than the desktop's disclosed opt-out: the arcade already counts
page loads for every game, without cookies or identifiers, so a second count
would add nothing; a browser has no `settings.json`, console line or
environment variable through which the desktop's notice and one-step opt-out
are given, and a substitute would have to be a cookie banner; and a trace
write key compiled into a public page would be anyone's to use.

## Building

```bash
dotnet workload install wasm-tools        # once; brings the Emscripten the build needs
dotnet publish src/BeyondNations.Web/BeyondNations.Web.csproj -c Release -o web-publish
python3 -m http.server 8080 --directory web-publish/wwwroot
# open http://localhost:8080/?seed=42
```

`BeyondNations.Web` is deliberately **not** in `BeyondNations.sln`, so
`dotnet build` and `dotnet test` on the solution still need nothing but the .NET
SDK. Its plain-C# parts (the URL options, the key table and the browser input
source) are covered by `tests/BeyondNations.Web.Tests`, which is in the solution
and runs in `Build and test`.

- **AOT**: on by default for publish. On a walk through a seed-42 world in
  headless Chromium the AOT build spends 2 to 4 ms of main-thread time a frame
  at the 95th percentile; the interpreter spends 6 to 12 ms and climbs as the
  world grows. `/p:RunAOTCompilation=false` builds the interpreter version in
  about half the time, which is handy when only the page is changing.
- **Size**: the first load is about 3.4 MB (the runtime and assemblies are
  fetched gzip-compressed and inflated in the browser by
  `webhost/loader.js`, because the arcade serves files as they are).
- **cimgui**: ImGui.NET's native half has no browser build, so
  `native/cimgui.targets` downloads the exact cimgui and Dear ImGui commits
  ImGui.NET 1.90.8.1 was built from (pinned by SHA-256), compiles them with the
  workload's Emscripten and links them into `dotnet.native.wasm`.
- `WasmStripILAfterAOT` stays off: with it, .NET 8's runtime fails to start
  this app. If an AOT publish ever fails at start-up with an interpreter
  assertion, delete `src/BeyondNations.Web/obj` and `bin` first: incremental
  AOT builds after a settings change have produced a broken runtime here.

## Tests

`.github/workflows/browser.yml` publishes the site, then
`src/BeyondNations.Web/smoke/smoke.mjs` drives it in headless Chromium
(Playwright, WebGL2 on SwiftShader):

- desktop profile: the title screen appears, a key opens the main menu, Start
  builds a world, the world is drawn (pixels sampled from the frame, not the
  clear colour), W walks, a right-drag looks (exactly 9 degrees of yaw and 1.5
  of pitch for a 60 by 10 pixel drag) and a plain move does not (#291), a key
  tap shorter than a frame toggles the view (#290), and there are no console
  errors, page errors or failed requests;
- iPhone 13 profile: the keyboard notice appears, the on-screen controls come
  up, a tap leaves the title, Start can be tapped and the world is drawn.

Screenshots and the console log are uploaded as the `web-smoke` artifact.

```bash
cd src/BeyondNations.Web/smoke && npm ci && npx playwright install chromium
node smoke.mjs http://localhost:8080/ out            # desktop
node smoke.mjs http://localhost:8080/ out --phone    # iPhone 13
```

`window.beyondNations.status()` in the browser console shows the same summary
the test reads (screen, frames, what was drawn, look angles, camera mode).

## Deploying

On a push to `develop`, when the repository variable `ARCADE_ENABLED` is
`true`, the same workflow uploads the site to the arcade with
`Stephenson-Software/arcade-deploy` as version `<GameVersion>+g<sha>` (the
`ARCADE_TOKEN` secret is the game's upload token; the slug `beyond-nations` is
registered in the gateway's `config/arcade/games.yaml`). `workflow_dispatch`
deploys by hand.

## The web host, for other games

`wwwroot/webhost/` and `host/` know nothing about Beyond Nations, so another
.NET game of the Strata family can reuse them:

- `webhost/host.js`: `startWebHost({ canvas, ui, assembly, exportsName })`
  boots the runtime, registers the `webhost` JSImport module, forwards input
  and runs the frame clock;
- `webhost/gfx.js`: the WebGL2 device (instanced primitives, textured labels,
  Dear ImGui draw lists), driven by pointers into wasm memory so frame data is
  never copied through interop;
- `webhost/input.js`, `canvas.js`, `loader.js`, `touch.js`: keyboard by
  `KeyboardEvent.code`, right-drag pointer-lock look, `devicePixelRatio`-aware
  sizing, the gzip loader, on-screen controls;
- `host/WebHost.cs`: the C# side (`WebGpu`, `WebPage`, the `IWebGame` a game
  implements, the `[JSExport]`s); `host/ImGuiWebBackend.cs`: Dear ImGui for the
  browser.

A game supplies an `IWebGame`, its page and its own renderer on top of `WebGpu`.

### `--renderer native` in the browser (follow-up)

The desktop's `--renderer native` draws through Strata over P/Invoke. Strata's
WebGL2/Emscripten backend is being built separately; once it exists, the
browser equivalent is a second `IWorldRenderer` in `BeyondNations.Web` that
calls Strata linked into `dotnet.native.wasm` as a `NativeFileReference` (the
way cimgui is linked here), selected by the same `renderer=native` option that
today falls back to silk in `WebGame`'s constructor. Nothing else in the host
needs to change.
