# Winter seed sweep

Measures the first winter headless, over many seeds, so a winter change can be compared before and after. It produced the tables in #495, #497 and #499, and is how winter tuning (#504) is measured.

```sh
dotnet build BeyondNations.sln -c Release
SWEEP=tools/BeyondNations.WinterSweep/bin/Release/net8.0/BeyondNations.WinterSweep.dll

# an AI world through its first winter, seeds 41-56 (the TestSeasonalRules scenario)
dotnet $SWEEP world --seeds 41-56 --csv sweep-after

# a player-led camp of 6, each member stocked to the winter target at winter's first tick, seeds 101-108
dotnet $SWEEP camp --seeds 101-108
```

Each prints one line a seed, then a summary (mean, and max where it matters). Seeds run in parallel, one Simulation each; a seed's world is the one the game would run, since the measuring draws no random numbers.

## `world`: an AI world

The default start with no player action, from day 0 to the end of the first day of the second spring, as `TestSeasonalRules` runs it. Per seed:

| field | what it counts |
| --- | --- |
| `winterDeaths` | pawn deaths from winter's first tick to spring (`strangers`: of those, pawns in no nation) |
| `springDeaths` | deaths in the first day of spring; `q=[...]` splits them by quarter day |
| `foodPerMemberAtWinter` | food nation members carry at winter's first tick, per member (compare with `WINTER_FOOD_STORE`) |
| `winterBirths`, `winterJoins` | births and strangers joining in winter; `famine`: of those, into nations with a quarter or more of members hungry |
| `eaten`, `sold`, `spoiled` | where nation members' food went in winter: eaten (a drop with an energy gain), spoiled (a drop on a spoil tick), or to market (any other drop) |
| `yearDeaths`, `pawnsEnd`, `nations`, `births`, `immigrants` | the world at the end |

Options:

- `--csv DIR` writes `DIR/<seed>.csv`, a row every 300 ticks: season, members, food, hungry, starving, `wanted` (members × the target), births, joins, the food tallies, deaths and pawns.
- `--fed` tops every nation member up to 2 apples on each winter tick: what a winter eats when no one goes short.

To compare two builds, run the same seeds with `--csv` on each (for example, in a worktree of `develop` and in your branch) and chart them:

```sh
python3 tools/winter-sweep-chart.py sweep-before sweep-after 41-56 winter.png \
    src/BeyondNations.Desktop/assets/fonts/DejaVuSans.ttf 14 [OLD_TARGET]
```

The chart (ImageMagick) has three panels from autumn on, mean over the seeds, grey before and blue after: food per member with the target marked, the share of members hungry, and deaths since autumn.

## `camp`: a player-led camp

The 0.8.0 winter playtests' nation ([docs/playtests/0.8.0/winter](../../docs/playtests/0.8.0/winter/README.md)): from the first day of autumn, a camp the player founded, no buildings or stalls, and 6 members; the player is kept away. At winter's first tick every member is set to exactly `--stock` apples (default `WINTER_FOOD_STORE`), and the winter is watched tick by tick. Per seed: food eaten per member, energy lost to the full bar, the share of time indoors, energy burned per member, when the stores ran out (`emptyAt`, in winter days, or `never`), pawn deaths in the whole world over the winter (`worldDeaths`, not only members), and members' mean energy at spring.

Options: `--stock N`, `--members N`, `--town` (the staged town from `--stage-settlement` instead of the camp), and `--timeline` (food, members and mean energy every 3000 ticks).

## Cost

One `world` seed runs about 261,000 ticks (a year of 8 days of 30,000 ticks, and a day of spring) with about 100 pawns; one `camp` seed about 120,000 (autumn and winter). All 16 `world` seeds took 21 minutes of wall time (28 CPU minutes) on a 20-core machine under a load of 60-100; run it in the background. `develop` at 2167959c gives winter deaths 2.31 (max 7), spring deaths 0.69 (max 2), food per member at winter 13.8, the same as the #499 table.
