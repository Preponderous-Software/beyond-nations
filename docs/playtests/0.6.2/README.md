# Playtest evidence: 0.6.2

The release-candidate playtest for 0.6.2: four AI agents, run through [playtester](https://github.com/Preponderous-Software/playtester) with its `beyond-nations` profile ([docs/PLAYTESTING.md](../../PLAYTESTING.md)). Each re-checked fixes merged since 0.6.1. It is a focused run of four because the machine was busy (load average about 27 to 34).

| | |
| --- | --- |
| Build tested | `develop` at `6ac1e3f` (#398 to #406). The game showed `0.6.1`; the version was bumped afterwards. |
| Released as | 0.6.2 |
| Date | 2026-10-24, 11:52 to about 12:25 MDT |
| Harness | playtester at `b69e15e` (`pt`), software rendering at 960x540 |
| Results | [summary.md](summary.md) (readable) and [summary.json](summary.json): 0 blocker, 0 major, 21 minor, 10 polish |

## Sessions

| Session | Goal | Seed | Budget | Outcome |
| --- | --- | --- | --- | --- |
| s01 | Sieges: the Era box names a siege under way; a minute to capture; time to react (#396, #404) | 111 (`--stage-diplomacy`) | 30 min | achieved |
| s02 | Survive the first winter; count hunger deaths and leaders among them (#353, #395, #406) | 103 (`--season winter`) | 60 min | achieved |
| s03 | Settlement HUD at 960x540; Era box scrolls; Sell Resources with only food (#350, #397) | 109 (`--stage-settlement`) | 30 min | partial |
| s04 | First 20 minutes as a new player; AI nations found their settlements (#402) | 1 | 30 min | achieved |

Each `sNN/` directory holds the agent's `report.json`, its notes (`findings.jsonl`), the game's event log (`events.jsonl`), every harness command it ran (`actions.jsonl`), `session.json` and the screenshots its findings cite. Game logs, state snapshots, save data and the filled prompts were left out.

## What the run found

- **Confirmed fixed:**
  - #396 and #404: the Era box shows "Your village: Besieged by Greenshire (n/12)!". Left alone, the village fell 50 game seconds after spawn. One tribute (25 coins) or an alliance made at round 4/12 lifted the siege, so a minute is enough to react.
  - #353, #395 and #406: from the start of winter on seed 103 to spring, 3 NPCs starved and none was a sitting leader. The 0.6.1 run saw 5 deaths (4 of them leaders) over two thirds of the winter. The player's 17-member nation lost no one, with Feed Residents used 19 times.
  - #402: on seed 1, all 37 AI nations founded a settlement within 1 to 30 game seconds of being declared, and none of about 15 pawns tracked in the wilds stood still.
  - #350 and #397: the right column fits at 960x540 with a busy market, Player Info names the player's nation, and the open Era detail scrolls instead of covering the command bar.
- **Minor, open:**
  - The compact merchant line still wraps onto two lines.
  - Hunger counts disagree between the banner, the button and the result.
  - The command bar still shifts under the cursor after Feed Residents or crafting.
  - Lifting a siege gives no message.
  - A nation captured out of existence gets the epitaph "never raised so much as a hut".
  - The chronicle fills with AI activity (495 entries by day 2).
  - Raids report "coins taken" without lowering the player's coins.

## What went wrong in the run

- **Slow worlds.** The simulation ran at about half speed (load average about 30), so s04 reached about 8 game minutes in 16 minutes of play.
- **A placeholder in s01's launch.** The first session was launched with a placeholder in place of the run directory and corrected by message before it started a game. It confirmed that nothing ran under the placeholder.
