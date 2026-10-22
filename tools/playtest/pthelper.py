"""The parts of tools/playtest/pt.sh that are easier in Python: reading
state.json, the closed-loop face and walkto commands, and the JSON logs.

Standard library only, and run with `python3 -I` (docs/PLAYTESTING.md).
Not meant to be called directly; pt.sh passes the session directory.
"""

import json
import os
import subprocess
import sys
import time

# Player.fixedUpdate turns 2 degrees a tick while A or D is held, at 50 ticks a second.
TURN_DEGREES_PER_SECOND = 100.0
# InteractCommand acts on a tree, rock, pawn, chicken or settlement closer than 5 units.
INTERACT_RANGE = 5.0
# walkto stops this close, leaving room for the frame-rate overshoot of a key hold.
WALK_STOP = 4.0
FACE_TOLERANCE = 8.0
KINDS = ("tree", "rock", "pawn", "chicken", "settlement", "stall")
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def now_ms():
    return int(time.time() * 1000)


def out(obj):
    print(json.dumps(obj, separators=(",", ":"), ensure_ascii=False))


def normalise(deg):
    d = deg % 360.0
    if d > 180.0:
        d -= 360.0
    return d


def state_path(session):
    return os.path.join(session, "out", "state.json")


def read_state(session):
    try:
        with open(state_path(session), encoding="utf-8") as f:
            return json.load(f)
    except (OSError, ValueError):
        return None


def fresh_state(session, after_ms, timeout=4.0):
    """The first state written after after_ms (the game rewrites it about twice a second)."""
    deadline = time.time() + timeout
    st = None
    while time.time() < deadline:
        st = read_state(session)
        if st is not None and st.get("wallTimeMs", 0) > after_ms:
            return st
        time.sleep(0.08)
    return st


def xdo(*args):
    subprocess.run(["xdotool"] + [str(a) for a in args], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def hold(key, seconds):
    xdo("keydown", key)
    time.sleep(max(0.0, seconds))
    xdo("keyup", key)


def trace(session, what, **fields):
    """One line per closed-loop step in the session's trace.jsonl, for working out why a walk went wrong."""
    fields.update({"t": now_ms(), "what": what})
    append_json(os.path.join(session, "trace.jsonl"), fields)


def append_json(path, obj):
    with open(path, "a", encoding="utf-8") as f:
        f.write(json.dumps(obj, ensure_ascii=False) + "\n")


# --- commands -------------------------------------------------------------

def cmd_log_action(path, cmd, *args):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    append_json(path, {"t": now_ms(), "cmd": cmd, "args": list(args)})


def git_commit():
    try:
        r = subprocess.run(["git", "-C", REPO, "rev-parse", "--short", "HEAD"],
                           stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, universal_newlines=True, timeout=10)
        return r.stdout.strip() or None
    except (OSError, subprocess.SubprocessError):
        return None


def cmd_started(session, display, window, width, height, *game_args):
    st = read_state(session) or {}
    info = {
        "session": os.path.basename(session),
        "dir": session,
        "display": ":" + display,
        "window": window,
        "size": "%sx%s" % (width, height),
        "args": list(game_args),
        "startedAtMs": now_ms(),
        "version": st.get("version"),
        "commit": git_commit(),
    }
    with open(os.path.join(session, "session.json"), "w", encoding="utf-8") as f:
        json.dump(info, f, indent=2)
    out(info)


def compact(st):
    if st is None:
        return {"error": "no state.json yet (is the game running with --playtest-out?)"}
    c = {"screen": st.get("screen"), "fps": st.get("fps"), "panels": st.get("openPanels")}
    if st.get("tip"):
        c["tip"] = st["tip"]
    if not st.get("world"):
        return c
    p = st.get("player", {})
    o = st.get("objective", {})
    c.update({
        "tick": st.get("tick"), "day": st.get("day"), "timeOfDay": st.get("timeOfDay"),
        "season": st.get("season"), "era": st.get("era"), "region": st.get("region"), "seed": st.get("seed"),
        "player": {k: p.get(k) for k in ("x", "z", "yawDeg", "energy", "coins", "inventory", "nation", "role", "settlement", "alive")},
        "objective": "#%s %s%s" % (o.get("number"), o.get("title"), " (%s)" % o["progress"] if o.get("progress") else ""),
        "status": st.get("status"),
    })
    if st.get("worldEvent"):
        c["worldEvent"] = st["worldEvent"]
    near = {}
    for kind in KINDS:
        items = st.get("nearest", {}).get(kind) or []
        if items:
            near[kind] = ["%s %.1fm %+.0f" % (t["name"], t["distance"], t["bearing"]) for t in items]
    c["nearest"] = near
    return c


def cmd_state(session, *flags):
    st = read_state(session)
    if "--full" in flags:
        print(json.dumps(st, indent=2, ensure_ascii=False))
    else:
        out(compact(st))


def pid_alive(path):
    try:
        with open(path) as f:
            pid = int(f.read().strip())
        os.kill(pid, 0)
        return pid
    except (OSError, ValueError):
        return None


def cmd_status(session):
    game = pid_alive(os.path.join(session, "game.pid"))
    xvfb = pid_alive(os.path.join(session, "xvfb.pid"))
    info = {"session": os.path.basename(session), "running": bool(game), "gamePid": game, "xvfbPid": xvfb}
    try:
        with open(os.path.join(session, "display")) as f:
            info["display"] = ":" + f.read().strip()
    except OSError:
        pass
    if game:
        r = subprocess.run(["ps", "-o", "%cpu=,rss=", "-p", str(game)], stdout=subprocess.PIPE, universal_newlines=True)
        parts = r.stdout.split()
        if len(parts) == 2:
            info["cpuPercent"] = float(parts[0])
            info["rssMb"] = round(int(parts[1]) / 1024.0, 1)
    st = read_state(session)
    if st:
        info["fps"] = st.get("fps")
        info["screen"] = st.get("screen")
        info["stateAgeSec"] = round((now_ms() - st.get("wallTimeMs", 0)) / 1000.0, 1)
    out(info)


def cmd_drag(window, x1, y1, x2, y2, secs, button="3"):
    x1, y1, x2, y2, secs = int(x1), int(y1), int(x2), int(y2), float(secs)
    xdo("mousemove", "--window", window, x1, y1)
    time.sleep(0.3)
    xdo("mousedown", button)
    steps = max(10, int(secs * 30))
    for i in range(1, steps + 1):
        xdo("mousemove", "--window", window, x1 + (x2 - x1) * i // steps, y1 + (y2 - y1) * i // steps)
        time.sleep(secs / steps)
    time.sleep(0.1)
    xdo("mouseup", button)


def face(session, relative, tolerance=FACE_TOLERANCE, attempts=8):
    """Turn by `relative` degrees (+ right) with A/D, re-reading the yaw after each turn."""
    st = fresh_state(session, now_ms())
    if st is None or not st.get("world"):
        return {"ok": False, "error": "no world on screen", "screen": st and st.get("screen")}
    # + right lowers the yaw (a rising yaw turns left)
    target = (st["player"]["yawDeg"] - relative) % 360.0
    remaining = 0.0
    for attempt in range(attempts):
        remaining = normalise(st["player"]["yawDeg"] - target)
        if abs(remaining) <= tolerance:
            return {"ok": True, "yawDeg": st["player"]["yawDeg"], "offBy": round(remaining, 1), "turns": attempt}
        if st.get("screen") != "WORLD":
            return {"ok": False, "error": "left the world screen", "screen": st.get("screen")}
        # a little short of the whole turn: key holds overshoot by up to a frame
        trace(session, "face", yawDeg=st["player"]["yawDeg"], target=round(target, 1), remaining=round(remaining, 1), fps=st.get("fps"))
        hold("d" if remaining > 0 else "a", max(0.03, abs(remaining) / TURN_DEGREES_PER_SECOND * 0.9))
        st = fresh_state(session, now_ms() + 30)
    remaining = normalise(st["player"]["yawDeg"] - target)
    return {"ok": abs(remaining) <= tolerance, "yawDeg": st["player"]["yawDeg"], "offBy": round(remaining, 1), "turns": attempts}


def cmd_face(session, degrees):
    result = face(session, float(degrees))
    out(result)
    return 0 if result.get("ok") else 1


def cmd_walkto(session, kind, max_seconds="60"):
    if kind not in KINDS:
        out({"ok": False, "error": "kind must be one of " + ", ".join(KINDS)})
        return 2
    started = time.time()
    max_seconds = float(max_seconds)
    target_id, best, stalls, walked = None, None, 0, False
    while True:
        st = fresh_state(session, now_ms())
        if st is None or st.get("screen") != "WORLD":
            out({"ok": False, "error": "not on the world screen", "screen": st and st.get("screen")})
            return 1
        targets = (st.get("nearest") or {}).get(kind) or []
        if not targets:
            out({"ok": False, "error": "no %s nearby" % kind})
            return 1
        t = targets[0]
        trace(session, "walkto", kind=kind, id=t["id"], distance=t["distance"], bearing=t["bearing"],
              x=st["player"]["x"], z=st["player"]["z"], yawDeg=st["player"]["yawDeg"], fps=st.get("fps"))
        if t["distance"] < WALK_STOP:
            out({"ok": True, "kind": kind, "name": t["name"], "id": t["id"], "distance": t["distance"],
                 "bearing": t["bearing"], "seconds": round(time.time() - started, 1),
                 "note": "within reach (Interact range %g); press e" % INTERACT_RANGE})
            return 0
        if time.time() - started > max_seconds:
            out({"ok": False, "error": "timed out", "kind": kind, "distance": t["distance"], "bearing": t["bearing"]})
            return 1
        # Stuck means walking towards the same target without getting closer.
        # The nearest target often changes on the way (pawns chop trees too),
        # and turning on the spot never closes the distance, so neither counts.
        if t["id"] != target_id:
            target_id, best, stalls = t["id"], t["distance"], 0
        elif walked and t["distance"] > best - 0.5:
            stalls += 1
            if stalls >= 4:
                out({"ok": False, "error": "stuck: not getting closer", "kind": kind, "distance": t["distance"],
                     "bearing": t["bearing"], "hint": "take a screenshot; try face 45 then hold w 1, or jump (space)"})
                return 1
        else:
            best = min(best, t["distance"])
            stalls = 0 if walked else stalls
        walked = False
        if abs(t["bearing"]) > 10.0:
            face(session, t["bearing"], tolerance=6.0, attempts=5)
            continue
        speed = (st.get("player") or {}).get("speed") or 25
        hold("w", min(1.0, max(0.05, (t["distance"] - WALK_STOP + 1.5) / float(speed))))
        walked = True


def cmd_await(session, screen, seconds="15"):
    deadline = time.time() + float(seconds)
    st = None
    while time.time() < deadline:
        st = read_state(session)
        if st is not None and st.get("screen") == screen:
            out({"ok": True, "screen": screen})
            return 0
        time.sleep(0.2)
    out({"ok": False, "error": "still not on " + screen, "screen": st and st.get("screen")})
    return 1


def cmd_note(session, shot, text):
    entry = {"t": now_ms(), "text": text}
    if shot:
        entry["screenshot"] = shot
    st = read_state(session)
    if st and st.get("world"):
        entry["tick"] = st.get("tick")
    append_json(os.path.join(session, "findings.jsonl"), entry)
    out(entry)


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 2
    cmd, args = argv[1], argv[2:]
    handlers = {
        "log-action": cmd_log_action, "started": cmd_started, "state": cmd_state, "status": cmd_status,
        "drag": cmd_drag, "face": cmd_face, "walkto": cmd_walkto, "note": cmd_note, "await": cmd_await,
    }
    if cmd not in handlers:
        print("pthelper: unknown command " + cmd, file=sys.stderr)
        return 2
    result = handlers[cmd](*args)
    return result if isinstance(result, int) else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
