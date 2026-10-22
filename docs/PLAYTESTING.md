# Playtesting with agents

A kit for having several AI agents play the real desktop build at once, each
toward a different goal, and turning what they find into one summary:

- the game writes what is on screen to files when started with
  `--playtest-out DIR` (`state.json`, `events.jsonl`);
- `tools/playtest/pt.sh` runs one isolated session (its own virtual display,
  saves, screenshots and logs) and drives it with keys and clicks;
- each agent follows `tools/playtest/AGENT_PROMPT.md` and writes a
  `report.json` (`tools/playtest/report.schema.json`);
- `tools/playtest/aggregate.py` merges a run into `summary.md` and
  `summary.json`;
- the `/playtest` project skill (`.claude/skills/playtest/SKILL.md`) is the
  coordinator's procedure, start to finish.

The agents play the way a person does: they look at screenshots and press
keys. `state.json` exists because steering by screenshot alone was slow and
imprecise (mouse-look sensitivity is hard to judge from a picture, and
distances cannot be judged at all), so the harness can turn to a bearing and
walk to the nearest tree in a closed loop.

## Prerequisites

Linux (or WSL2) with:

- the .NET 8 SDK (`dotnet`; `DOTNET_ROOT` is set to `/root/.dotnet` by `pt.sh` when that exists)
- `Xvfb`, `xdotool`, ImageMagick (`import`), `flock` and `setsid` (util-linux), Python 3.8 or newer
- Mesa's llvmpipe (`libgl1-mesa-dri`) and the X11 client libraries the CI's
  strata-smoke job installs (see `.github/workflows/ci.yml`)

```bash
sudo apt-get install -y xvfb xdotool imagemagick libgl1-mesa-dri libglfw3
dotnet build src/BeyondNations.Desktop/BeyondNations.Desktop.csproj -c Release
```

Everything in `tools/playtest/` is bash or standard-library Python run with
`python3 -I`; nothing is installed with pip.

## One session by hand

```bash
export PT_SESSION=me PT_ROOT=/tmp/bn-playtest/try    # each shell call needs these
tools/playtest/pt.sh start --seed 42                  # Xvfb + game; waits for the window
tools/playtest/pt.sh shot title                       # prints the PNG path; open it
tools/playtest/pt.sh key space                        # leave the title
tools/playtest/pt.sh click 480 315                    # Start
tools/playtest/pt.sh click 480 315                    # Begin (the seed is filled in)
tools/playtest/pt.sh await WORLD
tools/playtest/pt.sh state                            # compact state.json
tools/playtest/pt.sh walkto tree                      # turn to it, walk until within reach
tools/playtest/pt.sh key e                            # chop
tools/playtest/pt.sh events 5                         # what the game says happened
tools/playtest/pt.sh note --shot hud "the season box is cut off"
tools/playtest/pt.sh stop
```

`pt.sh help` lists every command. `tools/playtest/smoke.sh` is the same thing
scripted: title, Start, Begin, then trees until the *Gather wood* objective
completes, confirmed from `events.jsonl`. It prints `PASS` and is the quickest
check that a machine can run the kit:

```bash
PT_SESSION=smoke tools/playtest/smoke.sh 42
```

### Commands

| Command | |
| --- | --- |
| `start [game args]` | claims a free display (`:100`-`:199`) under a lock, starts Xvfb and the game with `--playtest-out`, waits for the window and the first `state.json`; on failure prints the log tail and cleans up |
| `stop` | stops the game, then its Xvfb (safe to repeat) |
| `status` | running?, display, CPU, memory, fps |
| `key K...`, `hold K SECS` | xdotool key names; combos as `Shift_L+w` |
| `click X Y [BTN]`, `move X Y`, `drag X1 Y1 X2 Y2 SECS [BTN]`, `type TEXT` | window coordinates; a click hovers 0.3 s first |
| `shot NAME` | `shots/NAME.png`; prints the path |
| `state [--full]`, `events [N]`, `await SCREEN [SECS]` | read what the game wrote |
| `face DEG` | turn DEG degrees (+ right), re-reading the yaw until within 8 degrees |
| `walkto KIND [MAXSEC]` | the nearest `tree`, `rock`, `pawn`, `chicken`, `settlement` or `stall`: face it, walk in bursts sized from the distance, re-face, stop under 4 units (Interact reaches 5, `InteractCommand`) |
| `note [--shot NAME] TEXT` | append to `findings.jsonl` |
| `report` | validate `report.json` against the schema |
| `log [N]`, `dir`, `wait SECS` | |

`face` turns with A and D held for a computed time (the player turns 2
degrees a tick, 100 a second) rather than with a mouse drag: a key held for
a time turns by the same amount at any frame rate, while a drag's turn
depends on how the motion was split between frames.

## Running N in parallel

Every session keeps to `$PT_ROOT/$PT_SESSION`, so any number can run at
once; only the display number is shared, and it is claimed under a `flock`.
The coordinator skill does the rest: build once, a run directory
`/tmp/bn-playtest/<date-time>`, one subagent per goal with its own
`PT_SESSION` (`s01`..) and seed, wait, aggregate. In Claude Code: `/playtest`.

### What it costs

Measured with `tools/playtest/measure.py N` (sessions idle in a world:
`--start-screen world --no-saves`, settled 30 s, sampled over 20 s; CPU from
`/proc`, frame rate and measured tick rate from `state.json`). The machine
is a 20-core, 24 GB WSL2 box, and it was not idle: other builds ran
alongside, at a load average of 7 to 18, which is part of what these numbers
show.

| Sessions | Size | `PT_LP_THREADS` | CPU cores, total | Memory per session (game + Xvfb) | fps | Ticks/s (50 = real time) |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 960x540 | 2 | 1.9 | 280 MB | 9-11 | 45 |
| 5 | 960x540 | 2 | 9.3-9.7 | 280 MB | 6-9 | 25-35 |
| 5 | 960x540 | 4 | 13.5 | 280 MB | 8-9 | 34 |
| 10 | 960x540 | 2 | 13.2 | 277 MB | 3-4 | 14 |
| 10 | 960x540 | 1 | 10.1 | 277 MB | 4 | 18 |
| 10 | 800x450 | 1 | 10.0 | 275 MB | 4 | 16 |

What it means:

- **Memory is not the limit**: about 280 MB a session, under 3 GB for ten.
- **Frame rate is**, and through it the speed of the world. The host runs at
  most five simulation steps per frame (`GameOptions.MaxCatchUpSteps`), so
  below 10 fps the world runs slower than real time: at 4 fps it gets about
  16 of its 50 ticks a second. Nothing is distorted -- every rule is
  tick-driven -- but an agent gets about a third of the game time per minute,
  which is why timings in the summary are game time (ticks / 50).
- A smaller window does not help (800x450 is no faster than 960x540) and
  more llvmpipe threads barely do: the cost per frame is not in filling
  pixels.
- **Recommended:** up to **6** sessions at the defaults (960x540, 2 threads)
  when worlds should run near real time; **10** works, at roughly a third of
  real time, with `PT_LP_THREADS=1` to leave cores for everything else.
  Give the agents' time budgets room for it.

Agents spend much of their time thinking between commands while the game
keeps running, so a slower world costs less than it sounds.

## Writing goals

A goal is one sentence an agent can finish, fail or partly do inside its
time budget, with something to measure:

- **Concrete:** "Reach the Village era as fast as you can; record the game
  time of each objective", not "test the eras".
- **Sized to game time:** a day is 10 minutes of game time, and the Village
  era cannot come before day 3. Use `--season` or a `--stage-*` switch to get
  there sooner when the goal is about the later game, and say so in the goal.
- **One focus:** a crafting goal and a diplomacy goal find different things;
  one goal that asks for both finds neither well.
- **A persona helps:** "as a player who has never seen the game" or "as a
  player who only uses the mouse" changes what is noticed.

The skill's default ten are a spread: onboarding, the critical path to the
Village era, winter, diplomacy, trade, rulers and laws, crafting, world
events, a text and accessibility audit, and save/load plus seed sharing.

## Data layout

```
/tmp/bn-playtest/20261021-2200/          the run (PT_ROOT)
  summary.md, summary.json               written by aggregate.py
  s01/                                   one session (PT_SESSION)
    session.json                         display, window, game args, version, commit
    report.json                          the agent's report (report.schema.json)
    findings.jsonl                       pt.sh note: raw notes as they were taken
    actions.jsonl                        every pt.sh command with its time, for repro
    trace.jsonl                          each step of face and walkto
    shots/*.png                          screenshots
    out/state.json                       the game's view now (--playtest-out)
    out/events.jsonl                     what happened, in order
    data/BeyondNations/                  XDG_DATA_HOME: saves, settings, crash reports
    game.log, xvfb.log
    display, game.pid, xvfb.pid, window  while running
```

### `state.json`

Rewritten about twice a second, atomically (written beside it and renamed
over it). Off the world it holds only the screen; in the world:

```json
{"wallTimeMs":1792642042971,"version":"0.4.1","screen":"WORLD","fps":13,"world":true,"tps":49.8,
 "openPanels":["chronicle-recent","tip"],"tip":"Chop a tree: Walk up to a tree and press E to chop it for wood.",
 "seed":42,"tick":1420,"day":0,"timeOfDay":0.347,"season":"Spring","region":"Highlands","era":"Camp",
 "player":{"x":117.63,"z":14.23,"yawDeg":64,"speed":25,"energy":80.3,"coins":142,"inventory":{"wood":4},
           "nation":null,"role":null,"settlement":null,"alive":true},
 "objective":{"number":1,"title":"Gather wood","progress":"Wood 4/10","completed":0},
 "status":"Gathered 4 wood and 1 apple from tree.","worldEvent":null,
 "nearest":{"tree":[{"name":"Tree","id":"8efc84cd","distance":2.5,"bearing":-8.1}],"rock":[],"pawn":[],
            "chicken":[],"settlement":[],"stall":[]}}
```

`nearest` lists up to three of each kind, closest first; `bearing` is where it
lies relative to the way the player faces, -180 to 180, + to the right.
`openPanels` can hold `chronicle-recent`, `chronicle-full`, `era-detail`,
`caravan-dispatch`, `world-event`, `debug` and `tip`. `worldEvent`, while a
choice is waiting, has the title and the options.

### `events.jsonl`

One object a line, each with `type`, `tick` and `wallTimeMs`: `screen` (from,
to), `world` (created or loaded, slot, seed), `objective_completed` (number,
id, title, ticks taken), `era_changed`, `status` (every new status line),
`chronicle` (kind, text), `world_event_offered` (title, body, options),
`world_event_resolved` (title, the status line after), `player_death`,
`pawn_death` (count, total) and `save`. A session restarted with the same
`PT_SESSION` keeps appending, so a save/load round trip is one file.

Without `--playtest-out` nothing is written and nothing else changes: the
recorder only reads what the game already shows.

## Reading the summary

`summary.md` has, in order:

1. **Outcomes:** one row a session: goal, achieved/partial/blocked, fun
   (1-5), minutes played, objectives, era and deaths (from `events.jsonl`, not
   the agent's word), number of findings, verdict.
2. **Findings:** grouped across agents. Titles are normalised (case,
   punctuation, filler words) and grouped when they are near-duplicates, so
   "Season panel clipped at the right edge" and "The season panel is clipped
   at the right edge" are one entry reported by two agents. A group takes its
   most severe report's title and severity and lists the other titles under
   *Also reported as* (check those: the grouping is a heuristic). A finding
   several agents hit independently is more likely to be real.
3. **Objective timings:** game seconds to each objective in each session,
   with the minimum, median and maximum.
4. **Problems with the run:** missing or invalid reports and games that
   crashed (a crash in `game.log` also becomes a blocker finding).

`summary.json` has the same and more (every session's derived metrics, the
agent's own metrics) for anything that wants to process it.

## Troubleshooting

- **Xvfb exits at once, or the game cannot create a GL 4.5 context** --
  `LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe` must be set for both Xvfb
  and the game; `pt.sh` exports them. Running Xvfb by hand without them aborts
  on WSL2.
- **`start` says the game did not come up** -- it prints the end of
  `game.log`. A missing `DOTNET_ROOT` (`You must install .NET to run this
  application`) is the usual cause; so is a build that is not there
  (`dotnet build ... -c Release`).
- **Clicks on buttons do nothing** -- at a few frames a second ImGui only
  registers a click when the pointer has hovered for a few frames first.
  `pt.sh click` hovers 0.3 s and holds the button 0.15 s; with ten sessions
  on a busy box, retry once before calling it a bug.
- **Screenshots are cut off** -- GLFW opens the window at +50+50 and with no
  window manager nothing moves it; `pt.sh start` moves it to the origin and
  makes the screen a little larger than the window. Use window coordinates
  (`xdotool mousemove --window`), as `pt.sh` does, never screen ones.
- **Shift does nothing** -- the xdotool name is `Shift_L` (`hold Shift_L+w 2`
  sprints).
- **`walkto` says stuck** -- something is in the way, or the target keeps
  changing (pawns fell trees too). Look at a screenshot, `face 45`, walk a
  little and retry; `trace.jsonl` has every step it took.
- **`e` does nothing near the tree** -- Interact picks a settlement, then a
  pawn, then a tree within 5 units; a pawn standing by the tree wins.
- **Saves, settings or screenshots of different sessions mix** -- they cannot
  while `XDG_DATA_HOME` is per session, which `pt.sh` sets (`AppDataPaths`
  honours it). Do not run the game outside `pt.sh` against a session's
  directory.
- **Something is still running afterwards** -- `pt.sh stop` for each session;
  then `pgrep -a Xvfb` and `pgrep -af 'BeyondNations.Desktop '`. Do not
  `pkill -f` a pattern your own shell's command line also contains.
