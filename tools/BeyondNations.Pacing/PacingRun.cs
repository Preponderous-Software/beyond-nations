using System;
using System.Collections.Generic;

namespace beyondnations.pacing {

    /** How long one objective took on one seed. */
    public sealed class ObjectiveTiming {
        public int Index;
        public string Id;
        public string Title;
        public bool Ambition;
        /** Ticks spent on this objective: from the previous completion (or the start) to this one. */
        public int DurationTicks;
        /** Total ticks when it completed, or -1 when the run ended first. */
        public int CompletedAtTick;
        public bool Completed;
        /** What the bot was doing when the run ended, for an objective it never finished. */
        public string StuckActivity;
        public string StuckProgress;
    }

    /** One point on the run's curves. */
    public sealed class PacingSample {
        public int Tick;
        public int Nations;
        public int Pawns;
        public int PlayerNationMembers;
        public int PlayerSettlements;
        public int Coins;
        public int Funds;
        public int Wood;
        public int Stone;
        public int ObjectivesCompleted;
        /** What the bot was doing. */
        public string Activity;
    }

    public sealed class PacingRunOptions {
        public int Seed = 42;
        public int TicksPerSecond = TickCounter.DefaultTicksPerSecond;
        /** Stop once this many objectives are done (the arc plus a lap of ambitions by default). */
        public int ObjectiveLimit = 13;
        /** Stop here regardless: two in-game days at the default rate. */
        public int MaxTicks = 2 * TimeOfDay.DefaultTicksPerDay;
        /** A curve sample every this many ticks. */
        public int SampleEvery = 1500;
        /** When set, every new status line the player sees is written here with its time. */
        public System.IO.TextWriter Trace = null;
    }

    /** The result of driving one seed with the bot. */
    public sealed class PacingRunResult {
        public int Seed;
        public int TicksPerSecond;
        public int TicksPerDay;
        public int TicksRun;
        public List<ObjectiveTiming> Objectives = new List<ObjectiveTiming>();
        public List<PacingSample> Samples = new List<PacingSample>();
    }

    /**
    * Drives one seeded Simulation with a PacingBot and records, from the
    * ObjectiveTracker, when each objective completes. The objectives are read
    * from whatever the tracker exposes (getObjectives / getCompletedCount),
    * so a reworked objective list is measured without changes here.
    */
    public static class PacingRun {

        public static PacingRunResult run(PacingRunOptions options) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(options.Seed);
            config.setTicksPerSecond(options.TicksPerSecond);
            Simulation simulation = new Simulation(config);
            PacingBot bot = new PacingBot(simulation);
            ObjectiveTracker tracker = simulation.getObjectiveTracker();

            PacingRunResult result = new PacingRunResult {
                Seed = options.Seed,
                TicksPerSecond = options.TicksPerSecond,
                TicksPerDay = simulation.getTimeOfDay().getTicksPerDay(),
            };

            float dt = 1f / options.TicksPerSecond;
            int recorded = 0;
            int previousCompletionTick = 0;
            int tick = 0;
            string lastStatus = null;
            sample(simulation, bot, result);
            while (tick < options.MaxTicks && recorded < options.ObjectiveLimit) {
                bot.beforeTick();
                simulation.fixedUpdate(dt);
                tick = simulation.getTickCounter().getTotalTicks();
                if (options.Trace != null) {
                    string status = simulation.getPlayer().getStatus().getStatus();
                    if (!string.IsNullOrEmpty(status) && status != lastStatus) {
                        options.Trace.WriteLine("seed " + options.Seed + " " + PacingReport.clock((double) tick / options.TicksPerSecond)
                            + " [" + bot.getActivity() + "] " + status.Replace("\n", " / "));
                    }
                    lastStatus = status;
                }

                // at most one objective completes per evaluation, but loop in case
                while (recorded < tracker.getCompletedCount() && recorded < options.ObjectiveLimit) {
                    Objective done = tracker.getObjectives()[recorded];
                    result.Objectives.Add(new ObjectiveTiming {
                        Index = recorded,
                        Id = done.getId(),
                        Title = done.getTitle(),
                        Ambition = done.isAmbition(),
                        DurationTicks = tick - previousCompletionTick,
                        CompletedAtTick = tick,
                        Completed = true,
                    });
                    previousCompletionTick = tick;
                    recorded++;
                }
                if (tick % options.SampleEvery == 0) {
                    sample(simulation, bot, result);
                }
            }
            if (tick % options.SampleEvery != 0) {
                sample(simulation, bot, result);
            }

            Objective unfinished = tracker.getCurrent();
            if (recorded < options.ObjectiveLimit && unfinished != null) {
                result.Objectives.Add(new ObjectiveTiming {
                    Index = recorded,
                    Id = unfinished.getId(),
                    Title = unfinished.getTitle(),
                    Ambition = unfinished.isAmbition(),
                    DurationTicks = tick - previousCompletionTick,
                    CompletedAtTick = -1,
                    Completed = false,
                    StuckActivity = bot.getActivity(),
                    StuckProgress = tracker.getCurrentProgress().describe(),
                });
            }
            result.TicksRun = tick;
            return result;
        }

        private static void sample(Simulation simulation, PacingBot bot, PacingRunResult result) {
            Inventory inventory = simulation.getPlayer().getInventory();
            result.Samples.Add(new PacingSample {
                Tick = simulation.getTickCounter().getTotalTicks(),
                Nations = simulation.getNationRepository().getNumberOfNations(),
                Pawns = simulation.getEntityRepository().getEntitiesOfType(EntityType.PAWN).Count,
                PlayerNationMembers = WorldView.members(simulation),
                PlayerSettlements = WorldView.settlements(simulation),
                Coins = inventory.getNumItems(ItemType.COIN),
                Funds = WorldView.funds(simulation),
                Wood = inventory.getNumItems(ItemType.WOOD),
                Stone = inventory.getNumItems(ItemType.STONE),
                ObjectivesCompleted = simulation.getObjectiveTracker().getCompletedCount(),
                Activity = bot.getActivity(),
            });
        }
    }
}
