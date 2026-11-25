using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace beyondnations.wintersweep {

    /**
    * A player-led nation through one winter, as in the 0.8.0 winter
    * playtests (seed 103, --season autumn): from the first day of autumn a
    * camp the player founded, with no buildings or stalls and a handful of
    * members (or, with --town, the staged town from --stage-settlement). The
    * player is kept away. At winter's first tick every member is set to
    * exactly `stock` apples and the winter is watched tick by tick: food
    * eaten, energy lost to the full bar, time indoors, and when the stores
    * ran out. These are the numbers in the #497 player-led table.
    */
    public static class CampProbe {
        public const int TimelineEvery = 3000;

        public class Result {
            public int seed;
            public int members;
            public int deaths;
            public double eatenPerMember;
            public double emptyAtDays = -1;
            public double meanEnergyAtSpring;
            public string text;
        }

        public static Result run(int seed, int stock, int memberCount, bool town, bool timeline) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            Simulation sim = new Simulation(config);
            sim.getSeasonCalendar().advanceClockTo(sim.getTickCounter(), Season.AUTUMN);
            sim.startPlayerFresh();
            if (town) SettlementShowcase.stage(sim); else stageCamp(sim, memberCount);
            NationId nationId = sim.getPlayer().getNationId();
            while (!sim.getSeasonCalendar().isWinter()) {
                sim.fixedUpdateWithPlayerAway(1f / 50f);
            }
            List<Pawn> members = membersOf(sim, nationId);
            foreach (Pawn p in members) {
                foreach (ItemType food in new[] { ItemType.APPLE, ItemType.CHICKEN_MEAT, ItemType.COOKED_MEAT }) {
                    p.getInventory().removeItem(food, p.getInventory().getNumItems(food));
                }
                p.getInventory().addItem(ItemType.APPLE, stock);
            }
            int start = members.Count * stock;
            long indoor = 0, outdoor = 0;
            int eaten = 0;
            double waste = 0, gained = 0, burned = 0;
            int deaths0 = sim.getNumPawnDeaths();
            int t = 0, emptyAt = -1;
            int dayTicks = TimeOfDay.DefaultTicksPerDay;
            List<string> lines = new List<string>();
            while (sim.getSeasonCalendar().isWinter()) {
                var before = members.Where(p => !p.isMarkedForDeletion()).ToDictionary(p => p,
                    p => (food: FoodItems.countFood(p.getInventory()), energy: p.getEnergy(), inside: p.isCurrentlyInSettlement()));
                sim.fixedUpdateWithPlayerAway(1f / 50f);
                t++;
                foreach (var kv in before) {
                    Pawn p = kv.Key;
                    if (kv.Value.inside) indoor++; else outdoor++;
                    float rate = p.getMetabolism() * (kv.Value.inside
                        ? config.getSettlementMetabolismMultiplier() * SeasonalRules.shelterMultiplier(Season.WINTER, p.isStarving())
                        : SeasonalRules.WINTER_COLD_MULTIPLIER);
                    burned += rate;
                    int drop = kv.Value.food - FoodItems.countFood(p.getInventory());
                    if (drop > 0 && p.getEnergy() > kv.Value.energy) {
                        eaten++;
                        float raw = kv.Value.energy + 15f;
                        if (raw > Pawn.MaxEnergy) waste += raw - Pawn.MaxEnergy;
                        gained += 15;
                    }
                }
                if (t % TimelineEvery == 0) {
                    List<Pawn> alive = members.Where(p => !p.isMarkedForDeletion()).ToList();
                    int food = alive.Sum(p => FoodItems.countFood(p.getInventory()));
                    lines.Add($"  t+{t,6}: food {food,3}  members {alive.Count}  mean energy {(alive.Count == 0 ? 0 : alive.Average(p => p.getEnergy())):F0}");
                    if (food == 0 && emptyAt < 0) emptyAt = t;
                }
            }
            List<Pawn> survivors = members.Where(p => !p.isMarkedForDeletion()).ToList();
            Result r = new Result {
                seed = seed,
                members = members.Count,
                deaths = sim.getNumPawnDeaths() - deaths0,
                eatenPerMember = (double) eaten / members.Count,
                emptyAtDays = emptyAt < 0 ? -1 : emptyAt / (double) dayTicks,
                meanEnergyAtSpring = survivors.Count == 0 ? 0 : survivors.Average(p => p.getEnergy()),
            };
            StringBuilder text = new StringBuilder();
            text.Append($"seed={seed} members={members.Count} stock={stock} start={start} end={survivors.Sum(p => FoodItems.countFood(p.getInventory()))}"
                + $" eaten={eaten} perMember={r.eatenPerMember:F1} wasteEnergy={waste:F0} ({100 * waste / Math.Max(1, gained):F0}% of eaten)"
                + $" indoor={100.0 * indoor / Math.Max(1, indoor + outdoor):F0}% burnPerMember={burned / members.Count:F0}"
                + $" emptyAt={(emptyAt < 0 ? "never" : r.emptyAtDays.ToString("F2") + "d")} worldDeaths={r.deaths}"
                + $" meanEnergyAtSpring={r.meanEnergyAtSpring:F0} gluttons={members.Count(p => p.hasTrait(PawnTrait.GLUTTON))}");
            if (timeline) {
                foreach (string line in lines) text.Append('\n').Append(line);
            }
            r.text = text.ToString();
            return r;
        }

        public static string summarise(Result[] results) {
            return $"summary over {results.Length} seeds ({results.First().seed}-{results.Last().seed}):\n"
                + $"  stores ran out           {results.Count(r => r.emptyAtDays >= 0)} of {results.Length} seeds"
                + (results.Any(r => r.emptyAtDays >= 0) ? $", at winter day {results.Where(r => r.emptyAtDays >= 0).Min(r => r.emptyAtDays):F1}-{results.Where(r => r.emptyAtDays >= 0).Max(r => r.emptyAtDays):F1}" : "") + "\n"
                + $"  world deaths in winter   {Program.meanMax(results.Select(r => (double) r.deaths))}\n"
                + $"  eaten per member         {Program.meanMax(results.Select(r => r.eatenPerMember))}\n"
                + $"  mean energy at spring    {results.Average(r => r.meanEnergyAtSpring):F0}";
        }

        // the playtest's nation: a camp the player founded, no buildings, no stalls, a handful of members
        private static Settlement stageCamp(Simulation sim, int count) {
            Player player = sim.getPlayer();
            new NationCreateCommand(sim.getNationRepository(), sim.getEventProducer(), sim.getRandom(), sim.getNationNameGenerator()).execute(player);
            Nation nation = sim.getNationRepository().getNation(player.getNationId());
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            new FoundSettlementCommand(sim.getNationRepository(), sim.getEventProducer(), sim.getEntityRepository(), sim.getGameConfig(), sim.getRandom()).execute(player);
            Settlement settlement = (Settlement) sim.getEntityRepository().getEntity(player.getHomeSettlementId());
            for (int i = 0; i < count; i++) {
                Pawn pawn = new Pawn(settlement.getPosition(), sim.getPawnNameGenerator().generate(), sim.getRandom());
                pawn.setTraits(PawnTraits.roll(sim.getTraitRandom()));
                sim.getEntityRepository().addEntity(pawn);
                nation.addMember(pawn.getId());
                pawn.setNationId(nation.getId());
                pawn.setHomeSettlementId(settlement.getId());
                SettlementLife.admit(pawn, settlement);
            }
            settlement.setResidentCount(SettlementLife.countResidents(sim.getEntityRepository(), settlement.getId()));
            return settlement;
        }

        private static List<Pawn> membersOf(Simulation sim, NationId nationId) {
            List<Pawn> list = new List<Pawn>();
            foreach (Entity e in sim.getEntityRepository().getEntities()) {
                if (e is Pawn p && nationId.Equals(p.getNationId())) list.Add(p);
            }
            return list;
        }
    }
}
