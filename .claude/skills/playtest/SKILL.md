---
name: playtest
description: Run a parallel AI playtest of Beyond Nations - build the desktop game once, spawn N subagents (default 10) that each play the real build headlessly toward a different goal through tools/playtest/pt.sh, then aggregate their report.json files into summary.md and offer to file issues for confirmed blocker/major findings. Use when asked to playtest the game, run playtesters, or find UX/balance/bug issues by playing.
---

# Playtest (coordinator)

You coordinate; subagents play. The kit is documented in
`docs/PLAYTESTING.md`; the subagent prompt is `tools/playtest/AGENT_PROMPT.md`.
Do not play yourself and do not fix anything you find: the output is a
summary and, if the owner agrees, issues.

## 1. Preflight

```bash
export PATH=/root/.dotnet:$PATH            # wherever dotnet lives
which Xvfb xdotool import python3          # docs/PLAYTESTING.md lists the packages
pgrep -a Xvfb; pgrep -af 'BeyondNations.Desktop '   # anything already running?
nproc; free -g; cat /proc/loadavg
```

Pick N from the measured table in docs/PLAYTESTING.md: up to about 6
sessions keeps each world near full speed on a 20-core box; 10 works, with
the worlds running at roughly a third of real time. If the box is already
busy, use fewer.

## 2. Build once

```bash
dotnet build src/BeyondNations.Desktop/BeyondNations.Desktop.csproj -c Release
```

Every session runs this one binary (`pt.sh` finds it). Record the commit:
`git rev-parse --short HEAD`.

## 3. Create the run directory

```bash
RUN=/tmp/bn-playtest/$(date +%Y%m%d-%H%M)
mkdir -p "$RUN"
```

Then quick-check the harness with one scripted session before spending
agent time (about a minute):

```bash
PT_ROOT="$RUN/_smoke" PT_SESSION=smoke tools/playtest/smoke.sh 42
```

It must print `PASS`. If it fails, read `$RUN/_smoke/smoke/game.log` and fix
the environment first (docs/PLAYTESTING.md, Troubleshooting).

## 4. Choose goals

Use the owner's goals if given; otherwise these ten, in this order (take the
first N):

1. Play the first 10 minutes as a brand-new player who has never seen the game; note every moment of confusion.
2. Reach the Village era as fast as you can (it cannot come before day 3, about 17 minutes of game time); record the game time of every objective.
3. Survive the first winter. Start with `--season autumn` (a fresh world otherwise reaches winter only after about 57 minutes of game time). Note how food and cold feel.
4. Diplomacy and war: lead a nation, meet a neighbour, try alliance, rivalry and war from the Nations screen (O).
5. Trade and caravans: build a market stall, buy and sell, get a Storehouse and send a caravan.
6. Ruler, laws and heirs: lead a nation, enact and repeal laws, and see what happens when the ruler dies.
7. Crafting: find and use every recipe on the crafting screen (C); are tools worth making?
8. World events: play until world events are offered, take different choices, and judge whether the consequences are clear and fair.
9. Accessibility and text audit, touching only what a mouse-and-keyboard player would: every screen and panel; cut-off, overlapping, unclear or misspelled text; contrast; anything only explained on hover.
10. Save/load and share-seed round trip: save from the pause menu, stop, `pt start --continue`, confirm the world is identical; then start a new world from the seed shown on the Share screen and confirm it matches.

Give each a distinct seed (e.g. 101..110) unless the goal needs the same
world; goal 10 should use a seed given as text, e.g. `Ironvale`.

## 5. Spawn the playtesters

Read `tools/playtest/AGENT_PROMPT.md`, take everything below its `---`, and
fill in `{{GOAL}}`, `{{SESSION}}` (s01..sNN), `{{RUN_DIR}}` ($RUN, absolute),
`{{SEED}}`, `{{TIME_BUDGET_MIN}}` and `{{REPO}}` (the absolute path of this
checkout). Budget about 30 minutes, and 60 for goals 2 and 3: a day is 10
minutes of game time, and with many sessions the worlds run slower than
real time (docs/PLAYTESTING.md, resource table). Spawn all N subagents **in one message**, in
parallel, general-purpose, each with its filled prompt. Do not give them
worktree isolation: they must not modify the repo and need no copy of it.

Stagger nothing; `pt.sh start` takes a lock for the X display, so
simultaneous starts are safe.

## 6. Wait, then clean up

Wait for every subagent to report back (do not poll their files). Then make
sure nothing is left running:

```bash
for d in "$RUN"/s*/; do PT_ROOT="$RUN" PT_SESSION=$(basename "$d") tools/playtest/pt.sh stop; done
pgrep -a Xvfb; pgrep -af 'BeyondNations.Desktop '
```

Never `pkill -f` a pattern that also matches your own shell.

## 7. Aggregate and present

```bash
python3 -I tools/playtest/aggregate.py "$RUN"
```

Read `$RUN/summary.md` and present it: the outcome table, the blocker and
major groups with their screenshots (open the PNGs and look before you
vouch for a finding), and the problems section (missing or invalid reports,
crashes). Say which findings more than one agent hit.

## 8. Offer issues

For **confirmed** blocker and major findings only (you looked at the
screenshot and the repro is plausible), offer to file GitHub issues. Before
filing, dedupe against open issues:

```bash
gh issue list --repo Preponderous-Software/beyond-nations --state open --limit 200 --search "<key words>"
```

Only after the owner says yes: one issue per finding group, with the
title, detail, repro, seed, build commit, the session(s) that saw it, and the
screenshots attached or described. Follow the repo's conventions for issue
text (passive voice, attribution footer).
