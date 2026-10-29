#!/usr/bin/env bash
# Film the README's flyover: a seeded world run to mid-game by the real
# simulation, drawn by the game's own Strata renderer, the camera gliding
# from one nation's town across the border to its neighbour's.
#
#   tools/readme-flyover.sh [seed] [ticks] [frames] [out-dir]
#
# Defaults: seed 12, 150000 ticks (50 minutes of game time; about a minute
# and a half to run), 60 frames, out-dir readme-flyover-out/. Writes the raw
# frames there, then .github/media/flyover.gif and .github/media/flyover.png.
#
# Needs: the .NET 8 SDK, uv, and an OpenGL 4.5 driver. With no display it
# runs under xvfb-run with Mesa's llvmpipe, which works anywhere (about two
# frames a second). The world is the same for the same seed and ticks; the
# pixels can differ slightly between GPUs and drivers.
set -euo pipefail

seed="${1:-12}"
ticks="${2:-150000}"
frames="${3:-60}"
out="${4:-readme-flyover-out}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(dirname "$here")"

dotnet build "$root/src/BeyondNations.Desktop" -c Release
rm -rf "$out/frames"
mkdir -p "$out"

run=(dotnet "$root/src/BeyondNations.Desktop/bin/Release/net8.0/BeyondNations.Desktop.dll"
    --seed "$seed" --start-screen world --no-saves --no-usage-reporting
    --width 960 --height 540
    --advance-ticks "$ticks" --flyover-out "$out/frames" --flyover-frames "$frames")
# keep the run's settings and crash files out of the real ones
export XDG_DATA_HOME="$out/data"
if [ -n "${DISPLAY:-}" ]; then
    "${run[@]}"
else
    export LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe
    xvfb-run -a -s "-screen 0 1280x720x24" "${run[@]}"
fi

uv run --python 3.12 --with pillow "$here/readme-flyover.py" \
    "$out/frames" "$root/.github/media/flyover.gif" "$root/.github/media/flyover.png"
