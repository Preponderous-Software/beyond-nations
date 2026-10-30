# Playtest evidence: 0.6.4

This is the release-candidate playtest for the next release, run on `develop` itself at **`a44aa8d`**. It **replaces** the earlier 0.6.4 evidence. That run played `b6a0019`, the head of PR #426, and #426 was squash-merged, so `b6a0019` is not in `develop`'s history and the evidence could not vouch for any release candidate. Every session here records the build commit `a44aa8d`, which is an ancestor of (or equal to) any candidate cut from `develop` from now on.

The run used [playtester](https://github.com/Preponderous-Software/playtester) with its `beyond-nations` profile ([docs/PLAYTESTING.md](../../PLAYTESTING.md)). Ten sessions played the profile's ten default goals with the same seeds, budgets and targeted checks as the earlier run. s01 and s05 checked #423, s04 checked #424, and s10 checked the save format 6 round trip.

| | |
| --- | --- |
| Build tested | `develop` at **`a44aa8d`**: #426 (fixes for #423 and #424), #429 (Strata 0.7.0) and #427 (README media) on top of `8474dab`. The game showed `0.6.3`; the version is bumped when the release is cut. |
| Released as | 0.6.4 or 0.7.0 (expected). The save format went from 5 to 6, so the release may be cut as 0.7.0, and this directory is renamed with it. |
| Date | 2026-10-29, 20:53 to about 21:24 MDT |
| Harness | playtester at `b69e15e` (`tools/playtest/playtester.ref`), software rendering at 960x540. The harness smoke printed `PASS`. |
| Budgets | 30 minutes a session (35 for s02 and s03) |
| Results | [summary.md](summary.md) and [summary.json](summary.json): 0 blocker, 10 major, 46 minor, 18 polish (74 reports in 64 groups) |

## Sessions

| Session | Goal | Seed | Outcome |
| --- | --- | --- | --- |
| s01 | First 10 minutes as a brand-new player, plus the #423 stall check | 101 | achieved (6 objectives) |
| s02 | Race to the Village era | 102 | partial (settlement became a Village tier; Members 3/10, day 1 of 3) |
| s03 | Survive the first winter (`--season autumn`) | 103 | partial (winter not reached; one world event resolved) |
| s04 | Diplomacy and war, plus the #424 envoy check | 104 | achieved (envoy verified; alliance with Southton at +70) |
| s05 | Trade and caravans, plus the #423 stall check | 105 | partial (own stall yes; traded only at a foreign town; no caravan) |
| s06 | Ruler, laws and heirs | 106 | partial (laws and an heir; succession seen through `--stage-succession`) |
| s07 | Crafting: every recipe, are tools worth it | 107 | partial (3 of 5 recipes) |
| s08 | World events and their choices | 108 | blocked (no player event before the budget ran out) |
| s09 | Accessibility and text audit | 109 | achieved |
| s10 | Save/load and share-seed round trip, with an envoy away | Ironvale | achieved |

## The targeted checks

- **#423 holds on `develop`.** In s01, the market of the player's own settlement stayed at 0/4 stalls for about 4,360 ticks with 4 residents, while NPCs built stalls in other nations' settlements. *Build a market stall* completed only from the player's own B press (stall at tick 10687, objective at tick 10703). In s05, the market was 0/4 until the player built, and stayed at 1/4 for about 3,600 ticks after.
- **#424 holds on `develop`.** In s04, each accepted act sent that nation's envoy away for 20 s of play. The row counted down "envoy back in N s" (11 s left at tick 1493 for an act at 1037). A blocked act said "Your envoy to X is still on the road..." and took no coins. Refused offers sent no envoy: Ally was refused at 0, +28 and +41, and Peace at -60. Southton, Farmwood and Bigford had envoys out at the same time. Acts were spread from tick 1037 to 7345, ending in an alliance with Southton.
- **Save format 6 round trip (s10).** A Tribute to Fortresstown at tick 1686 sent a 20 s envoy. The game was saved from the pause menu at tick 2206, with 9.6 s of the trip left. After `pt stop` and `pt start --continue`, the row read "envoy back in 6 s" at tick 2406, as the timing predicts, and the envoy came back on schedule. Relation (+10), coins (231), position, energy, objective and the LEADER role all matched. A new world from the Share screen's seed (1450162717) matched the original start.
- **Committed save fixtures.** The fixtures for formats 1 to 6 (`tests/BeyondNations.Core.Tests/save/fixtures/`) were each loaded with `--continue` in this exact Release build on llvmpipe. All six loaded at tick 2400 with "Welcome back" and no error in the log, and none of the files changed. (The first v2 attempt hit the harness's window-start timeout while the ten sessions were starting; the retry loaded normally.) The full test suite also passed at `a44aa8d`: 1,089 Core tests (including `TestSaveMigrations`), 405 Desktop, 58 Web, 11 HistoryExport and 8 Pacing.

Minor findings about the envoy, not filed: the status line keeps saying "20 seconds" instead of counting down (s04, s10). The buttons of a nation whose envoy is away look enabled, and nothing says when the envoy returns (s04, s10). The alliance threshold reads +30, but a larger nation wants +60 (s04).

## What the run found

There was no blocker. The 10 major reports went to the following:

- **Filed:**
  - #432: at low frame rates the Nations screen loses the row order and armed confirm that #368 added, so a two-click Denounce landed on another nation (s04). The earlier run saw the same and left it for a re-check. It has now been seen in two runs.
  - #433: founding a first settlement becomes a chase, because NPC nations claim the open ground the F hint points to (s02, also s01 as minor). It took 134 game seconds in s02.
  - #434: the context action buttons are translucent, about 1.8:1 contrast over grass or sky, and disabled looks like enabled (s09).
- **Already open:**
  - The Laws tab cannot show Rationing at 960x540 (s06): #431.
  - The HUD panels overlap near a settlement (s09): #350.
  - The player's own new market cannot trade, because its merchant has 0 coins and no stock (s05): #369.
  - Members stall short of the Village era's 10, so Planks and Bricks stay locked (s02, s07): #371.
  - The first player world event comes too late for a session (s08): #370.
- **Not filed, the run's conditions:** ten sessions shared the machine on software rendering, at a load average of about 30. The game ran at 1 to 5 fps and roughly 3 to 30 ticks a second instead of 50. As in both earlier 0.6.4 runs, winter (s03), the Day 3 Village gate (s02) and a world event (s08) could not be reached in their budgets.

Repeated minor findings worth a look:
- The settlement's floating label is clipped at the top of the screen (s01, s03, s05, s08, s09).
- Chronicle lines are cut off with "..." (s01, s06, s09).
- The objective 7 progress text is clipped on the left ("mbers 4/10") (s01, s02, s07).
- "Settlement is now a Village!" appears while the era still reads Camp (s01, s03, s07).
- The confirm for a mouse build lapses at low fps (s03, s05).

## What went wrong in the run

- The machine was quieter than in the earlier run (load average about 30 rather than 80 to 110), but ten software-rendered sessions still held the game to a few frames a second.
- s06 used `--stage-succession` to see a ruler's death, because a natural death was out of reach (energy fell about 20 points in 2 game minutes). Its laws and heir findings come from an ordinary start.
- All ten sessions ran `pt stop`, and all ten reports validated. The aggregator recorded no problems.
