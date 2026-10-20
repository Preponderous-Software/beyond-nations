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
| Eat something | 58 | 58 | 59 | 59 | 59 | 30 – 180 |
| Create or join a nation | 1 | 1 | 1 | 1 | 1 | 0 – 60 |
| Found a settlement | 6 | 3 | 4 | 1 | 5 | 0 – 180 |
| Enter your settlement | 1 | 1 | 1 | 1 | 1 | 0 – 60 |
| Build a market stall | 2 | 4 | 2 | 2 | 2 | 0 – 120 |
| Grow your nation to 5 | 60 | 18 | 144 | 24 | 24 | 5 – 180 |
| Sell resources at market | 13 | 4 | 16 | 7 | 15 | 0 – 180 |
| Found a second settlement | 6 | 6 | 12 | 6 | 6 | 0 – 120 |
| Grow your nation to 10 (ambition) | 176 | 12 | never | 58 | 25 | 5 – 600, advisory |
| Amass 500 coins (ambition) | 82 | 1 | – | 1 | 1 | 30 – 900, advisory |
| Hold 3 settlements (ambition) | 6 | 1 | – | 10 | 8 | 0 – 300, advisory |
| Survive to day 2 (ambition) | 606 | 908 | – | 848 | 871 | 300 – 1800, advisory |

Measured on develop with seasons and regions (#318). The whole first arc takes between 1 m 38 s and 4 m 01 s of game time.

## Known issues the harness found

- **"Grow your nation to 5" used to be met within a second on some seeds.** Before seasons and regions (#318), settlers joined a new settlement on their own, and on seed 3 the nation already had five members when the step came up (1 s). The 5 s minimum fails that. On develop with #318 the step takes 18 s to 2 m 24 s, because nations now take up almost every settler in the first two minutes. So the target is unchanged, and the minimum stays to catch a return of the problem.
- **On most seeds a new player's nation is besieged and taken within about ten minutes.** By minute two the world has 25 to 45 nations and its full 100 pawns, so few nationless settlers are left to recruit, and Hostile neighbours at least twice the player's strength lay siege. Gifts only buy time. On seed 3 the bot's nation falls at about minute 5. Every nation it founds after that falls within a few minutes, so "Grow your nation to 10" never completes there. Before #318 this happened on four of the five seeds. This is why the ambitions are advisory for now. Make them binding once a new player can hold on.
- **"Amass 500 coins" is usually met when it comes up.** The rewards from the arc, the starting purse and the treasuries already add up to more than 500.
- **The first minute leaves the player rich in wood.** "Eat something" is a one-minute wait for hunger. A player who keeps chopping trees in that minute ends it with about 400 wood, so both "Found a settlement" steps take only seconds afterwards.
