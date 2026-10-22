# Third-party assets and licences

Everything Beyond Nations ships that was not written for it, and the terms it
arrives under.

## Fonts

### DejaVu Sans

| | |
| --- | --- |
| File | `src/BeyondNations.Desktop/assets/fonts/DejaVuSans.ttf` |
| Licence | `src/BeyondNations.Desktop/assets/fonts/LICENSE-DejaVu.txt` |
| Upstream | <https://dejavu-fonts.org/> |
| Terms | Bitstream Vera licence; DejaVu's own changes are public domain |

Used for world-space nametags above pawns and settlements, packed into a glyph
atlas once at startup.

A font had to be bundled because the Unity build drew its labels with
`LegacyRuntime.ttf`, a font that ships inside the Unity editor and disappears
along with it. See #222.

DejaVu Sans was chosen because its licence permits redistribution and
modification with only an attribution and a non-endorsement condition, it
carries wide glyph coverage, and it is a stable, long-maintained face rather
than one that might vanish.

The licence requires that the copyright notice travels with the font, which is
why `LICENSE-DejaVu.txt` sits beside the `.ttf` and is copied to the build
output rather than living only here.

## Sound

None. Every sound in the browser build (`src/BeyondNations.Web/wwwroot/audio.js`)
is synthesised with WebAudio oscillators and filtered noise when it plays, so no
audio file, sample or sound font is shipped and there is no audio licence to
record. A future recorded asset must be CC0 or similarly permissive and listed
here.

## Vendored source

### trace-client (C#)

| | |
| --- | --- |
| File | `src/BeyondNations.Desktop/telemetry/TraceClient.cs` |
| Upstream | <https://github.com/Stephenson-Software/trace-client-csharp>, 0.3.0 |
| Licence | MIT (stated in the file's header, which is kept as is) |

The one-file client behind the `startup` usage report; see "Usage reporting"
in the README. Copied rather than referenced as a package, the way its
upstream intends, so it adds no NuGet dependency.

## Packages

Restored from nuget.org at build time; none is vendored into this repository.

| Package | Used for | Licence |
| --- | --- | --- |
| `Silk.NET.Windowing`, `Silk.NET.OpenGL`, `Silk.NET.Input` | Window, GL context and input in the desktop host | MIT |
| `Silk.NET.OpenGL.Extensions.ImGui` | The GL backend and input plumbing behind Dear ImGui | MIT |
| `ImGui.NET` | The menus, the heads-up display and the info boxes | MIT |
| `FontStashSharp` | Glyph atlas packing and text layout | MIT |
| `StbImageWriteSharp` | PNG encoding for screenshots | Public domain / MIT |
| `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` | Tests only; not shipped | Apache-2.0 / MIT |

`ImGui.NET` is a managed wrapper around [Dear ImGui](https://github.com/ocornut/imgui),
which is MIT-licensed in its own right. It is the one package here that carries a
**native** binary — `cimgui` — restored alongside the managed assembly and copied
into the build output, so it travels with anything the host ships. That native
binary is why the version is pinned to 1.90.8.1: the one shipped with 1.91.6.1 is
built against GLIBC 2.38 and will not load on an older distribution. The pin is
recorded beside the `PackageReference` in
`src/BeyondNations.Desktop/BeyondNations.Desktop.csproj`. See #221.

The browser build (`src/BeyondNations.Web`, see [WEB.md](./WEB.md)) uses the same
`ImGui.NET` 1.90.8.1, but no native binary from the package: `native/cimgui.targets`
downloads the cimgui (`7c16d31`, MIT) and Dear ImGui (`6d948ab`, docking 1.90.8,
MIT) sources that package was built from, checks their SHA-256 and compiles them
to WebAssembly. Its smoke test uses `playwright` (Apache-2.0), a test-only npm
package pinned in `src/BeyondNations.Web/smoke/package-lock.json`.

`BeyondNations.Core` has **no** package dependencies at all. That is deliberate:
the simulation is meant to build and run with nothing but the .NET SDK.

## Native libraries

### Strata

| | |
| --- | --- |
| Files | `runtimes/linux-x64/native/libstrata.so`, `runtimes/win-x64/native/strata.dll` in the build output |
| Source | <https://github.com/Preponderous-Software/strata>, release `v0.4.0` (the browser build compiles the same release's source, commit `2e03be8`) |
| Licence | MIT; its own third-party notices (SDL3, zlib licence; glm, glad, stb, MIT or public domain) are in `THIRD_PARTY_NOTICES.md` in the Strata release archive |
| Integrity | Each file's SHA-256 is pinned in `BeyondNations.Desktop.csproj`, and the build refuses a download that does not match |

This is the shared native engine that draws the world (#274; the only world renderer since the Silk renderer was removed). SDL3 is
linked into it statically, so its zlib-licence notice travels with Strata's
notices.
