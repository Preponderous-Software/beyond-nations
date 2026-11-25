# Playtest evidence: 0.8.0 winter sessions

Two single-session follow-ups to the [0.8.0 re-run](../rerun/README.md), whose s03 found that winter stores collapsed within one winter day. Each played s03's goal again, "survive the first winter as a prepared ruler", on a newer `develop`, and each drove a fix:

| | First winter session | Second winter session |
| --- | --- | --- |
| Directory | [c392340/s03](c392340/s03) | [bdae9bf/s03](bdae9bf/s03) |
| Build tested | `develop` at **`c392340b`** (after #495), shown as `0.7.1-SNAPSHOT` | `develop` at **`bdae9bf6`** (after #497), shown as `0.7.1-SNAPSHOT` |
| Date | 2026-11-16, from 00:26 MST | 2026-11-16, from 01:26 MST |
| Seed and start | 103, `--season autumn --advance-ticks 25000` | 103, `--season autumn --advance-ticks 25000` |
| Length | 25 min | 37 min |
| Outcome | partial: winter day 1 reached, stores projected to run out early on day 2 | partial: winter day 2 reached, stores ran out on day 1, spring not reached |
| Findings | 1 major, 4 minor, 1 polish | 3 major, 3 minor, 1 polish |
| Fixed by | #497 | #499 |

The harness was [playtester](https://github.com/Preponderous-Software/playtester) with its `beyond-nations` profile ([docs/PLAYTESTING.md](../../../PLAYTESTING.md)), software rendering at 960x540.

Each session directory holds `report.json`, `session.json`, `actions.jsonl`, `findings.jsonl`, `events.jsonl` (the game's `out/events.jsonl`) and the screenshots that findings cite, in `shots/`, as in the other 0.8.0 directories. The screenshots were re-encoded as 256-colour PNGs. Left out: the 24 and 46 screenshots no finding cites, `game.log`, `trace.jsonl`, `xvfb.log`, `state.json`, the `data/` folder (settings and the session's saves), the prompts, the `_smoke` start-up checks and a scratch `waitloop.sh`.

## First winter session (c392340)

The ruler stocked every member to the target (then 12 a member) with Feed Residents before winter. At winter's start the camp held 78 food for 6 members (13 each); by midday on winter day 1 it was down to 8.5 each, all 6 members sheltering at home. That is about 24 food a member a winter, twice the target. No one joined or was born while the nation was short.

- **Major:** the winter target was not honest; a nation meeting it ran short within half a winter day ([famine_line.png](c392340/s03/shots/famine_line.png), [winter_midday.png](c392340/s03/shots/winter_midday.png)). Fixed by #497: pawns no longer waste a third of each apple against the full bar, and the target became 14.
- Minor: stocked food dropped 101 → 78 before winter began, unexplained ([winter_day1_start.png](c392340/s03/shots/winter_day1_start.png); food above the 12 kept rotted). #497 made the kept amount equal the store and added "N rotting" to the Season box.
- Minor: the banner said "1 is short. Feed Residents", but the button read "(0 hungry)" and nothing named the member ([feed_disabled.png](c392340/s03/shots/feed_disabled.png)). #497 names the short members.
- Minor: Purchase Food was pressable in a settlement with no stalls ([purchase.png](c392340/s03/shots/purchase.png)).
- Minor: own members labelled "Stranger" on the Talk button ([wait1.png](c392340/s03/shots/wait1.png)); see #507.
- Polish: a gap where an em dash should be in the "A claimant stirs" event ([emdash_missing.png](c392340/s03/shots/emdash_missing.png)).

## Second winter session (bdae9bf)

A camp with a farm and a stall, and one member, Katie White, who became the stall's merchant. "Food: 15 of 14" on the last autumn night became "1 of 14" by winter morning: she had stocked her stall with her winter store. The ruler carried 29-37 apples through winter with no way to give her any.

- **Major:** the banner said to Feed Residents, but the settlement screen had no such button ([no_feed_button.png](bdae9bf/s03/shots/no_feed_button.png)). Fixed by #499 (Feed Residents on the settlement screen).
- **Major:** the merchant's winter store went onto her stall overnight ([winter_start_food_crash.png](bdae9bf/s03/shots/winter_start_food_crash.png), [katie_merchant.png](bdae9bf/s03/shots/katie_merchant.png)). Fixed by #499 (`WinterStoreKeeping`).
- **Major:** a stocked ruler could not stock a member before she was hungry ([no_prestock.png](bdae9bf/s03/shots/no_prestock.png)). #499 relabelled the button "(N short for winter)" and explained it on hover.
- Minor: the hunger banner did not name the hungry member ([banner_no_name.png](bdae9bf/s03/shots/banner_no_name.png)). Fixed by #499.
- Minor: no winter food line until a pawn joins ([katie_joined.png](bdae9bf/s03/shots/katie_joined.png)); filed as #500. "Winter in 1 day!" shown hours before winter; filed as #501 (fixed by #519).
- Minor: world events pause the world in the middle of a long wait ([w4.png](bdae9bf/s03/shots/w4.png)); filed as #502.
- Polish: Found Settlement pressable on another nation's land ([loc.png](bdae9bf/s03/shots/loc.png)); filed as #503 (fixed by #520).

## Measuring winter without a window

The numbers in the #495, #497 and #499 tables came from a headless seed sweep, now in [tools/BeyondNations.WinterSweep](../../../../tools/BeyondNations.WinterSweep/README.md): `world` runs an AI world through a winter on seeds 41-56, and `camp` runs these sessions' player-led camp on seeds 101-108. Winter tuning (#504) is measured with it.
