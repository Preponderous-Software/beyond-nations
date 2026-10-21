#!/usr/bin/env bash
# Record a seeded world and make its history film, end to end.
#
#   BLENDER=/path/to/blender tools/history-film/make-film.sh [seed] [ticks] [out-dir]
#
# Needs: the .NET 8 SDK, uv, and Blender 4.2+ (tested with 5.2). Defaults: seed 12,
# 600000 ticks (about 3 h 20 min of game time; the export takes about four minutes),
# one film frame per 800 ticks, out-dir history-film-out/.
set -euo pipefail

seed="${1:-12}"
ticks="${2:-600000}"
out="${3:-history-film-out}"
stride="${STRIDE:-800}"            # ticks per film frame (a multiple of 200)
blender="${BLENDER:-blender}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
tools="$(dirname "$here")"

mkdir -p "$out"
dotnet run --project "$tools/BeyondNations.HistoryExport" -c Release -- \
    --seed "$seed" --ticks "$ticks" --every 200 --out "$out/history.jsonl"

uv run --python 3.12 "$tools/history-to-usd.py" "$out/history.jsonl" "$out/usd/history.usda" \
    --stride "$stride" --chunk-size 200 --flatten

# Blender's EEVEE and Workbench need OpenGL; Cycles renders on the CPU anywhere.
"$blender" -b --threads "${THREADS:-4}" --python "$here/render.py" -- \
    "$out/usd/history_flat.usdc" "$out/frames" --engine cycles --samples "${SAMPLES:-16}"

uv run --python 3.12 "$here/finish.py" "$out/usd/history_flat.usdc" "$out/frames" "$out/film"
