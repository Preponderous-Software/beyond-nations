"""Merges every session of a playtest run into summary.md and summary.json.

    python3 -I tools/playtest/aggregate.py RUN_DIR

RUN_DIR holds one directory per session (pt.sh's PT_ROOT/<run>/<session>),
each with the agent's report.json and the game's out/events.jsonl. The
summary has a per-goal outcome table, the findings of every agent grouped
so that near-duplicates reported by different agents become one entry,
counts by severity, objective timings across sessions taken from
events.jsonl, and links to the screenshots. Standard library only; see
docs/PLAYTESTING.md.
"""

import datetime
import difflib
import importlib.util
import json
import os
import re
import statistics
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SEVERITIES = ["blocker", "major", "minor", "polish"]
SIMILAR = 0.6
# GameOptions.TicksPerSecond; timings are game time, which a busy machine slows down but never distorts
TICKS_PER_SECOND = 50.0
STOPWORDS = {"a", "an", "the", "is", "are", "was", "be", "of", "to", "in", "on", "at", "and", "or", "it", "its",
             "when", "while", "with", "for", "by", "from", "after", "can", "not", "no", "does", "do", "this", "that"}


def load_validator():
    # -I leaves this directory off sys.path, so the sibling is loaded by path
    # (and no __pycache__ is left beside it)
    sys.dont_write_bytecode = True
    spec = importlib.util.spec_from_file_location("validate_report", os.path.join(HERE, "validate_report.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def read_json(path):
    try:
        with open(path, encoding="utf-8") as f:
            return json.load(f), None
    except OSError:
        return None, "missing"
    except ValueError as e:
        return None, "not valid JSON (%s)" % e


def read_jsonl(path):
    rows = []
    try:
        with open(path, encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line:
                    try:
                        rows.append(json.loads(line))
                    except ValueError:
                        pass
    except OSError:
        pass
    return rows


def derive_metrics(events):
    """The standard metrics, from the game's own events rather than the agent's account."""
    objectives, eras, deaths, pawn_deaths, offered, resolved, saves, loads = [], [], 0, 0, 0, 0, 0, 0
    world_started = None
    for e in events:
        kind = e.get("type")
        if kind == "world":
            # timings run from the latest world; a load continues a world, so it does not restart the clock
            if e.get("how") == "created" or world_started is None:
                world_started = e.get("wallTimeMs")
            if e.get("how") == "loaded":
                loads += 1
        elif kind == "objective_completed":
            wall = None
            if world_started is not None and e.get("wallTimeMs") is not None:
                wall = round((e["wallTimeMs"] - world_started) / 1000.0, 1)
            # the tick counter starts at 0 with the world and is saved with it, so it is game time since the start
            seconds = round(e["tick"] / TICKS_PER_SECOND, 1) if isinstance(e.get("tick"), int) else None
            objectives.append({"number": e.get("number"), "title": e.get("title"), "seconds": seconds,
                               "wallSeconds": wall, "tick": e.get("tick")})
        elif kind == "era_changed":
            eras.append(e.get("to"))
        elif kind == "player_death":
            deaths += 1
        elif kind == "pawn_death":
            pawn_deaths += e.get("count", 1)
        elif kind == "world_event_offered":
            offered += 1
        elif kind == "world_event_resolved":
            resolved += 1
        elif kind == "save":
            saves += 1
    played = world_started is not None
    return {
        "objectivesCompleted": len(objectives),
        "objectives": objectives,
        "eraReached": eras[-1] if eras else ("Camp" if played else None),
        "deaths": deaths,
        "pawnDeaths": pawn_deaths,
        "worldEventsOffered": offered,
        "worldEventsResolved": resolved,
        "saves": saves,
        "loads": loads,
        "lastTick": max([e.get("tick", 0) for e in events] or [0]),
    }


def crashed(session_dir):
    try:
        with open(os.path.join(session_dir, "game.log"), encoding="utf-8", errors="replace") as f:
            text = f.read()
    except OSError:
        return None
    match = re.search(r"\[error\] the (game crashed|host failed to start): (.*)", text)
    return match.group(0) if match else None


def stem(word):
    """A crude suffix strip, so "truncated", "truncates" and "truncate" compare equal."""
    for suffix in ("ing", "ed", "es", "s", "e"):
        if word.endswith(suffix) and len(word) - len(suffix) >= 3:
            return word[:-len(suffix)]
    return word


def normalise_title(title):
    """Lower case, no punctuation, no filler words, no window sizes or bare numbers, crudely stemmed."""
    words = re.sub(r"[^a-z0-9 ]+", " ", title.lower()).split()
    return " ".join(stem(w) for w in words
                    if w not in STOPWORDS and not re.fullmatch(r"\d+(x\d+)?", w))


def similarity(a, b):
    """Near-duplicate titles: a character ratio, or enough shared words."""
    if not a or not b:
        return 0.0
    ratio = difflib.SequenceMatcher(None, a, b).ratio()
    sa, sb = set(a.split()), set(b.split())
    jaccard = len(sa & sb) / float(len(sa | sb)) if sa | sb else 0.0
    return max(ratio, jaccard)


def group_findings(findings):
    """Greedy grouping, most severe first, so a group is named by its most serious report."""
    ordered = sorted(findings, key=lambda f: (SEVERITIES.index(f["severity"]) if f.get("severity") in SEVERITIES else 9))
    groups = []
    for f in ordered:
        key = normalise_title(f.get("title", ""))
        best, best_score = None, 0.0
        for g in groups:
            score = max(similarity(key, k) for k in g["keys"])
            if score > best_score:
                best, best_score = g, score
        if best is not None and best_score >= SIMILAR:
            best["members"].append(f)
            best["keys"].append(key)
        else:
            groups.append({"keys": [key], "members": [f]})
    result = []
    for g in groups:
        members = g["members"]
        lead = members[0]
        sessions = sorted({m["session"] for m in members})
        result.append({
            "title": lead.get("title"),
            "severity": lead.get("severity"),
            "categories": sorted({m.get("category") for m in members if m.get("category")}),
            "sessions": sessions,
            "reports": len(members),
            "detail": lead.get("detail"),
            "repro": next((m.get("repro") for m in members if m.get("repro")), None),
            "screenshots": [m["screenshot"] for m in members if m.get("screenshot")],
            "alsoTitled": sorted({m.get("title") for m in members[1:] if m.get("title") != lead.get("title")}),
        })
    result.sort(key=lambda g: (SEVERITIES.index(g["severity"]) if g["severity"] in SEVERITIES else 9, -len(g["sessions"]), g["title"] or ""))
    return result


def relative_shot(run_dir, session_dir, shot):
    if not shot:
        return None
    path = shot if os.path.isabs(shot) else os.path.join(session_dir, shot)
    return os.path.relpath(path, run_dir)


def collect(run_dir):
    validator = load_validator()
    schema, _ = read_json(os.path.join(HERE, "report.schema.json"))
    sessions, findings, problems = [], [], []
    for name in sorted(os.listdir(run_dir)):
        session_dir = os.path.join(run_dir, name)
        if not os.path.isdir(session_dir) or name.startswith("."):
            continue
        has_any = any(os.path.exists(os.path.join(session_dir, f)) for f in ("report.json", "session.json", "out"))
        if not has_any:
            continue
        report, report_error = read_json(os.path.join(session_dir, "report.json"))
        events = read_jsonl(os.path.join(session_dir, "out", "events.jsonl"))
        info, _ = read_json(os.path.join(session_dir, "session.json"))
        derived = derive_metrics(events)
        errors = []
        if report is None:
            problems.append("%s: report.json %s" % (name, report_error))
        else:
            errors = validator.check_report(report, schema, session_dir)
            for e in errors:
                problems.append("%s: report.json %s" % (name, e))
        crash = crashed(session_dir)
        if crash:
            problems.append("%s: game.log says %s" % (name, crash))
        r = report if isinstance(report, dict) else {}
        session = {
            "session": name,
            "goal": r.get("goal"),
            "outcome": r.get("outcome", "no report"),
            "fun": r.get("fun"),
            "durationSec": r.get("durationSec"),
            "seed": r.get("seed"),
            "build": r.get("build") or {"version": (info or {}).get("version"), "commit": (info or {}).get("commit")},
            "verdict": r.get("verdict"),
            "reportValid": report is not None and not errors,
            "reported": r.get("metrics") or {},
            "derived": derived,
            "notes": len(read_jsonl(os.path.join(session_dir, "findings.jsonl"))),
            "crash": crash,
        }
        sessions.append(session)
        for f in r.get("findings") or []:
            if not isinstance(f, dict):
                continue
            item = dict(f)
            item["session"] = name
            item["screenshot"] = relative_shot(run_dir, session_dir, f.get("screenshot"))
            findings.append(item)
        if crash and not any((f.get("category") == "crash") for f in r.get("findings") or [] if isinstance(f, dict)):
            findings.append({"session": name, "severity": "blocker", "category": "crash",
                             "title": "Game crashed: " + crash.split(": ", 1)[-1][:100], "detail": crash})
    return sessions, findings, problems


def timings_table(sessions):
    titles = []
    for s in sessions:
        for o in s["derived"]["objectives"]:
            key = (o.get("number") or 0, o.get("title"))
            if key not in titles:
                titles.append(key)
    titles.sort()
    rows = []
    for number, title in titles:
        cells = {}
        for s in sessions:
            for o in s["derived"]["objectives"]:
                if o.get("title") == title and o.get("seconds") is not None:
                    cells[s["session"]] = o["seconds"]
                    break
        values = sorted(cells.values())
        rows.append({"number": number, "title": title, "seconds": cells, "sessions": len(values),
                     "min": values[0] if values else None, "median": statistics.median(values) if values else None,
                     "max": values[-1] if values else None})
    return rows


def md_escape(text):
    return (text or "").replace("|", "\\|").replace("\n", " ")


def fmt(value):
    if value is None:
        return "-"
    if isinstance(value, float):
        return "%g" % round(value, 1)
    return str(value)


def render_markdown(run_dir, summary):
    lines = []
    sessions = summary["sessions"]
    builds = sorted({"%s (%s)" % (s["build"].get("version"), s["build"].get("commit")) for s in sessions if s.get("build")})
    lines.append("# Playtest summary: %s" % os.path.basename(os.path.abspath(run_dir)))
    lines.append("")
    lines.append("%d sessions, build %s, generated %s." % (len(sessions), ", ".join(builds) or "unknown", summary["generated"]))
    lines.append("")
    counts = summary["severityCounts"]
    lines.append("**Findings:** %d reports in %d groups -- %s." % (
        summary["findingReports"], len(summary["findings"]),
        ", ".join("%d %s" % (counts[s], s) for s in SEVERITIES)))
    lines.append("")
    lines.append("## Outcomes")
    lines.append("")
    lines.append("| Session | Goal | Outcome | Fun | Minutes | Objectives | Era | Deaths | Findings | Verdict |")
    lines.append("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |")
    for s in sessions:
        d = s["derived"]
        n = sum(1 for g in summary["findings"] if s["session"] in g["sessions"])
        minutes = round(s["durationSec"] / 60.0, 1) if isinstance(s.get("durationSec"), (int, float)) else None
        lines.append("| %s | %s | %s | %s | %s | %s | %s | %s | %s | %s |" % (
            s["session"], md_escape(s.get("goal")), s["outcome"] + ("" if s["reportValid"] or s["outcome"] == "no report" else " (invalid report)"),
            fmt(s.get("fun")), fmt(minutes), d["objectivesCompleted"], fmt(d["eraReached"]), d["deaths"], n,
            md_escape(s.get("verdict"))))
    lines.append("")
    lines.append("## Findings")
    lines.append("")
    if not summary["findings"]:
        lines.append("None reported.")
    for i, g in enumerate(summary["findings"], 1):
        lines.append("### %d. [%s] %s" % (i, g["severity"], g["title"]))
        lines.append("")
        lines.append("- **Category:** %s; **reported by** %d agent(s): %s" % (", ".join(g["categories"]) or "-", len(g["sessions"]), ", ".join(g["sessions"])))
        if g["alsoTitled"]:
            lines.append("- **Also reported as:** %s" % "; ".join('"%s"' % t for t in g["alsoTitled"]))
        lines.append("- **Detail:** %s" % md_escape(g["detail"]))
        if g["repro"]:
            lines.append("- **Repro:** %s" % md_escape(g["repro"]))
        if g["screenshots"]:
            lines.append("- **Screenshots:** %s" % " ".join("[%s](%s)" % (os.path.basename(p), p) for p in g["screenshots"]))
        lines.append("")
    lines.append("## Objective timings")
    lines.append("")
    lines.append("Game seconds from the world's creation to each objective (tick / 50, from events.jsonl). "
                 "Game time, not wall time: with many sessions on one machine the world runs slower than real time, "
                 "and wall-clock timings would mostly measure the machine.")
    lines.append("")
    if not summary["objectiveTimings"]:
        lines.append("No objective was completed.")
    else:
        names = [s["session"] for s in sessions]
        lines.append("| # | Objective | " + " | ".join(names) + " | min | median | max |")
        lines.append("| --- | --- | " + " | ".join("---" for _ in names) + " | --- | --- | --- |")
        for row in summary["objectiveTimings"]:
            lines.append("| %s | %s | %s | %s | %s | %s |" % (
                fmt(row["number"]), md_escape(row["title"]), " | ".join(fmt(row["seconds"].get(n)) for n in names),
                fmt(row["min"]), fmt(row["median"]), fmt(row["max"])))
    lines.append("")
    if summary["problems"]:
        lines.append("## Problems with the run")
        lines.append("")
        for p in summary["problems"]:
            lines.append("- %s" % md_escape(p))
        lines.append("")
    return "\n".join(lines)


def aggregate(run_dir):
    sessions, findings, problems = collect(run_dir)
    groups = group_findings(findings)
    counts = {s: 0 for s in SEVERITIES}
    for f in findings:
        if f.get("severity") in counts:
            counts[f["severity"]] += 1
    summary = {
        "runDir": os.path.abspath(run_dir),
        "generated": datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
        "sessions": sessions,
        "severityCounts": counts,
        "findingReports": len(findings),
        "findings": groups,
        "objectiveTimings": timings_table(sessions),
        "problems": problems,
    }
    with open(os.path.join(run_dir, "summary.json"), "w", encoding="utf-8") as f:
        json.dump(summary, f, indent=2, ensure_ascii=False)
    with open(os.path.join(run_dir, "summary.md"), "w", encoding="utf-8") as f:
        f.write(render_markdown(run_dir, summary) + "\n")
    return summary


def main(argv):
    if len(argv) != 2 or not os.path.isdir(argv[1]):
        print(__doc__.strip())
        return 2
    summary = aggregate(argv[1])
    print("%s: %d sessions, %d findings in %d groups, %d problems" % (
        os.path.join(argv[1], "summary.md"), len(summary["sessions"]), summary["findingReports"],
        len(summary["findings"]), len(summary["problems"])))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
