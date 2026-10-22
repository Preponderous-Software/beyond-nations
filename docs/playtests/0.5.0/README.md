# Playtest evidence: 0.5.0

The first parallel playtest, run with the playtest kit (#341, [docs/PLAYTESTING.md](../../PLAYTESTING.md)): ten AI agents each played the real desktop build headlessly toward a different goal, looking at screenshots and driving the keyboard and mouse.

| | |
| --- | --- |
| Build tested | `develop` at `df53ae3`, an experimental build after 0.4.1 (the game still showed `0.4.1`) |
| Released as | 0.5.0 (the changeset since 0.4.1 adds features, so the release is a minor one) |
| Date | 2026-10-21, 22:57 to 23:57 MDT |
| Sessions | 10, in parallel on one 20-core machine, software rendering at 960x540 |
| Results | [summary.md](summary.md) (readable) and [summary.json](summary.json) |

This directory was named for 0.5.0 directly: by the time the evidence was committed, the next release was known to be 0.5.0. Evidence for an experimental build is otherwise filed under the next expected version and renamed when the release is cut ([docs/RELEASING.md](../../RELEASING.md)).

## Sessions

| Session | Goal | Seed | Budget | Outcome |
| --- | --- | --- | --- | --- |
| s01 | First 10 minutes as a brand-new player | 101 | 30 min | partial |
| s02 | Reach the Village era as fast as possible | 102 | 60 min | achieved |
| s03 | Survive the first winter | 103 | 60 min | partial |
| s04 | Diplomacy and war | 104 | 30 min | partial |
| s05 | Trade and caravans | 105 | 30 min | partial |
| s06 | Ruler, laws and heirs | 106 | 30 min | partial |
| s07 | Crafting | 107 | 30 min | partial |
| s08 | World events | 108 | 30 min | partial |
| s09 | Accessibility and text audit | 109 | 30 min | achieved |
| s10 | Save/load and share-seed round trip | `Ironvale` | 30 min | achieved |

Each `sNN/` directory holds the agent's `report.json`, its own notes (`findings.jsonl`), the game's event log (`events.jsonl`), every harness command it ran (`actions.jsonl`), `session.json` and the screenshots its findings cite. Game logs, state snapshots, save data and uncited screenshots were left out.

## What went wrong in the run

- **Shared scratch directory.** The agents shared one scratch directory, and several kept a `pt` wrapper script there that another agent then overwrote. A few commands went to the wrong game: s09's note "Title screen contrast" and one key press went to s02 (s02 left the note out of its report; it is still in `s02/findings.jsonl`), s01 and s05 each sent a key press to another title screen, s02's first turn, walk and "No tips" click went to s04, and s06's first click, wait and screenshot went to s01. None changed a finding's substance. `AGENT_PROMPT.md` now tells agents to keep every file in their own session directory.
- **Slow worlds.** With ten games sharing the machine, the simulation ran at about 7 to 20 of its 50 ticks a second. Timings in the summary are game time (tick / 50), not wall time.
- **s03 changed worlds.** Winter was about 70 wall minutes away from an autumn start at that speed, so s03 saved its autumn world and restarted seed 103 with `--season winter`. Its pawns had no autumn to lay in food, so its death count (50 pawns) is a worst case.
- **Grouping.** `aggregate.py` groups near-duplicate titles. It missed some duplicates: pawns in the player's own settlement being out of reach is findings 9, 15 and 17 (s01, s03, s06, and s09 as a minor), and the bottom button bar changing under the cursor was reported by s01, s03 and s05.

## Confirmed findings

The coordinator looked at the screenshots behind the major findings before filing them as issues. The most reported:

- Founding a settlement: "Too close to another settlement" gives no distance or direction (7 agents).
- Pawns in or near the player's own settlement cannot be talked to, gifted or invited (4 agents); this blocks the Village-era members objective.
- Neighbours turn hostile and raid a new player's nation with nothing on screen to explain it (4 agents).
- The bottom button bar re-lays itself out under the cursor, so a click can raid a caravan or build the wrong building (3 agents).
- The Nations screen lists only the oldest 7 or 8 nations and cannot scroll (s04).
- Inside a settlement the right-hand HUD column overflows and the bottom buttons are hidden (s09).
