# Beyond Nations in the browser

`src/BeyondNations.Web` is the game compiled for the browser: .NET 8
`browser-wasm`, ahead-of-time compiled, with the world drawn by **Strata**,
the same engine as the desktop, built for WebGL 2 and linked into the page. It is published on
the arcade at **https://beyond-nations.play.danielstephenson.dev** and listed at
<https://danielstephenson.dev/play>.

The simulation is `BeyondNations.Core`, unchanged. The desktop host's
window-free code is compiled in from `src/BeyondNations.Desktop` as linked
source, not copied: the input layer (`InputService`, `KeyBindings`,
`PlayerInputController`, `MouseLook`, `PressLatch`), the camera and culler, the
label maths, `GameOptions` and every ImGui screen and info
box. Only what touches a window or OpenGL has a browser counterpart.

| Desktop (`BeyondNations.Desktop`) | Browser (`BeyondNations.Web`) |
| --- | --- |
| `Game` (Silk.NET window, Update/Render) | `game/WebGame.cs` (`requestAnimationFrame`) |
| `SilkInputSource` | `game/BrowserInputSource.cs` (`KeyboardEvent.code` via `game/BrowserKeys.cs`) |
| `NativeWorldRenderer` (Strata over P/Invoke) | `game/StrataWorldRenderer.cs`: the same calls, to Strata linked into `dotnet.native.wasm` (`strata/StrataWeb.cs`, `native/strata.targets`) |
| `WorldLabelRenderer` (FontStashSharp + GL) | `game/WebWorldLabelRenderer.cs` (FontStashSharp + WebGL2) |
| `UiHost` + Silk's `ImGuiController` | `game/WebUiHost.cs` + `host/ImGuiWebBackend.cs` |
| Command-line options | the URL query (`game/WebOptions.cs`) |
| `NullSoundService` (no sound) | `game/WebAudioSoundService.cs`: cues synthesised with WebAudio in `wwwroot/audio.js` |

So the browser shows what the desktop shows: Strata's sun and moon following
the time of day, its sky, cascaded shadows and fog.

## Playing

- Keyboard and mouse, as on the desktop. Hold the **right mouse button** and
  move the mouse to steer: left/right turns the player, so W walks wherever the
  drag points, and up/down tilts the camera (the page takes pointer lock for
  the drag, so the cursor does not wander).
- Text and the user interface are laid out in CSS pixels and drawn at the
  screen's `devicePixelRatio`, so they are the intended size on a high-DPI
  screen rather than shrunk, and the ImGui font is rasterised at that ratio.
- **Phones and tablets** get touch controls; see [Touch controls](#touch-controls).
- What a browser cannot do is changed rather than faked: Exit on the main menu
  shows an "exited" panel (play again, or back to the arcade); F12 downloads a
  PNG; the window size is the page's.

### Loading screen

The page loads behind the Strata web kit's loading shell
(`wwwroot/strata-kit/strata-loader.js`, Strata's `web/kit/strata-loader.js`
vendored at the commit in `strata-kit/PIN`, with its `LICENSE` and
`SHA256SUMS`; `strata-kit/update.sh <full sha>` re-vendors it). The .NET
runtime fetches its own files, so the page shows the shell with
`StrataLoader.show()` and drives its bar with `StrataLoader.progress()`:

- **Backdrop:** the /play trailer's first frame
  (`https://danielstephenson.dev/trailers/beyond-nations.jpg`), blurred.
- **Progress:** downloading is the first 90%, whichever is further of the
  runtime's count of boot files fetched (`onDownloadResourceProgress`) and the
  compressed bytes (`webhost/loader.js`) against the usual 5 MB, with the
  megabytes so far; starting the runtime is 95%; the game's `ready()` fades
  the shell out. The bar never moves back.
- **Fallback:** without WebGL 2, or when the game fails before it is up, the
  trailer plays (the still under `prefers-reduced-motion`) with the reason
  and a link back to the arcade. After the game is up, a failure shows the
  error panel (Reload, Report it) as before.

The controls are in the game's own Controls panel (`/`, or ? on a touch
screen), which used to be repeated on the loading panel.

### Sound

The browser build has sound; the desktop does not yet (see
[SYSTEMS.md](./SYSTEMS.md#sound-feedback-and-tips-host)). `wwwroot/audio.js` is
the `bn-audio` JSImport module (`game/WebAudioSoundService.cs`): every cue is
synthesised with WebAudio oscillators and filtered noise as it plays, and the
ambient loop is looped noise through a low-pass filter with scheduled bird or
cricket chirps. No audio file is fetched. Browsers allow sound only after a
user gesture, so the `AudioContext` is created on the first `keydown`,
`pointerdown` or `touchend`; cues asked for before then are dropped and the
ambient loop starts on that gesture. Volume, mute and the tips are kept in
localStorage under `beyond-nations/feel-settings`.
`window.beyondNations.audio()` reports `locked`, `running` or `unavailable`.

### Touch controls

On a touch screen the page shows its own controls over the world, and the
game lays its screens out for a finger (`TouchLayout` in
`src/BeyondNations.Desktop/ui`, set only by the browser build):

![A phone on its side: the joystick bottom left with its knob pushed up, the Bag, Craft, Realm, News, View, ? and Menu buttons along the bottom, and Jump, Run and Use bottom right, with the objective tracker, season and player boxes of the HUD before the contextual one above them](../.github/media/touch-landscape.png)

| Control | Does |
| --- | --- |
| Joystick (bottom left) | Walk: up forward, down back; sideways turns, as A and D do, as far as it is pushed. A nearly straight push is snapped straight, so walking does not also turn |
| Drag on the world | Steer and tilt the view, like the desktop's right-drag (#309), at twice the angle per pixel, since a thumb's sweep is short |
| Tap on the world | Use whatever is within reach (a tree, rock, chicken, pawn, drop or settlement), as E does. With nothing in reach a tap does nothing, so a short look never answers "No entities within range" |
| Use, Jump, Run (bottom right) | E, Space, and Shift as a switch (tap on, tap off) |
| Bag, Craft, Realm, News, View, ?, Menu | I, C, O (nations, diplomacy and laws), Y (the chronicle: full history, hidden, recent), V, the controls list, Escape |
| HUD command bar | Tappable, and kept above the controls |

- The controls show only while the world is up, and step aside while a world
  event's bottom sheet waits for a choice or the caravan dispatch panel is
  open (their buttons are tapped directly; the panel starts at the top). Every other screen has its
  own buttons: the inventory, nations and crafting screens get a large
  **Close** in the top right, the pause, main menu and settings screens use
  44-pixel buttons from the top of the screen, and the controls list
  starts with these touch controls.
- A drag on a panel (crafting, nations, inventory, the full chronicle,
  the saved worlds) scrolls it; a press is only sent when the finger lifts
  without having travelled, so a drag that starts on a button never presses
  it. A tap on a slider (the settings screen) sets it there.
- The HUD leaves the controls' band clear: the command bar and every chip,
  box and card stop above it (`HudPanels`). Bag opens the full inventory. A
  town's floating name keeps below what the HUD stacks at the top, and is
  left out when that would push it too far down the screen (`LabelPlacement`).
- **The HUD shows only what is needed now** (see [SYSTEMS.md](./SYSTEMS.md#the-world-hud-host)):
  a thin top strip (season and day, icon meters lit only when low or
  changing, and **Journal**; the button row keeps **?** and **Menu**), one
  *Next* line, and chips and boxes only while they matter, every one a tap
  target that opens the Journal and none of them over the command bar or the
  controls. Tips, unlock cards and toasts show one at a time, under the
  middle of the HUD, with touch-sized *Got it* and *No tips*.

  <img src="screenshots/contextual-hud/portrait-world-pair.jpg" alt="A phone held upright in a new world, before and after: before, the era, objective, season and player boxes and the chronicle strip fill the top half; after, a thin strip with Spring 1/2, the energy, food and coin meters and Journal, the Next line, and the tip card" width="420">
  <img src="screenshots/contextual-hud/portrait-autumn-pair.jpg" alt="The last day of autumn on a phone held upright, before and after: after, the winter and food chips and the winter banner under the top strip" width="420">
  <img src="screenshots/contextual-hud/portrait-market-pair.jpg" alt="A market town on a phone held upright, before and after: after, the settlement and market boxes side by side under the Next line" width="420">
  <img src="screenshots/contextual-hud/landscape-world-pair.jpg" alt="A new world on a phone on its side, before and after: after, the top strip, the Next line and the tip, clear of the joystick and the buttons" width="420">
  <img src="screenshots/contextual-hud/landscape-autumn-pair.jpg" alt="Autumn on a phone on its side, before and after" width="420">
  <img src="screenshots/contextual-hud/landscape-market-pair.jpg" alt="A market town on a phone on its side, before and after: after, the settlement and market boxes down the right" width="420">

  <img src="screenshots/journal/phone-goals.jpg" alt="The Journal on a phone held upright, on its Goals tab: the goals as picture tiles" width="180"> <img src="screenshots/journal/phone-nation.jpg" alt="The Journal's Nation tab on a phone held upright" width="180"> <img src="screenshots/journal/landscape-goals.jpg" alt="The Journal's Goals tab on a phone on its side" width="380">

  <img src="screenshots/hud-fixes/before-phone-inside.png" alt="Before: inside Bigburg on a phone held upright, a tall Market Info box covers the Next line and the tip, beside the Settlement Info box" width="180"> <img src="screenshots/hud-fixes/after-phone-inside.png" alt="After: the settlement and market boxes side by side under the Next line, the market box short, and the tip clear below them" width="180"> <img src="screenshots/hud-fixes/after-phone-inside-tall-bar.png" alt="After, with a taller command bar: the boxes still fit above it" width="180">
- The command bar takes the rows left between the HUD above it and the
  controls, across the whole width, and when it has more buttons than that
  it shows the first page of them beside a **More** button at its bottom
  left, which opens the rest in a sheet (below, #512). Inside a settlement on
  a phone held upright the settlement and market boxes sit side by side
  under the Next line, and the market lists fewer prices when room is short,
  so neither runs over the bar; on its side they go down the right, as on a
  desktop. Chronicle news comes as toasts, one at a time; News opens the full
  chronicle (#466). The pictures below are from #512, before the contextual
  HUD.

  <img src="screenshots/phone-market-command-bar/phone-before.jpg" alt="Before: a phone held upright in a market town, the command bar running under the objective, chronicle and season boxes, and the market box under the touch controls" width="180"> <img src="screenshots/phone-market-command-bar/phone-after-more.jpg" alt="After: the same town, the bar below the season box with More 2/8 and Trade at Market, the right-hand column only the player box" width="180"> <img src="screenshots/phone-market-command-bar/phone-trader-pawn-page.jpg" alt="The last page of the bar beside a serf: Order Ursula Williams: Gather Wood, and Appoint Ursula Williams Trader greyed, the one trader slot being taken" width="180">
  <img src="screenshots/phone-market-command-bar/landscape-before.jpg" alt="Before: a phone on its side in a market town, the command bar under the era, objective and chronicle boxes and the market box under Jump, Run and Use" width="380"> <img src="screenshots/phone-market-command-bar/landscape-after.jpg" alt="After: the bar between the box columns under the chronicle strip, with More 1/5, Purchase Food, Sell Resources and Trade at Market, and the Season and Player boxes clear of the controls" width="380">
- A line too wide for the screen (a panel footer on a phone held upright) is
  drawn smaller rather than cut off at both edges.
- The nations screen sizes its text by the screen's width as well as its
  height (`Overlay.textHeightFor`: a phone held upright counts as 1.3 times
  its width high), so its lines wrap whole instead of being cut. Each tab's
  list ends above a band at the foot of the screen that holds the footer and
  up to three lines of status, which grow up from the footer. A header with
  no room under it for the first row (a phone on its side) scrolls with the
  list, in front of its rows, so every row's buttons can be reached (#467,
  #506, #511). Every button on it, the caravan panel's and the market
  panel's are at least 44 pixels tall (#469); on a wide screen its tabs sit
  beside the corner Close rather than under it.
- A controls list too narrow for its two columns wraps each row into them
  (#468). The Season and player boxes that #465 and #512 fitted clear of the
  joystick and the bar are gone; the top strip and the chips that replaced
  them keep clear of both through `HudPanels`.

  <img src="screenshots/phone-touch-layout/portrait-nations-pair.jpg" alt="The Realm screen's Nations tab on a phone held upright, before and after: before, the text is cut off, the status line is printed over the nation list and the rows run off the bottom; after, the header wraps whole and both nations show with Tribute, Ally, Peace, War and Denounce" width="420">
  <img src="screenshots/phone-touch-layout/landscape-nations-pair.jpg" alt="The Nations tab on a phone on its side, before and after: before, the status line is printed over the nation rows; after, the header scrolls with the list under Up and Down, and Close sits in the corner beside the tabs" width="560">
  <img src="screenshots/phone-touch-layout/portrait-members-pair.jpg" alt="The Members tab on a phone held upright, before and after: before, the text is so large the header runs into the rows and only two members fit; after, it is sized by the width and all three members show" width="420">
  <img src="screenshots/phone-touch-layout/landscape-members-pair.jpg" alt="The Members tab on a phone on its side, before and after" width="560">
  <img src="screenshots/phone-touch-layout/portrait-laws-pair.jpg" alt="The Laws tab on a phone held upright, before and after" width="420">
  <img src="screenshots/phone-touch-layout/landscape-laws-pair.jpg" alt="The Laws tab on a phone on its side, before and after" width="560">
  <img src="screenshots/phone-touch-layout/portrait-manage-pair.jpg" alt="The Manage tab on a phone held upright, before and after" width="420">
  <img src="screenshots/phone-touch-layout/landscape-manage-pair.jpg" alt="The Manage tab on a phone on its side, before and after" width="560">
  <img src="screenshots/phone-touch-layout/portrait-controls-pair.jpg" alt="The controls list on a phone held upright, before and after: after, each row wraps into the two columns" width="420">
  <img src="screenshots/phone-touch-layout/portrait-world-pair.jpg" alt="A new world on a phone held upright, before and after: after, the era box reads Era: Camp (1/4) in full and the objective hint fits on two lines" width="420">
  <img src="screenshots/phone-touch-layout/landscape-world-pair.jpg" alt="The world on a phone on its side, before and after the touch-layout fixes" width="560">
  <img src="screenshots/phone-touch-layout/portrait-market-town-pair.jpg" alt="A market town on a phone held upright, before and after" width="420">
  <img src="screenshots/phone-touch-layout/desktop390-market-town-pair.jpg" alt="A market town in a desktop browser window 390 pixels wide, before and after" width="420">

  An earlier pass on the same screens on a phone on its side (844x390), the
  text drawn whole and at a readable size instead of printed over itself or
  shrunk:

  <img src="screenshots/leftovers/landscape-nations-844x390-pair.jpg" alt="The Nations tab at 844x390, before and after: before, the Tribute and Each dealing lines are printed over each other; after, each wraps whole" width="380"> <img src="screenshots/leftovers/landscape-members-844x390-pair.jpg" alt="The Members tab at 844x390, before and after" width="380">
  <img src="screenshots/leftovers/landscape-manage-844x390-pair.jpg" alt="The Manage tab at 844x390, before and after" width="380"> <img src="screenshots/leftovers/landscape-settlement-844x390-pair.jpg" alt="The settlement screen at 844x390, before and after: before, its lines are tiny and clipped; after, Ownership, People and Buildings read at full size above Scroll for more" width="380">
- A phone's command bar keeps one page (#512). The buttons used most where
  the player stands come first (`WorldCommandBar.frontFirst`): in another
  nation's town Trade at Market, Purchase Food and Sell Resources; in their
  own settlement Feed Residents, Settlement and Exit Settlement; out in the
  world Enter Settlement and Teleport Home. More, at the bar's bottom left,
  opens a sheet over the HUD with every other button under headings in the
  bar's group order (`CommandSheet`), the ones that cannot be undone last and
  still asking twice; Close takes More's place. Every sheet button waits the
  same half second as the bar's before it takes a tap (`ButtonSettle`), and
  the sheet says why a greyed one cannot be used. The desktop's bar, which
  is never paged, is unchanged.

  <img src="screenshots/command-bar-grouping/portrait-market-town-pair.jpg" alt="A phone held upright in a market town: before, More 1/3 with the town's buttons spread over three pages; after, one page with More, Trade at Market, Purchase Food, Sell Resources, Exit Settlement and Settlement, and More opening a sheet with the foreign nation's buttons, Talk, Gift and Invite for the person in reach, the tools and Eat, and Leave Nation, Denounce and Declare War in red at the bottom" width="560">
  <img src="screenshots/command-bar-grouping/portrait-camp-pair.jpg" alt="A phone held upright in the player's own camp: before, More 1/6 with Collect Stockpile, Exit Settlement and Withdraw Funds; after, More with Feed Residents, Settlement and Exit Settlement, and the sheet with Nation, This place, Build, Trade and other nations, Tools and food, and Cannot be undone" width="560">
  <img src="screenshots/command-bar-grouping/landscape-market-town-pair.jpg" alt="The same market town on a phone on its side, before and after: the town's buttons on one page beside More" width="560">
  <img src="screenshots/command-bar-grouping/landscape-camp-pair.jpg" alt="The player's own camp on a phone on its side, before and after: Feed Residents, Settlement and Exit Settlement first, the rest under More" width="560">
- The inventory box, which #513 left out of a window too narrow for a second
  column of boxes (390 wide), is no longer drawn at all: the *Carrying* chip
  shows what the player holds, and the Journal's Inventory tab and the
  inventory screen list it all. The pictures below are from #513.

  <img src="screenshots/phone-ui-leftovers/phone-town-pair.jpg" alt="A phone held upright in a town, before and after: before, the tip card covers Enter Settlement and Teleport Home and the town's name is cut off at the top behind the boxes; after, the Season box is compact, the tip sits over the chronicle strip, the command bar is clear, and the name is left out" width="420">
- No line names a key or a mouse button: objective hints, status lines, panel
  footers, the chronicle and the tips name the button instead ("press Use",
  "Realm", "Tap Close"), all from one table (`ObjectiveKeyNames`), and the
  controls list puts the keyboard's rows under "Keyboard:" headings.
- The UI stays inside the screen's safe area (`env(safe-area-inset-*)`), clear
  of notches and rounded corners; the world still fills the whole screen.
  ImGui's widgets get more padding and ImGui's `TouchExtraPadding`.
- The page never scrolls, zooms, bounces, refreshes on a pull or opens a
  long-press menu while playing (`touch-action`, `overscroll-behavior` and a
  few listeners for what iOS Safari does regardless).
- Both orientations work. On its side the panel buttons sit between the
  joystick and the action buttons; held upright they take two rows above
  them, and a hint (once, dismissable) suggests turning the phone for a wider
  view.
- Text is laid out in CSS pixels and rasterised at the screen's
  `devicePixelRatio`, so it is as sharp as the screen.

**When they show**: the setting is *Auto*, *On* or *Off*. Auto turns the
touch layout on for a device whose main pointer is a finger, and on any
device the moment a finger touches the screen; a desktop used with a mouse
never sees it, and nothing about the desktop build changes. The setting is
on the pause screen and the settings screen (the button cycles Auto, On,
Off), is remembered in `localStorage` (`beyond-nations/touch-controls`), and
`?touch=on|off|auto` overrides it for one visit.

### URL options

The desktop's command-line options work as URL query parameters with the same
names, so `?ticks-per-second=20&seed=42&start-screen=world&debug-mode` is
`--ticks-per-second 20 --seed 42 --start-screen world --debug-mode`.

| Parameter | Meaning |
| --- | --- |
| `ticks-per-second=N` | simulation rate (default 50) |
| `seed=N` or `seed=word` | world seed: a number, or any text, hashed to one exactly as the New World screen does ([SHARING.md](./SHARING.md)); the New World screen opens with it filled in. A share link is `?seed=N`. |
| `start-screen=title\|main-menu\|config\|world\|pause\|inventory\|load\|new-world\|share` | first screen |
| `continue` | open in the most recently saved world |
| `no-saves` | do not save or offer saved worlds this run |
| `render-distance=N` | starting render distance |
| `time-of-day=F` | starting time of day, 0 to 1 |
| `stage-feel` | replay the feedback effects every 20 frames, for screenshots |
| `stage-settlement` | open with a lived-in settlement standing ahead of the player, as the desktop's `--stage-settlement` |
| `stage-outpost` | open leading a Village-era nation with two settlements |
| `stage-diplomacy`, `stage-succession`, `stage-event`, `stage-trade`, `stage-market`, `stage-camp`, `stage-caravan`, `stage-victory`, `stage-defeat` | the desktop's other staged openings ([CONTRIBUTING.md](./CONTRIBUTING.md)) |
| `stage-journal` or `stage-journal=TAB` | open on the Journal over the world, on `goals`, `chronicle`, `era`, `inventory` or `nation`; `TAB:N` also opens the N-th tile's card, for screenshots |
| `advance-ticks=N` | run the world N ticks so it opens mid-game, at most 50,000 (a larger number is lowered to that, with a warning). Unlike the desktop, which runs them before its first frame, the page runs them in slices of 200 ms a frame and draws the world fast-forwarding meanwhile, so it stays responsive; `window.beyondNations.status().advancing` counts the ticks left. As on the desktop, the player sits it out: they are kept fed, come in at full energy, and their reign starts when they do (#490). `?stage-settlement&season=autumn&advance-ticks=22000` reaches the winter warning. |
| `season=NAME` | start on the first day of `spring`, `summer`, `autumn` or `winter` |
| `debug-mode`, `first-person`, `no-labels`, `no-culling`, `render-stats` | switches (`=1`/`=true` also work; `=0`/`=false` leave them off) |
| `exit-after-frames=N`, `screenshot-after-frames=N` | for automated runs |
| `touch=on\|off\|auto` | touch controls (read by the page; see above) |

Anything unknown or malformed is dropped with a console warning. (On the
desktop a bad option ends the process; in a page that would take the runtime
down with it, so nothing unusable reaches `GameOptions.parse`.)

The warning the day before winter in the browser, `?no-saves&seed=42&start-screen=world&stage-settlement&season=autumn&advance-ticks=22000` (the `phone-staged` and `desktop-staged` shots in `tools/capture/web.json`):

<img src="./screenshots/web-phone-shotlist/desktop-winter-warning.jpg" alt="The browser build at 1280x720 the night before winter: a banner says winter comes tomorrow and that 9 of Southernland's 17 members are short of food, and the Season box reads Autumn, day 2 of 2, Winter in 1 day" width="560"> <img src="./screenshots/web-phone-shotlist/phone-winter-warning.jpg" alt="The same warning on a phone held upright" width="180">

### Sharing

Share on the pause screen ([SHARING.md](./SHARING.md)) uses the page:
*Share link* opens the system share sheet (`navigator.share`) on a touch
device that has one and copies to the clipboard everywhere else; the replay
and history exports download, as F12's screenshot does; and the New World
screen's seed field asks with `window.prompt`, because a canvas cannot open a
phone's keyboard. The smoke test downloads both exports from the Share screen
and opens a `?seed=Ironvale` link.

### Saves

Worlds are saved to the browser's `localStorage`, under keys starting
`beyond-nations/saves/`, gzipped and base64-encoded (the arcade's games share
one origin and its roughly 5 MB of storage). The game autosaves every couple
of minutes of play, when the player leaves the world, and when the tab is
hidden. Private windows and browsers with site data turned off may not keep
them; a save that cannot be written says so on the pause screen. See
[SAVES.md](./SAVES.md).

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

`src/BeyondNations.Web/global.json` selects a .NET 8 SDK for that directory
(the workload is per SDK band; with a newer SDK it would be `wasm-tools-net8`),
so run the workload install and the publish from there if newer SDKs are
installed.

`BeyondNations.Web` is deliberately **not** in `BeyondNations.sln`, so
`dotnet build` and `dotnet test` on the solution still need nothing but the .NET
SDK. Its plain-C# parts (the URL options, the key table and the browser input
source) are covered by `tests/BeyondNations.Web.Tests`, which is in the solution
and runs in `Build and test`.

- **Strata**: `native/strata.targets` pins the commit of a Strata release
  (`v0.7.0`), checks the source tarball's SHA-256 and runs
  `native/build-strata.sh`, which builds it with the **workload's own
  Emscripten (3.1.34)**, not the newer one Strata's CI uses, so the objects
  link with .NET's `wasm-ld`. The release's own
  `strata-<version>-wasm32-emscripten.tar.gz` is not used: it is built with
  emsdk 6.0.11 and has SDL3 merged into `libstrata.a`, which the 3.1.34
  toolchain cannot link. The source build is configured with
  `-DSTRATA_WITH_SDL=OFF` (the page owns the canvas) and `-DSTRATA_WASM_EXCEPTIONS=wasm` (the mode .NET 8 links the
  module with). It needs CMake 3.24 or newer (`STRATA_CMAKE=/path/to/cmake`
  if the one on PATH is older). The archive is linked as `strata.a`, the name
  `[DllImport("strata")]` resolves to. `native/bnweb_gl.c` makes the
  canvas's WebGL 2 context known to Emscripten, which Strata's
  `STRATA_SURFACE_HOST_CONTEXT` requires; the labels and Dear ImGui are then
  drawn by `webhost/gfx.js` in the same context.
- **AOT**: on by default for publish. With the WebGL2 renderer used before Strata the
  AOT build spent 2 to 4 ms of main-thread time a frame at the 95th
  percentile on a seed-42 walk, the interpreter 6 to 12 ms. With Strata,
  headless Chromium's software GPU (SwiftShader) is the bottleneck (about 9
  fps at 1280x720 with Strata's default four 2048-pixel shadow cascades,
  AOT or not), so frame times measured there say nothing about a real GPU.
  `/p:RunAOTCompilation=false` builds the interpreter version in about half
  the time, which is handy when only the page is changing.
- **Size**: the first load is about 3.6 MB (the runtime and assemblies are
  fetched gzip-compressed and inflated in the browser by
  `webhost/loader.js`, because the arcade serves files as they are).
- **cimgui**: ImGui.NET's native half has no browser build, so
  `native/cimgui.targets` downloads the exact cimgui and Dear ImGui commits
  ImGui.NET 1.90.8.1 was built from (pinned by SHA-256), compiles them with the
  workload's Emscripten and links them into `dotnet.native.wasm`.
  Its printf-style functions cannot be called from here; see the next section.
- `WasmStripILAfterAOT` stays off: with it, .NET 8's runtime fails to start
  this app. If an AOT publish ever fails at start-up with an interpreter
  assertion, delete `src/BeyondNations.Web/obj` and `bin` first: incremental
  AOT builds after a settings change have produced a broken runtime here.

### Variadic ImGui functions

Don't call `ImGui.TextColored`, `ImGui.Text`, `ImGui.TextWrapped`,
`ImGui.TextDisabled`, `ImGui.BulletText`, `ImGui.LabelText`,
`ImGui.SetTooltip`, `ImGui.SetItemTooltip` or `ImGui.LogText` in code the
browser build compiles. That covers every screen under
`src/BeyondNations.Desktop/ui`.
Their C functions are variadic (`igTextColored(ImVec4 col, const char* fmt, ...)`),
but ImGui.NET P/Invokes them with a fixed argument list (`(Vector4, byte*)`).
On x64 the extra argument is simply never read. On WebAssembly a variadic
function's signature has an extra parameter, a pointer to the varargs buffer,
so the call fails the signature check. The first frame that reaches it ends the
page with `RuntimeError: null function or function signature mismatch`, and the
stack has no managed frames to show where.

Use these instead:

- `Overlay.textColoured(colour, text)` in place of `TextColored`;
- `ImGui.TextUnformatted(text)` in place of `Text`;
- `TextWrap.wrap` with one `TextUnformatted` per line, or
  `ImGui.PushTextWrapPos(0f)`, then `TextUnformatted`, then `PopTextWrapPos()`,
  in place of `TextWrapped`.

Calls with a fixed signature are fine, even ones that pass a struct by value.
`PushTextWrapPos`, `ProgressBar`, `PushStyleColor` (either overload),
`SetNextWindowBgAlpha`, `BeginChild`, `GetScrollY` and `SetScrollHereY` all
work in the browser. They were checked with the objective tracker and the full
chronicle view. `TestNoVariadicImGuiCalls` (in
`tests/BeyondNations.Desktop.Tests`) fails the desktop build if a variadic call
appears in the shared source.

To name the function behind any wasm trap, publish with a symbol map:
`dotnet publish ... -p:WasmEmitSymbolMap=true -p:WasmNativeStrip=false`. The
browser's stack trace then names each frame. For this trap the top frame was
`aot_wrapper_..._ImGuiNative__igTextColored_pinvoke_void_cls11_Numerics_dVector4_cl7_byte_2a_`.

## Tests

`.github/workflows/browser.yml` publishes the site, then
`src/BeyondNations.Web/smoke/smoke.mjs` drives it in headless Chromium
(Playwright, WebGL2 on SwiftShader):

- desktop profile: the title screen appears, a key opens the main menu, Start
  builds a world, the world is drawn (pixels sampled from the frame, not the
  clear colour), W walks, a right-drag steers (the player turns exactly 9
  degrees and the camera tilts 1.5 for a 60 by 10 pixel drag, with no camera
  yaw offset) and a plain move does not (#291), the first tip of a new world
  shows (`hint` in the status is `move`), a key
  tap shorter than a frame toggles the view (#290), and there are no console
  errors, page errors or failed requests;
- phone profiles, held upright (390x844) and on its side (844x390), with
  touch: no controls over the title, a tap leaves it, Start is tapped, the
  touch layout comes up with its band reported to the game (and, upright,
  the rotate hint, dismissed by a tap); the joystick pushed up walks the
  player without turning and pushed right turns them right; a 60 pixel drag
  on the world turns the player exactly 18 degrees; a tap on the world with
  nothing in reach does nothing; Bag, Craft and Realm open their panels,
  the controls step aside and the corner Close brings the world back; a drag
  on the crafting list scrolls it; News cycles the chronicle; ? opens the
  controls list; Menu pauses and the touch Resume resumes; the first tip shows; and the
  page has neither scrolled nor zoomed. On its side, a staged world event
  (`?stage-event`) hides the controls until a tap on a choice answers it.

Both profiles also check the loading screen: Strata's shell is up while the
game loads, its bar only moves forward, and it is gone (and lets taps through)
once the title screen is drawn. `--no-webgl2` instead runs the page with
WebGL 2 taken away and checks that the shell shows the trailer with the reason
and that none of the runtime is downloaded.

Screenshots and the console log are uploaded as the `web-smoke` artifact.

```bash
cd src/BeyondNations.Web/smoke && npm ci && npx playwright install chromium
node smoke.mjs http://localhost:8080/ out            # desktop
node smoke.mjs http://localhost:8080/ out --phone              # phone, upright
node smoke.mjs http://localhost:8080/ out --phone-landscape    # phone, on its side
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

- `webhost/host.js`: `startWebHost({ canvas, ui, assembly, exportsName, modules })`
  boots the runtime, registers the `webhost` JSImport module (and any of the
  game's own, such as Beyond Nations' `bn-audio`), forwards input and runs
  the frame clock;
- `webhost/gfx.js`: the WebGL2 passes drawn over the engine's world
  (textured labels, Dear ImGui draw lists), driven by pointers into wasm
  memory so frame data is never copied through interop;
- `webhost/input.js`, `canvas.js`, `loader.js`, `touch.js`: keyboard by
  `KeyboardEvent.code`, right-drag pointer-lock look and touch drags (look,
  scroll or pointer, as the page decides), `devicePixelRatio`-aware sizing,
  the gzip loader, the joystick, key buttons, safe-area insets and gesture
  blocking;
- `host/WebHost.cs`: the C# side (`WebGpu`, `WebPage`, the `IWebGame` a game
  implements, the `[JSExport]`s); `host/ImGuiWebBackend.cs`: Dear ImGui for the
  browser.

A game supplies an `IWebGame` and its page, and links Strata the way
`native/strata.targets` and `native/bnweb_gl.c` do here.
