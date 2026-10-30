# Pacing

How long each objective takes is measured, not guessed. `tools/BeyondNations.Pacing` drives the Core simulation with no window and a scripted player, records when each objective completes on several seeds, and checks the times against a budget checked in at [`pacing-budget.json`](./pacing-budget.json). CI runs it on every push and pull request (see [CI-CD.md](./CI-CD.md)).

## The bot

`PacingBot` plays like a person who reads the objective hints:

- It works on the current objective and nothing else, pressing one key at a time and at most five a second.
- It walks to things (sprinting, turning to face them) and never teleports. Gifts, conversations and market sales are at most one a second, the speed at which someone can read the reply.
- It only runs the Core commands that the keys and HUD buttons run (`InteractCommand`, `NationCreateCommand`, `FoundSettlementCommand`, `BuildStallCommand`, `SellResourcesAtMarketCommand`, `GiftPawnCommand`, `InvitePawnToNationCommand`, `GiftNationCommand`). It never uses a debug command.
- It recruits nationless settlers first. They join at Acquaintance, as the hint says. Only when there are none left anywhere does it court another nation's members who neither lead it nor are Loyal by nature. Those need Loyal standing, and poaching them sours relations. It sticks with one settler for up to two minutes while that settler is out of doors.
- When a siege warning tells it to "Gift, ally or grow it", it sends the besieger a gift if it can afford one. If it loses its nation anyway, it founds a new one and starts again.

The bot reads the objective list generically. Ids it knows get a specific plan. Any other objective is planned from the label on its progress counter (`Wood`, `Stone`, `Members`, `Settlements`, `Funds`, `Day`), and anything else gets general play. The harness takes the objectives from `ObjectiveTracker.getObjectives()` and `getCompletedCount()`, so a reworked objective list is still measured, and it reports objectives it has no budget for instead of crashing.

A run is deterministic: the same seed gives the same times on every run and every machine, and the seeds run in parallel without changing a result.

## What is measured

For every objective on every seed:

- **time**: seconds of game time from the previous objective's completion (or the start of the world) to this one's. This is the number the budget checks.
- **completion time**: total ticks, game time and in-game day when it completed.

And, every 30 seconds of game time, the curves: nations alive, pawns alive, the player's nation's members and settlements, the player's coins, funds (coins plus the nation's treasuries), wood and stone, objectives completed, and what the bot was doing.

And, over the whole run, how many times the player's nation was raided (and how many an hour) and how many raids it made (a **Raids** table; `raidsOnPlayer`, `raidsOnPlayerPerHour`, `raidsByPlayer` in the JSON).

The report goes to `pacing-out/pacing-report.md` (readable, with one curve row per minute) and `pacing-out/pacing-report.json` (everything). In CI it is the job summary of **Build and test** and the `pacing-report` artifact.

## Running it locally

```bash
export PATH=/root/.dotnet:$PATH   # wherever your .NET 8 SDK is
dotnet run --project tools/BeyondNations.Pacing -c Release -- --check
```

That runs the seeds, objective limit and tick cap in the budget file, writes the report to `pacing-out/`, and exits 1 if any objective is out of budget. Options:

| Option | Meaning |
|---|---|
| `--budget FILE` | the budget to read (default `docs/pacing-budget.json`) |
| `--seeds 1,2,3` | other seeds |
| `--objectives N` | stop each run after N objectives |
| `--max-ticks N` | stop each run after N ticks |
| `--out DIR` | where the report goes (default `pacing-out`) |
| `--check` | exit 1 on a failure |
| `--write-budget` | rewrite the budget file from these runs |
| `--trace` | print every status line the bot's player sees, with its time, one seed at a time |

`--trace` is the way to find out why an objective is slow: it shows what the bot was doing and what the game told it.

## The budget

Each entry in `docs/pacing-budget.json` gives an objective's `id`, its `title` (for people reading it), `minSeconds` and `maxSeconds`, and optionally a `note` and `advisory`. The file also sets the `seeds`, `objectiveLimit` (how many objectives a run plays) and `maxTicks` (when a run gives up).

A run **fails** the check when an objective:

- finishes in **less** than `minSeconds`. A minimum states a design intent: it is the time below which the player did not really do anything for the objective. A teaching step that is one key press has a minimum of 0.
- finishes in **more** than `maxSeconds`;
- **never** finishes before the run ends.

It only **warns** when:

- the objective has no budget entry, which is what happens when someone adds or renames objectives;
- a budget entry is never reached (it was renamed or removed, or a run stopped before it);
- the entry is `advisory`: it is reported as too fast, too slow or never, but does not fail the build. Use this for objectives that are still being balanced.

### Updating the budget

When you change pacing on purpose (a cost, a target, a new objective):

1. Run `dotnet run --project tools/BeyondNations.Pacing -c Release -- --write-budget`. It sets each maximum to twice the slowest time plus a minute, and each minimum to half the fastest time, keeping a hand-set minimum that is lower. It keeps `advisory` and `note` and marks new ambitions as advisory.
2. Read the diff. The minimums in particular are design decisions, not measurements, so set them to what the objective should take, not what it happens to take now.
3. Run `--check`, and commit the budget with the change that needed it, saying why in the pull request.

A failing check is a real result. Before you widen a budget, read the report and a `--trace` and decide whether the game or the budget is wrong.

## Measured timings (seeds 1 to 5)

Seconds of game time per objective, at 50 ticks a second and 30 000 ticks (10 minutes) a day:

| Objective | seed 1 | seed 2 | seed 3 | seed 4 | seed 5 | budget |
|---|---|---|---|---|---|---|
| Gather wood | 3 | 3 | 2 | 2 | 2 | 0 – 120 |
| Eat something | 1 | 1 | 1 | 1 | 1 | 0 – 60 |
| Create or join a nation | 1 | 1 | 1 | 1 | 1 | 0 – 60 |
| Found a settlement | 3 | 7 | 6 | 3 | 3 | 0 – 180 |
| Enter your settlement | 1 | 1 | 1 | 1 | 1 | 0 – 60 |
| Build a market stall | 6 | 14 | 6 | 6 | 5 | 0 – 120 |
| Reach the Village era | 1005 | 993 | 1003 | 1006 | 1007 | 300 – 1800 |
| Sell resources at market | 1 | 1 | 4 | 3 | 7 | 0 – 180 |
| Found a second settlement | 10 | 15 | 18 | 13 | 17 | 0 – 120 |
| Grow your nation to 16 (ambition) | 1 | 1 | 1 | 1 | 1 | 5 – 2400, advisory |
| Amass 1000 coins (ambition) | 77 | 1 | 8 | 1 | 1 | 30 – 3900, advisory |
| Hold 3 settlements (ambition) | 10 | 13 | 17 | 12 | 10 | 0 – 300, advisory |
| Reach the Town era (ambition) | 2301 | 2369 | 2352 | 2370 | 2364 | 0 – 5400, advisory |
| Grow your nation to 25 (ambition) | 161 | 1 | 138 | 1 | 1 | 300 – 3600, advisory |

Measured with eras (Camp to Kingdom, see SYSTEMS.md) and the hungry start (the player begins at 85 energy), on develop with seasons, regions, rulers, world events, trade and spoiling apples, for one hour of game time a seed (`maxTicks` 180 000, 14 objectives). Spoiling apples (SYSTEMS.md, "Seasons") left every binding objective inside its budget and the budget unchanged; the arc to the Village era is unchanged to the second. Among the advisory ambitions, seed 3 now amasses its 1000 coins and goes on to the Town era (it stalled on the coins before), seed 1 now grows to 25, and the Town era takes longer on seed 1 (36 minutes after the previous step, against 24). The arc up to the Village era takes 17 to 20 minutes of game time on seeds 1 to 15. Starving pawns foraging rather than walking home to a market that cannot feed them (SYSTEMS.md, "Seasons") left the binding objectives and the arc to the Village era unchanged; it moved the advisory ambitions a little (seed 3 grows to 16 four minutes later and amasses its coins later, the Town era comes earlier on seeds 3 and 5), and the budget is unchanged. The table was last regenerated on develop with pawn energy capped at a full bar (#340) and Interact acting on what the player faces (below, "Known issues"); the budget is unchanged.

### The eras

When each era was reached, in minutes of game time from the start of the world, over four hours a seed (`--objectives 30 --max-ticks 720000 --seeds 1,...,10`):

| Era | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | aimed at |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Village | 19.9 | 17.0 | 19.2 | 17.0 | 17.0 | 17.0 | 17.0 | 17.0 | 17.0 | 17.0 | 10 – 20 |
| Town | 171 | 57.0 | 164 | 101 | 57.0 | 64.5 | 57.0 | 57.0 | 57.0 | 57.0 | about 60 |
| Kingdom | – | – | – | – | 177 | 177 | – | – | 177 | – | a few hours |

Each era waits for its day as well as its requirements (`EraRules`: day 3, 7 and 19, about 17, 57 and 177 minutes in), because settlers flock to a new nation and without the calendar most seeds were a village within two or three minutes and a town within ten. The calendar is the floor on most seeds; where the times run past it (seeds 1, 3 and 4 for the Town) the bot was short of members. A seed where the Kingdom is a dash had not been seen to reach it by the end of its four hours (the objective list shows an era only when its ambition comes up). The bot never won: Prosperity needs 40 members and 6,000 funds, and it spends its coins on gifts. The Village era is binding in the budget again (the calendar means it can no longer be met within a second); the Town and Kingdom eras are advisory.

### Recruiting

The bot courts settlers faster than a person can: it walks straight to the nearest one, never loses it, and gifts once a second. Playtesters at 3 to 17 frames a second could not keep a pawn in reach, and by the time their settlement stood, the AI nations had signed up every nationless settler. `--no-courting` measures the other way to grow: the bot builds its settlement and stalls as usual but never talks to, gifts or invites a pawn, so every member comes from the world (settlers who wander in, newcomers, births). The console line and the JSON (`villageMembersTick`, `membersInvited`, `membersJoinedOtherwise`) say when the nation first had the Village era's 10 members.

`--no-courting --seeds 1,...,10 --objectives 7 --max-ticks 90000` (30 minutes of game time a seed):

| Seed | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| 10 members, before #371's fix | never (4) | never (5) | never (6) | 8m 00s | never (6) | never (6) | 3m 31s | never (3) | never (6) | never (6) |
| 10 members, after | 10m 01s | 13m 01s | 9m 30s | 8m 00s | 7m 00s | 6m 30s | 3m 31s | 13m 00s | 10m 01s | 7m 30s |

(The number in brackets is the nation's members after 30 minutes.) Before, the world reached its 100-pawn limit within three to six minutes, and a full world drew no newcomer and bore no child, so the nation stopped growing for good; the Village era came on 2 of the 10 seeds. After, the player's nation may grow 20 pawns past the limit (SYSTEMS.md, "Population growth"), it gains a member every minute or two, and the Village era comes at day 3 (17m 00s) on all 10 seeds. The world peaked at 111 pawns. Courting still pays: the bot that courts has 10 members within about two minutes.

## Known issues the harness found

- **"Grow your nation to 5" used to be met within a second on some seeds.** *It is now "Reach the Village era" (a village, 10 members, 2 stalls and day 3), binding at 300 – 1800 s.* Before seasons and regions (#318), settlers joined a new settlement on their own, and on seed 3 the nation already had five members when the step came up (1 s). The 5 s minimum fails that. On develop with #318 the step takes 18 s to 2 m 24 s, because nations now take up almost every settler in the first two minutes. So the target is unchanged, and the minimum stays to catch a return of the problem.
- **On most seeds a new player's nation was besieged and taken within about ten minutes.** *Addressed by eras: a nation the player leads cannot be besieged until the Town era (`EraRules.isSheltered`; raids still come), and a nation that falls apart for good ends the run as a defeat.* The original finding: By minute two the world has 25 to 45 nations and its full 100 pawns, so few nationless settlers are left to recruit, and Hostile neighbours at least twice the player's strength lay siege. Gifts only buy time. On seed 3 the bot's nation falls at about minute 5. Every nation it founds after that falls within a few minutes, so "Grow your nation to 10" never completes there. Before #318 this happened on four of the five seeds. This is why the ambitions are advisory for now. Make them binding once a new player can hold on.
- **"Amass 500 coins" was usually met when it came up.** The rewards from the arc, the starting purse and the treasuries already add up to more than 500. *Addressed by eras: the funds ambitions start from the era's floor, 1,000 in the Village era.*
- **"Eat something" was a one-minute wait for hunger.** *Addressed: a new world starts the player at 85 energy (`Player.NewWorldEnergy`), below the line under which carried food is eaten (90), so the first food picked up is eaten at once and the step takes about a second. Eating is still refused above 90, and the rate hunger drains is unchanged.* The original finding: the player started full and only eats below 90, so the step was about 58 s of waiting, and a player who kept chopping trees in that minute ended it with about 400 wood, so both "Found a settlement" steps took only seconds afterwards. They still take seconds: the bot gathers that wood while the settlement objective is current.
- **"Found a second settlement" took 134 s on seed 5 (budget 120 s) once pawn energy was capped at a full bar (#340).** *Addressed in the game: Interact acts on what the player faces.* The trace showed the bot standing 2.5 units from the tree it had walked up to and faced, while pawns strolled within 1 to 3 units of it; Interact gave a pawn in reach priority over a tree whatever the player faced, so for about 90 seconds every press talked ("Yes, leader? I hardly know you...", 338 status lines) instead of felling the tree. A person at that tree would have met the same wall (PLAYTESTING.md listed "a pawn standing by the tree wins" as a known trap). Now, past the drops and the well, E acts on whichever pawn, tree, rock or chicken in reach is most squarely in front (within 60 degrees), and keeps the old order only when nothing is. Seed 5 now takes 17 s; seeds 1 to 4 are unchanged to the second, and on seed 5 only the advisory ambitions after it moved (Hold 3 settlements 9 s to 10 s, the Town era 37m 28s to 39m 24s). The budget is unchanged.
