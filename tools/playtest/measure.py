"""Measures what N concurrent playtest sessions cost on this machine.

    python3 -I tools/playtest/measure.py N [SETTLE_SEC] [SAMPLE_SEC] [--brief]

Starts N sessions with pt.sh (straight into a world: --start-screen world),
lets them settle, then samples CPU (from /proc, so it is the rate over the
sample window, not ps's lifetime average), resident memory of each game and
its Xvfb, and the frame rate each game reports in state.json. Stops every
session it started, even on Ctrl-C. PT_WIDTH, PT_HEIGHT and PT_LP_THREADS
are passed through, so configurations can be compared:

    PT_WIDTH=800 PT_HEIGHT=450 PT_LP_THREADS=1 python3 -I tools/playtest/measure.py 10

Prints one JSON object; docs/PLAYTESTING.md has the table it fed.
"""

import json
import os
import statistics
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
PT = os.path.join(HERE, "pt.sh")
TICK = os.sysconf("SC_CLK_TCK")


def cpu_ticks(pid):
    try:
        with open("/proc/%d/stat" % pid) as f:
            fields = f.read().rsplit(")", 1)[1].split()
        return int(fields[11]) + int(fields[12])  # utime + stime
    except (OSError, IndexError, ValueError):
        return None


def rss_mb(pid):
    try:
        with open("/proc/%d/status" % pid) as f:
            for line in f:
                if line.startswith("VmRSS:"):
                    return int(line.split()[1]) / 1024.0
    except OSError:
        pass
    return None


def read_pid(path):
    try:
        with open(path) as f:
            return int(f.read().strip())
    except (OSError, ValueError):
        return None


def mem_available_mb():
    with open("/proc/meminfo") as f:
        for line in f:
            if line.startswith("MemAvailable:"):
                return int(line.split()[1]) / 1024.0
    return None


def main(argv):
    if len(argv) < 2:
        print(__doc__.strip())
        return 2
    args = [a for a in argv[1:] if not a.startswith("--")]
    n = int(args[0])
    settle = float(args[1]) if len(args) > 1 else 30.0
    window = float(args[2]) if len(args) > 2 else 20.0
    root = os.environ.get("PT_ROOT", "/tmp/bn-playtest") + "/measure-%d-%d" % (n, int(time.time()))
    env = dict(os.environ, PT_ROOT=root)
    names = ["m%02d" % i for i in range(1, n + 1)]
    mem_before = mem_available_mb()
    load_before = os.getloadavg()[0]
    started = []
    try:
        procs = []
        for i, name in enumerate(names):
            e = dict(env, PT_SESSION=name)
            procs.append((name, subprocess.Popen([PT, "start", "--seed", str(100 + i), "--start-screen", "world", "--no-saves"],
                                                 env=e, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, universal_newlines=True)))
        t0 = time.time()
        for name, p in procs:
            output = p.communicate()[0]
            if p.returncode == 0:
                started.append(name)
            else:
                print("start failed for %s:\n%s" % (name, output), file=sys.stderr)
        startup = time.time() - t0
        time.sleep(settle)
        pids = {}
        for name in started:
            d = os.path.join(root, name)
            pids[name] = (read_pid(os.path.join(d, "game.pid")), read_pid(os.path.join(d, "xvfb.pid")))
        before = {name: (cpu_ticks(g), cpu_ticks(x)) for name, (g, x) in pids.items()}
        time.sleep(window)
        rows = []
        for name, (g, x) in pids.items():
            g1, x1 = cpu_ticks(g), cpu_ticks(x)
            g0, x0 = before[name]
            st = {}
            try:
                with open(os.path.join(root, name, "out", "state.json")) as f:
                    st = json.load(f)
            except (OSError, ValueError):
                pass
            rows.append({
                "session": name,
                "gameCpuCores": round((g1 - g0) / TICK / window, 2) if None not in (g0, g1) else None,
                "xvfbCpuCores": round((x1 - x0) / TICK / window, 2) if None not in (x0, x1) else None,
                "gameRssMb": round(rss_mb(g) or 0, 1),
                "xvfbRssMb": round(rss_mb(x) or 0, 1),
                "fps": st.get("fps"),
                "tps": st.get("tps"),
                "screen": st.get("screen"),
            })
        mem_after = mem_available_mb()
        fps = [r["fps"] for r in rows if isinstance(r["fps"], int)]
        cores = [r["gameCpuCores"] + (r["xvfbCpuCores"] or 0) for r in rows if r["gameCpuCores"] is not None]
        result = {
            "sessions": n, "started": len(started), "size": "%sx%s" % (env.get("PT_WIDTH", "960"), env.get("PT_HEIGHT", "540")),
            "lpThreads": env.get("PT_LP_THREADS", "2"), "cpuCount": os.cpu_count(), "loadBefore": round(load_before, 1),
            "startupSec": round(startup, 1),
            "totalCpuCores": round(sum(cores), 1),
            "perSessionCpuCores": round(statistics.mean(cores), 2) if cores else None,
            "perSessionRssMb": round(statistics.mean([r["gameRssMb"] + r["xvfbRssMb"] for r in rows]), 0) if rows else None,
            "memUsedMb": round(mem_before - mem_after, 0) if mem_before and mem_after else None,
            "fpsMin": min(fps) if fps else None, "fpsMedian": statistics.median(fps) if fps else None,
            "fpsMax": max(fps) if fps else None,
            "tpsMin": min([r["tps"] for r in rows if r["tps"] is not None] or [None]) if rows else None,
            "rows": rows,
        }
        if "--brief" not in argv:
            print(json.dumps(result, indent=2))
        else:
            result.pop("rows")
            print(json.dumps(result))
    finally:
        for name in names:
            subprocess.run([PT, "stop"], env=dict(env, PT_SESSION=name), stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
