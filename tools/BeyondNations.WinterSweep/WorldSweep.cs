using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace beyondnations.wintersweep {

    /**
    * An AI world, as TestSeasonalRules runs it: the default start, no player
    * action, from day 0 through the first day of the second spring.
    * Per seed: winter and spring deaths, births and joins in winter (and how
    * many went to nations with a quarter or more of their members hungry),
    * and where nation members' food went in winter -- eaten, sold or
    * stocked at market, or spoiled.
    *
    * The food accounting reads each member's pack before and after every
    * winter tick, so it draws no random numbers and changes nothing: a seed's
    * world is the one the game would run. A drop with an energy gain counts
    * one food eaten; the rest of the drop is spoilage on a spoil tick and
    * market otherwise. These are the numbers in the #495, #497 and #499 tables.
    */
    public static class WorldSweep {
        public const string CsvHeader = "tick,season,progress,members,food,hungry,starving,wanted,births,joins,eaten,sold,spoiled,deaths,pawns";
        public const int CsvEvery = 300;

        public class Result {
            public int seed;
            public int winterDeaths;
            public int springDeaths;
            public int strangerWinterDeaths;
            public double foodPerMemberAtWinter;
            public int winterBirths;
            public int winterJoins;
            public int eaten;
            public int sold;
            public int spoiled;
            public string line;
        }

        public static Result run(int seed, string csvDir, bool fed) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            Simulation sim = new Simulation(config);
            int toSpring = (int) ((SeasonCalendar.DAYS_PER_YEAR - TimeOfDay.DefaultStartFraction) * TimeOfDay.DefaultTicksPerDay) + 100;
            int total = toSpring + TimeOfDay.DefaultTicksPerDay;
            List<string> rows = new List<string> { CsvHeader };
            Dictionary<EntityId, NationId> lastNation = new Dictionary<EntityId, NationId>();
            Dictionary<NationId, double> hungryShare = new Dictionary<NationId, double>();
            Dictionary<Pawn, (int food, float energy)> before = new Dictionary<Pawn, (int, float)>();
            Result r = new Result { seed = seed, foodPerMemberAtWinter = -1 };
            int winterJoinsFamine = 0, winterBirthsFamine = 0;
            int deathsAtSpring = -1, deathsAtWinter = -1;
            int[] springQuarter = new int[4];

            for (int t = 1; t <= total; t++) {
                Season season = sim.getSeasonCalendar().getSeason();
                bool winter = season == Season.WINTER;
                if (winter && fed) {
                    foreach (Entity e in sim.getEntityRepository().getEntities()) {
                        if (e is Pawn p && p.getNationId() != null && p.getInventory().getNumItems(ItemType.APPLE) < 2) {
                            p.getInventory().addItem(ItemType.APPLE, 2);
                        }
                    }
                }
                List<Pawn> strangers = null;
                if (winter) {
                    before.Clear();
                    strangers = new List<Pawn>();
                    foreach (Entity e in sim.getEntityRepository().getEntities()) {
                        if (!(e is Pawn p)) continue;
                        if (p.getNationId() != null) before[p] = (FoodItems.countFood(p.getInventory()), p.getEnergy());
                        else if (!p.isMarkedForDeletion()) strangers.Add(p);
                    }
                    if (r.foodPerMemberAtWinter < 0) {
                        r.foodPerMemberAtWinter = before.Count == 0 ? 0 : before.Values.Sum(v => v.food) / (double) before.Count;
                    }
                }
                int deathsBefore = sim.getNumPawnDeaths();

                sim.fixedUpdate(1f / 50f);

                if (winter) {
                    foreach (var kv in before) {
                        int drop = kv.Value.food - FoodItems.countFood(kv.Key.getInventory());
                        if (drop <= 0) continue;
                        if (kv.Key.getEnergy() > kv.Value.energy) { r.eaten += 1; drop -= 1; }
                        if (drop > 0) {
                            if (sim.getTickCounter().getTotalTicks() % SeasonalRules.APPLE_SPOIL_INTERVAL_TICKS == 0) r.spoiled += drop;
                            else r.sold += drop;
                        }
                    }
                }
                int deaths = sim.getNumPawnDeaths();
                if (winter && deaths > deathsBefore) {
                    foreach (Pawn p in strangers) if (p.isMarkedForDeletion()) r.strangerWinterDeaths++;
                }
                if (t == toSpring) deathsAtSpring = deaths;
                if (winter && deathsAtWinter < 0) deathsAtWinter = deathsBefore;
                if (t > toSpring) {
                    int q = Math.Min(3, (t - toSpring - 1) * 4 / TimeOfDay.DefaultTicksPerDay);
                    springQuarter[q] += deaths - deathsBefore;
                }
                if (t % CsvEvery != 0) continue;

                // every CsvEvery ticks: joins and births since the last look, and the nations' stores
                int births = sim.getPopulationGrowth().getNumBirths();
                int joins = 0;
                Dictionary<EntityId, NationId> nowNation = new Dictionary<EntityId, NationId>();
                Dictionary<NationId, List<Pawn>> members = new Dictionary<NationId, List<Pawn>>();
                foreach (Entity e in sim.getEntityRepository().getEntities()) {
                    if (!(e is Pawn p) || p.isMarkedForDeletion()) continue;
                    nowNation[p.getId()] = p.getNationId();
                    if (p.getNationId() == null) continue;
                    if (!members.TryGetValue(p.getNationId(), out List<Pawn> list)) members[p.getNationId()] = list = new List<Pawn>();
                    list.Add(p);
                    bool existed = lastNation.TryGetValue(p.getId(), out NationId was);
                    bool famine = hungryShare.TryGetValue(p.getNationId(), out double share) && share >= 0.25;
                    if (existed && was == null) {
                        joins++;
                        if (winter) { r.winterJoins++; if (famine) winterJoinsFamine++; }
                    }
                    else if (!existed && lastNation.Count > 0 && winter) {
                        r.winterBirths++;
                        if (famine) winterBirthsFamine++;
                    }
                }
                lastNation = nowNation;
                int m = 0, food = 0, hungry = 0, starving = 0;
                hungryShare.Clear();
                foreach (var kv in members) {
                    int h = kv.Value.Count(p => p.needsFood());
                    hungryShare[kv.Key] = (double) h / kv.Value.Count;
                    m += kv.Value.Count;
                    hungry += h;
                    starving += kv.Value.Count(p => p.isStarving());
                    food += kv.Value.Sum(p => FoodItems.countFood(p.getInventory()));
                }
                rows.Add(string.Join(",", t, season, sim.getSeasonCalendar().getSeasonProgress().ToString("F3"), m, food, hungry, starving,
                    m * SeasonalRules.WINTER_FOOD_STORE, births, joins, r.eaten, r.sold, r.spoiled, deaths,
                    sim.getEntityRepository().getNumEntitiesOfType(EntityType.PAWN)));
            }

            r.springDeaths = sim.getNumPawnDeaths() - deathsAtSpring;
            r.winterDeaths = deathsAtSpring - deathsAtWinter;
            r.line = $"seed={seed} winterDeaths={r.winterDeaths} (strangers {r.strangerWinterDeaths}) springDeaths={r.springDeaths} q=[{string.Join(" ", springQuarter)}]"
                + $" foodPerMemberAtWinter={r.foodPerMemberAtWinter:F1} yearDeaths={deathsAtSpring}"
                + $" winterBirths={r.winterBirths} (famine {winterBirthsFamine}) winterJoins={r.winterJoins} (famine {winterJoinsFamine})"
                + $" eaten={r.eaten} sold={r.sold} spoiled={r.spoiled}"
                + $" pawnsEnd={sim.getEntityRepository().getNumEntitiesOfType(EntityType.PAWN)} nations={sim.getNationRepository().getNations().Count}"
                + $" births={sim.getPopulationGrowth().getNumBirths()} immigrants={sim.getPopulationGrowth().getNumImmigrants()}";
            if (csvDir != null) {
                File.WriteAllLines(Path.Combine(csvDir, seed + ".csv"), rows);
            }
            return r;
        }

        public static string summarise(Result[] results) {
            return $"summary over {results.Length} seeds ({results.First().seed}-{results.Last().seed}), mean (max):\n"
                + $"  winter deaths            {Program.meanMax(results.Select(r => (double) r.winterDeaths))}\n"
                + $"  spring deaths            {Program.meanMax(results.Select(r => (double) r.springDeaths))}\n"
                + $"  food per member, winter  {Program.meanMax(results.Select(r => r.foodPerMemberAtWinter))} (target {SeasonalRules.WINTER_FOOD_STORE})\n"
                + $"  births / joins in winter {results.Average(r => r.winterBirths):F2} / {results.Average(r => r.winterJoins):F2}\n"
                + $"  winter food eaten / to market / spoiled  {results.Average(r => r.eaten):F0} / {results.Average(r => r.sold):F1} / {results.Average(r => r.spoiled):F0}";
        }
    }
}
