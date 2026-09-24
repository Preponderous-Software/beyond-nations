# Contributing Guide

## Getting Started

The only prerequisite is the [.NET SDK](https://dotnet.microsoft.com/download) 8.0 or newer.

```bash
git clone https://github.com/Preponderous-Software/beyond-nations
cd beyond-nations
dotnet build
dotnet test
dotnet run --project src/BeyondNations.Desktop
```

`dotnet build` and `dotnet test` need nothing but the SDK. `dotnet run` additionally needs an OpenGL 3.3 capable driver, which any desktop install has.

### Useful switches

The game takes arguments that make it checkable without a person watching it, which is how the automated checks and most manual verification work:

| Switch | Effect |
| --- | --- |
| `--exit-after-frames N` | render N frames, then exit |
| `--start-screen NAME` | open on `title`, `main-menu`, `config`, `world` or `pause` |
| `--seed N` | fix the world seed, so a run is reproducible |
| `--screenshot-after-frames N` | write a PNG and carry on |
| `--render-stats` | report draw calls, instance counts and frame times on exit |
| `--debug-mode` | open with the F1 overlay already on |
| `--first-person` | open in first person, which `V` otherwise toggles |
| `--no-usage-reporting` | send no usage report this run (see *Usage reporting* in the README) |
| `--help` | list every switch |

### Troubleshooting

- **`dotnet: command not found`** — install the .NET SDK 8.0 or newer.
- **The window does not open over SSH** — the game needs a display. On a headless machine, run it under a virtual framebuffer such as `Xvfb`, or use `--exit-after-frames` to check it starts without needing to see it.
- **`Unable to load shared library 'cimgui'`** — the bundled Dear ImGui native library needs a reasonably current C library. The version pinned here works on Ubuntu 20.04 and newer; if you have changed the `ImGui.NET` version, check that first.

## Layout

```
src/BeyondNations.Core/      the simulation. No graphics, no engine, no packages.
src/BeyondNations.Desktop/   the host: window, renderer, camera, input, UI.
tests/BeyondNations.Core.Tests/
tests/BeyondNations.Desktop.Tests/
docs/
```

The split is the most important convention in the project and is explained in the [Architecture Note](./ARCHITECTURE.md). The short version: **`BeyondNations.Core` must never reference a game engine, a graphics API, or a window.** A continuous integration job fails the build if it does.

If you are unsure which project your change belongs in, ask whether it could be tested with no window open. If yes, it belongs in core.

## Conventions

- Target framework is **net8.0**.
- Methods are `camelCase`, matching the existing code, rather than the usual .NET `PascalCase`. Consistency with the surrounding code wins.
- Four-space indentation, opening brace on the same line.
- The repository has been formatted with [csharpier](https://csharpier.com/) in the past; the tool manifest is in `.config/dotnet-tools.json`. Restore it with `dotnet tool restore` if you want it.
- Prefer a plain class that can be constructed in a test over one that needs a graphics context.

## Tests

Tests are [xUnit](https://xunit.net/), under `tests/`, mirroring the structure of the code they cover.

```bash
dotnet test                                        # everything
dotnet test tests/BeyondNations.Core.Tests         # the simulation only
```

Write tests for what you change. Two things are worth knowing:

- **Assertions must be able to fail.** The suite this replaced used a Unity call that only wrote a note when something was wrong, so it could not fail a build and did not for years. Use `Assert.Equal`, `Assert.True` and friends, which stop the run.
- **Randomness is injected and seedable.** Construct a `RandomSource` with a fixed seed in a test rather than relying on a default, and world generation will produce the same world every time.

## Branching

The model is close to Git Flow. `main` holds the latest stable version. `develop` holds the latest development version and is what pull requests target.

## Pull Requests

1. Branch from `develop`.
2. Make your changes, with tests.
3. Run `dotnet build` and `dotnet test` locally.
4. Push and open a pull request against `develop`.
5. Wait for review. The automated checks must pass before it can be merged.

### Running the checks locally

The checks are the same two commands the pipeline runs, so there is nothing special to reproduce:

```bash
dotnet build BeyondNations.sln --configuration Release
dotnet test  BeyondNations.sln --configuration Release
```

And the guard on the core boundary:

```bash
grep -rn --include='*.cs' -E '^[[:space:]]*using[[:space:]]+UnityEngine' src/BeyondNations.Core
```

See the [CI/CD Documentation](./CI-CD.md) for what the pipeline does with them.

## Issues

If you find a bug or want a feature, open an issue describing it. Bug reports are more useful with the seed (`--seed`) and the switches you ran with, since that makes the world reproducible.
