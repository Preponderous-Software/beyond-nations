# Playtesting with agents

A kit for having several AI agents play the real desktop build at once, each
toward a different goal, and turning what they find into one summary. It
has two halves:

- **The game's half, here:** started with `--playtest-out DIR`, the game
  writes what is on screen to `DIR/state.json` and `DIR/events.jsonl`
  (`src/BeyondNations.Desktop/playtest/`). Without the flag nothing is
  written and nothing else changes.
- **The harness, in [playtester](https://github.com/Preponderous-Software/playtester):**
  `pt` runs one isolated session (its own virtual display, saves,
  screenshots and logs) and drives it with keys and clicks; the
  `beyond-nations` profile tells it how to build, start and steer this game;
  agents follow playtester's prompt, write a `report.json`, and `pt
  aggregate` merges a run into `summary.md` and `summary.json`. Beyond
  Nations was the first game the kit was built for; it was moved to
  playtester so other games can use it too. This game's `--playtest-out`
  output is what playtester's probe contract describes.
- The `/playtest` project skill (`.claude/skills/playtest/SKILL.md`, installed
  with `pt install-skill . --profile beyond-nations`) is the coordinator's
  procedure, start to finish.

The agents play the way a person does: they look at screenshots and press
keys. `state.json` exists because steering by screenshot alone was slow and
imprecise (mouse-look sensitivity is hard to judge from a picture, and
distances cannot be judged at all), so the harness can turn to a bearing and
walk to the nearest tree in a closed loop.

## Prerequisites

Linux (or WSL2) with:

- the .NET 8 SDK (`dotnet`; the profile sets `DOTNET_ROOT` to `~/.dotnet` when that exists)
- `Xvfb`, `xdotool`, ImageMagick (`import`), Python 3.8 or newer
- Mesa's llvmpipe (`libgl1-mesa-dri`) and the X11 client libraries the CI's
  strata-smoke job installs (see `.github/workflows/ci.yml`)
- playtester, at the version pinned in `tools/playtest/playtester.ref`

```bash
sudo apt-get install -y xvfb xdotool imagemagick libgl1-mesa-dri libglfw3
pip install "git+https://github.com/Preponderous-Software/playtester@$(cat tools/playtest/playtester.ref)"
dotnet build src/BeyondNations.Desktop/BeyondNations.Desktop.csproj -c Release
```

`tools/playtest/pt.sh` is a shim that runs playtester's `pt` with the
`beyond-nations` profile against this checkout (`PT_PROFILE`, `PT_GAME_DIR`
and `PT_ROOT=/tmp/bn-playtest` by default), so `tools/playtest/pt.sh <command>`
and `pt <command>` run in this directory do the same thing.

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

`pt.sh help` lists every command; playtester's docs/AGENT_PLAYTESTING.md
describes them. `tools/playtest/smoke.sh` is the profile's scripted session:
title, Start, Begin, then trees until the *Gather wood* objective completes,
confirmed from `events.jsonl`. It prints `PASS` and is the quickest check
that a machine can run the kit:

```bash
PT_SESSION=smoke tools/playtest/smoke.sh
```

In this game, `face` turns with A and D held for a computed time (the player
turns 2 degrees a tick, 100 a second), and `walkto` knows `tree`, `rock`,
`pawn`, `chicken`, `settlement` and `stall` and stops under 4 units
(Interact reaches 5, `InteractCommand`).

## Running N in parallel

Every session keeps to `$PT_ROOT/$PT_SESSION`, so any number can run at
once; only the display number is shared, and it is claimed under a lock.
Each session's saves and settings go to its own `XDG_DATA_HOME`
(`AppDataPaths` honours it). The coordinator skill does the rest: build once,
a run directory, one subagent per goal with its own `PT_SESSION` (`s01`..)
and seed, wait, aggregate. In Claude Code: `/playtest`. The default goals are
the profile's (`pt prompt --profile beyond-nations --goal-index N`).

### What it costs

Measured with the first kit's `measure.py N`, now `pt.sh measure N` (sessions idle in a world:
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

A run is laid out as playtester's docs/AGENT_PLAYTESTING.md describes (`session.json`, `report.json`, `findings.jsonl`, `actions.jsonl`, `trace.jsonl`, `shots/`, `out/`, and `data/BeyondNations/` for this game's saves, settings and crash reports). What the game writes to `out/`:

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

`pt aggregate RUN` writes `summary.md` and `summary.json`: outcomes per session (objectives, era reached and deaths from `events.jsonl`, not the agent's word), findings grouped across agents (check each group's *Also reported as* titles: the grouping is a heuristic), objective timings in game time (tick / 50), and problems with the run (missing or invalid reports; a crash in `game.log` also becomes a blocker finding). playtester's docs/AGENT_PLAYTESTING.md has the details.

## Evidence in the repository

Every run's evidence is committed under `docs/playtests/<version>/`, named
after the version tested: a build of `develop` past the last release is filed
under the next expected version, and renamed at release time if the release
turns out to be a different version (see [RELEASING.md](RELEASING.md); the
release workflow checks the names). The playtest skill's step 8 lists what to
copy in.

Runs so far:

- [0.5.0](playtests/0.5.0/README.md) (develop `df53ae3`, 2026-10-21): the first 10-agent run.
- [0.6.0](playtests/0.6.0/README.md) (develop `ae87506` and `3246ba2`, 2026-10-22): the first run through playtester, plus two re-checks of the released build.
- [0.6.1](playtests/0.6.1/README.md) (develop `fc45ac8`, 2026-10-23): four sessions re-checking the fixes since 0.6.0.
- [0.6.2](playtests/0.6.2/README.md) (develop `6ac1e3f`, 2026-10-24): four sessions re-checking the fixes since 0.6.1.

## Troubleshooting

- **`pt.sh: playtester is not installed`** -- install the pinned version
  (Prerequisites).
- **Xvfb exits at once, or the game cannot create a GL 4.5 context** --
  `LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe` must be set for both Xvfb
  and the game; the harness exports them. Running Xvfb by hand without them
  aborts on WSL2.
- **`start` says the game did not come up** -- it prints the end of
  `game.log`. A missing `DOTNET_ROOT` (`You must install .NET to run this
  application`) is the usual cause; so is a build that is not there
  (`dotnet build ... -c Release`).
- **Clicks on buttons do nothing** -- at a few frames a second ImGui only
  registers a click when the pointer has hovered for a few frames first.
  `pt click` hovers 0.3 s and holds the button 0.15 s; with ten sessions on a
  busy box, retry once before calling it a bug.
- **Shift does nothing** -- the xdotool name is `Shift_L` (`hold Shift_L+w 2`
  sprints).
- **`walkto` says stuck** -- something is in the way, or the target keeps
  changing (pawns fell trees too). Look at a screenshot, `face 45`, walk a
  little and retry; `trace.jsonl` has every step it took.
- **`e` does nothing near the tree** -- Interact picks a settlement's well
  within 5 units first, then whatever within 5 units the player faces most
  squarely (within 60 degrees); `face` the tree. With nothing in front, a
  pawn in reach still wins over the tree.
- **Something is still running afterwards** -- `pt.sh stop` for each session;
  then `pgrep -a Xvfb` and `pgrep -af 'BeyondNations.Desktop '`. Do not
  `pkill -f` a pattern your own shell's command line also contains.
