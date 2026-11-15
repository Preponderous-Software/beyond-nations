"""Draws the pacing milestone timeline from a pacing-report.json with ImageMagick (no SVG delegate here).

usage: python3 chart.py REPORT.json OUT.png FONT.ttf
"""
import json, subprocess, sys

report, out, font = sys.argv[1], sys.argv[2], sys.argv[3]
data = json.load(open(report))
runs = data["runs"]

W, H = 1200, 120 + 70 * len(runs) + 110
L, R, T = 150, 40, 90
PLOT_W = W - L - R
MAX_MIN = 60.0
def x(minutes):
    return L + PLOT_W * minutes / MAX_MIN

COLORS = {
    "village": "#7a7a7a",
    "storehouse": "#b5651d",
    "caravan": "#1f77b4",
    "succession": "#8e44ad",
}
draw = []
def text(px, py, s, size=16, color="#222", gravity=None):
    s = s.replace("'", "\\'")
    draw.append(["-fill", color, "-stroke", "none", "-pointsize", str(size), "-draw", "text %d,%d '%s'" % (px, py, s)])
def line(x1, y1, x2, y2, color="#999", width=1, dash=False):
    args = ["-stroke", color, "-strokewidth", str(width), "-fill", "none"]
    if dash:
        args += ["-draw", "stroke-dasharray 6 5 line %d,%d %d,%d" % (x1, y1, x2, y2)]
    else:
        args += ["-draw", "line %d,%d %d,%d" % (x1, y1, x2, y2)]
    draw.append(args)
def dot(px, py, color, shape="circle"):
    if shape == "circle":
        draw.append(["-stroke", "#fff", "-strokewidth", "2", "-fill", color, "-draw", "circle %d,%d %d,%d" % (px, py, px + 9, py)])
    elif shape == "square":
        draw.append(["-stroke", "#fff", "-strokewidth", "2", "-fill", color, "-draw", "rectangle %d,%d %d,%d" % (px - 8, py - 8, px + 8, py + 8)])
    else:  # diamond
        draw.append(["-stroke", "#fff", "-strokewidth", "2", "-fill", color, "-draw",
                     "polygon %d,%d %d,%d %d,%d %d,%d" % (px, py - 11, px + 11, py, px, py + 11, px - 11, py)])

text(L, 40, "When late content is first reached, pacing bot, seeds 1-5 (minutes of game time)", 22, "#111")
bottom = T + 70 * len(runs)
# grid
for m in range(0, 61, 10):
    line(x(m), T - 10, x(m), bottom, "#dddddd")
    text(x(m) - 8, bottom + 24, str(m), 15, "#555")
text(L + PLOT_W / 2 - 60, bottom + 48, "minutes from the start", 15, "#555")
# reference lines
line(x(57), T - 10, x(57), bottom, "#c0392b", 2, dash=True)
text(x(57) - 340, T - 16, "before: Storehouse at Town era, day 7 (57 min+)", 14, "#c0392b")
line(x(40), T - 10, x(40), bottom, "#8e44ad", 2, dash=True)
text(x(40) - 150, T - 34, "old age: 40 min into a reign", 14, "#8e44ad")

for i, run in enumerate(runs):
    y = T + 35 + 70 * i
    tps = run["ticksPerSecond"]
    text(20, y + 6, "seed %d" % run["seed"], 17, "#222")
    line(x(0), y, x(MAX_MIN), y, "#bbbbbb", 2)
    village = next((o for o in run["objectives"] if o["id"] == "grow-nation" and o["completed"]), None)
    if village:
        dot(x(village["completedAtTick"] / tps / 60), y, COLORS["village"], "square")
    ms = run.get("milestones", {})
    if "storehouse" in ms:
        dot(x(ms["storehouse"] / tps / 60), y - 14, COLORS["storehouse"], "square")
    if "caravan" in ms:
        dot(x(ms["caravan"] / tps / 60), y + 14, COLORS["caravan"], "circle")
    if "succession" in ms:
        dot(x(ms["succession"] / tps / 60), y, COLORS["succession"], "diamond")

# legend
ly = bottom + 85
items = [("Village era", COLORS["village"], "square"), ("Storehouse built", COLORS["storehouse"], "square"),
         ("first caravan sent", COLORS["caravan"], "circle"), ("rule passes to an heir", COLORS["succession"], "diamond")]
lx = L
for label, color, shape in items:
    dot(lx, ly, color, shape)
    text(lx + 18, ly + 6, label, 15, "#333")
    lx += 230

cmd = ["convert", "-size", "%dx%d" % (W, H), "xc:white", "-font", font]
for d in draw:
    cmd += d
cmd.append(out)
subprocess.run(cmd, check=True)
print("wrote", out)
