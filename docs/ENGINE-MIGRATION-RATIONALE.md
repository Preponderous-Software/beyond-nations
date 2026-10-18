# Engine Migration Rationale

**Status:** Accepted · **Date:** 2026-08-15 · **Tracking issue:** [#226](https://github.com/Preponderous-Software/beyond-nations/issues/226)

**Decision:** Unity is to be replaced with a purpose-built C# engine built on
[Silk.NET](https://github.com/dotnet/Silk.NET) and [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET).

This document records why. It is intended for anyone who arrives at the repository and asks the
reasonable question of why an established engine was abandoned. The migration plan itself lives in
issue #226; this is the reasoning behind it.

## Context

The codebase was assessed at commit `4359091`. The finding that drove the decision is that Unity
was being used far more shallowly than the project structure suggested.

| Measured | Value |
| --- | --- |
| C# files / lines | 113 / 7,863 |
| Classes deriving from `MonoBehaviour` | **1** |
| Shaders, animators, navmeshes, terrains, prefab spawns | **0** |
| Scenes / prefabs in the project | 1 / 1 |
| `UnityEngine.Vector3` references | 175 |
| `GameObject` references | 318 |
| Legacy `OnGUI` call sites (the entire UI) | 106 |
| Unity modules listed in `manifest.json` vs. actually used | 40 vs. ~4 |

Everything the game draws is a coloured primitive. `Location` is a
`GameObject.CreatePrimitive(PrimitiveType.Cube)` with a random green `material.color`; pawns and
settlements are capsules; trees are a capsule trunk with a cube of leaves. No mesh is imported, no
material is authored and no shader is written anywhere in the repository.

Physics is similarly thin. `Player` and `Pawn` add a `Rigidbody`, freeze all three rotation axes,
assign `velocity` directly, and use exactly one `AddForce` — an upward impulse for jumping. Several
entity classes immediately `Destroy()` the collider Unity attached to their primitive.

What Unity genuinely provides is `Vector3`, a transform tree, immediate-mode UI and gravity.

## Costs being paid today

- **Continuous integration has never passed.** Every `Unity CI` run fails at
  `Missing Unity License File and no Serial was found`, for both the test job and the build job. No
  pull request in the project's history has been gated by a passing test.
- **The tests require a game engine to run.** The Unity Test Framework was already abandoned in
  favour of a hand-rolled static `Tests.runTests()` dispatcher, yet 21 classes of pure logic —
  market pricing, pawn behaviour, inventory, nation naming — still cannot execute without a
  licensed editor.
- **Onboarding is heavy, and the instructions are wrong.** The README asks contributors to install
  Unity Hub and editor `2022.3.7f1`, while `ProjectSettings/ProjectVersion.txt` records
  `6000.0.30f1`. For a project whose stated purpose is serving as a reference for aspiring
  developers, that is a poor first impression.
- **A dedicated CI job exists to police `.meta` files**, an artefact of the editor's asset database.
- **Scale is capped by per-object overhead.** With `maxNumChunks = 10000` and a chunk size of 7, the
  configured ceiling is roughly 490,000 individual cube `GameObject`s, each carrying its own material
  instance. `LagPreventer` exists to delete entities and chunks at random when counts run away.
  Instanced rendering draws that in a handful of calls.

## Alternatives considered

**Stay on Unity and repair the pipeline.** Adding a `UNITY_LICENSE` secret would turn the badge
green in an afternoon, and this remains the cheapest option by a wide margin. It was rejected
because it addresses only the most visible symptom: the tests would still require an engine, the
onboarding burden would remain, and the per-object scale ceiling would be untouched.

**Port to Godot with C#.** Godot is free software, requires no licence server, and retains an
editor along with physics and model import. It was rejected because the presentation layer has to be
rewritten either way — the `GameObject` layer is the bulk of the work regardless of destination — and
paying that cost buys less control than building the host directly. The project has one scene and one
prefab and constructs every screen in code, so the editor being retained is worth little here.

**MonoGame or Stride.** Reasonable middle grounds, and closer in spirit to the chosen path than
Godot is. Silk.NET was preferred for having the smallest dependency surface for what this game
actually needs: a window, an OpenGL context, input, and nothing else.

## Consequences

Accepted deliberately, rather than overlooked:

- **No WebGL target.** Unity ships one; Silk.NET effectively does not. Should browser playability
  become a requirement, this decision should be revisited.
- **No asset import pipeline.** Imported meshes, textures and animations would need explicit glTF or
  Assimp loading. Note that this does *not* apply to the tree model tracked in
  [#140](https://github.com/Preponderous-Software/beyond-nations/issues/140), which is being
  generated procedurally in code and therefore ports without an import pipeline.
- **No editor.** Scene view, inspector and profiler are lost. Given one scene, one prefab and all
  screens built in code, little is given up in practice.
- **A narrower contributor pool.** Unity C# is a common skill; a bespoke host is not. Set against a
  permanently red CI badge and a README naming the wrong editor version, the status quo was not a
  strong recruiting position either.
- **Engine features not yet reached become work.** Audio, particles and controller support are free
  in Unity and would each have to be built.

Two consequences run the other way. [#173](https://github.com/Preponderous-Software/beyond-nations/issues/173)
(First Person View) reduces to a camera-mode switch once the replacement camera exists, and the
outstanding UI issues are considerably more pleasant to build against an immediate-mode library than
against `OnGUI`.

## Architecture

The codebase splits in two:

- **`BeyondNations.Core`** — the simulation. No graphics dependency, testable with `dotnet test`,
  roughly 80% of the existing code.
- **`BeyondNations.Desktop`** — the host. Window, renderer, camera, input, user interface.

That split is the most instructive thing about the project's structure, and is worth understanding
before contributing to either half.

---

_drafted by Claude on behalf of Daniel Stephenson_
