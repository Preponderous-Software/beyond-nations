# Playtest evidence: 0.6.1

The release-candidate playtest for 0.6.1: four AI agents, run through [playtester](https://github.com/Preponderous-Software/playtester) with its `beyond-nations` profile ([docs/PLAYTESTING.md](../../PLAYTESTING.md)), each re-checking fixes merged since 0.6.0. A focused run of four rather than the usual ten, because the machine was busy with other work (load average 16 to 31).

| | |
| --- | --- |
| Build tested | `develop` at `fc45ac8` (#382 to #390). The game showed `0.6.0`; the version was bumped afterwards. |
| Released as | 0.6.1 |
| Date | 2026-10-23, 23:05 to about 23:55 MDT |
| Harness | playtester at `b69e15e` (`pt`), software rendering at 960x540 |
| Results | [summary.md](summary.md) (readable) and [summary.json](summary.json) |

## Sessions

| Session | Goal | Seed | Budget | Outcome |
| --- | --- | --- | --- | --- |
| s01 | Two-click confirmations, 8 s window, Nations list order, truce warning, L key, command bar (#345, #368) | 111 (`--stage-diplomacy`) | 30 min | achieved |
| s02 | Survive the first winter; hunger warning and Feed Residents (#353) | 103 (`--season winter`) | 60 min | partial (about two thirds of the winter fitted) |
| s03 | Crafting before and after a nation; Sell Resources refusals (#356, #354) | 107 | 30 min | partial |
| s04 | HUD at 960x540 inside a settlement (#350) | 109 (`--stage-diplomacy`) | 30 min | achieved |

Each `sNN/` directory holds the agent's `report.json`, its notes (`findings.jsonl`), the game's event log (`events.jsonl`), every harness command it ran (`actions.jsonl`), `session.json` and the screenshots its findings cite. Game logs, state snapshots, save data and the filled prompts were left out.

## What the run found

- **Confirmed fixed:**
  - #368 and #345's confirm window: second clicks at 6.3 to 7.6 s confirmed and at 8.6 and 11.1 s re-armed. The truce warning shows, L asks twice, and the Nations list holds its order.
  - #356: the Workshop lock reads as an era lock before a nation exists.
  - #354: the sell refusals that can be reached explain themselves.
  - #350: the right-hand column at 960x540 fits and wraps.
  - #353: the winter warning and Feed Residents work, and the player's nation lost no one.
- **Winter on seed 103:** 5 NPC hunger deaths over about two thirds of the winter, against 16 over the whole winter in the 0.6.0 run (s11, without #389's shelter rationing). 4 of the 5 were AI nation leaders.
- **Fixed before release:** Sell Resources was greyed out when the player carried only food or saplings (s03, confirmed in `WorldCommandBar`). This was fixed by #392.
- **Major, open:**
  - AI leaders starve disproportionately in winter.
  - The diplomacy stage loses the player's village about 20 s in, while the HUD shows "Sieges can begin in 4:58".
  - Expanding the Era box inside a settlement pushes the Objective box over the command bar.
- **Minor:**
  - The pawn buttons (Talk, Gift, Invite) and Feed Residents still move on the command bar.
  - A stale "click again" prompt stays after the window lapses.
  - Compact mode drops the player's Nation and Role while keeping the debug line "PCIS".
  - Feed Residents gives one item per press.

## What went wrong in the run

- **Slow worlds.** The simulation ran at about 15 to 30 of its 50 ticks a second, so s02 saw only about two thirds of the winter in its 60 minutes.
- **One untested refusal.** s03 could not reach "only your own stall": pawns filled every stall in its new camp within a minute of founding.
- **A restarted world.** s03 restarted seed 107 once, and the pawn settlements came out in different places the second time.
