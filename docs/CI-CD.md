# Continuous Integration

## Overview

Every push and pull request against `main` and `develop` runs [`.github/workflows/ci.yml`](../.github/workflows/ci.yml). It needs **no repository secret**, no licence and no account.

`Build and test` is a required status check on `develop`, so a failing test stops a merge.

## Jobs

### 1. Build and test

```
dotnet restore BeyondNations.sln
dotnet build   BeyondNations.sln --configuration Release --no-restore
dotnet test    BeyondNations.sln --configuration Release --no-build --verbosity normal
```

A failing test fails the job. That is the whole point of it: a project that compiles is not a project that works. The solution holds four test projects (`BeyondNations.Core.Tests`, `BeyondNations.Desktop.Tests`, `BeyondNations.Web.Tests` and `tools/BeyondNations.HistoryExport.Tests`), so the log reports four `Total tests:` counts.

### 2. Core is engine-agnostic

Fails if any `using UnityEngine` appears under `src/BeyondNations.Core`, or if core reaches for the native Strata engine or P/Invoke (`[DllImport]`, `[LibraryImport]`, `NativeLibrary.`, `StrataNative`, `using Strata`). Core hands render items to whoever draws them and never draws anything itself.

`BeyondNations.Core` exists to be buildable and testable with nothing but the .NET SDK. That property is easy to lose by accident — one convenient reference and the simulation can no longer be run in a test — and it costs almost nothing to check, so it is checked rather than trusted.

The check lives in the workflow rather than in the project file, because a build file is the wrong place for a policy.

### 3. Game smoke run (Strata, llvmpipe)

Builds the desktop game and runs it under `xvfb-run` on Mesa's software rasteriser (`LIBGL_ALWAYS_SOFTWARE=1`, `GALLIUM_DRIVER=llvmpipe`):

```
--seed 42 --start-screen world --exit-after-frames 120 --screenshot-after-frames 60 --render-stats --no-usage-reporting
```

The job fails unless the log contains `world renderer: strata`. It uploads the log and the screenshot as the `strata-smoke` artifact. It is the only job in this workflow that actually executes drawing code. It shows that the game starts, draws and exits, but it does not check what the frame looks like.

### 4. History replay (usd-replay/1)

Exports seed 42 twice with `tools/BeyondNations.HistoryExport` (40 000 ticks, a sample every 200), converts each export with `tools/history-to-replay.py`, and validates both with [usd-recorder](https://github.com/Preponderous-Software/usd-recorder)'s `replay validate`. It then asserts that the two replays have 100 samples each with identical state hashes, so the same seed has to produce the same history. It uploads the replay as the `history-replay-seed42` artifact.

### Browser build (`browser.yml`)

[`.github/workflows/browser.yml`](../.github/workflows/browser.yml) publishes `src/BeyondNations.Web` for the browser (it installs the `wasm-tools` workload first), then drives the page in headless Chromium on a desktop and an iPhone 13 profile (`src/BeyondNations.Web/smoke/smoke.mjs`): title, main menu, Start, a drawn world, walking, right-drag look, a short key tap, and no console errors. On a push to `develop` with the `ARCADE_ENABLED` variable set it deploys the site to the arcade. See [WEB.md](./WEB.md).

## Running the checks locally

They are the same commands, so there is nothing to reproduce:

```bash
dotnet build BeyondNations.sln --configuration Release
dotnet test  BeyondNations.sln --configuration Release
grep -rn --include='*.cs' -E '^[[:space:]]*using[[:space:]]+UnityEngine' src/BeyondNations.Core
grep -rn --include='*.cs' -E '\[(DllImport|LibraryImport)|NativeLibrary\.|StrataNative|using[[:space:]]+Strata' src/BeyondNations.Core
```

The two `grep`s should print nothing.

To check the game actually starts, without a person watching a window:

```bash
dotnet run --project src/BeyondNations.Desktop -- --exit-after-frames 120 --seed 7
```

It exits with status 0 when the window, the graphics context and the loop all came up.

On a headless machine, run it under a virtual framebuffer:

```bash
Xvfb :99 -screen 0 1024x768x24 &
DISPLAY=:99 dotnet run --project src/BeyondNations.Desktop -- --exit-after-frames 120
```

## Viewing results

On a pull request, the checks appear at the bottom of the conversation tab; select **Details** on a failed one for its log. Every run is also listed under the repository's **Actions** tab.

## Troubleshooting

**A test fails in the pipeline but passes locally.** Check whether the test depends on randomness without fixing a seed. `RandomSource` is injected precisely so tests can pin it; a test that constructs one without a seed will differ between runs.

**`Project file does not exist`.** The solution or a project file is missing from the commit. `*.sln` and `*.csproj` are deliberately **not** ignored — see the note in `.gitignore` — but check that a new project was actually added and committed.

**`Core is engine-agnostic` fails.** Something under `src/BeyondNations.Core` has taken a dependency on a game engine. The fix is to move the code into `src/BeyondNations.Desktop`, or to express what it needs as data the host reads. See the [Architecture Note](./ARCHITECTURE.md).

## History

This pipeline replaced a Unity one that had **never passed**. Every run failed at `Missing Unity License File and no Serial was found`, for the test job and the build job alike, so no pull request in the project's history had ever been gated by a passing test.

The licence was not the only cause. The test suite also asserted through a Unity call that only logs, so even with a licence it could not have failed a build. Both had to be fixed to get a working gate: see #215 and #214.
