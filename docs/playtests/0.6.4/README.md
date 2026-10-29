# Playtest evidence: 0.6.4

The release-candidate playtest for 0.6.4, run through [playtester](https://github.com/Preponderous-Software/playtester) with its `beyond-nations` profile ([docs/PLAYTESTING.md](../../PLAYTESTING.md)). Ten sessions played the profile's ten default goals. Since 0.6.3, `develop` changed only the browser build's loading shell (#421) and tests (#420). The desktop game code was unchanged, so this run was a regression sweep across the whole game rather than a re-check of specific fixes.

| | |
| --- | --- |
| Build tested | `develop` at `4645cdf` (#421). The game showed `0.6.3`; the version is bumped when the release is cut. #422 (the Collect Food count) landed after this run and was not played. |
| Released as | 0.6.4 (expected) |
| Date | 2026-10-29, 07:50 to about 08:56 MDT |
| Harness | playtester at `b69e15e` (`tools/playtest/playtester.ref`), software rendering at 960x540, `PT_LP_THREADS=1` |
| Budgets | 30 minutes a session (35 for s02 and s03) |
| Results | [summary.md](summary.md) and [summary.json](summary.json): 0 blocker, 11 major, 41 minor, 22 polish |

## Sessions

| Session | Goal | Seed | Outcome |
| --- | --- | --- | --- |
| s01 | First 10 minutes as a brand-new player | 101 | partial (6 objectives) |
| s02 | Race to the Village era | 102 | partial (Members 6/10, day 1 of 3) |
| s03 | Survive the first winter (`--season autumn`) | 103 | blocked (winter not reached) |
| s04 | Diplomacy and war from the Nations screen | 104 | achieved |
| s05 | Trade and caravans | 105 | partial (no own stall, no caravan) |
| s06 | Ruler, laws and heirs | 106 | partial (laws yes, succession not reached) |
| s07 | Crafting: every recipe, are tools worth it | 107 | partial (3 of 5 recipes) |
| s08 | World events and their choices | 108 | partial (one event, Drought, all three choices) |
| s09 | Accessibility and text audit | 109 | achieved |
| s10 | Save/load and share-seed round trip | Ironvale | achieved |

## What the run found

There was no blocker. Save, continue, Load and the share seed held (s10), and the crafting, Nations and world-event screens were judged clear.

The 11 major groups and where each went:
- **Filed as #423:** NPC residents fill a new settlement's market before the player can build a stall. The *Build a market stall* objective then completes from their stall (s01, s02, s03, s05; groups 1 and 7).
- **Filed as #424:** diplomacy takes no game time. The Nations screen pauses the world, so tribute, alliance, rivalry, war and peace all happened on one tick (s04; group 6).
- **Already open:**
  - Cooked Meat wasted by auto-eating (s07): #357.
  - The player's nation cannot gain members (s06): #371.
  - Succession cannot be seen or reached (s06): #372 and #358.
- **Not filed, the run's conditions:**
  - The game ran at about 12 ticks a second instead of 50 (s01, s02). Two other games' playtests shared the machine, at a load average of about 55. This is not a game regression.
  - `--season autumn` leaves winter two game days away (s03). That is too far for a 35-minute session at this speed. The goal's start arguments belong to playtester's profile, not the game.
- **Not filed, not confirmed:** the click-again confirmation lapsed for s03, s04 and s05 (groups 2 and 10). `ConfirmGate`'s window is 8 seconds of wall-clock time (`ImGui.GetTime()` on the command bar). At 2 to 5 fps, an agent's think time between its two clicks often ran past that. s05 also saw one lapse with only 2 s between clicks, which could not be explained from the code. It should be re-checked on an unloaded machine before anything is filed.

Repeated minor findings worth a look: the objective progress text is clipped on the left ("mbers 6/10", 5 sessions); *Eat something* completes without the player choosing to eat; the "open ground lies N to the X" hint has no compass to read it against (#374); and the Inventory panel jumps to the middle of the screen once a nation exists.

## What went wrong in the run

- The machine was shared with two other playtests, so every session ran at 2 to 5 fps and 5 to 18 ticks a second. Goals that need day 3 or winter could not be reached in their budgets (s02, s03, s05, s06, s07).
- The first `pt start` timed out for several sessions; each succeeded on the second try.
- s08 ran 55 minutes, past its 30-minute budget, because its only world event arrived about 50 minutes of real time in.
