"""Draws the winter seed sweep, before and after a change, from two directories of per-seed CSVs
written by `BeyondNations.WinterSweep world --csv DIR`, with ImageMagick (no matplotlib needed).

usage: python3 tools/winter-sweep-chart.py BEFORE_DIR AFTER_DIR SEEDS OUT.png FONT.ttf [TARGET] [OLD_TARGET]

Three panels share the time axis, autumn to the end of the run: food each nation member carries,
the share of members hungry, and pawns dead since autumn began. Each line is the mean over SEEDS
(as 41-56 or 41,42,50): grey for BEFORE, blue for AFTER. TARGET (and OLD_TARGET, if the change
moves it) draws the winter food target on the food panel. See tools/BeyondNations.WinterSweep/README.md.
"""
import csv
import subprocess
import sys

before_dir, after_dir, seeds_arg, out, font = sys.argv[1:6]
target = int(sys.argv[6]) if len(sys.argv) > 6 else None
old_target = int(sys.argv[7]) if len(sys.argv) > 7 else None


def parse_seeds(text):
    seeds = []
    for part in text.split(","):
        if "-" in part[1:]:
            a, b = part.split("-", 1)
            seeds.extend(range(int(a), int(b) + 1))
        elif part:
            seeds.append(int(part))
    return seeds


seeds = parse_seeds(seeds_arg)

def load(d, seed):
    rows = list(csv.DictReader(open("%s/%d.csv" % (d, seed))))
    return rows

def series(d):
    runs = [load(d, s) for s in seeds]
    by_t = {}
    for rows in runs:
        d0 = None
        for r in rows:
            t = int(r["tick"])
            if t < START or t > END:
                continue
            if d0 is None:
                d0 = int(r["deaths"])
            m = int(r["members"]) or 1
            by_t.setdefault(t, []).append((int(r["food"]) / m, 100.0 * int(r["hungry"]) / m, int(r["deaths"]) - d0))
    out = []
    for t in sorted(by_t):
        v = by_t[t]
        if len(v) != len(seeds):
            continue
        out.append((t, sum(x[0] for x in v) / len(v), sum(x[1] for x in v) / len(v), sum(x[2] for x in v) / len(v)))
    return out

first = load(before_dir, seeds[0])
START = next(int(r["tick"]) for r in first if r["season"] == "AUTUMN")
WINTER = next(int(r["tick"]) for r in first if r["season"] == "WINTER")
SPRING = next(int(r["tick"]) for r in first if r["season"] == "SPRING" and int(r["tick"]) > WINTER)
END = int(first[-1]["tick"])

before = series(before_dir)
after = series(after_dir)

W, H = 1100, 1060
L, R, T = 80, 250, 70
GAP = 70
PH = 250
PW = W - L - R
GREY, BLUE = "#6f6f6f", "#1f6fbf"
draw = []

def text(px, py, s, size=16, color="#222"):
    s = s.replace("'", "\\'")
    draw.append(["-fill", color, "-stroke", "none", "-pointsize", str(size), "-draw", "text %d,%d '%s'" % (px, py, s)])

def line(x1, y1, x2, y2, color="#999", width=1, dash=False):
    args = ["-stroke", color, "-strokewidth", str(width), "-fill", "none"]
    args += ["-draw", ("stroke-dasharray 6 5 " if dash else "") + "line %d,%d %d,%d" % (x1, y1, x2, y2)]
    draw.append(args)

def polyline(points, color, width):
    # one line per segment: a long polyline or path drew a thin stray chord
    args = ["-stroke", color, "-strokewidth", str(width), "-fill", "none"]
    for (x1, y1), (x2, y2) in zip(points, points[1:]):
        args += ["-draw", "line %d,%d %d,%d" % (x1, y1, x2, y2)]
    draw.append(args)

def x(t):
    return L + PW * (t - START) / (END - START)

def panel(top, idx, label, ymax, step, refs=()):
    def y(v):
        return top + PH - PH * min(v, ymax) / ymax
    for v in range(0, ymax + 1, step):
        line(L, y(v), L + PW, y(v), "#e4e4e4")
        text(L - 40, y(v) + 5, str(v), 14, "#555")
    line(L, top + PH, L + PW, top + PH, "#555", 2)
    line(L, top, L, top + PH, "#555", 2)
    for t, mark in ((WINTER, "winter"), (SPRING, "spring")):
        line(x(t), top, x(t), top + PH, "#7aa7d6", 1, True)
        text(x(t) + 6, top + 16, mark, 13, "#2b5f95")
    for v, col, lab in refs:
        line(L, y(v), L + PW, y(v), col, 2, True)
        text(L + PW + 8, y(v) + 5, lab, 13, col)
    polyline([(x(p[0]), y(p[idx])) for p in before], GREY, 4)
    polyline([(x(p[0]), y(p[idx])) for p in after], BLUE, 4)
    text(L, top - 12, label, 16, "#111")

top1 = T + 10
top2 = top1 + PH + GAP
top3 = top2 + PH + GAP
refs = []
if old_target is not None:
    refs.append((old_target, "#b5651d", "old target %d" % old_target))
if target is not None:
    refs.append((target, "#2e7d32", ("new target %d" if old_target is not None else "target %d") % target))
panel(top1, 1, "Food each member carries", 16, 4, refs)
panel(top2, 2, "Members hungry (%), needing food and carrying none", 100, 20)
panel(top3, 3, "Pawns dead since autumn began", 16, 4)
day = 30000
t = START - (START % day) + day
while t <= END:
    line(x(t), top3 + PH, x(t), top3 + PH + 6, "#555")
    text(x(t) - 20, top3 + PH + 26, "day %d" % (t // day), 14, "#555")
    t += day
text(L, 36, "Autumn to spring, mean of seeds %d-%d (headless AI world)" % (seeds[0], seeds[-1]), 20, "#111")
lx = L + PW + 24
line(lx, top2 + 40, lx + 30, top2 + 40, GREY, 4)
text(lx + 38, top2 + 46, "before", 15)
line(lx, top2 + 72, lx + 30, top2 + 72, BLUE, 4)
text(lx + 38, top2 + 78, "after", 15)

cmd = ["convert", "-size", "%dx%d" % (W, H), "xc:white", "-font", font]
for d in draw:
    cmd += d
cmd.append(out)
subprocess.run(cmd, check=True)
print("wrote", out)
