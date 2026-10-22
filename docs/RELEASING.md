# Releasing

A release is a bare version tag on `main` (`0.4.1`, never `v0.4.1`) with a GitHub release on it. Publishing the release starts [`.github/workflows/release.yml`](../.github/workflows/release.yml), which builds the desktop game for Windows x64 and Linux x64 and attaches the downloads to the release. macOS is not supported (Strata has no macOS build).

## Steps

0. **Playtest the candidate.** Run the parallel playtest ([PLAYTESTING.md](PLAYTESTING.md)) against `develop` and commit its evidence under `docs/playtests/<version>/`. A playtest of an unreleased build is filed under the next expected version, so the directory may already exist from an earlier run. Its findings do not block the release on their own; file the confirmed blocker and major ones as issues.

1. **Bump the version.** On a branch from `develop`, set `Version` in [`src/BeyondNations.Desktop/ui/GameVersion.cs`](../src/BeyondNations.Desktop/ui/GameVersion.cs) to the new version, and open a pull request to `develop`. The release workflow refuses to build a tag that does not match this string, because it is the version the game shows in the corner of every screen. (#326 is an example.)

   In the same pull request, make sure the playtest evidence is named for the version being released. If `docs/playtests/` holds a directory for a different version than the one this release turned out to be (for example `0.4.2/` when the release is `0.5.0`), rename it with `git mv` and update the version inside its README. The release workflow fails a tag whose evidence directory is missing, and fails it if any evidence directory is named for a later version.

2. **Merge `develop` into `main`.** Once the bump is merged, open a pull request from `develop` to `main` and merge it with a merge commit, as #327 was.

3. **Create the release on the merge commit.** Pass the full SHA of the merge commit, not a branch name, so the tag lands on exactly the commit that was reviewed:

   ```bash
   git fetch origin
   sha=$(git rev-parse origin/main)   # the develop -> main merge commit; check it with git log
   gh release create 0.4.1 --target "$sha" --prerelease \
     --title "Beyond Nations 0.4.1 (Name of the release)" --notes-file notes.md
   ```

   Every release so far has been a pre-release; drop `--prerelease` when that changes.

4. **Wait for the builds.** The `Release` workflow runs on the published release (Actions tab). After about ten minutes the release has four more assets:

   | Asset | Contents |
   | --- | --- |
   | `BeyondNations-<version>-win-x64.zip` | Self-contained Windows build; run `BeyondNations.Desktop.exe` |
   | `BeyondNations-<version>-linux-x64.tar.gz` | Self-contained Linux build; run `./BeyondNations.Desktop` |
   | `*.sha256` | The SHA-256 of each archive |

   If the workflow fails, fix the cause and re-run it from the Actions tab (or `gh run rerun <id>`); uploads replace assets of the same name.

Pushing a version tag without creating a release also works: the workflow builds the tag and creates a pre-release for it with generated notes. Creating the release with `gh release create` pushes its tag too, so that starts two runs; the later one cancels the earlier, and one set of assets is uploaded.

## What is in a download

Each archive is a `dotnet publish --self-contained` build: no .NET install is needed. Besides the game it carries every native library the game loads:

- **Strata**, the world renderer: `runtimes/<rid>/native/libstrata.so` or `strata.dll`, from the Strata release pinned (and SHA-256 checked) in `BeyondNations.Desktop.csproj`.
- **cimgui** (`libcimgui.so` / `cimgui.dll`), the native half of Dear ImGui, from `ImGui.NET`.
- **GLFW** (`libglfw.so.3` / `glfw3.dll`), which Silk.NET opens the window with.
- **On Windows, the Visual C++ runtime** (`msvcp140.dll`, `vcruntime140.dll`, `vcruntime140_1.dll`). `strata.dll` and `glfw3.dll` link against it, and a Windows install without the Visual C++ Redistributable does not have it. The workflow copies the redistributable files from the runner's Visual Studio, next to the exe and next to `strata.dll`.
- `LICENSE.txt` and `THIRD-PARTY.md`, and the DejaVu font with its licence.

What the player still needs:

- An **OpenGL 4.5** capable graphics driver, on Windows 10/11 x64 or Linux x64.
- On Linux, the X11 (or XWayland) client libraries every desktop already has. The bundled GLFW needs glibc 2.34 or newer (Ubuntu 22.04, Debian 12, Fedora 35 and later). On an older distribution, Ubuntu 20.04 for example, install the system GLFW (`sudo apt install libglfw3`) and the game uses that instead.

## Checking the packaging without a release

Run the workflow by hand from the Actions tab (**Release** > **Run workflow**) or with:

```bash
gh workflow run release.yml --ref <branch>
```

With no `tag` input it builds the branch and keeps the archives as workflow artifacts on the run, attaching nothing. With `tag` set to an existing release's tag it builds that tag and attaches to that release, which is how builds can be added to a release made before this workflow existed. A pull request that changes `release.yml` or the desktop project file runs the same dry run.

`workflow_dispatch` only works once the workflow file is on the default branch (`main`).

What each run checks:

- **Linux:** the packaged binary, unpacked from the publish folder, runs the world screen headlessly on Mesa llvmpipe under Xvfb for 120 frames, and the log must say `world renderer: strata`.
- **Windows:** the runner has no OpenGL 4.5 driver, so the game cannot draw there. The job checks that every required file is in the package, loads `cimgui.dll`, `glfw3.dll` and `strata.dll` from it, and asks Strata for its version and ABI.

The Linux steps can be reproduced locally:

```bash
dotnet publish src/BeyondNations.Desktop/BeyondNations.Desktop.csproj -c Release -r linux-x64 --self-contained true -o out/bn
cd out/bn
LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe xvfb-run -a -s "-screen 0 1280x800x24" \
  ./BeyondNations.Desktop --seed 42 --start-screen world --exit-after-frames 120 --no-usage-reporting --no-saves
```
