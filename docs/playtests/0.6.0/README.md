# Playtest evidence: 0.6.0

The release-candidate playtest for 0.6.0, the first run through [playtester](https://github.com/dmccoystephenson/playtester) with its `beyond-nations` profile ([docs/PLAYTESTING.md](../../PLAYTESTING.md)). Ten AI agents each played the real desktop build headlessly toward a different goal, by looking at screenshots and driving the keyboard and mouse. Two more sessions then re-checked the build that was released.

| | |
| --- | --- |
| Builds tested | s01-s10: `develop` at `ae87506`. s11-s12: `develop` at `3246ba2`, which adds #340 (pawn energy cap), #376 (E acts on what the player faces) and #367 (merchant crash fix). The game showed `0.5.0` in both. |
| Released as | 0.6.0 |
| Date | 2026-10-22, 18:02 to about 21:00 MDT |
| Harness | playtester 0.2.0 (`pt`), software rendering at 960x540 |
| Results | [summary.md](summary.md) (readable) and [summary.json](summary.json), aggregated over all 12 sessions |

## Sessions

Several goals also re-checked fixes from the 0.5.0 playtest (#344-#358).

| Session | Goal | Seed | Budget | Build | Outcome |
| --- | --- | --- | --- | --- | --- |
| s01 | First 10 minutes as a new player; founding hint (#349) | 101 | 30 min | ae87506 | achieved |
| s02 | Village era as fast as possible (#351, #352) | 102 | 60 min | ae87506 | blocked (crash) |
| s03 | Survive the first winter (#353) | 103 | 60 min | ae87506 | partial |
| s04 | Diplomacy and war (#346, #347, #348) | 104 | 30 min | ae87506 | achieved |
| s05 | Trade and caravans (#354, #355) | 105 | 30 min | ae87506 | partial |
| s06 | Ruler, laws and heirs (#344, #358) | 106 | 30 min | ae87506 | partial |
| s07 | Crafting (#356) | 107 | 30 min | ae87506 | partial |
| s08 | World events | 108 | 30 min | ae87506 | partial |
| s09 | Accessibility and text audit (#345, #350) | 109 | 30 min | ae87506 | achieved |
| s10 | Save/load and share-seed round trip | `Ironvale` | 30 min | ae87506 | achieved |
| s11 | First winter again, with #340 | 103 | 60 min | 3246ba2 | partial |
| s12 | Merchant crash re-check (#367) | 102 | 30 min | 3246ba2 | partial |

Each `sNN/` directory holds the agent's `report.json`, its own notes (`findings.jsonl`), the game's event log (`events.jsonl`), every harness command it ran (`actions.jsonl`), `session.json` and the screenshots its findings cite. `s02/` also keeps the two crash reports. Game logs, state snapshots, save data, the filled prompts and uncited screenshots were left out.

## What the run found

- **Blocker, fixed before release.** s02 crashed with a `NullReferenceException` in the merchant branch of `PawnBehaviorCalculator`, and loading the autosave crashed again at the same point, so the save could not be played. The cause was a recruited merchant that bought back a stall in its old, foreign settlement. #367 fixes the behaviour, refuses foreign stall purchases and repairs affected saves on load. The crashing save was replayed headlessly for 20,000 ticks and in the desktop build past the crash point. s12 recruited a stall-keeping merchant, saved and continued with no crash.
- **Re-checked and closed:** #344, #346, #347, #348, #349 and #355.
- **Still open, with new evidence:** #345 (pawn rows still shift the command bar and reset its confirmation), #350, #351, #353, #354, #356 (mostly fixed), #358.
- **Filed from this run:** #368 to #375.
- **Winter with #340.** On seed 103, s03 (without #340) saw 4 NPC hunger deaths in the 1.2 winter days it reached; s11 (with #340) saw 16 across 7 nations over the full winter, none with any warning. #340 was kept deliberately; the winter rebalance is #353.

## What went wrong in the run

- **Slow worlds.** With ten games on one machine the simulation ran at about 12 to 28 of its 50 ticks a second, so goals that need a later era or day (caravans, ruler death, a second world event, a full winter) did not fit their budgets. Timings in the summary are game time.
- **Short soak in s12.** s12's automation sat on a paused world-event sheet for about 8 minutes, so it played about 30 game seconds after recruiting and 84 after continuing, against the 5 and 3 minutes asked.
- **#376 not exercised in play.** Neither s11 nor s12 found a pawn within reach of a tree they faced, so the new E targeting is covered by its unit tests and the pacing run, not by this playtest.
- **Grouping.** The aggregator groups near-duplicate titles and can miss some; the HUD overlap at 960x540 and the missing compass were each reported by several sessions under different titles.
