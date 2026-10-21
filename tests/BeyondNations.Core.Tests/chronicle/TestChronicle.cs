using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The Chronicle turns published events into dated, human-readable lines,
    * skips the noise, varies its phrasing deterministically and remembers who
    * each line is about.
    */
    public class TestChronicle {
        private const float FixedDeltaTime = 1f / 60f;

        private static Simulation makeSimulation(int seed = 20261018) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            config.setRespawnPawns(false);
            return new Simulation(config);
        }

        private static Pawn addPawn(Simulation simulation, string name) {
            Pawn pawn = new Pawn(new Vector3(0, 1.5f, 0), name, simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            return pawn;
        }

        private static Nation pawnFoundsNation(Simulation simulation, Pawn leader, string name) {
            Nation nation = new Nation(name, leader.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            leader.setNationId(nation.getId());
            simulation.getEventProducer().produceNationCreationEvent(nation);
            return nation;
        }

        private static ChronicleEntry last(Simulation simulation) {
            return simulation.getChronicle().getEntries().Last();
        }

        [Fact]
        public void testSimulation_WiresAChronicleToItsEvents() {
            Simulation simulation = makeSimulation();
            Assert.NotNull(simulation.getChronicle());
            Assert.Equal(1, simulation.getEventRepository().getNumberOfListeners());
        }

        [Fact]
        public void testNoise_IsNotChronicled() {
            Simulation simulation = makeSimulation();
            Chronicle chronicle = simulation.getChronicle();
            Pawn pawn = addPawn(simulation, "Osric");

            Assert.Null(chronicle.record(new ChunkGenerateEvent(0, 0)));
            Assert.Null(chronicle.record(new PawnSpawnEvent(Vector3.Zero, pawn)));
            pawn.increaseRelationship(simulation.getPlayer(), 1);
            Assert.Null(chronicle.record(new PawnRelationshipIncreaseEvent(pawn, simulation.getPlayer(), 1)));
            Assert.Null(chronicle.record(null));

            // The first tick generates the starting chunks and their pawns.
            simulation.fixedUpdate(FixedDeltaTime);
            Assert.True(simulation.getEventRepository().getTotalNumberOfEvents() > 0);
            Assert.Equal(0, chronicle.getCount());
        }

        [Fact]
        public void testRelationshipMilestones_FriendAndLoyalOnly() {
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            Pawn pawn = addPawn(simulation, "Edda");
            EventProducer producer = simulation.getEventProducer();

            PawnRelations.raise(pawn, player, RelationshipTiers.ACQUAINTANCE_THRESHOLD, producer);
            Assert.Equal(0, simulation.getChronicle().getCount());

            PawnRelations.raise(pawn, player, RelationshipTiers.FRIEND_THRESHOLD - RelationshipTiers.ACQUAINTANCE_THRESHOLD, producer);
            ChronicleEntry friend = last(simulation);
            Assert.Contains("Edda", friend.getText());
            Assert.Contains("friend", friend.getText());
            Assert.True(friend.concerns(player));

            PawnRelations.raise(pawn, player, 1, producer);
            Assert.Equal(1, simulation.getChronicle().getCount());

            PawnRelations.raise(pawn, player, RelationshipTiers.LOYAL_THRESHOLD, producer);
            Assert.Equal(2, simulation.getChronicle().getCount());
            Assert.Contains("Edda", last(simulation).getText());
        }

        [Fact]
        public void testPawnFoundsNation_NamedAndStamped() {
            Simulation simulation = makeSimulation();
            Pawn mira = addPawn(simulation, "Mira");
            Nation nation = pawnFoundsNation(simulation, mira, "Ironvale");

            ChronicleEntry entry = last(simulation);
            Assert.Equal(EventType.NationCreation, entry.getEventType());
            Assert.Contains("Mira", entry.getText());
            Assert.Contains("Ironvale", entry.getText());
            Assert.EndsWith("It is the first nation this world has known.", entry.getText());
            // A new world opens mid-morning on its first day.
            Assert.Equal(1, entry.getDay());
            Assert.Equal("morning", entry.getPartOfDay());
            Assert.StartsWith("Day 1, morning - ", entry.getLine());
            Assert.True(entry.involvesNation(nation.getId()));
            Assert.True(entry.involvesEntity(mira.getId()));
        }

        [Fact]
        public void testOnlyTheFirstNation_IsCalledTheFirst() {
            Simulation simulation = makeSimulation();
            pawnFoundsNation(simulation, addPawn(simulation, "Mira"), "Ironvale");
            pawnFoundsNation(simulation, addPawn(simulation, "Osric"), "Ashford");

            Assert.DoesNotContain("first nation", last(simulation).getText());
        }

        [Fact]
        public void testPlayerFoundsNation_SecondPerson_ConcernsPlayer() {
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();

            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);

            ChronicleEntry entry = last(simulation);
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            Assert.StartsWith("You", entry.getText().Replace("Under your hand", "You"));
            Assert.Contains(nation.getName(), entry.getText());
            Assert.DoesNotContain("Player", entry.getText());
            Assert.True(entry.concerns(player));
        }

        [Fact]
        public void testHighlight_FollowsThePlayersCurrentNation() {
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            Nation nation = pawnFoundsNation(simulation, addPawn(simulation, "Mira"), "Ironvale");
            ChronicleEntry founding = last(simulation);
            Assert.False(founding.concerns(player));

            new NationJoinCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom())
                .execute(player);

            Assert.Equal(nation.getId(), player.getNationId());
            Assert.True(founding.concerns(player));
            Assert.True(last(simulation).concerns(player));
            Assert.Equal(EventType.NationJoin, last(simulation).getEventType());
        }

        [Fact]
        public void testLeaderStarves_DeathThenSuccession() {
            Simulation simulation = makeSimulation();
            Pawn leader = addPawn(simulation, "Osric");
            Pawn survivor = addPawn(simulation, "Mira");
            Nation nation = pawnFoundsNation(simulation, leader, "Ironvale");
            nation.addMember(survivor.getId());
            survivor.setNationId(nation.getId());
            leader.setEnergy(0);

            simulation.fixedUpdate(FixedDeltaTime);

            List<ChronicleEntry> entries = simulation.getChronicle().getEntries().ToList();
            ChronicleEntry death = entries.Single(e => e.getEventType() == EventType.PawnDeath);
            ChronicleEntry succession = entries.Single(e => e.getEventType() == EventType.LeadershipChange);
            Assert.Contains("Osric", death.getText());
            Assert.Contains("Ironvale", death.getText());
            Assert.Contains("Mira", succession.getText());
            Assert.Contains("Ironvale", succession.getText());
            Assert.True(death.getSequence() < succession.getSequence());
            Assert.True(succession.involvesEntity(survivor.getId()));
        }

        [Fact]
        public void testLastMemberStarves_NationFalls() {
            Simulation simulation = makeSimulation();
            Pawn leader = addPawn(simulation, "Osric");
            Nation nation = pawnFoundsNation(simulation, leader, "Ironvale");
            leader.setEnergy(0);

            simulation.fixedUpdate(FixedDeltaTime);

            ChronicleEntry fall = simulation.getChronicle().getEntries().Single(e => e.getEventType() == EventType.NationDisband);
            Assert.Contains("Ironvale", fall.getText());
            Assert.True(fall.involvesNation(nation.getId()));
        }

        [Fact]
        public void testPlayerFoundsSettlementAndBuildsStall() {
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD + Stall.WOOD_COST_TO_BUILD);
            player.getInventory().addItem(ItemType.STONE, Stall.STONE_COST_TO_BUILD);

            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom())
                .execute(player);
            ChronicleEntry founded = last(simulation);
            Assert.Equal(EventType.SettlementFounded, founded.getEventType());
            Assert.Contains("first", founded.getText());
            Assert.Contains(nation.getName(), founded.getText());
            Assert.True(founded.concerns(player));

            player.setCurrentSettlementId(player.getHomeSettlementId());
            new BuildStallCommand(simulation.getNationRepository(), simulation.getEntityRepository(), simulation.getEventProducer())
                .execute(player);
            ChronicleEntry stall = last(simulation);
            Assert.Equal(EventType.StallBuilt, stall.getEventType());
            Assert.Contains(nation.getName(), stall.getText());
            Assert.Contains("stall", stall.getText());
        }

        [Fact]
        public void testDiplomacy_RelationsRaidsAndCapturesAreChronicled() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation ironvale = world.pawnNation("Ironvale", 3);
            Nation ashford = world.pawnNation("Ashford", 3);
            Settlement town = world.settlement(ashford, 40, 40);
            EventProducer producer = world.simulation.getEventProducer();
            Chronicle chronicle = world.simulation.getChronicle();

            producer.produceNationRelationChangeEvent(ironvale, ashford, NationRelationState.NEUTRAL, NationRelationState.ALLIED);
            ChronicleEntry alliance = chronicle.getEntries().Last();
            Assert.Equal(EventType.NationRelationChange, alliance.getEventType());
            Assert.Contains("alliance", alliance.getText());
            Assert.True(alliance.involvesNation(ironvale.getId()));
            Assert.True(alliance.involvesNation(ashford.getId()));

            producer.produceNationRelationChangeEvent(ironvale, ashford, NationRelationState.RIVAL, NationRelationState.HOSTILE);
            Assert.Equal("Ironvale and Ashford are now hostile.", chronicle.getEntries().Last().getText());

            // Drift between neutral and friendly is quiet unless the player's nation is involved.
            int told = chronicle.getTotalRecorded();
            producer.produceNationRelationChangeEvent(ironvale, ashford, NationRelationState.NEUTRAL, NationRelationState.FRIENDLY);
            producer.produceNationRelationChangeEvent(ironvale, ashford, NationRelationState.FRIENDLY, NationRelationState.NEUTRAL);
            Assert.Equal(told, chronicle.getTotalRecorded());
            Nation mine = world.playerNation(1);
            producer.produceNationRelationChangeEvent(mine, ashford, NationRelationState.NEUTRAL, NationRelationState.FRIENDLY);
            Assert.Equal("Relations between " + mine.getName() + " and Ashford are now friendly.", chronicle.getEntries().Last().getText());

            producer.produceNationRaidEvent(ironvale, ashford, town, "12 coins");
            ChronicleEntry raid = chronicle.getEntries().Last();
            Assert.Equal(EventType.NationRaid, raid.getEventType());
            Assert.Contains("12 coins", raid.getText());
            Assert.Contains("Ironvale", raid.getText());
            Assert.Contains("Ashford", raid.getText());
            Assert.True(raid.involvesNation(ashford.getId()));

            producer.produceSettlementCaptureEvent(town, ashford, ironvale);
            ChronicleEntry capture = chronicle.getEntries().Last();
            Assert.Equal(EventType.SettlementCapture, capture.getEventType());
            Assert.Contains("Ironvale", capture.getText());
            Assert.Contains("Ashford", capture.getText());
            Assert.Contains(SettlementTiers.getDisplayName(town.getTier()).ToLowerInvariant(), capture.getText());
            Assert.True(capture.involvesNation(ironvale.getId()));
        }

        [Fact]
        public void testPopulatedWorld_JoinsAndBirthsAreToldOnlyForThePlayersNation() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation theirs = world.pawnNation("Ashford", 2);
            Nation mine = world.playerNation(1);
            EventProducer producer = world.simulation.getEventProducer();
            Chronicle chronicle = world.simulation.getChronicle();
            int before = chronicle.getTotalRecorded();

            // An immigrant joining someone else's nation, and a child born there: quiet.
            Pawn immigrant = world.addMember(theirs);
            producer.produceNationJoinEvent(theirs, immigrant.getId());
            Pawn theirChild = world.addMember(theirs);
            producer.producePawnSpawnEvent(Vector3.Zero, theirChild);
            // A nationless wanderer turning up: quiet.
            producer.producePawnSpawnEvent(Vector3.Zero, world.pawn());
            Assert.Equal(before, chronicle.getTotalRecorded());

            Pawn recruit = world.addMember(mine);
            producer.produceNationJoinEvent(mine, recruit.getId());
            ChronicleEntry join = chronicle.getEntries().Last();
            Assert.Equal(EventType.NationJoin, join.getEventType());
            Assert.Contains(recruit.getName(), join.getText());

            Pawn child = world.addMember(mine);
            producer.producePawnSpawnEvent(Vector3.Zero, child);
            ChronicleEntry birth = chronicle.getEntries().Last();
            Assert.Equal(EventType.PawnSpawn, birth.getEventType());
            Assert.Contains(child.getName(), birth.getText());
            Assert.True(birth.concerns(world.player));
        }

        [Fact]
        public void testRaidsBetweenOtherNations_OneLineADayPerPair_PlayersAlwaysTold() {
            DiplomacyFixture world = new DiplomacyFixture();
            Nation ironvale = world.pawnNation("Ironvale", 2);
            Nation ashford = world.pawnNation("Ashford", 2);
            Nation mine = world.playerNation(1);
            Settlement town = world.settlement(ashford, 40, 40);
            Settlement home = world.settlement(mine, -40, -40);
            EventProducer producer = world.simulation.getEventProducer();
            Chronicle chronicle = world.simulation.getChronicle();

            producer.produceNationRaidEvent(ironvale, ashford, town, "5 wood");
            producer.produceNationRaidEvent(ironvale, ashford, town, "5 wood");
            producer.produceNationRaidEvent(ironvale, ashford, town, "5 wood");
            Assert.Equal(1, chronicle.getEntries().Count(e => e.getEventType() == EventType.NationRaid));
            // The other way round is a different pair.
            producer.produceNationRaidEvent(ashford, ironvale, town, "2 coins");
            Assert.Equal(2, chronicle.getEntries().Count(e => e.getEventType() == EventType.NationRaid));

            producer.produceNationRaidEvent(ironvale, mine, home, "3 stone");
            producer.produceNationRaidEvent(ironvale, mine, home, "3 stone");
            Assert.Equal(4, chronicle.getEntries().Count(e => e.getEventType() == EventType.NationRaid));
            Assert.True(chronicle.getEntries().Last().concerns(world.player));
        }

        [Fact]
        public void testPlayerLeavesAsLeader_SuccessionIsChronicled() {
            Simulation simulation = makeSimulation();
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            Pawn heir = addPawn(simulation, "Edda");
            nation.addMember(heir.getId());
            heir.setNationId(nation.getId());

            new NationLeaveCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository())
                .execute(player);

            ChronicleEntry succession = simulation.getChronicle().getEntries().Single(e => e.getEventType() == EventType.LeadershipChange);
            Assert.Contains("Edda", succession.getText());
            Assert.StartsWith("You", succession.getText());
            Assert.Equal(EventType.NationLeave, last(simulation).getEventType());
        }

        [Fact]
        public void testPhrasing_IsDeterministicPerSeed_AndVaries() {
            List<string> first = tellJoins(42);
            List<string> again = tellJoins(42);
            List<string> otherSeed = tellJoins(43);

            Assert.Equal(first, again);
            Assert.NotEqual(first, otherSeed);
            // Several templates are in use, not one sentence repeated.
            Assert.True(first.Select(templateOf).Distinct().Count() > 1);
        }

        private static List<string> tellJoins(int seed) {
            Simulation simulation = makeSimulation(seed);
            Nation nation = pawnFoundsNation(simulation, addPawn(simulation, "Mira"), "Ironvale");
            // Pawn joins are told for the player's nation only.
            nation.addMember(simulation.getPlayer().getId());
            simulation.getPlayer().setNationId(nation.getId());
            for (int i = 0; i < 12; i++) {
                Pawn pawn = addPawn(simulation, "Pawn" + i);
                nation.addMember(pawn.getId());
                simulation.getEventProducer().produceNationJoinEvent(nation, pawn.getId());
            }
            return simulation.getChronicle().getEntries().Select(e => e.getText()).ToList();
        }

        private static string templateOf(string text) {
            return System.Text.RegularExpressions.Regex.Replace(text, "Pawn[0-9]+", "P");
        }

        [Fact]
        public void testCapacity_KeepsTheNewest() {
            RandomSource random = new RandomSource(1);
            EntityRepository entities = new EntityRepository(random);
            NationRepository nations = new NationRepository(random);
            EventRepository events = new EventRepository();
            Chronicle chronicle = new Chronicle(events, entities, nations, null, null, 1, capacity: 3);

            for (int i = 0; i < 5; i++) {
                events.addEvent(new NationDisbandEvent(new Nation("Nation" + i, new EntityId(), random)));
            }

            Assert.Equal(3, chronicle.getCount());
            Assert.Equal(5, chronicle.getTotalRecorded());
            Assert.Contains("Nation2", chronicle.getEntries()[0].getText());
            Assert.Contains("Nation4", chronicle.getEntries()[2].getText());
            Assert.Equal(2, chronicle.getRecent(2).Count);
            Assert.Contains("Nation4", chronicle.getRecent(2)[1].getText());
            Assert.Equal(3, chronicle.getRecent(10).Count);
        }

        [Fact]
        public void testTimestamp_FollowsTheInGameClock() {
            RandomSource random = new RandomSource(1);
            TickCounter ticks = new TickCounter();
            TimeOfDay time = new TimeOfDay(ticks, 100, 0f);
            EventRepository events = new EventRepository();
            Chronicle chronicle = new Chronicle(events, new EntityRepository(random), new NationRepository(random), time, ticks, 1);

            for (int i = 0; i < 175; i++) {
                ticks.increment();
            }
            events.addEvent(new NationDisbandEvent(new Nation("Ironvale", new EntityId(), random)));

            ChronicleEntry entry = chronicle.getEntries()[0];
            Assert.Equal(2, entry.getDay());
            Assert.Equal("dusk", entry.getPartOfDay());
            Assert.Equal(175, entry.getTick());
            Assert.StartsWith("Day 2, dusk - ", entry.getLine());
        }

        [Theory]
        [InlineData(0.0f, "night")]
        [InlineData(0.25f, "dawn")]
        [InlineData(0.35f, "morning")]
        [InlineData(0.5f, "midday")]
        [InlineData(0.6f, "afternoon")]
        [InlineData(0.75f, "dusk")]
        [InlineData(0.9f, "night")]
        public void testPartOfDay(float fraction, string expected) {
            Assert.Equal(expected, Chronicle.partOfDay(fraction));
        }

        [Theory]
        [InlineData(2, "second")]
        [InlineData(11, "11th")]
        [InlineData(21, "21st")]
        [InlineData(22, "22nd")]
        [InlineData(113, "113th")]
        public void testOrdinal(int n, string expected) {
            Assert.Equal(expected, Chronicle.ordinal(n));
        }

        [Fact]
        public void testChronicle_DoesNotDrawFromTheWorldRandom() {
            Simulation simulation = makeSimulation(5);
            Simulation twin = makeSimulation(5);
            Nation nation = pawnFoundsNation(simulation, addPawn(simulation, "Mira"), "Ironvale");
            pawnFoundsNation(twin, addPawn(twin, "Mira"), "Ironvale");

            // Many more entries in one world than the other: the next roll still matches.
            for (int i = 0; i < 20; i++) {
                simulation.getChronicle().record(new NationDisbandEvent(nation));
            }
            Assert.Equal(twin.getRandom().range(0, 1000000), simulation.getRandom().range(0, 1000000));
        }
    }
}
