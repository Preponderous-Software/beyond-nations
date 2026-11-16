# Playtest evidence: 0.8.0

> **Re-run after the fixes:** four sessions (s01, s02, s03, s05) were re-run on `2f10aca1`, after #481, #482 and #483, at low load. See [rerun/README.md](rerun/README.md).

This is the release-candidate playtest for 0.8.0, run on `develop` at **`eefb1f7b`**. The game reported its version as `0.7.1-SNAPSHOT`; the version is bumped when the release is cut. The code at `eefb1f7b` is identical to the release candidate `0cab574`: the commits between them change only the README, its media, docs/CONTRIBUTING.md and the media-capture tooling under `tools/capture/`.

The run used [playtester](https://github.com/Preponderous-Software/playtester) with its `beyond-nations` profile ([docs/PLAYTESTING.md](../../PLAYTESTING.md)).

| | |
| --- | --- |
| Build tested | `develop` at **`eefb1f7b`** (code identical to RC `0cab574`), shown in game as `0.7.1-SNAPSHOT` |
| Released as | 0.8.0 (expected). If the release turns out to be a different version, this directory is renamed with it ([docs/RELEASING.md](../../RELEASING.md)). |
| Date | 2026-11-15, 15:03 to 15:41 MST |
| Harness | playtester `beyond-nations` profile, software rendering at 960x540 |
| Sessions | 10 (s01 to s10), run in parallel |
| Results | [summary.md](summary.md) and [summary.json](summary.json): 0 blocker, 12 major, 51 minor, 26 polish (89 reports in 73 groups) |

## Sessions

s02, s03, s05, s06 and s07 played updated goals written by the coordinator for this release, so that they covered the 0.8.0 changes (the day-2 Village era, winter food, the Trade at Market panel and traders, abdication and succession, second settlements and the Manage tab). The other five played the profile's default goals. The full goal text is in each session's `report.json` and in [summary.md](summary.md).

| Session | Goal | Seed | Budget | Outcome |
| --- | --- | --- | --- | --- |
| s01 | First 10 minutes as a brand-new player (profile goal) | 101 | 30 min | partial (6 objectives) |
| s02 | Reach the Village era as fast as possible, from day 2 with 10 members (updated goal) | 102 | 30 min | partial (Members 3/10; day 2 not reached) |
| s03 | Survive the first winter as a prepared ruler (`--season autumn`, updated goal) | 103 | 35 min | blocked (winter never reached) |
| s04 | Diplomacy and war from the Nations screen (profile goal) | 104 | 30 min | partial (denounce, war and peace; alliance refused at +60) |
| s05 | Trade: the Trade at Market panel, a Trader, a Storehouse and a caravan (updated goal) | 105 | 35 min | partial (panel tested; trader, Storehouse and caravan out of reach) |
| s06 | Ruling: laws, the Members tab, heirs, abdication or old age (updated goal) | 106 | 45 min | partial (laws tested; no heir, so no succession) |
| s07 | Settlements: a second settlement, Give/Take Back, the Manage tab (updated goal) | 107 | 40 min | partial (second settlement gated by the Village era) |
| s08 | World events and their choices (profile goal) | 108 | 30 min | partial (one event, no second to compare) |
| s09 | Accessibility and text audit (profile goal) | 109 | 30 min | achieved |
| s10 | Save/load and share-seed round trip (`--continue`, profile goal) | Ironvale | 30 min | achieved |

Each session directory holds `report.json`, `session.json`, `actions.jsonl`, `findings.jsonl`, `events.jsonl` (the game's `out/events.jsonl`) and the screenshots that findings cite, in `shots/`. Screenshots no finding uses, `game.log`, `state.json`, the `data/` folder and the prompts were left out. The screenshots were re-encoded as 256-colour PNGs where that made them smaller, so their names and the links in the summary are unchanged.

## What went wrong in the run

- **The shared machine was heavily loaded.** Its load average was 40 to 115 during the run. The game ran at 2 to 20 ticks a second instead of 50, and at 1 to 3 fps. The timing findings in the summary, **#5** (low fps and tps; the day-2 Village gate about 35 real minutes away), **#6** (the first winter out of reach) and **#10** (only one world event per session), reflect the host load, not the game.
- **s03 never reached winter.** From an autumn start, winter is about 60,000 ticks away; at 5 to 12 ticks a second that is close to two hours of real time. The winter-preparation findings in s03 come from the run-up only.
- The same slowness put the day-2 Village era, and with it the Storehouse, caravans and a second settlement, out of reach for s02, s05 and s07, and old age out of reach for s06.
- The two-click confirms (Build House and others) lapsed at 1 to 2 fps before the second click landed (minor group #11, s02, s03, s07).

## Follow-ups

- **Fix pull requests in progress:**
  - The settlement-screen flow: the first entry completes objective 5 (group #1), and B works on the settlement screen instead of needing the unexplained Back first (group #4).
  - Recruiting: Invite and Gift shown on screen next to a pawn (group #7), and fewer pawns founding one-person nations, so a ruler can gain members (group #3).
  - The market: the Trade at Market panel and tills big enough for the prices (group #8).
- **Filed:**
  - #478: the Nations screen promises an alliance at +30, but larger nations quietly require +60 (group #9).
  - #479: Send Caravan looks pressable in the Camp era although caravans unlock in the Village era (groups #2 and #16).
- **Re-run:** s01, s02, s03 and s05 will be played again on the fixed build. Their results will be added to this directory.
