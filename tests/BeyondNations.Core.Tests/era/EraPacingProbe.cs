using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;
using Xunit.Abstractions;

using beyondnations;

namespace beyondnationstests {

    /**
    * How long the eras take a busy player, measured: a scripted player that
    * chops, quarries, builds, founds and sells with the same Core commands
    * the keys run, charged game time for every walk, on a few seeds. Off by
    * default, since it plays hours of game time; run it with
    *
    *   BN_ERA_PACING=1 dotnet test tests/BeyondNations.Core.Tests --filter EraPacingProbe --logger "console;verbosity=detailed"
    *
    * BN_ERA_PACING_HOURS sets how long each seed plays (default 4) and
    * BN_ERA_PACING_SEEDS which seeds (default 42,7,2026). The targets the
    * era requirements are tuned to: Village within about 10-20 minutes, Town
    * about an hour, Kingdom a few hours (docs/SYSTEMS.md, Eras).
    */
    public class EraPacingProbe {
        private const float Step = 1f / 50f;
        // game time a scripted action costs: walking to a tree or rock and
        // working it, walking into a settlement, walking out to a new site
        private const int GatherTicks = 150;
        private const int TownActionTicks = 100;
        private const int FoundTicks = 600;

        private readonly ITestOutputHelper output;

        public EraPacingProbe(ITestOutputHelper output) {
            this.output = output;
        }

        [Fact]
        public void measureEraPacing_WhenAskedTo() {
            if (System.Environment.GetEnvironmentVariable("BN_ERA_PACING") != "1") {
                return;
            }
            string hoursText = System.Environment.GetEnvironmentVariable("BN_ERA_PACING_HOURS");
            double hours = string.IsNullOrEmpty(hoursText) ? 4 : double.Parse(hoursText, System.Globalization.CultureInfo.InvariantCulture);
            string seedsText = System.Environment.GetEnvironmentVariable("BN_ERA_PACING_SEEDS");
            List<int> seeds = new List<int>();
            foreach (string seed in (string.IsNullOrEmpty(seedsText) ? "42,7,2026" : seedsText).Split(',')) {
                seeds.Add(int.Parse(seed.Trim()));
            }
            foreach (int seed in seeds) {
                new Bot(seed, output).play((int) (hours * 3600 * 50));
            }
        }

        private class Bot {
            private readonly Simulation simulation;
            private readonly Player player;
            private readonly ITestOutputHelper output;
            private readonly int seed;
            private int lastReport = 0;
            private int turn = 0;
            private Pawn recruit;

            public Bot(int seed, ITestOutputHelper output) {
                this.seed = seed;
                this.output = output;
                GameConfig config = new GameConfig();
                config.setWorldSeed(seed);
                simulation = new Simulation(config);
                player = simulation.getPlayer();
            }

            private void tick(int count) {
                for (int i = 0; i < count; i++) {
                    simulation.fixedUpdate(Step);
                    player.setEnergy(System.Math.Max(player.getEnergy(), 60f));
                }
            }

            private string minutes(int ticks) {
                return ticks < 0 ? "-" : (ticks / 3000.0).ToString("0.0") + " min";
            }

            public void play(int totalTicks) {
                DateTime started = DateTime.UtcNow;
                EraTracker eras = simulation.getEraTracker();
                while (simulation.getTickCounter().getTotalTicks() < totalTicks && eras.getOutcome() == RunOutcome.NONE) {
                    act();
                    int now = simulation.getTickCounter().getTotalTicks();
                    if (now - lastReport >= 30000) {
                        lastReport = now;
                        report("progress");
                    }
                }
                report("end");
                output.WriteLine("seed " + seed + ": Village " + minutes(eras.getReachedTick(Era.VILLAGE))
                    + ", Town " + minutes(eras.getReachedTick(Era.TOWN)) + ", Kingdom " + minutes(eras.getReachedTick(Era.KINGDOM))
                    + ", outcome " + eras.getOutcome() + " at " + minutes(eras.getOutcomeTick())
                    + " (" + (DateTime.UtcNow - started).TotalSeconds.ToString("0") + " s real)");
            }

            private void report(string label) {
                NationStanding standing = NationStanding.of(simulation);
                EraTracker eras = simulation.getEraTracker();
                string requirements = "";
                foreach (EraRequirement requirement in eras.getRequirements(simulation)) {
                    requirements += " " + requirement.describe();
                }
                output.WriteLine("seed " + seed + " " + label + " " + minutes(simulation.getTickCounter().getTotalTicks()) + ": " + eras.getEra()
                    + (standing == null ? " no nation" : " members " + standing.members + " settlements " + standing.settlements
                        + " villages " + standing.villages + " towns " + standing.towns + " buildings " + standing.buildings
                        + " stalls " + standing.stalls + " funds " + standing.funds)
                    + " |" + requirements + " | objective: " + (simulation.getObjectiveTracker().getCurrent() == null ? "-" : simulation.getObjectiveTracker().getCurrent().getTitle())
                    + " | nations " + simulation.getNationRepository().getNumberOfNations() + " world settlements " + NationStanding.countAllSettlements(simulation));
            }

            // --- one decision a turn ---

            private void act() {
                if (player.getNationId() == null) {
                    exitSettlement();
                    new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                        simulation.getRandom(), simulation.getNationNameGenerator()).execute(player);
                    tick(50);
                    return;
                }
                Nation nation = simulation.getNationRepository().getNation(player.getNationId());
                if (nation.getLeaderId() != player.getId()) {
                    tick(50);
                    return;
                }
                bool wantsSettlement = nation.getNumberOfSettlements() == 0
                    || (nation.canFoundSettlement() && simulation.getEraTracker().getEra() >= Era.VILLAGE && nation.getNumberOfSettlements() < 8);
                if (wantsSettlement && wood() >= Settlement.WOOD_COST_TO_BUILD) {
                    found();
                    return;
                }

                turn++;
                if (turn % 2 == 0 && members(nation) < membersWanted()) {
                    recruitStep(nation);
                    return;
                }

                Settlement stalled = null;
                Settlement buildable = null;
                foreach (EntityId id in nation.getSettlements()) {
                    Settlement settlement = simulation.getEntityRepository().getEntity(id) as Settlement;
                    if (settlement == null) {
                        continue;
                    }
                    if (stalled == null && settlement.getMarket().getNumStalls() < 1 && Stall.canAffordToBuild(player.getInventory(), settlement.getInventory())) {
                        stalled = settlement;
                    }
                    if (buildable == null && BuildingPlanner.chooseNext(settlement, player.getInventory()) != null) {
                        buildable = settlement;
                    }
                }
                if (stalled != null) {
                    enter(stalled);
                    new BuildStallCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player);
                    tick(TownActionTicks);
                    return;
                }
                if (buildable != null) {
                    enter(buildable);
                    new BuildBuildingCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player, null);
                    tick(TownActionTicks);
                    return;
                }
                // surplus goes to market for funds once there is plenty in hand
                if (wood() + stone() > 160 && nation.getNumberOfSettlements() > 0) {
                    Settlement home = simulation.getEntityRepository().getEntity(nation.getSettlement(0)) as Settlement;
                    if (home != null) {
                        enter(home);
                        new SellResourcesAtMarketCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player);
                        tick(TownActionTicks);
                        return;
                    }
                }
                gather(wood() <= stone() * 1.5f || wantsSettlement ? EntityType.TREE : EntityType.ROCK);
            }

            private static int members(Nation nation) {
                return nation.getNumberOfMembers();
            }

            /** The members the era asks for, and a few more once it asks for none. */
            private int membersWanted() {
                foreach (EraRequirement requirement in simulation.getEraTracker().getRequirements(simulation)) {
                    if (requirement.label == "Members") {
                        return requirement.target;
                    }
                }
                return VictoryRules.ProsperityMembers;
            }

            /**
            * A player recruiting: picks the nearest pawn not in the nation
            * (nationless first), then talks, gifts and asks it to join on
            * each visit until it does.
            */
            private void recruitStep(Nation nation) {
                if (recruit == null || recruit.isMarkedForDeletion() || recruit.getNationId() == nation.getId()
                        || simulation.getEntityRepository().getEntity(recruit.getId()) == null || isLeader(recruit)) {
                    recruit = chooseRecruit(nation);
                }
                if (recruit == null) {
                    tick(GatherTicks);
                    return;
                }
                exitSettlement();
                if (recruit.isCurrentlyInSettlement()) {
                    // wait for it to come out
                    tick(GatherTicks);
                    return;
                }
                player.setPosition(recruit.getPosition() + new Vector3(2, 0, 0));
                new TalkToPawnCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getTickCounter()).execute(player, recruit);
                if (GiftPawnCommand.canGift(player)) {
                    new GiftPawnCommand(simulation.getEventProducer()).execute(player, recruit);
                }
                new InvitePawnToNationCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository()).execute(player, recruit);
                if (recruit.getNationId() == nation.getId()) {
                    recruit = null;
                }
                tick(GatherTicks);
            }

            private bool isLeader(Pawn pawn) {
                Nation nation = pawn.getNationId() == null ? null : simulation.getNationRepository().getNation(pawn.getNationId());
                return nation != null && nation.getLeaderId() == pawn.getId();
            }

            private Pawn chooseRecruit(Nation nation) {
                Pawn best = null;
                float bestScore = float.MaxValue;
                foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(EntityType.PAWN)) {
                    Pawn pawn = (Pawn) entity;
                    if (pawn.getNationId() == nation.getId() || isLeader(pawn) || pawn.isMarkedForDeletion()) {
                        continue;
                    }
                    float score = Vector3.Distance(player.getPosition(), pawn.getPosition()) + (pawn.getNationId() == null ? 0 : 400);
                    if (score < bestScore) {
                        bestScore = score;
                        best = pawn;
                    }
                }
                return best;
            }

            private int wood() { return player.getInventory().getNumItems(ItemType.WOOD); }
            private int stone() { return player.getInventory().getNumItems(ItemType.STONE); }

            private void exitSettlement() {
                if (player.isCurrentlyInSettlement()) {
                    new ExitSettlementCommand(simulation.getEntityRepository()).execute(player);
                }
            }

            private void enter(Settlement settlement) {
                if (player.getCurrentSettlementId() == settlement.getId()) {
                    return;
                }
                exitSettlement();
                player.setPosition(settlement.getPosition());
                new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            }

            private void interact() {
                new InteractCommand(simulation.getEnvironment(), simulation.getNationRepository(),
                    simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getRandom()).execute(player);
            }

            private void gather(EntityType source) {
                exitSettlement();
                Entity resource = nearestClear(source) ?? nearestClear(source == EntityType.TREE ? EntityType.ROCK : EntityType.TREE);
                if (resource == null) {
                    tick(GatherTicks);
                    return;
                }
                player.setPosition(resource.getPosition());
                interact();
                exitSettlement();
                tick(GatherTicks);
            }

            private void found() {
                exitSettlement();
                int minDistance = simulation.getGameConfig().getMinDistanceBetweenSettlements();
                Vector3 start = player.getPosition();
                for (int ring = 1; ring < 40; ring++) {
                    for (int k = 0; k < 8; k++) {
                        double angle = k * Math.PI / 4 + ring;
                        Vector3 site = new Vector3(start.X + (float) Math.Cos(angle) * minDistance * ring * 0.5f, 1f,
                            start.Z + (float) Math.Sin(angle) * minDistance * ring * 0.5f);
                        if (isClearSite(site, minDistance)) {
                            player.setPosition(site);
                            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                                simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom()).execute(player);
                            tick(FoundTicks);
                            return;
                        }
                    }
                }
                tick(FoundTicks);
            }

            private bool isClearSite(Vector3 site, int minDistance) {
                foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT)) {
                    if (Vector3.Distance(site, entity.getPosition()) < minDistance * 1.3f) {
                        return false;
                    }
                }
                return Territory.findClaimAt(simulation.getEntityRepository(), site) == null;
            }

            private Entity nearestClear(EntityType source) {
                Entity best = null;
                float bestDistance = float.MaxValue;
                foreach (Entity candidate in simulation.getEntityRepository().getEntitiesOfType(source)) {
                    if (candidate.isMarkedForDeletion() || somethingElseWithinReach(candidate.getPosition())) {
                        continue;
                    }
                    float distance = Vector3.Distance(player.getPosition(), candidate.getPosition());
                    if (distance < bestDistance) {
                        bestDistance = distance;
                        best = candidate;
                    }
                }
                return best;
            }

            private bool somethingElseWithinReach(Vector3 position) {
                foreach (EntityType blocker in new[] { EntityType.SETTLEMENT, EntityType.PAWN }) {
                    foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(blocker)) {
                        if (Vector3.Distance(position, entity.getPosition()) < 6) {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
