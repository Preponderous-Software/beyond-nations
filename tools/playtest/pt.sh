#!/bin/bash
# Playtest harness: one session of the real desktop build in its own Xvfb,
# driven with xdotool and read through --playtest-out. Any number of sessions
# can run at once; each keeps everything under $PT_ROOT/$PT_SESSION.
# See docs/PLAYTESTING.md.
#
#   PT_SESSION=s01 tools/playtest/pt.sh start --seed 42
#   PT_SESSION=s01 tools/playtest/pt.sh state
#   PT_SESSION=s01 tools/playtest/pt.sh stop
#
# Commands
#   start [game args...]      Xvfb on a free display + the Release build; waits for the window
#   stop                      stop the game and its Xvfb (safe to repeat)
#   status                    running?, display, CPU/RSS, fps
#   key KEY...                tap keys; a KEY may be a combo such as Shift_L+w
#   hold KEY SECS             hold a key or combo (Shift_L+w sprints forward)
#   click X Y [BUTTON]        click at window coordinates (hover first, so ImGui sees it)
#   drag X1 Y1 X2 Y2 SECS [BUTTON]   drag with a button held (default 3, right = steer)
#   move X Y                  move the mouse (hover) without clicking
#   type TEXT                 type text into a focused text field
#   shot NAME                 screenshot to shots/NAME.png; prints the path
#   state [--full]            the game's state.json, compact (or whole)
#   events [N]                the last N lines of events.jsonl (default 20)
#   face DEG                  turn DEG degrees (+ right, - left), closed-loop on state.json
#   walkto KIND [MAXSEC]      walk to the nearest tree|rock|pawn|chicken|settlement|stall
#                             until it is within reach (Interact range is 5)
#   wait SECS                 sleep
#   await SCREEN [SECS]       wait until state.json shows SCREEN (e.g. WORLD); fails after SECS (default 15)
#   note [--shot NAME] TEXT   append a finding to findings.jsonl (optionally with a screenshot)
#   report                    validate report.json against tools/playtest/report.schema.json
#   log [N]                   tail of the game log
#   dir                       print the session directory
#
# Environment
#   PT_SESSION   required: a name for this session ([A-Za-z0-9._-])
#   PT_ROOT      where sessions live (default /tmp/bn-playtest); a run uses PT_ROOT/<run>
#   PT_WIDTH, PT_HEIGHT   window size (default 960x540)
#   PT_LP_THREADS         llvmpipe threads per game (default 2)
#   PT_GAME_ARGS extra game arguments for every start, e.g. "--render-distance 60"
#   PT_BIN       the game binary (default: this checkout's Release build)
#   DOTNET_ROOT  default /root/.dotnet when it exists

set -u

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
HELPER="$HERE/pthelper.py"

die() { echo "pt.sh: $*" >&2; exit 1; }

[ -n "${PT_SESSION:-}" ] || die "PT_SESSION is not set (each session needs its own name, e.g. PT_SESSION=s01)"
case "$PT_SESSION" in
  *[!A-Za-z0-9._-]*) die "PT_SESSION may only contain letters, digits, '.', '_' and '-'";;
esac

PT_ROOT="${PT_ROOT:-/tmp/bn-playtest}"
S="$PT_ROOT/$PT_SESSION"
W="${PT_WIDTH:-960}"
H="${PT_HEIGHT:-540}"
BIN="${PT_BIN:-$REPO/src/BeyondNations.Desktop/bin/Release/net8.0/BeyondNations.Desktop}"
LOCK="${TMPDIR:-/tmp}/bn-playtest-displays.lock"

if [ -z "${DOTNET_ROOT:-}" ] && [ -d /root/.dotnet ]; then export DOTNET_ROOT=/root/.dotnet; fi
# Xvfb aborts on this kind of box without these, and the game needs them for llvmpipe
export LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe
export LP_NUM_THREADS="${PT_LP_THREADS:-2}"

mkdir -p "$S/shots"
export PT_SESSION_DIR="$S"
if [ -f "$S/display" ]; then export DISPLAY=":$(cat "$S/display")"; fi

cmd="${1:-help}"; [ $# -gt 0 ] && shift

# every command, for repro: {"t": wall ms, "cmd": ..., "args": [...]}
python3 -I "$HELPER" log-action "$S/actions.jsonl" "$cmd" "$@" 2>/dev/null

alive() { [ -f "$S/$1.pid" ] && kill -0 "$(cat "$S/$1.pid")" 2>/dev/null; }
win() { cat "$S/window" 2>/dev/null; }
need_game() { alive game || die "the game is not running in session $PT_SESSION (pt.sh start first; pt.sh log shows why it stopped)"; }

focus() {
  local w; w="$(win)"
  [ -n "$w" ] || return 0
  xdotool windowfocus "$w" 2>/dev/null
}

# keydown each part of a combo in order, keyup in reverse
combo_down() { local IFS=+; for k in $1; do xdotool keydown "$k"; done; }
combo_up() { local IFS=+; local parts=($1); local i; for ((i=${#parts[@]}-1; i>=0; i--)); do xdotool keyup "${parts[$i]}"; done; }

stop_session() {
  local pid
  if [ -f "$S/game.pid" ]; then
    pid="$(cat "$S/game.pid")"
    if kill -0 "$pid" 2>/dev/null; then
      kill -TERM "$pid" 2>/dev/null
      for _ in 1 2 3 4 5 6 7 8 9 10; do kill -0 "$pid" 2>/dev/null || break; sleep 0.5; done
      kill -KILL "$pid" 2>/dev/null
    fi
    rm -f "$S/game.pid"
  fi
  if [ -f "$S/xvfb.pid" ]; then
    pid="$(cat "$S/xvfb.pid")"
    if kill -0 "$pid" 2>/dev/null; then
      kill -TERM "$pid" 2>/dev/null
      for _ in 1 2 3 4 5 6; do kill -0 "$pid" 2>/dev/null || break; sleep 0.5; done
      kill -KILL "$pid" 2>/dev/null
    fi
    rm -f "$S/xvfb.pid"
  fi
  rm -f "$S/window"
}

start_session() {
  [ -x "$BIN" ] || die "no game binary at $BIN (dotnet build src/BeyondNations.Desktop -c Release)"
  if alive game; then die "session $PT_SESSION is already running (pt.sh stop first)"; fi
  stop_session
  mkdir -p "$S/data" "$S/out"
  : > "$S/xvfb.log"

  # A free display, claimed under a lock so concurrent starts never pick the same one.
  local n="" xpid=""
  exec 9>"$LOCK"
  flock -w 60 9 || die "could not take the display lock $LOCK"
  for cand in $(seq 100 199); do
    if [ ! -e "/tmp/.X$cand-lock" ] && [ ! -e "/tmp/.X11-unix/X$cand" ]; then n=$cand; break; fi
  done
  if [ -z "$n" ]; then flock -u 9; die "no free X display in :100-:199"; fi
  setsid Xvfb ":$n" -screen 0 "$((W + 64))x$((H + 64))x24" -nolisten tcp >"$S/xvfb.log" 2>&1 < /dev/null 9>&- &
  xpid=$!
  echo "$xpid" > "$S/xvfb.pid"
  echo "$n" > "$S/display"
  for _ in $(seq 1 40); do [ -e "/tmp/.X11-unix/X$n" ] && break; kill -0 "$xpid" 2>/dev/null || break; sleep 0.25; done
  flock -u 9
  exec 9>&-
  if ! kill -0 "$xpid" 2>/dev/null || [ ! -e "/tmp/.X11-unix/X$n" ]; then
    echo "pt.sh: Xvfb :$n did not start; its log:" >&2; tail -n 20 "$S/xvfb.log" >&2
    stop_session; exit 1
  fi
  export DISPLAY=":$n"

  rm -f "$S/out/state.json"
  ( cd "$(dirname "$BIN")" && XDG_DATA_HOME="$S/data" exec setsid "$BIN" \
      --width "$W" --height "$H" --no-usage-reporting --playtest-out "$S/out" ${PT_GAME_ARGS:-} "$@" ) \
      >"$S/game.log" 2>&1 < /dev/null &
  echo $! > "$S/game.pid"

  local w=""
  for _ in $(seq 1 120); do
    w="$(xdotool search --name '^Beyond Nations$' 2>/dev/null | head -1)"
    [ -n "$w" ] && [ -f "$S/out/state.json" ] && break
    alive game || break
    sleep 0.5
  done
  if [ -z "$w" ] || ! alive game || [ ! -f "$S/out/state.json" ]; then
    echo "pt.sh: the game did not come up in session $PT_SESSION (display :$n). Last lines of $S/game.log:" >&2
    tail -n 25 "$S/game.log" >&2
    stop_session; exit 1
  fi
  echo "$w" > "$S/window"
  # GLFW opens the window at +50+50; with no window manager nothing moves it,
  # and the part past the screen's edge would be missing from screenshots
  xdotool windowmove "$w" 0 0 2>/dev/null
  xdotool mousemove --window "$w" $((W / 2)) $((H / 2)) 2>/dev/null
  python3 -I "$HELPER" started "$S" "$n" "$w" "$W" "$H" "$@"
}

case "$cmd" in
  start) start_session "$@";;
  stop) stop_session; echo "stopped $PT_SESSION";;
  status) python3 -I "$HELPER" status "$S";;
  key)
    need_game; focus
    for k in "$@"; do combo_down "$k"; sleep 0.12; combo_up "$k"; sleep 0.25; done;;
  hold)
    need_game; [ $# -ge 2 ] || die "usage: hold KEY SECS"; focus
    combo_down "$1"; sleep "$2"; combo_up "$1";;
  move)
    need_game; [ $# -ge 2 ] || die "usage: move X Y"
    xdotool mousemove --window "$(win)" "$1" "$2";;
  click)
    need_game; [ $# -ge 2 ] || die "usage: click X Y [BUTTON]"; focus
    # ImGui at a low frame rate misses a click unless the pointer hovers for a few frames first
    xdotool mousemove --window "$(win)" "$1" "$2"; sleep 0.3
    xdotool mousedown "${3:-1}"; sleep 0.15; xdotool mouseup "${3:-1}"; sleep 0.2;;
  drag)
    need_game; [ $# -ge 5 ] || die "usage: drag X1 Y1 X2 Y2 SECS [BUTTON]"; focus
    python3 -I "$HELPER" drag "$(win)" "$@";;
  type)
    need_game; focus; xdotool type --delay 80 -- "$*";;
  shot)
    need_game; [ $# -ge 1 ] || die "usage: shot NAME"
    name="${1//[^A-Za-z0-9._-]/_}"
    import -window "$(win)" "$S/shots/$name.png" 2>/dev/null || die "screenshot failed"
    echo "$S/shots/$name.png";;
  state) python3 -I "$HELPER" state "$S" "$@";;
  events) tail -n "${1:-20}" "$S/out/events.jsonl" 2>/dev/null;;
  face)
    need_game; [ $# -ge 1 ] || die "usage: face DEG"; focus
    python3 -I "$HELPER" face "$S" "$1";;
  walkto)
    need_game; [ $# -ge 1 ] || die "usage: walkto KIND [MAXSEC]"; focus
    python3 -I "$HELPER" walkto "$S" "$@";;
  wait) sleep "${1:?usage: wait SECS}";;
  await) python3 -I "$HELPER" await "$S" "${1:?usage: await SCREEN [SECS]}" "${2:-15}";;
  note)
    shotpath=""
    if [ "${1:-}" = "--shot" ]; then
      [ $# -ge 2 ] || die "usage: note [--shot NAME] TEXT"
      need_game; name="${2//[^A-Za-z0-9._-]/_}"; shift 2
      import -window "$(win)" "$S/shots/$name.png" 2>/dev/null && shotpath="$S/shots/$name.png"
    fi
    python3 -I "$HELPER" note "$S" "$shotpath" "$*";;
  report) python3 -I "$HERE/validate_report.py" "$HERE/report.schema.json" "$S/report.json";;
  log) tail -n "${1:-25}" "$S/game.log";;
  dir) echo "$S";;
  help|-h|--help) sed -n '2,40p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//';;
  *) die "unknown command: $cmd (pt.sh help)";;
esac
