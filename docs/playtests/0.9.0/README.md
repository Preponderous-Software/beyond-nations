# Playtest evidence: 0.9.0

This is the playtest of the 2026-11-24 polish pass, which is expected to become 0.9.0. It was run on uncommitted trees on top of `develop` at **`2167959c`**. The game reported its version as `0.8.1-SNAPSHOT`; the version is bumped when the release is cut.

It was one playtest agent running sessions one after another, not the 10-session parallel run of earlier releases. The run used [playtester](https://github.com/Preponderous-Software/playtester) with its `beyond-nations` profile ([docs/PLAYTESTING.md](../../PLAYTESTING.md)), software rendering on a headless display.

| | |
| --- | --- |
| Builds tested | `bn-integration`: develop + every gameplay lane, no contextual HUD. `bn-full`: the same plus the contextual HUD, Journal and unlocks. Each session's `report.json` gives its tree in `build.commit`. |
| Released as | 0.9.0 (expected). If the release turns out to be a different version, rename this directory to match ([docs/RELEASING.md](../../RELEASING.md)). |
| Date | 2026-11-24, about 14:40 to 16:40 MST |
| Sessions | w1, w2 (bn-integration); f1, w2f, w3 (bn-full) |
| Results | [summary.md](summary.md) and [summary.json](summary.json): 27 findings in 25 groups |

## Sessions

| Session | Tree | Goal | Seed | Outcome |
| --- | --- | --- | --- | --- |
| w1 | bn-integration | Opening: gather, create a nation, found a settlement, order a Stranger member, Pay with Coins | 103 | done; the open-ground hint, the coin toggle, the no-members food line and the event long-wait note all work |
| w2 | bn-integration | First winter from `--season autumn --advance-ticks 25000` | 103 | stopped at winter start (switched to bn-full) |
| f1 | bn-full | New game on the contextual HUD: unlock order, Next line, Journal | 103 | done; the first bar starts empty and unlocks arrive in a sensible order |
| w2f | bn-full | First winter, stocked to 14 food per member | 103 | 0 deaths over 50,300 winter ticks, about 3 food each projected left at the thaw |
| w3 | bn-full | Phone size (390x844) inside a settlement | 103 | found the box/card overlap (fixed, below) |

## Findings and what became of them

Fixed in the same pull request before it was opened:
- The settlement and market boxes covered the Next line and the tip card on a window held upright.
- Chip text was cut off.
- The winter banner sat over the chips.
- The old 5-tip track contradicted the Next line.
- Advanced and season-started worlds got the new-game command bar.
- The famine line came on early in winter.
- The rotting count was confusing.
- The two building prices had no explanation.

Left as follow-ups:
- Toasts and banners can overlap in wide windows.
- The Journal footer names keys on touch.
- The Nations screen has no backing panel at phone size.
- Ordering a pawn at low frame rates is fiddly.

Shots are the ones the findings cite, reduced to 256 colours.
