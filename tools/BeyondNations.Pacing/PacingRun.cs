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
        /** Pawns in no nation: the strangers left to recruit. */
        public int Nationless;
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
        /**
        * When set, the bot never talks to, gifts or invites a pawn: it plays
        * like someone who builds up a settlement and lets the world bring the
        * members (docs/PACING.md, "Recruiting").
        */
        public bool NoCourting = false;
        /**
        * Milestones (PacingMilestones) to watch for. A run goes on past its
        * objective limit, up to MaxTicks, until each has been reached.
        */
        public List<string> Milestones = new List<string>();
        /**
        * Ticks at which to save the bot's world, as an ordinary save file
        * (world-<seed>-pacing-t<tick>.bnsave) in SaveDirectory, to load in
        * the game with --continue: how the docs' screenshots of a bot's
        * run are taken.
        */
        public List<int> SaveAtTicks = new List<int>();
        public string SaveDirectory = null;
    }

    /** The result of driving one seed with the bot. */
    public sealed class PacingRunResult {
        public int Seed;
        public int TicksPerSecond;
        public int TicksPerDay;
        public int TicksRun;
        public List<ObjectiveTiming> Objectives = new List<ObjectiveTiming>();
        public List<PacingSample> Samples = new List<PacingSample>();
        /** Raids by another nation on the player's nation, and by the player's nation on another. */
        public int RaidsOnPlayer;
        public int RaidsByPlayer;
        /** Of RaidsOnPlayer, those that came while its era sheltered it (EraRules.isSheltered). */
        public int RaidsOnPlayerSheltered;
        /** The first tick the player's nation had the Village era's members (EraRules.VillageMembers), or -1. */
        public int VillageMembersTick = -1;
        /** The first tick the player's nation had a member besides the player (an heir), or -1. */
        public int FirstMemberTick = -1;
        /** Members the player's nation took by invitation (courting), and that joined it otherwise. */
        public int MembersInvited;
        public int MembersJoinedOtherwise;
        /** The tick each watched milestone was first reached; a milestone never reached is absent. */
        public Dictionary<string, int> Milestones = new Dictionary<string, int>();
        /** The tick each HUD unlock's own condition first held (PacingUnlocks), by UnlockId name; one never met is absent. */
        public Dictionary<string, int> Unlocks = new Dictionary<string, int>();
        /**
        * The calendar waits: for each era the player's nation was in, the
        * first tick it had every requirement to leave it but the day
        * (EraRules.requirementsToLeave), and how many ticks it spent so,
        * waiting on nothing but the calendar. Sampled once a second of game
        * time. An era whose other requirements were never all met is absent.
        */
        public Dictionary<string, int> CalendarReadyTick = new Dictionary<string, int>();
        public Dictionary<string, int> CalendarWaitTicks = new Dictionary<string, int>();
        /** The tick the player's nation reached each era after the camp (EraTracker), by era name; one never reached is absent. */
        public Dictionary<string, int> EraTicks = new Dictionary<string, int>();
        /** How the run ended (RunOutcome name, NONE while it goes on) and the tick it did, or -1. */
        public string Outcome = "NONE";
        public int OutcomeTick = -1;
        /** Building upgrades the bot bought (UpgradeBuildingCommand), and the coins they cost. */
        public int UpgradesBought;
        public int UpgradeCoins;

        /** Raids on the player's nation per hour of game time. */
        public double raidsOnPlayerPerHour() {
            return TicksRun <= 0 ? 0 : RaidsOnPlayer * 3600.0 * TicksPerSecond / TicksRun;
        }
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
            PacingBot bot = new PacingBot(simulation, options.NoCourting);
            ObjectiveTracker tracker = simulation.getObjectiveTracker();

            PacingRunResult result = new PacingRunResult {
                Seed = options.Seed,
                TicksPerSecond = options.TicksPerSecond,
                TicksPerDay = simulation.getTimeOfDay().getTicksPerDay(),
            };

            simulation.getEventRepository().subscribe(EventType.NationRaid, e => {
                NationRaidEvent raid = (NationRaidEvent) e;
                NationId own = simulation.getPlayer().getNationId();
                if (own != null && own.Equals(raid.getVictim().getId())) {
                    result.RaidsOnPlayer++;
                    if (raid.getVictim().isSheltered()) {
                        result.RaidsOnPlayerSheltered++;
                    }
                }
                else if (own != null && own.Equals(raid.getRaider().getId())) {
                    result.RaidsByPlayer++;
                }
            });

            simulation.getEventRepository().subscribe(EventType.NationJoin, e => {
                NationJoinEvent join = (NationJoinEvent) e;
                NationId own = simulation.getPlayer().getNationId();
                if (own == null || !own.Equals(join.getNation().getId()) || join.getEntityId().Equals(simulation.getPlayer().getId())) {
                    return;
                }
                if (bot.isInviting()) {
                    result.MembersInvited++;
                }
                else {
                    result.MembersJoinedOtherwise++;
                }
            });

            float dt = 1f / options.TicksPerSecond;
            int recorded = 0;
            int previousCompletionTick = 0;
            int tick = 0;
            string lastStatus = null;
            SuccessionNotice shownNotice = null;
            int noticeSince = 0;
            sample(simulation, bot, result);
            PacingUnlocks.observe(simulation, 0, result.Unlocks);
            while (tick < options.MaxTicks && (recorded < options.ObjectiveLimit || result.Milestones.Count < options.Milestones.Count)) {
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
                int recordedBefore = recorded;
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
                // the HUD's unlocks, read-only: every second, and as each objective comes up
                if (tick % options.TicksPerSecond == 0 || recorded != recordedBefore) {
                    PacingUnlocks.observe(simulation, tick, result.Unlocks);
                }
                if (result.FirstMemberTick < 0 && WorldView.members(simulation) >= 2) {
                    result.FirstMemberTick = tick;
                }
                if (result.VillageMembersTick < 0 && WorldView.members(simulation) >= EraRules.VillageMembers) {
                    result.VillageMembersTick = tick;
                }
                // the succession screen: a player reads it for five seconds and presses Continue,
                // so a world saved later does not open on an old succession
                SuccessionNotice notice = simulation.getRealm().getNotice();
                if (notice != shownNotice) {
                    shownNotice = notice;
                    noticeSince = tick;
                }
                else if (notice != null && !notice.isLineEnded() && tick - noticeSince == 5 * options.TicksPerSecond) {
                    simulation.getRealm().consumeNotice();
                }
                if (options.SaveAtTicks.Contains(tick) && options.SaveDirectory != null) {
                    System.IO.Directory.CreateDirectory(options.SaveDirectory);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(options.SaveDirectory,
                        "world-" + options.Seed + "-pacing-t" + tick + SaveSlots.Extension), SaveGame.encode(simulation, "pacing"));
                }
                if (tick % options.TicksPerSecond == 0) {
                    recordCalendarWait(simulation, result, options.TicksPerSecond);
                    foreach (string milestone in options.Milestones) {
                        if (!result.Milestones.ContainsKey(milestone) && PacingMilestones.isReached(simulation, milestone)) {
                            result.Milestones[milestone] = tick;
                        }
                    }
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
            EraTracker eras = simulation.getEraTracker();
            foreach (Era era in System.Enum.GetValues<Era>()) {
                if (era != Era.CAMP && era <= eras.getEra()) {
                    result.EraTicks[Eras.getDisplayName(era)] = eras.getReachedTick(era);
                }
            }
            result.Outcome = eras.getOutcome().ToString();
            result.OutcomeTick = eras.hasEnded() ? eras.getOutcomeTick() : -1;
            result.UpgradesBought = bot.getUpgradesBought();
            result.UpgradeCoins = bot.getUpgradeCoins();
            return result;
        }

        /**
        * Whether the player's nation has everything it needs to leave its
        * era except the day: the stretch in which a player has nothing that
        * can bring the next era sooner (docs/PACING.md, "Calendar waits").
        */
        public static bool waitingOnlyForTheDay(List<EraRequirement> requirements) {
            bool dayUnmet = false;
            foreach (EraRequirement requirement in requirements) {
                if (requirement.isMet()) {
                    continue;
                }
                if (requirement.label != "Day") {
                    return false;
                }
                dayUnmet = true;
            }
            return dayUnmet;
        }

        private static void recordCalendarWait(Simulation simulation, PacingRunResult result, int ticks) {
            if (WorldView.playerNation(simulation) == null) {
                return;
            }
            EraTracker tracker = simulation.getEraTracker();
            if (!waitingOnlyForTheDay(tracker.getRequirements(simulation))) {
                return;
            }
            string era = tracker.getEra().ToString();
            int tick = simulation.getTickCounter().getTotalTicks();
            if (!result.CalendarReadyTick.ContainsKey(era)) {
                result.CalendarReadyTick[era] = tick;
                result.CalendarWaitTicks[era] = 0;
            }
            result.CalendarWaitTicks[era] += ticks;
        }

        private static void sample(Simulation simulation, PacingBot bot, PacingRunResult result) {
            Inventory inventory = simulation.getPlayer().getInventory();
            result.Samples.Add(new PacingSample {
                Tick = simulation.getTickCounter().getTotalTicks(),
                Nations = simulation.getNationRepository().getNumberOfNations(),
                Pawns = simulation.getEntityRepository().getEntitiesOfType(EntityType.PAWN).Count,
                Nationless = WorldView.nationlessPawns(simulation),
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
