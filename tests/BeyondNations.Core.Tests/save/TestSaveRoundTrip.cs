using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

using beyondnations;

namespace beyondnationstests {

    /**
    * The key gate for saves (docs/SAVES.md): a world saved and loaded into a
    * fresh Simulation is the same world. Each test runs a seed for N ticks,
    * saves, loads, and then runs the original and the copy side by side for
    * M more ticks. They must be identical at the moment of loading and still
    * identical after: every field compared by WorldComparer, and the
    * determinism fingerprint besides. A field the save forgot, a random
    * stream restored one draw out, a list loaded in the wrong order -- each
    * of these makes the two worlds drift apart within a few ticks.
    */
    public class TestSaveRoundTrip {
        private const float Step = 1f / 50f;
        private const string Version = "test";
        private readonly ITestOutputHelper output;

        public TestSaveRoundTrip(ITestOutputHelper output) {
            this.output = output;
        }

        private static Simulation newWorld(int seed) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            return new Simulation(config);
        }

        private static void run(Simulation simulation, int ticks) {
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(Step);
            }
        }

        private static void assertSame(Simulation original, Simulation copy, string when) {
            assertSame(original, copy, when, null);
        }

        /** savedIds null: exact, every id equal. Otherwise ids made since the save need only correspond. */
        private static void assertSame(Simulation original, Simulation copy, string when, HashSet<Guid> savedIds) {
            string difference = savedIds == null ? WorldComparer.difference(original, copy) : WorldComparer.difference(original, copy, savedIds);
            Assert.True(difference == null, when + ": " + difference);
            Assert.Equal(WorldComparer.fingerprint(original), WorldComparer.fingerprint(copy));
        }

        /** Saves, loads, checks they match, runs both on, checks again. Returns the copy. */
        private Simulation roundTrip(Simulation original, int moreTicks, string label) {
            byte[] file = SaveGame.encode(original, Version);
            Simulation copy = SaveGame.decode(file);
            assertSame(original, copy, label + " straight after loading");
            HashSet<Guid> savedIds = WorldComparer.idsOf(original);

            // saving the loaded world gives the same bytes, header aside
            byte[] again = SaveGame.encode(copy, Version);
            Assert.Equal(body(file), body(again));

            run(original, moreTicks);
            run(copy, moreTicks);
            assertSame(original, copy, label + " after " + moreTicks + " more ticks", savedIds);
            output.WriteLine(label + ": " + file.Length + " bytes, " + WorldComparer.countObjects(original, copy, savedIds) + " objects compared, "
                + original.getEntityRepository().getNumEntities() + " entities, " + original.getNationRepository().getNumberOfNations()
                + " nations, fingerprint " + WorldComparer.hash(original) + " at tick " + original.getTickCounter().getTotalTicks());
            return copy;
        }

        private static string body(byte[] file) {
            int start = Array.IndexOf(file, (byte) '\n') + 1;
            return System.Text.Encoding.UTF8.GetString(file, start, file.Length - start);
        }

        [Fact]
        public void testSeed42_SavedAtTick0_LoadsIdentical() {
            roundTrip(newWorld(42), 600, "seed 42 at tick 0");
        }

        [Fact]
        public void testSeed42_SavedAfterItsFirstSettlement_RunsOnIdentically() {
            // seed 42 founds its first nation at tick 2000 and settlement at 2050
            Simulation original = newWorld(42);
            run(original, 2400);
            Assert.NotEmpty(original.getNationRepository().getNations());
            roundTrip(original, 3000, "seed 42 at tick 2400");
        }

        [Theory]
        [InlineData(1, 3000)]
        [InlineData(7, 4500)]
        [InlineData(2026, 6000)]
        public void testOtherSeeds_RunOnIdentically(int seed, int ticks) {
            Simulation original = newWorld(seed);
            run(original, ticks);
            roundTrip(original, 2500, "seed " + seed + " at tick " + ticks);
        }

        [Fact]
        public void testALongerWorld_WithGrowthAndBirths_RunsOnIdentically() {
            Simulation original = newWorld(42);
            run(original, 15000);
            roundTrip(original, 3000, "seed 42 at tick 15000");
        }

        [Fact]
        public void testSavedTwice_InARow_StaysIdentical() {
            Simulation original = newWorld(9);
            run(original, 2500);
            Simulation copy = roundTrip(original, 1000, "seed 9 first load");
            Simulation copyOfCopy = SaveGame.decode(SaveGame.encode(copy, Version));
            assertSame(copy, copyOfCopy, "a save of a loaded save");
            HashSet<Guid> savedIds = WorldComparer.idsOf(copy);
            run(copy, 1000);
            run(copyOfCopy, 1000);
            assertSame(copy, copyOfCopy, "a save of a loaded save, run on", savedIds);
        }

        [Fact]
        public void testMidSiege_TheCaptureStillHappens_OnTheSameTick() {
            Simulation original = newWorld(42);
            DiplomacyShowcase.Staged staged = DiplomacyShowcase.stage(original);
            run(original, Diplomacy.ROUND_TICKS);
            Assert.Equal(3, original.getDiplomacy().getSiegeProgress(staged.playerSettlement));
            Assert.NotNull(original.getDiplomacy().getBesieger(staged.playerSettlement));

            Simulation copy = roundTrip(original, Diplomacy.ROUND_TICKS * 3 + 5, "mid-siege");

            // the siege ran to its end in both: the village fell, the nation with it
            Assert.Equal(staged.hostile.getId(), staged.playerSettlement.getNationId());
            Assert.Null(copy.getPlayer().getNationId());
            Assert.Equal(1, copy.getEventRepository().getNumberOfEvents(EventType.SettlementCapture));
        }

        [Fact]
        public void testMidOrders_WithWornTools_RunOnIdentically() {
            Simulation original = newWorld(42);
            DiplomacyShowcase.Staged staged = DiplomacyShowcase.stage(original);
            Player player = original.getPlayer();

            // the player crafts tools and has worn one
            player.getInventory().addItem(ItemType.WOOD, 40);
            player.getInventory().addItem(ItemType.STONE, 20);
            CraftItemCommand craft = new CraftItemCommand(original.getEntityRepository());
            Assert.True(craft.execute(player, RecipeBook.Axe));
            Assert.True(craft.execute(player, RecipeBook.Pickaxe));
            player.getInventory().setToolWear(ItemType.AXE, 3);

            // every pawn of the player's nation is under orders, half way through some; some carry worn tools
            int ordered = 0;
            foreach (Entity entity in original.getEntityRepository().getEntitiesOfType(EntityType.PAWN)) {
                Pawn pawn = (Pawn) entity;
                if (pawn.getNationId() == null) {
                    continue;
                }
                NationOrder order = new NationOrder(pawn.getNationId(), ordered % 2 == 0 ? ItemType.WOOD : ItemType.STONE, 10);
                order.recordDelivery(ordered % 4);
                pawn.setOrder(order);
                pawn.getInventory().addItem(ItemType.AXE, 1);
                pawn.getInventory().setToolWear(ItemType.AXE, 1 + ordered % 5);
                ordered++;
            }
            Assert.True(ordered > 0);
            run(original, 700);

            roundTrip(original, 2500, "mid-orders with tools");
        }

        [Fact]
        public void testObjectivesAndChronicle_PartWayThrough_RunOnIdentically() {
            Simulation original = newWorld(42);
            run(original, 2400);
            // the first objective (gather wood) completes and pays out
            original.getPlayer().getInventory().addItem(ItemType.WOOD, 60);
            run(original, 200);
            Assert.True(original.getObjectiveTracker().getCompletedCount() > 0);
            Assert.NotNull(original.getObjectiveTracker().getLastCompleted());
            Assert.NotEmpty(original.getChronicle().getEntries());

            Simulation copy = roundTrip(original, 3000, "objectives and chronicle");
            Assert.Equal(original.getObjectiveTracker().getCompletedCount(), copy.getObjectiveTracker().getCompletedCount());
            Assert.Equal(original.getChronicle().getTotalRecorded(), copy.getChronicle().getTotalRecorded());
            // the loaded chronicle is still subscribed: it went on recording
            Assert.Equal(original.getChronicle().getEntries()[original.getChronicle().getCount() - 1].getText(),
                copy.getChronicle().getEntries()[copy.getChronicle().getCount() - 1].getText());
        }

        [Fact]
        public void testAPawnHoldingATargetThatHasBeenSweptAway_LoadsHoldingIt() {
            Simulation original = newWorld(42);
            run(original, 300);
            Pawn pawn = (Pawn) original.getEntityRepository().getEntitiesOfType(EntityType.PAWN)[0];
            AppleTree gone = new AppleTree(pawn.getPosition() + new System.Numerics.Vector3(30, 0, 0), 5, original.getRandom());
            original.getEntityRepository().addEntity(gone);
            pawn.setTargetEntity(gone);
            gone.markForDeletion();
            original.getEntityRepository().removeEntity(gone);

            Simulation copy = SaveGame.decode(SaveGame.encode(original, Version));
            assertSame(original, copy, "with an orphaned target");
            Pawn loaded = (Pawn) copy.getEntityRepository().getEntity(pawn.getId());
            Assert.True(loaded.hasTargetEntity());
            Assert.True(loaded.getTargetEntity().isMarkedForDeletion());
            Assert.Null(copy.getEntityRepository().getEntity(gone.getId()));
        }

        [Fact]
        public void testIdentity_ReferencesResolveToTheSameObjects() {
            Simulation original = newWorld(42);
            DiplomacyShowcase.stage(original);
            Simulation copy = SaveGame.decode(SaveGame.encode(original, Version));

            // the simulation compares ids by reference (stall.getOwnerId() == pawn.getId())
            foreach (Entity entity in copy.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT)) {
                foreach (Stall stall in ((Settlement) entity).getMarket().getStalls()) {
                    if (stall.getOwnerId() != null) {
                        Entity owner = copy.getEntityRepository().getEntity(stall.getOwnerId());
                        if (owner != null) {
                            Assert.Same(owner.getId(), stall.getOwnerId());
                        }
                    }
                }
            }
            foreach (Nation nation in copy.getNationRepository().getNations()) {
                foreach (EntityId settlementId in nation.getSettlements()) {
                    Settlement settlement = (Settlement) copy.getEntityRepository().getEntity(settlementId);
                    Assert.Same(nation.getId(), settlement.getNationId());
                }
            }
            Assert.Same(copy.getPlayer(), copy.getEntityRepository().getEntity(copy.getPlayer().getId()));
        }

        [Fact]
        public void testLoadedWorld_KeepsItsTimeOfDayAndCounters() {
            Simulation original = newWorld(3);
            original.getTimeOfDay().setStartFraction(0.7f);
            run(original, 1234);
            Simulation copy = SaveGame.decode(SaveGame.encode(original, Version));
            Assert.Equal(1234, copy.getTickCounter().getTotalTicks());
            Assert.Equal(original.getTimeOfDay().getFraction(), copy.getTimeOfDay().getFraction());
            Assert.Equal(original.getNumPawnDeaths(), copy.getNumPawnDeaths());
            Assert.Equal(original.getEventRepository().getTotalNumberOfEvents(), copy.getEventRepository().getTotalNumberOfEvents());
        }

        [Fact]
        public void testEventRecord_KeepsCountsWhole_WhenOnlyARecentTailIsSaved() {
            Simulation original = newWorld(42);
            EventProducer producer = original.getEventProducer();
            for (int i = 0; i < EventRepository.SavedEventsPerType + 50; i++) {
                producer.produceChunkGenerateEvent(1000 + i, 0);
            }
            int total = original.getEventRepository().getTotalNumberOfEvents();
            int chunks = original.getEventRepository().getNumberOfEvents(EventType.ChunkGenerate);

            Simulation copy = SaveGame.decode(SaveGame.encode(original, Version));
            Assert.Equal(total, copy.getEventRepository().getTotalNumberOfEvents());
            Assert.Equal(chunks, copy.getEventRepository().getNumberOfEvents(EventType.ChunkGenerate));
            List<Event> kept = copy.getEventRepository().getEvents(EventType.ChunkGenerate);
            Assert.Equal(EventRepository.SavedEventsPerType, kept.Count);
            Assert.Equal("A chunk has been generated at (" + (1000 + EventRepository.SavedEventsPerType + 49) + ", 0).", kept[kept.Count - 1].getDescription());
        }
    }
}
