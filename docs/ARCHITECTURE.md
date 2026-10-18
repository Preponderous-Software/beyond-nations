# Architecture

Beyond Nations is two projects with one rule between them.

```
src/BeyondNations.Core/      the simulation
src/BeyondNations.Desktop/   the host
```

**The simulation knows nothing about how it is drawn.** `BeyondNations.Core`
references no game engine, no graphics API, no window, and in fact no NuGet
package at all. It builds and its tests run with nothing but the .NET SDK.

Everything to do with a screen — the window, the OpenGL context, the renderer,
the camera, the input devices, the user interface, screenshots — lives in
`BeyondNations.Desktop`.

## Why the split exists

This project exists partly as a reference for people learning to build games,
and the split is the most instructive thing in it, so it is worth explaining
rather than just stating.

The game used to be built the ordinary way: every entity owned the object that
drew it. A tree, on construction, created a cylinder and a cube, set their
colours and positioned them. That is a natural way to write a game and it has
one consequence that is easy to miss until it hurts.

**You could not create a tree without a running graphics engine.** Not because
a tree is graphical, but because the code that knew what a tree *was* had been
mixed together with the code that knew how a tree *looked*.

Everything followed from that. The tests could not run without a licensed
editor, because constructing a tree meant constructing graphics objects. Market
pricing, pawn behaviour, inventories and nation naming — none of which has
anything to do with drawing — could not be exercised without an engine.
Continuous integration therefore needed a paid licence, and never got one, so
nothing was ever checked.

After the split, a tree records that it is a cylinder of a certain height with a
cube of leaves above it, in a certain colour. That is data. It can be created,
inspected, saved, and tested with no window open anywhere.

## How a frame works

The host drives everything; the simulation never calls into the host.

1. The host reads input and hands the player its movement intent.
2. The host advances the simulation by a fixed number of steps, so behaviour
   ticks at the same rate regardless of frame rate.
3. The host asks the simulation for a **snapshot**: a flat list of primitives
   and labels to draw, in world space.
4. The host culls what the camera cannot see, fills its instance buffers, and
   draws.

Step 3 is the seam. `WorldSnapshot` is the entire vocabulary the host needs:

```csharp
struct RenderItem { PrimitiveKind kind; Vector3 position; Vector3 scale; Rgba color; }
struct LabelItem  { Vector3 position; string text; }
```

The simulation never holds a texture, a buffer or a window handle, so it has
nothing to release. Deleting an entity is removing it from a repository; it
stops appearing in the next snapshot, and that is all deletion means.

## The rule, enforced

A continuous integration job fails the build if `using UnityEngine` appears
under `src/BeyondNations.Core`. The engine it names is gone, but the check is
the cheap way of stating the principle: **if core ever needs a graphics
reference, something has been put in the wrong project.**

The test for where code belongs is simple. Ask whether it could be exercised
with no window open. If yes, it is simulation. If it needs a context, a device
or a screen, it is host.

## What lives where

| Concern | Project |
| --- | --- |
| Nations, settlements, markets, inventories, items | Core |
| Pawn behaviour, world generation, the tick counter | Core |
| Positions, velocities, gravity and the ground clamp | Core |
| What something looks like, as data (`Appearance`, `Rgba`, `PrimitiveKind`) | Core |
| The snapshot the host draws from | Core |
| Seedable randomness, the logging seam, screen state | Core |
| Window, OpenGL context, the game loop | Desktop |
| Instanced primitive renderer, shaders, meshes | Desktop |
| Camera, projection, frustum and distance culling | Desktop |
| Keyboard and mouse, key bindings | Desktop |
| Menus, heads-up display, info boxes | Desktop |
| Glyph atlas and world-space nametags | Desktop |
| Screenshots and file paths | Desktop |
| Usage reporting (the `startup` event, `settings.json`, the vendored trace client) | Desktop |

## Consequences worth knowing

**Randomness is injected, not static.** A `RandomSource` is constructed with a
seed and passed down. That is what makes world generation reproducible: the
same seed produces the same world, which tests rely on and bug reports can
quote.

**Logging goes through a seam.** Core calls `Log.info` and the host decides
where it goes. The default sink discards everything, so tests stay quiet.

**The simulation has no notion of time of its own.** The host tells it how much
time has passed, which is why a slow machine produces the same simulation as a
fast one rather than a slower one.

**There is no editor.** The world is generated in code and every shape is
generated in code. Nothing is authored in a tool and imported, which is why the
repository contains no meshes, no materials and no scenes — only a font.

## Further reading

- [Engine Migration Rationale](./ENGINE-MIGRATION-RATIONALE.md) — why the engine was replaced, what it cost and what was given up
- [Systems](./SYSTEMS.md) — the gameplay systems and their status
- [Contributing](./CONTRIBUTING.md) — conventions and how to run the checks
