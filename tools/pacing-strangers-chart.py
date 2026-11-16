"""Draws strangers (pawns in no nation) and nations alive over time, before and after a change, from
two pacing-report.json files, with ImageMagick (no SVG delegate or matplotlib needed).

usage: python3 tools/pacing-strangers-chart.py BEFORE.json AFTER.json OUT.png FONT.ttf [TITLE] [MINUTES]

Two panels share the time axis: the `nationless` curve on top and the `nations` curve below, each
sampled every 30 s. Each seed is drawn thin and the mean over the seeds thick: grey for BEFORE, blue
for AFTER. Day 1 sunset and the start of day 2 (when the early cap on new nations lifts) are marked.
MINUTES is how far the chart runs (default 15).
"""
import json
import subprocess
import sys

before_path, after_path, out, font = sys.argv[1:5]
title = sys.argv[5] if len(sys.argv) > 5 else "Strangers and nations, seeds 1 to 5"
minutes = int(sys.argv[6]) if len(sys.argv) > 6 else 15
before = json.load(open(before_path))["runs"]
after = json.load(open(after_path))["runs"]

TPS = 50
END_S = minutes * 60
W, H = 1100, 820
L, R, T = 80, 230, 70
GAP = 70
PH = 300  # height of each panel
PW = W - L - R
MARKS = ((270, "day 1 sunset"), (420, "day 2: cap lifts"))
GREY, GREY_THIN = "#6f6f6f", "#d0d0d0"
BLUE, BLUE_THIN = "#1f6fbf", "#a9c8ea"

draw = []


def text(px, py, s, size=16, color="#222"):
    s = s.replace("'", "\\'")
    draw.append(["-fill", color, "-stroke", "none", "-pointsize", str(size), "-draw", "text %d,%d '%s'" % (px, py, s)])


def line(x1, y1, x2, y2, color="#999", width=1, dash=False):
    args = ["-stroke", color, "-strokewidth", str(width), "-fill", "none"]
    if dash:
        args += ["-draw", "stroke-dasharray 6 5 line %d,%d %d,%d" % (x1, y1, x2, y2)]
    else:
        args += ["-draw", "line %d,%d %d,%d" % (x1, y1, x2, y2)]
    draw.append(args)


def polyline(points, color, width):
    if len(points) < 2:
        return
    pts = " ".join("%d,%d" % p for p in points)
    draw.append(["-stroke", color, "-strokewidth", str(width), "-fill", "none", "-draw", "polyline " + pts])


def x(seconds):
    return L + PW * seconds / END_S


def series(run, field):
    return [(p["tick"] / TPS, p[field]) for p in run["samples"] if p["tick"] <= END_S * TPS]


def mean(runs, field):
    by_t = {}
    for run in runs:
        for t, v in series(run, field):
            by_t.setdefault(t, []).append(v)
    return [(t, sum(v) / len(v)) for t, v in sorted(by_t.items())]


def nice_max(field):
    top = 5
    for run in before + after:
        for _, v in series(run, field):
            top = max(top, v)
    step = 5 if top <= 30 else 10
    return ((top + step - 1) // step) * step, step


def panel(top, field, label):
    ymax, step = nice_max(field)

    def y(v):
        return top + PH - PH * v / ymax

    for v in range(0, ymax + 1, step):
        line(L, y(v), L + PW, y(v), "#e4e4e4")
        text(L - 34, y(v) + 5, str(v), 14, "#555")
    line(L, top + PH, L + PW, top + PH, "#555", 2)
    line(L, top, L, top + PH, "#555", 2)
    for s, mark in MARKS:
        if s <= END_S:
            line(x(s), top, x(s), top + PH, "#c9a227", 1, True)
            text(x(s) + 6, top + 16, mark, 13, "#8a6d00")
    for runs, thin in ((before, GREY_THIN), (after, BLUE_THIN)):
        for run in runs:
            polyline([(x(t), y(v)) for t, v in series(run, field)], thin, 1)
    for runs, color in ((before, GREY), (after, BLUE)):
        polyline([(x(t), y(v)) for t, v in mean(runs, field)], color, 4)
    text(L, top - 12, label, 16, "#111")


top1 = T + 10
top2 = top1 + PH + GAP
panel(top1, "nationless", "Strangers: pawns in no nation, who can be invited")
panel(top2, "nations", "Nations alive")
step_min = 1 if minutes <= 15 else 5
for minute in range(0, minutes + 1, step_min):
    line(x(minute * 60), top2 + PH, x(minute * 60), top2 + PH + 6, "#555")
    text(x(minute * 60) - 16, top2 + PH + 26, "%d min" % minute, 14, "#555")

text(L, 36, title, 20, "#111")
lx = L + PW + 24
line(lx, top1 + 40, lx + 30, top1 + 40, GREY, 4)
text(lx + 38, top1 + 46, "before (mean)", 15)
line(lx, top1 + 72, lx + 30, top1 + 72, BLUE, 4)
text(lx + 38, top1 + 78, "this change (mean)", 15)
line(lx, top1 + 104, lx + 30, top1 + 104, GREY_THIN, 1)
line(lx, top1 + 110, lx + 30, top1 + 110, BLUE_THIN, 1)
text(lx + 38, top1 + 112, "each seed", 15)

cmd = ["convert", "-size", "%dx%d" % (W, H), "xc:white", "-font", font]
for d in draw:
    cmd += d
cmd.append(out)
subprocess.run(cmd, check=True)
