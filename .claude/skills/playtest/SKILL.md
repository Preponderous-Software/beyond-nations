---
name: playtest
description: Run a parallel AI playtest of this game with playtester - build the game once, check the harness with a scripted smoke session, spawn N subagents (default 10) that each play the real build headlessly toward a different goal through the pt harness, then aggregate their report.json files into summary.md, commit the evidence and offer to file issues for confirmed blocker/major findings. Use when asked to playtest the game, run playtesters, or find UX/balance/bug issues by playing.
---

# Playtest (coordinator)

PROFILE=beyond-nations

You coordinate; subagents play. The harness, the agent prompt, the report
schema and the aggregator come from **playtester** (`pip install playtester`
or `pip install -e <checkout>`; its docs/AGENT_PLAYTESTING.md explains the
kit). This game's profile is `$PROFILE` (`pt profiles` lists the bundled
ones; a path to a profile JSON file works too). Do not play yourself and do
not fix anything you find: the output is a summary, committed evidence and,
if the owner agrees, issues.

`pt` below is the playtester command line. If it is not on `PATH`, use
`python3 -m playtester` with the interpreter playtester is installed for.

## 1. Preflight

```bash
pt --version && pt profiles                   # playtester installed? which profiles?
which Xvfb xdotool import python3             # system tools (docs/AGENT_PLAYTESTING.md)
pt validate-profile $PROFILE                  # its build command and toolchain (build.requires)
pgrep -a Xvfb                                 # anything already running?
nproc; free -g; cat /proc/loadavg
```

Pick N from the cost table in playtester's docs/AGENT_PLAYTESTING.md and
from `pt measure` on this machine (`pt measure 5 --profile $PROFILE --brief`):
software rendering costs one to four cores a session, and a busy machine
slows every game's world. If the box is already busy, use fewer.

## 2. Build once

Run the profile's build command (`pt validate-profile $PROFILE` prints it)
in this checkout, with its toolchain on `PATH`. Every session runs
this one build. Record the commit: `git rev-parse --short HEAD`.

## 3. Create the run directory and smoke the harness

```bash
RUN=/tmp/playtest-$PROFILE/$(date +%Y%m%d-%H%M)
mkdir -p "$RUN"
PT_ROOT="$RUN/_smoke" pt smoke --profile $PROFILE --game-dir "$PWD"
```

It must print `PASS` (a minute or two). If it fails, read
`$RUN/_smoke/smoke/game.log` and the `fail.png` screenshot and fix the
environment first (Troubleshooting in docs/AGENT_PLAYTESTING.md).

## 4. Choose goals

Use the owner's goals if given; otherwise the profile's `goals`, in order
(take the first N). Each has a time budget and usually a seed; give every
session a distinct seed unless the goal needs the same world. Write goals
the way docs/AGENT_PLAYTESTING.md ("Writing goals") describes: one focus,
sized to game time, something to measure.

## 5. Spawn the playtesters

For each session `s01`..`sNN`, fill the agent prompt:

```bash
pt prompt --profile $PROFILE --goal-index 1 --session s01 --run-dir "$RUN" --game-dir "$PWD" > "$RUN/s01.prompt.md"
# or with your own goal: --goal "..." --seed 101 --budget 30 [--start-args "--season autumn"]
```

Spawn all N subagents **in one message**, in parallel, general-purpose, each
with the full text of its filled prompt. Do not give them worktree
isolation: they must not modify the repository and need no copy of it.
Stagger nothing; `pt start` takes a lock for the X display, so simultaneous
starts are safe. Each agent keeps every file inside its own session
directory (rule 7 of the prompt): in an early run, agents sharing a scratch
directory overwrote each other's `pt` wrappers and sent keys to the wrong
game.

## 6. Wait, then clean up

Wait for every subagent to report back (do not poll their files). Then make
sure nothing is left running:

```bash
for d in "$RUN"/s*/; do PT_ROOT="$RUN" PT_SESSION=$(basename "$d") pt stop; done
pgrep -a Xvfb; pgrep -af "<the profile's process_name>"
```

Never `pkill -f` a pattern that also matches your own shell.

## 7. Aggregate and present

```bash
pt aggregate "$RUN"
```

Read `$RUN/summary.md` and present it: the outcome table, the blocker and
major groups with their screenshots (open the PNGs and look before you
vouch for a finding), and the problems section (missing or invalid reports,
crashes). Say which findings more than one agent hit, and check each
group's "Also reported as" titles: grouping is a heuristic.

## 8. Commit the evidence

Testing evidence goes into this repository, in a pull request to the branch
the project integrates into (`develop` where there is one), under the
profile's `evidence_dir` (default `docs/playtests/`) in a directory named
after the version that was tested. A build past the last release tag is
experimental: name the directory for the **next expected version** and
record the exact commit in its README. Whoever cuts the release renames the
directory if the release turns out to be a different version.

Copy in `summary.md`, `summary.json`, every session's `report.json`,
`session.json`, `actions.jsonl`, `findings.jsonl`, `out/events.jsonl` and the
screenshots that findings cite. Leave out `game.log`, `state.json`, the
`data/` folder, the filled prompts and screenshots no finding uses. Keep the
total small (shrink screenshots if needed). Write a README with the build,
commit, date, goals, seeds, budgets, sessions and anything that went wrong in
the run.

## 9. Offer issues

For **confirmed** blocker and major findings only (you looked at the
screenshot and the repro is plausible), offer to file GitHub issues. Before
filing, dedupe against open issues:

```bash
gh issue list --state open --limit 200 --search "<key words>"
```

Only after the owner says yes: one issue per finding group, with the title,
detail, repro, seed, build commit, the session(s) that saw it, and the
screenshots attached or described. Follow the repository's conventions for
issue text.
