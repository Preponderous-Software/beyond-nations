"""Tests for aggregate.py and validate_report.py, without pytest:

    python3 -I tools/playtest/test_aggregate.py

Runs the aggregator over a copy of fixtures/run (three sessions: two with
reports that share a near-duplicate finding, one with no report whose game
crashed) and checks the summary; checks the validator accepts the fixture
reports and names what is wrong with broken ones. Exits non-zero on failure.
"""

import copy
import importlib.util
import json
import os
import shutil
import sys
import tempfile
import traceback

HERE = os.path.dirname(os.path.abspath(__file__))
sys.dont_write_bytecode = True


def load(name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


aggregate = load("aggregate")
validate_report = load("validate_report")

with open(os.path.join(HERE, "report.schema.json"), encoding="utf-8") as f:
    SCHEMA = json.load(f)
with open(os.path.join(HERE, "fixtures", "run", "s01", "report.json"), encoding="utf-8") as f:
    GOOD = json.load(f)


def run_fixture():
    tmp = tempfile.mkdtemp(prefix="bn-aggregate-test-")
    run = os.path.join(tmp, "run")
    shutil.copytree(os.path.join(HERE, "fixtures", "run"), run)
    summary = aggregate.aggregate(run)
    with open(os.path.join(run, "summary.md"), encoding="utf-8") as f:
        markdown = f.read()
    with open(os.path.join(run, "summary.json"), encoding="utf-8") as f:
        on_disk = json.load(f)
    shutil.rmtree(tmp)
    return summary, markdown, on_disk


def test_every_session_is_listed_with_its_outcome():
    summary, markdown, on_disk = run_fixture()
    outcomes = {s["session"]: s["outcome"] for s in summary["sessions"]}
    assert outcomes == {"s01": "achieved", "s02": "partial", "s03": "no report"}, outcomes
    assert on_disk["sessions"][0]["session"] == "s01"
    assert "| s01 | Play the first 10 minutes as a new player | achieved |" in markdown, markdown


def test_near_duplicate_findings_from_different_agents_are_one_group():
    summary, markdown, _ = run_fixture()
    season = [g for g in summary["findings"] if "season panel" in g["title"].lower()]
    assert len(season) == 1, [g["title"] for g in summary["findings"]]
    assert season[0]["sessions"] == ["s01", "s02"], season[0]
    assert season[0]["severity"] == "minor", "a group takes its most severe report"
    assert len(season[0]["screenshots"]) == 2
    assert "s01/shots/season-clipped.png" in markdown
    # unrelated findings stay apart
    assert len(summary["findings"]) == 4, [g["title"] for g in summary["findings"]]


def test_severity_counts_and_order():
    summary, _, _ = run_fixture()
    assert summary["severityCounts"] == {"blocker": 1, "major": 1, "minor": 2, "polish": 1}, summary["severityCounts"]
    severities = [g["severity"] for g in summary["findings"]]
    assert severities == sorted(severities, key=aggregate.SEVERITIES.index), severities


def test_a_crash_in_the_game_log_becomes_a_blocker_and_a_problem():
    summary, _, _ = run_fixture()
    crash = summary["findings"][0]
    assert crash["severity"] == "blocker" and crash["categories"] == ["crash"] and crash["sessions"] == ["s03"], crash
    assert any("s03: report.json missing" == p for p in summary["problems"]), summary["problems"]
    assert any("game crashed" in p for p in summary["problems"]), summary["problems"]


def test_objective_timings_come_from_events():
    summary, markdown, _ = run_fixture()
    rows = {r["title"]: r for r in summary["objectiveTimings"]}
    wood = rows["Gather wood"]
    # game time, from ticks: 2200 and 3000 ticks at 50 a second
    assert wood["seconds"] == {"s01": 44.0, "s02": 60.0}, wood
    assert (wood["min"], wood["median"], wood["max"]) == (44.0, 52.0, 60.0), wood
    assert rows["Gather stone"]["seconds"] == {"s01": 104.0}
    s01 = [s for s in summary["sessions"] if s["session"] == "s01"][0]["derived"]
    s02 = [s for s in summary["sessions"] if s["session"] == "s02"][0]["derived"]
    assert s01["eraReached"] == "Village" and s01["objectivesCompleted"] == 2
    assert s02["deaths"] == 1 and s02["saves"] == 1
    assert "| 1 | Gather wood | 44 | 60 | - | 44 | 52 | 60 |" in markdown, markdown
    # wall time is kept too: s01's world started 10 s after its first event, so 60 s, not 70
    s01_wood = [o for o in s01["objectives"] if o["title"] == "Gather wood"][0]
    assert s01_wood["wallSeconds"] == 60.0, s01_wood


def test_similarity_is_about_wording_not_order():
    n = aggregate.normalise_title
    assert aggregate.similarity(n("Season panel clipped at right edge"), n("The season panel is clipped at the right edge")) >= aggregate.SIMILAR
    assert aggregate.similarity(n("Season panel clipped at right edge"), n("Pawn blocks chopping a tree")) < aggregate.SIMILAR


def test_normalising_ignores_sizes_and_word_endings():
    n = aggregate.normalise_title
    assert set(n("Chronicle strip truncates lines at 1280x720").split()) == set(n("Chronicle line truncated in strip").split())
    assert aggregate.similarity(n("Chronicle lines are truncated with ... in the compact strip"),
                                n("Chronicle strip truncates lines with \"...\" at 1280x720")) >= aggregate.SIMILAR


def test_the_fixture_reports_are_valid():
    for name in ("s01", "s02"):
        path = os.path.join(HERE, "fixtures", "run", name, "report.json")
        with open(path, encoding="utf-8") as f:
            report = json.load(f)
        errors = validate_report.check_report(report, SCHEMA, os.path.dirname(path))
        assert errors == [], (name, errors)


def test_the_validator_names_what_is_wrong():
    bad = copy.deepcopy(GOOD)
    del bad["verdict"]
    bad["outcome"] = "won"
    bad["fun"] = 6
    bad["findings"][0]["severity"] = "critical"
    bad["findings"][1]["tick"] = "soon"
    bad["surprise"] = True
    errors = validate_report.validate(bad, SCHEMA)
    joined = "\n".join(errors)
    for expected in ("missing required 'verdict'", "$.outcome: 'won' is not one of", "$.fun: 6 is above 5",
                     "$.findings[0].severity: 'critical'", "$.findings[1].tick: expected integer or null",
                     "$.surprise: unexpected property"):
        assert expected in joined, (expected, errors)


def test_a_screenshot_that_does_not_exist_is_reported():
    bad = copy.deepcopy(GOOD)
    bad["findings"][0]["screenshot"] = "shots/nope.png"
    errors = validate_report.check_report(bad, SCHEMA, os.path.join(HERE, "fixtures", "run", "s01"))
    assert errors == ["$.findings[0].screenshot: no such file shots/nope.png"], errors


def test_booleans_are_not_integers():
    bad = copy.deepcopy(GOOD)
    bad["fun"] = True
    assert validate_report.validate(bad, SCHEMA) == ["$.fun: expected integer, got bool"]


def main():
    tests = [(name, fn) for name, fn in sorted(globals().items()) if name.startswith("test_") and callable(fn)]
    failed = 0
    for name, fn in tests:
        try:
            fn()
            print("ok   " + name)
        except Exception:
            failed += 1
            print("FAIL " + name)
            traceback.print_exc()
    print("%d passed, %d failed" % (len(tests) - failed, failed))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
