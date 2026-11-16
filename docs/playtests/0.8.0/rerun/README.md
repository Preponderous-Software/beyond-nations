# Playtest evidence: 0.8.0 re-run

This is a re-run of four sessions from the [0.8.0 release-candidate playtest](../README.md). It was run after the three fixes for that playtest's majors were merged:

- #481: the settlement screen flow
- #483: the market panel and tills
- #482: strangers to recruit, and Invite/Gift on screen

The first run's machine was heavily loaded (load 40–115, 2–20 ticks a second), so its timing results could not be trusted. This run had a load of about 3, and the game ran at full speed.

| | |
| --- | --- |
| Build tested | `develop` at **`2f10aca1`**, shown in game as `0.7.1-SNAPSHOT` |
| Released as | 0.8.0 (expected) |
| Date | 2026-11-15, 17:35 to 18:02 MST |
| Harness | playtester 0.2.0, `beyond-nations` profile, software rendering at 960x540 |
| Sessions | 4 (s01, s02, s03, s05), run in parallel |
| Results | [summary.md](summary.md) and [summary.json](summary.json): 0 blocker, 6 major, 18 minor, 10 polish (34 reports in 31 groups) |

## Sessions

The sessions used the same goals and seeds as the first run. The only difference is that s03 started on the eve of winter (`--season autumn --advance-ticks 25000`), so that winter came within its budget.

| Session | Goal | Seed | Budget | Outcome | Before (first run) |
| --- | --- | --- | --- | --- | --- |
| s01 | First 10 minutes as a brand-new player | 101 | 30 min | achieved (6 objectives; objectives 5 and 6 completed on the settlement screen) | partial |
| s02 | Reach the Village era from day 2 with 10 members | 102 | 30 min | achieved (Village at 537 s of game time; Members was the last requirement) | partial (Members 3/10) |
| s03 | Survive the first winter as a prepared ruler | 103 | 35 min | partial (reached Winter day 2; no member died, but stores ran out on Winter day 1) | blocked (winter never reached) |
| s05 | Trade: market panel, a Trader, a Storehouse and a caravan | 105 | 35 min | partial (panel, Storehouse and caravan done; no Trader could be appointed) | partial (panel only) |

## Did the fixes hold?

- **Settlement screen (#481):** s01 finished "Enter your settlement" and "Build a market stall" from the settlement screen. The first-run blocker (objective 5 never completing, and a dead build key) did not recur.
- **Recruiting (#482):** s02 reached the Village era with 10 members by 537 s. The world still had strangers to court, though 9 of the 10 members joined through housing rather than courting.
- **Market panel and tills (#483):** in s05, quantity trades matched their quotes, the till limits held, and the receipt line was right. Tills still run dry after a few units (minor).

## New majors (filed as issues, not release blockers)

1. `--advance-ticks` starts the player starving and old (s03). This only affects that staging flag.
2. Caravan goods that the destination does not buy are lost (s05).
3. The command bar and the Send Caravan panel move when a pawn walks into or out of reach (s05). This happened before #482 as well.
4. Members keep joining and being born during a winter famine (s03).
5. The Trader role is invisible (s05).
6. Winter stores collapse within one winter day despite preparation (s03).

## What went wrong in the run

- Nothing at the host level. Load stayed low and every session finished within its budget.
- In s02, about 6 minutes of wall-clock time were lost when a world event paused the game while the agent's own scripted loop kept pressing keys.
- In s05, `pt face` turned the opposite way to the game's yaw (a known playtester gap).
