# Playtest evidence: 0.6.3

The release-candidate playtest for 0.6.3, run through [playtester](https://github.com/Preponderous-Software/playtester) with its `beyond-nations` profile ([docs/PLAYTESTING.md](../../PLAYTESTING.md)). Five sessions checked the fixes since 0.6.2 (#409–#411). Their majors led to #412–#415, and three more sessions then re-checked those on the build that became the release candidate.

| | |
| --- | --- |
| Builds tested | s01–s05: `develop` at `edb1a62`. s06–s08: `develop` at `7f89db5` (adds #412–#414). #415 was found by s07, then fixed and covered by tests; it was not re-played. The game showed `0.6.2`, and the version was bumped afterwards. |
| Released as | 0.6.3 |
| Date | 2026-10-24, 16:10 to about 18:00 MDT |
| Harness | playtester at `b69e15e` (`pt`), software rendering at 960x540 |
| Results | [summary.md](summary.md) and [summary.json](summary.json): 0 blocker, 7 major, 32 minor, 11 polish |

## Sessions

| Session | Goal | Seed | Build | Outcome |
| --- | --- | --- | --- | --- |
| s01 | Siege lifted message, lapsed confirm prompts, epitaphs (#409) | 111 (`--stage-diplomacy`) | edb1a62 | achieved |
| s02 | Winter: hunger banner, button and result agree (#410) | 103 (`--season winter`) | edb1a62 | achieved |
| s03 | Try to make the command bar move under the cursor (#410) | 109 (`--stage-settlement`) | edb1a62 | achieved |
| s04 | Chronicle "your news" by default, with a switch (#411) | 1 | edb1a62 | achieved |
| s05 | Race to the Village era on a fresh world | 2026 | edb1a62 | achieved (Village at 1,020 game s) |
| s06 | Re-check: moved buttons no longer take a stray click (#412) | 109 (`--stage-settlement`) | 7f89db5 | achieved |
| s07 | Re-check: what the player is told when their nation falls (#413) | 111 (`--stage-diplomacy`) | 7f89db5 | achieved |
| s08 | Re-check: Feed Residents through a winter (#414) | 103 (`--season winter`) | 7f89db5 | no report (see below) |

## What the run found

The 7 major groups and where each went:
- **Fixed by #412:**
  - A click meant for Talk bought a pickaxe for 299 coins when the pawn left (s03).
  - A double click on Enter Settlement withdrew funds (s03).
  - s06 confirmed fast double clicks are now held off. A deliberate second click 0.8 s or more later can still land on Withdraw Funds or Plant Sapling (minor, open).
- **Fixed by #414:** "You fed N", and residents were hungry again at once (s02).
- **Fixed by #413:**
  - The chronicle's shared 200 lines lost the player's founding (s04).
  - "Autosaved." hid the fall of the player's nation (s01). s07 confirmed the status line now says "Greenshire has captured your village! … You are without a nation."
- **Fixed by #415:** "Your news" and "Only my nation" dropped a fallen nation's raids, siege and capture (s07).
- **Open, for the owner (design):** reaching the Village era's 10 members now means recruiting from AI nations by gift grinds (s05), since every pawn belongs to one after #402.

The rest of #409–#411 held:
- **#409:** the lifted-siege message, "Not confirmed in time, so nothing was done.", and correct epitaphs.
- **#410:** banner, button and result agreed over 20 feeds, and the Talk button shows the standing without traits.
- **#411:** "your news" by default, with the switch and U.

## What went wrong in the run

- **s08 has no report.** Its agent stopped partway through when the account reached its weekly usage limit. Its actions, notes and events up to then are kept. #414 is covered by `testFeed_GivesEachEnoughToStopBeingHungry_RoundByRound` but was not re-played.
- **#415 is not re-played.** It is covered by `testALostNation_StaysYourNews`.
