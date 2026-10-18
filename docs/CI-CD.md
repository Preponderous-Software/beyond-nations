# Continuous Integration

## Overview

Every push and pull request against `main` and `develop` runs [`.github/workflows/ci.yml`](../.github/workflows/ci.yml). It needs **no repository secret**, no licence and no account.

`Build and test` is a required status check on `develop`, so a failing test stops a merge.

## Jobs

### 1. Build and test

```
dotnet restore BeyondNations.sln
dotnet build   BeyondNations.sln --configuration Release --no-restore
dotnet test    BeyondNations.sln --configuration Release --no-build
```

A failing test fails the job. That is the whole point of it: a project that compiles is not a project that works.

### 2. Core is engine-agnostic

Fails if any `using UnityEngine` appears under `src/BeyondNations.Core`.

`BeyondNations.Core` exists to be buildable and testable with nothing but the .NET SDK. That property is easy to lose by accident — one convenient reference and the simulation can no longer be run in a test — and it costs almost nothing to check, so it is checked rather than trusted.

The check lives in the workflow rather than in the project file, because a build file is the wrong place for a policy.

## Running the checks locally

They are the same commands, so there is nothing to reproduce:

```bash
dotnet build BeyondNations.sln --configuration Release
dotnet test  BeyondNations.sln --configuration Release
grep -rn --include='*.cs' -E '^[[:space:]]*using[[:space:]]+UnityEngine' src/BeyondNations.Core
```

The last one should print nothing.

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
