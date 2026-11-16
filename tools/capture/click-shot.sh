#!/usr/bin/env bash
# One still of the desktop game after a few clicks, for shots in media.json
# that need a panel opened by hand (the market panel is one press away from
# --stage-market, and no flag opens it).
#
#   click-shot.sh --out FILE [--click X,Y]... [--until-panel NAME] [--settle S] -- dotnet GAME.dll ARGS...
#
# Runs the game with --playtest-out in a scratch folder, waits for its first
# state.json with a world in it, clicks each point in turn (hovering first: at a
# few frames a second ImGui only takes a click it has seen the pointer over),
# waits until state.json lists NAME in openPanels (clicking again once if it
# does not), lets the frame settle, grabs the window with ImageMagick's import
# and stops the game. Needs DISPLAY (darkroom's xvfb sets it), xdotool, import
# and python3.
set -euo pipefail

out=""; settle=3; panel=""; clicks=()
while [ $# -gt 0 ]; do
  case "$1" in
    --out) out="$2"; shift 2 ;;
    --click) clicks+=("$2"); shift 2 ;;
    --until-panel) panel="$2"; shift 2 ;;
    --settle) settle="$2"; shift 2 ;;
    --) shift; break ;;
    *) echo "click-shot.sh: unknown argument $1" >&2; exit 2 ;;
  esac
done
[ -n "$out" ] && [ $# -gt 0 ] || { echo "usage: click-shot.sh --out FILE [--click X,Y]... [--until-panel NAME] [--settle S] -- GAME ARGS..." >&2; exit 2; }

pt="$(mktemp -d)"
"$@" --playtest-out "$pt" &
game=$!
trap 'kill "$game" 2>/dev/null || true; wait "$game" 2>/dev/null || true; rm -rf "$pt"' EXIT

state() {  # print one field of state.json (empty while it is missing or half written)
  python3 -I -c 'import json,sys
try: s=json.load(open(sys.argv[1]))
except Exception: sys.exit(0)
v=s
for k in sys.argv[2].split("."): v=v.get(k) if isinstance(v,dict) else None
print(json.dumps(v))' "$pt/state.json" "$1"
}

for _ in $(seq 1 1200); do
  kill -0 "$game" 2>/dev/null || { echo "click-shot.sh: the game exited early" >&2; exit 1; }
  [ "$(state world)" = "true" ] && [ "$(state tick)" != "0" ] && break
  sleep 0.5
done
# Xvfb has no window manager, so GLFW's window sits where it was put; move it
# to the corner so the click points are the window's own pixels
win="$(xdotool search --onlyvisible --pid "$game" 2>/dev/null | head -n 1 || true)"
if [ -n "$win" ]; then xdotool windowmove "$win" 0 0; fi
sleep 2

click_all() {
  for c in "${clicks[@]}"; do
    xdotool mousemove "${c%,*}" "${c#*,}"; sleep 0.6
    xdotool mousedown 1; sleep 0.25; xdotool mouseup 1; sleep 1.5
  done
}
[ ${#clicks[@]} -gt 0 ] && click_all

if [ -n "$panel" ]; then
  ok=""
  for attempt in 1 2; do
    for _ in $(seq 1 60); do
      if state openPanels | grep -q "\"$panel\""; then ok=1; break; fi
      sleep 0.5
    done
    [ -n "$ok" ] && break
    [ "$attempt" = 1 ] && click_all
  done
  [ -n "$ok" ] || { echo "click-shot.sh: $panel never opened" >&2; exit 1; }
fi

# the pointer off the buttons, so none is drawn hovered
xdotool mousemove 0 0
sleep "$settle"
if [ -n "$win" ]; then import -window "$win" "$out"; else import -window root "$out"; fi
