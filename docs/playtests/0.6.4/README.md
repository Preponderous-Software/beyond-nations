# Playtest evidence: 0.6.4

This is the release-candidate playtest for 0.6.4, re-run on PR #426. #426 fixes the two majors from the first 0.6.4 run (#423 and #424, on `develop` at `4645cdf`), and this run **replaces** that evidence. It was run through [playtester](https://github.com/Preponderous-Software/playtester) with its `beyond-nations` profile ([docs/PLAYTESTING.md](../../PLAYTESTING.md)). Ten sessions played the profile's ten default goals with the same seeds and budgets as the first run. Four goals had checks added for this PR: s01 and s05 checked #423, s04 checked #424 and s10 checked the save format 6 round trip.

| | |
| --- | --- |
| Build tested | `fix/playtest-064-majors` at **`b6a0019`** (PR #426's head; it already contained `develop` at `8474dab`). The game showed `0.6.3`; the version is bumped when the release is cut. |
| Released as | 0.6.4 (expected) |
| Date | 2026-10-29, 17:17 to about 18:27 MDT |
| Harness | playtester at `b69e15e` (`tools/playtest/playtester.ref`), software rendering at 960x540; harness smoke `PASS` |
| Budgets | 30 minutes a session (35 for s02 and s03) |
| Results | [summary.md](summary.md) and [summary.json](summary.json): 0 blocker, 12 major, 37 minor, 15 polish (64 reports in 49 groups) |

## Sessions

| Session | Goal | Seed | Outcome |
| --- | --- | --- | --- |
| s01 | First 10 minutes as a brand-new player, plus the #423 stall check | 101 | achieved (6 objectives) |
| s02 | Race to the Village era | 102 | partial (Members 7/10, day 1 of 3) |
| s03 | Survive the first winter (`--season autumn`) | 103 | blocked (winter not reached) |
| s04 | Diplomacy and war, plus the #424 envoy check | 104 | partial (envoy verified; no alliance, relations reached +29 of the +30 needed) |
| s05 | Trade and caravans, plus the #423 stall check | 105 | partial (own stall yes; no trade, no caravan) |
| s06 | Ruler, laws and heirs | 106 | partial (laws yes, succession not reached) |
| s07 | Crafting: every recipe, are tools worth it | 107 | partial (3 of 5 recipes) |
| s08 | World events and their choices | 108 | blocked (no event before the budget ran out) |
| s09 | Accessibility and text audit | 109 | achieved |
| s10 | Save/load and share-seed round trip, with an envoy away | Ironvale | achieved |

## The fixes in #426

- **#423 is fixed.** In s01, the market of the player's own settlement stayed at 0/4 stalls for about 1,950 ticks while a resident lived there, and *Build a market stall* completed only from the player's own B press. s05 saw the same. A stall built by a resident of a rival nation (tick 6800) did not complete the objective.
- **#424 is fixed.** In s04, acts toward four nations were spread over ticks 1508, 2644 and 3728. Each accepted act sent that nation's envoy away. A second act toward the same nation was refused and took no coins. A refused offer sent no envoy, and the row counted down "envoy back in N s".
- **Save format 6:** in s10, a tribute envoy was away ("envoy back in 15 s") when the game was saved at tick 1448. After a stop and `--continue`, the world was the same and the envoy read "envoy back in 12 s" 112 ticks later, exactly as expected. Separately, the committed fixtures for formats 1 to 6 (`tests/BeyondNations.Core.Tests/save/fixtures/`) were each loaded with `--continue` in this exact Release build on llvmpipe. All six loaded at tick 2400 with no error, and the files were not changed. The save tests (`TestSaveMigrations`, including the format 5 fixture from before envoys) and the full suite (1,089 Core, 400 Desktop and 11 HistoryExport tests) passed.

Minor findings about the envoy, not filed: the status line keeps saying "20 seconds" instead of counting down (s04). War and Denounce still ask for the confirm click before saying the envoy is away (s04). The buttons for a nation whose envoy is away are not dimmed (s04, s10). Relations fade by about a point per envoy trip, so three tributes at the envoy's pace reach +29, one short of the +30 an alliance needs (s04).

## What the run found

There was no blocker. The 12 major reports fell into 10 groups. Each went to one of the following:

- **Filed as #431:** the Laws tab cannot show the fourth law, Rationing, at 960x540. The rows are cut off and there is no scroll (s06). This has been so since #322 and is not caused by #426.
- **Already open:**
  - A freshly built market of the player's own cannot trade: its merchant has 0 coins and no stock (s05). See #369. #426 makes this easier to hit, because residents no longer stock the player's market with their own stalls.
  - Nobody can be recruited as an heir (s06): #371 and #358.
  - Ruler death cannot be reached (s06): #372 and #357.
  - The first world event comes too late for a session (s08): #370.
- **Not filed, the run's conditions:** the machine was shared with other sessions' builds, browsers and playtests, at a load average of 80 to 110. The game ran at 1 to 4 fps and 3 to 16 ticks a second instead of 50.
  - The Day 3 Village gate (s02), winter (s03) and caravans in the Town era (s05) could not be reached in their budgets. s03, s05 and s07 hit the same limits in the first run.
  - At about 1 fps, the Nations screen treats every frame as a reopen (`ReopenGapSeconds = 1.0`, since #385). The War/Denounce confirm then lapses and rows re-sort (s04). This is the same class as the click-again lapse the first run left unfiled. It needs a re-check on an unloaded machine.
- **Not filed, judged minor:** the huge settlement label near the player's own settlement (s09, also s01, s02, s05 and s08; the first run saw it too), and chronicle lines cut off with "..." (s09; the full text is on the Y screen).

Repeated minor findings worth a look: the objective 7 progress text is clipped on the left ("nbers 2/10"); *Eat something* completes on its own; the "open ground lies N to the X" hint has no compass (#374); and the Inventory panel jumps to the middle of the screen inside a settlement.

## What went wrong in the run

- The machine was heavily shared (see above), so most goals that need day 3, winter or a world event ran out of game time.
- Only seven sessions could start at once because of a subagent concurrency limit. s10, s08 and s09 started 15 to 25 minutes later, as slots freed up.
- Every session ran `pt stop`, and all ten reports validated. No problems were recorded.
