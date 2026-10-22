#!/bin/bash
# A scripted session that proves the harness end to end, driven only through
# pt.sh: title -> Start -> Begin -> walk to trees and chop them until the
# "Gather wood" objective completes, confirmed in events.jsonl.
#
#   PT_SESSION=smoke1 tools/playtest/smoke.sh [SEED]
#
# Exits 0 when the objective completed. Always stops its session. Assumes the
# default 960x540 window, where Start and Begin sit at (480, 315).

set -u
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PT="$HERE/pt.sh"
SEED="${1:-42}"
: "${PT_SESSION:?set PT_SESSION}"

trap '"$PT" stop >/dev/null' EXIT

fail() { echo "smoke $PT_SESSION: FAIL: $*" >&2; "$PT" shot "fail" >/dev/null 2>&1; exit 1; }

"$PT" start --seed "$SEED" >/dev/null || fail "start"
"$PT" await TITLE 30 >/dev/null || fail "title screen"
"$PT" key space
"$PT" await MAIN_MENU 10 >/dev/null || fail "main menu"
"$PT" shot 01-main-menu >/dev/null
"$PT" click 480 315
"$PT" await NEW_WORLD 10 >/dev/null || fail "new world screen"
"$PT" click 480 315
"$PT" await WORLD 30 >/dev/null || fail "world"
"$PT" wait 2
"$PT" shot 02-world >/dev/null

EVENTS="$("$PT" dir)/out/events.jsonl"
started=$(date +%s)
for attempt in $(seq 1 10); do
  "$PT" walkto tree 60 || { "$PT" shot "walk-$attempt" >/dev/null; continue; }
  "$PT" key e
  "$PT" wait 1.5
  if grep -q '"type":"objective_completed".*"title":"Gather wood"' "$EVENTS"; then
    "$PT" shot 03-gather-wood-done >/dev/null
    echo "smoke $PT_SESSION: PASS: Gather wood completed after $attempt tree(s), $(( $(date +%s) - started ))s in the world"
    grep '"objective_completed"' "$EVENTS"
    exit 0
  fi
done
fail "Gather wood did not complete after 10 trees"
