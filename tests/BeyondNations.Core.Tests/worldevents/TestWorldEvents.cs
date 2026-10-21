using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * World events (worldevents/): seeded and reproducible, rate-limited,
    * weighted by context, saved with their timers, and each one's choices
    * doing what they say through the ordinary systems.
    */
    public class TestWorldEvents {
        private const float Step = 1f / 50f;

        private static Simulation newWorld(int seed, int ticksPerSecond = 50) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            config.setTicksPerSecond(ticksPerSecond);
            return new Simulation(config);
        }

        private static void run(Simulation simulation, int ticks) {
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(Step);
            }
        }

        private static List<string> worldEventLines(Simulation simulation) {
            List<string> lines = new List<string>();
            foreach (ChronicleEntry entry in simulation.getChronicle().getEntries()) {
                if (entry.getEventType() == EventType.WorldEvent) {
                    lines.Add(entry.getTick() + " " + entry.getText());
                }
            }
            return lines;
        }

        /** A world where the player leads a nation (with a farm), beside a hostile and a friendly pawn-led one. */
        private static DiplomacyShowcase.Staged staged(Simulation simulation) {
            DiplomacyShowcase.Staged s = DiplomacyShowcase.stage(simulation);
            // the showcase's siege would take the player's only village within a
            // couple of minutes; moved far off and calmed down, the hostile nation
            // is still there to be stirred up, but the player's nation lasts
            System.Numerics.Vector3 at = s.hostileSettlement.getPosition();
            System.Numerics.Vector3 away = System.Numerics.Vector3.Normalize(at - s.playerSettlement.getPosition());
            s.hostileSettlement.setPosition(at + away * 600f);
            simulation.getNationRepository().getRelations().adjust(s.playerNation, s.hostile, -DiplomacyShowcase.HostileScore);
            simulation.getDiplomacy().runRound();
            return s;
        }

        private static WorldEventInstance offer(Simulation simulation, string id) {
            Assert.True(simulation.getWorldEvents().startPlayerEventNow(simulation, id), "could not offer " + id);
            WorldEventInstance pending = simulation.getWorldEvents().getPending();
            Assert.Equal(id, pending.getEventId());
            return pending;
        }

        private static string choose(Simulation simulation, int choice) {
            string outcome = simulation.getWorldEvents().choose(simulation, choice);
            Assert.NotNull(outcome);
            Assert.Null(simulation.getWorldEvents().getPending());
            return outcome;
        }

        // --- the registry ---------------------------------------------------

        [Fact]
        public void testTheDefaultRegistry_HasAtLeastEightEvents_EachWithTwoOrThreeChoices() {
            Simulation simulation = newWorld(42);
            staged(simulation);
            WorldEventRegistry registry = simulation.getWorldEvents().getRegistry();
            Assert.True(registry.getAll().Count >= 8);
            WorldEventContext context = new WorldEventContext(simulation, simulation.getWorldEvents());
            HashSet<string> ids = new HashSet<string>();
            foreach (IWorldEvent worldEvent in registry.getAll()) {
                Assert.True(ids.Add(worldEvent.getId()), "duplicate id " + worldEvent.getId());
                WorldEventInstance instance = new WorldEventInstance(worldEvent.getId(), 0, 0, true);
                List<WorldEventChoice> choices = worldEvent.getChoices(context, instance);
                Assert.InRange(choices.Count, 2, 3);
                Assert.False(string.IsNullOrEmpty(worldEvent.getTitle()));
            }
        }

        private class TestEvent : IWorldEvent {
            public int resolved = -1;
            public string getId() { return "test"; }
            public string getTitle() { return "Test"; }
            public bool isPersonal() { return true; }
            public int getWeight(WorldEventContext context, Nation nation) { return 1000; }
            public bool prepare(WorldEventContext context, Nation nation, WorldEventInstance instance) { instance.setMagnitude(7); return true; }
            public string describe(WorldEventContext context, WorldEventInstance instance) { return "A test."; }
            public List<WorldEventChoice> getChoices(WorldEventContext context, WorldEventInstance instance) {
                return new List<WorldEventChoice> { new WorldEventChoice("A", "a"), new WorldEventChoice("B", "b", "never") };
            }
            public int getDefaultChoice(WorldEventContext context, WorldEventInstance instance) { return 0; }
            public int chooseForNation(WorldEventContext context, WorldEventInstance instance) { return 0; }
            public string resolve(WorldEventContext context, WorldEventInstance instance, int choice) { resolved = choice; return "the test was taken."; }
        }

        [Fact]
        public void testANewEvent_IsJustARegisteredClass_AndDuplicateIdsAreRefused() {
            WorldEventRegistry registry = new WorldEventRegistry();
            TestEvent test = new TestEvent();
            registry.register(test);
            Assert.Same(test, registry.get("test"));
            Assert.Throws<ArgumentException>(() => registry.register(new TestEvent()));

            GameConfig config = new GameConfig();
            config.setWorldSeed(3);
            Simulation simulation = new Simulation(config);
            WorldEvents events = new WorldEvents(3, 50, registry);
            Assert.True(events.startPlayerEventNow(simulation));
            Assert.Equal(7, events.getPending().getMagnitude());
            // an unavailable choice is refused and changes nothing
            Assert.Null(events.choose(simulation, 1));
            Assert.NotNull(events.getPending());
            Assert.Equal("The test was taken.", events.choose(simulation, 0));
            Assert.Equal(0, test.resolved);
            Assert.Null(events.getPending());
        }

        // --- the clock ------------------------------------------------------

        [Fact]
        public void testTheSameSeed_ThrowsTheSameEvents_AndADifferentSeedOthers() {
            // five ticks a second: the clocks count seconds, so 40 minutes of play in 12,000 ticks
            Simulation a = newWorld(42, 5);
            Simulation b = newWorld(42, 5);
            run(a, 12000);
            run(b, 12000);
            List<string> linesA = worldEventLines(a);
            Assert.NotEmpty(linesA);
            Assert.Equal(linesA, worldEventLines(b));
            Assert.Equal(a.getWorldEvents().getNumEvents(), b.getWorldEvents().getNumEvents());
            Assert.Equal(a.getWorldEvents().getNextPlayerEventTick(), b.getWorldEvents().getNextPlayerEventTick());
            Assert.Equal(WorldComparer.fingerprint(a), WorldComparer.fingerprint(b));

            Simulation c = newWorld(7, 5);
            run(c, 12000);
            Assert.NotEqual(linesA, worldEventLines(c));
        }

        [Fact]
        public void testPlayerEvents_AreRateLimited_ToAboutOneEveryTenToFifteenMinutes() {
            Simulation simulation = newWorld(42, 5);
            WorldEvents events = simulation.getWorldEvents();
            Assert.Equal(WorldEvents.Unscheduled, events.getNextPlayerEventTick());
            // the clocks start with the first tick
            simulation.fixedUpdate(Step);
            int first = events.getNextPlayerEventTick() - 1;
            Assert.InRange(first, WorldEvents.FIRST_PLAYER_EVENT_MIN_SECONDS * 5, WorldEvents.FIRST_PLAYER_EVENT_MAX_SECONDS * 5);
            List<int> offeredAt = new List<int>();
            int seen = 0;
            for (int i = 0; i < 18000; i++) {
                simulation.fixedUpdate(Step);
                if (events.getNumPlayerEvents() > seen) {
                    seen = events.getNumPlayerEvents();
                    offeredAt.Add(simulation.getTickCounter().getTotalTicks());
                }
            }
            // an hour of play: the personal events alone always fit, so one every 10-15 minutes
            Assert.InRange(offeredAt.Count, 3, 6);
            for (int i = 1; i < offeredAt.Count; i++) {
                Assert.True(offeredAt[i] - offeredAt[i - 1] >= WorldEvents.PLAYER_EVENT_MIN_SECONDS * 5,
                    "player events " + offeredAt[i - 1] + " and " + offeredAt[i] + " came too close together");
            }
        }

        [Fact]
        public void testAnUnansweredChoice_TakesItsDefault_AfterTheTimeout() {
            // a personal event, so nothing the world does in three minutes can take its subject away
            Simulation simulation = newWorld(42);
            simulation.getPlayer().getInventory().setNumItems(ItemType.COIN, 60);
            offer(simulation, RareMerchantEvent.Id);
            run(simulation, WorldEvents.CHOICE_TIMEOUT_SECONDS * 50 - 1);
            Assert.NotNull(simulation.getWorldEvents().getPending());
            run(simulation, 2);
            Assert.Null(simulation.getWorldEvents().getPending());
            // the default is to decline
            Assert.Equal(60, simulation.getPlayer().getInventory().getNumItems(ItemType.COIN));
            Assert.Contains(worldEventLines(simulation), line => line.Contains("let it pass"));
        }

        [Fact]
        public void testPawnLedNations_MeetEventsToo_AndTheyAreNews() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            int before = worldEventLines(simulation).Count;
            WorldEventInstance happened = null;
            for (int i = 0; i < 20 && happened == null; i++) {
                happened = simulation.getWorldEvents().runNationEvent(simulation);
            }
            Assert.NotNull(happened);
            Assert.False(happened.isForPlayer());
            Assert.NotEqual(s.playerNation.getId(), happened.getNationId());
            Assert.True(worldEventLines(simulation).Count > before);
            // and never waits on the player
            Assert.Null(simulation.getWorldEvents().getPending());
        }

        [Fact]
        public void testContext_WeighsEvents_NoDroughtWithoutFarms_NoFireWithoutAStorehouse() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEventContext context = new WorldEventContext(simulation, simulation.getWorldEvents());
            // the friendly nation's camp has no buildings at all
            Assert.Equal(0, new DroughtEvent().getWeight(context, s.friendly));
            Assert.Equal(0, new StorehouseFireEvent().getWeight(context, s.friendly));
            Assert.True(new DroughtEvent().getWeight(context, s.hostile) > 0);
            // a nation already in drought gets no second one, nor a bountiful harvest
            simulation.getWorldEvents().addEffect(new WorldEffect(WorldEffectKind.DROUGHT, s.hostile.getId(), null, 0, 100000, 0));
            Assert.Equal(0, new DroughtEvent().getWeight(context, s.hostile));
            Assert.Equal(0, new BountifulHarvestEvent().getWeight(context, s.hostile));
            // seasons are a hook until a Season API lands
            Assert.Equal(Season.SPRING, context.getSeason());
        }

        [Fact]
        public void testNoDroughtOrBountifulHarvest_InWinter() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEventContext context = new WorldEventContext(simulation, simulation.getWorldEvents());
            Assert.True(new DroughtEvent().getWeight(context, s.playerNation) > 0);
            simulation.getSeasonCalendar().advanceClockTo(simulation.getTickCounter(), Season.WINTER);
            Assert.True(context.isWinter());
            Assert.Equal(0, new DroughtEvent().getWeight(context, s.playerNation));
            Assert.Equal(0, new BountifulHarvestEvent().getWeight(context, s.playerNation));
            Assert.True(new SicknessEvent().getWeight(context, s.hostile) > 0);
        }

        [Fact]
        public void testAWorldOpenedPartWayThroughItsYear_DoesNotMeetAnEventAtOnce() {
            Simulation simulation = newWorld(42);
            simulation.getSeasonCalendar().advanceClockTo(simulation.getTickCounter(), Season.WINTER);
            int tick = simulation.getTickCounter().getTotalTicks();
            run(simulation, 10);
            Assert.Null(simulation.getWorldEvents().getPending());
            Assert.True(simulation.getWorldEvents().getNextPlayerEventTick() >= tick + WorldEvents.FIRST_PLAYER_EVENT_MIN_SECONDS * 50);
        }

        // --- each event's effects -------------------------------------------

        [Fact]
        public void testDrought_StopsTheFarms_AndIrrigationShortensIt() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            s.playerSettlement.getInventory().addItem(ItemType.WOOD, 20);
            s.playerSettlement.getInventory().addItem(ItemType.STONE, 20);
            offer(simulation, DroughtEvent.Id);
            choose(simulation, 0);
            Assert.Equal(5, s.playerSettlement.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(10, s.playerSettlement.getInventory().getNumItems(ItemType.STONE));
            WorldEffect drought = simulation.getWorldEvents().findEffect(WorldEffectKind.DROUGHT, s.playerNation.getId(), null);
            Assert.NotNull(drought);
            Assert.Equal(DroughtEvent.IRRIGATED_DURATION_SECONDS * 50, drought.getEndTick() - drought.getStartTick());
            Assert.True(simulation.getWorldEvents().isDrought(s.playerNation.getId()));

            // no harvest while it lasts: a merchant stall would otherwise get apples
            Stall stall = s.playerSettlement.getMarket().getStalls()[0];
            stall.setOwnerId(simulation.getPlayer().getId());
            int apples = stall.getInventory().getNumItems(ItemType.APPLE);
            run(simulation, BuildingCatalog.FARM_TICKS_PER_HARVEST * 2);
            Assert.Equal(apples, stall.getInventory().getNumItems(ItemType.APPLE));
            run(simulation, drought.getEndTick() - simulation.getTickCounter().getTotalTicks() + 1);
            Assert.False(simulation.getWorldEvents().isDrought(s.playerNation.getId()));
            Assert.Contains(worldEventLines(simulation), line => line.Contains("Rain returned"));
            run(simulation, BuildingCatalog.FARM_TICKS_PER_HARVEST + 1);
            Assert.True(stall.getInventory().getNumItems(ItemType.APPLE) > apples);
        }

        [Fact]
        public void testDrought_BuyingGrain_FillsTheStores_AndWaitingCostsGoodwill() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            simulation.getPlayer().getInventory().setNumItems(ItemType.COIN, 100);
            offer(simulation, DroughtEvent.Id);
            choose(simulation, 1);
            Assert.Equal(75, simulation.getPlayer().getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(DroughtEvent.GRAIN_APPLES, s.playerSettlement.getInventory().getNumItems(ItemType.APPLE));

            Simulation other = newWorld(42);
            DiplomacyShowcase.Staged t = staged(other);
            Pawn member = (Pawn) other.getEntityRepository().getEntity(t.playerNation.getMemberIds()[1]);
            int before = PawnRelations.getRelationship(member, other.getPlayer());
            offer(other, DroughtEvent.Id);
            choose(other, 2);
            Assert.Equal(before - DroughtEvent.RESTLESS_RELATIONSHIP, PawnRelations.getRelationship(member, other.getPlayer()));
            WorldEffect drought = other.getWorldEvents().findEffect(WorldEffectKind.DROUGHT, t.playerNation.getId(), null);
            Assert.Equal(DroughtEvent.DURATION_SECONDS * 50, drought.getEndTick() - drought.getStartTick());
        }

        [Fact]
        public void testSickness_EachChoice() {
            foreach (int choice in new[] { 0, 1, 2 }) {
                Simulation simulation = newWorld(42);
                DiplomacyShowcase.Staged s = staged(simulation);
                addResidents(simulation, s.playerNation, s.playerSettlement, 2);
                simulation.getPlayer().getInventory().setNumItems(ItemType.COIN, 50);
                // one resident is already run down, and is the first to fall ill
                Pawn weakest = (Pawn) simulation.getEntityRepository().getEntity(s.playerNation.getMemberIds()[1]);
                weakest.setEnergy(50f);
                WorldEventInstance pending = offer(simulation, SicknessEvent.Id);
                WorldEventContext context = new WorldEventContext(simulation, simulation.getWorldEvents());
                List<Pawn> sick = SicknessEvent.getSick(context, pending);
                Assert.Equal(SicknessEvent.MAX_SICK, sick.Count);
                Assert.Same(weakest, sick[0]);
                float[] before = sick.ConvertAll(p => p.getEnergy()).ToArray();
                string outcome = choose(simulation, choice);
                if (choice == 0) {
                    Assert.Equal(30, simulation.getPlayer().getInventory().getNumItems(ItemType.COIN));
                    for (int i = 0; i < sick.Count; i++) Assert.Equal(before[i], sick[i].getEnergy());
                } else if (choice == 1) {
                    for (int i = 0; i < sick.Count; i++) Assert.Equal(before[i] - SicknessEvent.QUARANTINE_ENERGY, sick[i].getEnergy());
                } else {
                    Assert.True(sick[0].getEnergy() < 0f, "the weakest does not survive");
                    Assert.Contains("did not survive", outcome);
                    run(simulation, 1);
                    Assert.True(simulation.getNumPawnDeaths() >= 1);
                }
            }
        }

        private static void addResidents(Simulation simulation, Nation nation, Settlement settlement, int count) {
            WorldEventContext context = new WorldEventContext(simulation, simulation.getWorldEvents());
            for (int i = 0; i < count; i++) {
                Pawn pawn = context.spawnPawnNear(settlement.getPosition(), 0f);
                context.join(pawn, nation);
                pawn.setHomeSettlementId(settlement.getId());
                pawn.setEnergy(100f);
            }
        }

        [Fact]
        public void testWanderers_JoinTheNation_OrStayInTheWorldNationless() {
            foreach (int choice in new[] { 0, 1, 2 }) {
                Simulation simulation = newWorld(42);
                DiplomacyShowcase.Staged s = staged(simulation);
                simulation.getPlayer().getInventory().setNumItems(ItemType.APPLE, 10);
                WorldEventInstance pending = offer(simulation, WanderersEvent.Id);
                int members = s.playerNation.getNumberOfMembers();
                int pawns = simulation.getEntityRepository().getNumEntitiesOfType(EntityType.PAWN);
                choose(simulation, choice);
                int arriving = pending.getMagnitude();
                if (choice == 0) {
                    Assert.Equal(members + arriving, s.playerNation.getNumberOfMembers());
                    Assert.Equal(10 - arriving * WanderersEvent.FOOD_PER_WANDERER, simulation.getPlayer().getInventory().getNumItems(ItemType.APPLE));
                } else if (choice == 1) {
                    Assert.Equal(members + 1, s.playerNation.getNumberOfMembers());
                    Assert.Equal(10, simulation.getPlayer().getInventory().getNumItems(ItemType.APPLE));
                } else {
                    Assert.Equal(members, s.playerNation.getNumberOfMembers());
                    Assert.Equal(pawns + arriving, simulation.getEntityRepository().getNumEntitiesOfType(EntityType.PAWN));
                }
            }
        }

        [Fact]
        public void testBandits_RobUntilTheyLeave_UnlessDrivenOffOrPaid() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            s.playerSettlement.addFunds(100);
            s.playerSettlement.getInventory().addItem(ItemType.WOOD, 40);
            WorldEventInstance pending = offer(simulation, BanditCampEvent.Id);
            Assert.Equal(s.playerSettlement.getId(), pending.getSettlementId());
            choose(simulation, 2);
            Assert.True(simulation.getWorldEvents().hasBandits(s.playerSettlement.getId()));
            run(simulation, WorldEvents.BANDIT_THEFT_SECONDS * 50 + 1);
            Assert.True(s.playerSettlement.getInventory().getNumItems(ItemType.WOOD) <= 40 - WorldEvents.BANDIT_THEFT_GOODS);
            // a day and a half later, they move on (brought forward, to keep the test quick)
            WorldEffect camp = simulation.getWorldEvents().findEffect(WorldEffectKind.BANDITS, null, s.playerSettlement.getId());
            Assert.Equal(BanditCampEvent.STAY_SECONDS * 50, camp.getEndTick() - camp.getStartTick());
            camp.setEndTick(simulation.getTickCounter().getTotalTicks() + 10);
            run(simulation, 11);
            Assert.False(simulation.getWorldEvents().hasBandits(s.playerSettlement.getId()));
            Assert.Contains(worldEventLines(simulation), line => line.Contains("moved on"));

            // paying them off costs coin and leaves no camp
            Simulation paid = newWorld(42);
            DiplomacyShowcase.Staged p = staged(paid);
            p.playerSettlement.addFunds(100);
            paid.getPlayer().getInventory().setNumItems(ItemType.COIN, 40);
            offer(paid, BanditCampEvent.Id);
            choose(paid, 1);
            Assert.Equal(10, paid.getPlayer().getInventory().getNumItems(ItemType.COIN));
            Assert.Empty(paid.getWorldEvents().getEffects());

            // a militia (the player and two pawns) tires everyone out
            Simulation militia = newWorld(42);
            DiplomacyShowcase.Staged m = staged(militia);
            m.playerSettlement.addFunds(100);
            offer(militia, BanditCampEvent.Id);
            choose(militia, 0);
            Assert.Empty(militia.getWorldEvents().getEffects());
            Assert.True(militia.getPlayer().getEnergy() <= 100f - BanditCampEvent.MILITIA_ENERGY);
        }

        [Fact]
        public void testClaimant_BackingThemPutsThemInPower_AndWarmsRelations() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            simulation.getPlayer().getInventory().setNumItems(ItemType.COIN, 50);
            WorldEventInstance pending = offer(simulation, ClaimantEvent.Id);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            Assert.NotNull(other);
            Assert.NotEqual(s.playerNation, other);
            int score = simulation.getNationRepository().getRelations().getScore(s.playerNation, other);
            choose(simulation, 0);
            Assert.Equal(pending.getEntityId(), other.getLeaderId());
            Assert.Equal(NationRole.LEADER, other.getRole(pending.getEntityId()));
            Assert.Equal(score + ClaimantEvent.BACKED_RELATIONS, simulation.getNationRepository().getRelations().getScore(s.playerNation, other));
            Assert.Equal(30, simulation.getPlayer().getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testClaimant_WarningTheLeader_GetsTheClaimantThrownOut() {
            Simulation simulation = newWorld(42);
            DiplomacyShowcase.Staged s = staged(simulation);
            WorldEventInstance pending = offer(simulation, ClaimantEvent.Id);
            Nation other = simulation.getNationRepository().getNation(pending.getOtherNationId());
            EntityId leader = other.getLeaderId();
            choose(simulation, 1);
            Assert.False(other.isMember(pending.getEntityId()));
            Assert.Null(((Pawn) simulation.getEntityRepository().getEntity(pending.getEntityId())).getNationId());
            Assert.Equal(leader, other.getLeaderId());
        }

        [Fact]
        public void testBountifulHarvest_EachChoice() {
            foreach (int choice in new[] { 0, 1, 2 }) {
                Simulation simulation = newWorld(42);
                DiplomacyShowcase.Staged s = staged(simulation);
                simulation.getPlayer().setEnergy(30f);
                WorldEventInstance pending = offer(simulation, BountifulHarvestEvent.Id);
                int funds = s.playerSettlement.getFunds();
                choose(simulation, choice);
                if (choice == 0) {
                    Assert.Equal(pending.getMagnitude(), s.playerSettlement.getInventory().getNumItems(ItemType.APPLE));
                } else if (choice == 1) {
                    Assert.Equal(funds + pending.getMagnitude() * BountifulHarvestEvent.COINS_PER_APPLE, s.playerSettlement.getFunds());
                } else {
                    Assert.Equal(100f, simulation.getPlayer().getEnergy());
                }
            }
        }

        [Fact]
        public void testLostTraveller_TheMapPutsStoneAndACacheInTheWorld() {
            Simulation simulation = newWorld(42);
            simulation.getPlayer().getInventory().setNumItems(ItemType.APPLE, 2);
            int rocks = simulation.getEntityRepository().getNumEntitiesOfType(EntityType.ROCK);
            WorldEventInstance pending = offer(simulation, LostTravellerEvent.Id);
            System.Numerics.Vector3 spot = LostTravellerEvent.getSpot(simulation.getPlayer().getPosition(), pending);
            choose(simulation, 0);
            Assert.Equal(1, simulation.getPlayer().getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(rocks + LostTravellerEvent.ROCKS, simulation.getEntityRepository().getNumEntitiesOfType(EntityType.ROCK));
            bool cache = false;
            foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(EntityType.ITEM_DROP)) {
                ItemStack stack = ((ItemDrop) entity).getItemStack();
                if (System.Numerics.Vector3.Distance(entity.getPosition(), spot) < 1f
                        && stack.getItemType() == ItemType.COIN && stack.getQuantity() == LostTravellerEvent.CACHE_COINS) {
                    cache = true;
                }
            }
            Assert.True(cache, "a cache lies at the spot");
        }

        [Fact]
        public void testStorehouseFire_EachChoiceBurnsWhatItSays() {
            int[,] expected = { { 30, 15 }, { 0, 20 }, { 10, 5 } };
            for (int choice = 0; choice < 3; choice++) {
                Simulation simulation = newWorld(42);
                DiplomacyShowcase.Staged s = staged(simulation);
                s.playerSettlement.addBuilding(BuildingType.STOREHOUSE);
                s.playerSettlement.getInventory().addItem(ItemType.WOOD, 40);
                s.playerSettlement.getInventory().addItem(ItemType.STONE, 20);
                offer(simulation, StorehouseFireEvent.Id);
                choose(simulation, choice);
                Assert.Equal(expected[choice, 0], s.playerSettlement.getInventory().getNumItems(ItemType.WOOD));
                Assert.Equal(expected[choice, 1], s.playerSettlement.getInventory().getNumItems(ItemType.STONE));
            }
        }

        [Fact]
        public void testRareMerchant_SellsToolsCheaply_OrTradesWoodForStone() {
            Simulation simulation = newWorld(42);
            Inventory inventory = simulation.getPlayer().getInventory();
            inventory.setNumItems(ItemType.COIN, 60);
            inventory.setNumItems(ItemType.WOOD, 25);
            offer(simulation, RareMerchantEvent.Id);
            choose(simulation, 0);
            Assert.Equal(10, inventory.getNumItems(ItemType.COIN));
            Assert.Equal(1, inventory.getNumItems(ItemType.AXE));
            Assert.Equal(1, inventory.getNumItems(ItemType.PICKAXE));

            Simulation other = newWorld(42);
            other.getPlayer().getInventory().setNumItems(ItemType.COIN, 0);
            other.getPlayer().getInventory().setNumItems(ItemType.WOOD, 25);
            offer(other, RareMerchantEvent.Id);
            List<WorldEventChoice> choices = other.getWorldEvents().getPendingChoices(other);
            Assert.False(choices[0].isAvailable());
            Assert.Equal("Needs " + RareMerchantEvent.TOOLS_COINS + " coins", choices[0].getUnavailableReason());
            choose(other, 1);
            Assert.Equal(5, other.getPlayer().getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(RareMerchantEvent.TRADE_STONE, other.getPlayer().getInventory().getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testOutcomes_AreWrittenToTheChronicle_NamingThePlayer() {
            Simulation simulation = newWorld(42);
            staged(simulation);
            offer(simulation, BountifulHarvestEvent.Id);
            choose(simulation, 1);
            ChronicleEntry last = simulation.getChronicle().getEntries()[simulation.getChronicle().getCount() - 1];
            Assert.Equal(EventType.WorldEvent, last.getEventType());
            Assert.StartsWith("A bountiful harvest", last.getText());
            Assert.Contains(simulation.getPlayer().getId(), last.getEntityIds());
            Assert.Equal(last.getText(), simulation.getPlayer().getStatus().getStatus());
        }

        // --- saving ---------------------------------------------------------

        [Fact]
        public void testAPendingChoiceAndRunningEffects_SurviveASave_AndPlayOnTheSame() {
            Simulation original = newWorld(42);
            DiplomacyShowcase.Staged s = staged(original);
            s.playerSettlement.addFunds(100);
            s.playerSettlement.getInventory().addItem(ItemType.WOOD, 40);
            offer(original, BanditCampEvent.Id);
            choose(original, 2);
            original.getWorldEvents().addEffect(new WorldEffect(WorldEffectKind.DROUGHT, s.hostile.getId(), null, 0, 20000, 0));
            run(original, 100);
            offer(original, WanderersEvent.Id);
            Assert.True(original.getWorldEvents().isAwaitingPlayer());

            Simulation copy = SaveGame.decode(SaveGame.encode(original, "test"));
            Assert.Null(WorldComparer.difference(original, copy));
            Assert.NotNull(copy.getWorldEvents().getPending());
            Assert.Equal(WanderersEvent.Id, copy.getWorldEvents().getPending().getEventId());
            Assert.Equal(2, copy.getWorldEvents().getEffects().Count);
            Assert.Equal(original.getWorldEvents().describePending(original), copy.getWorldEvents().describePending(copy));

            // the same choice in both, and they go on the same
            HashSet<Guid> savedIds = WorldComparer.idsOf(original);
            Assert.Equal(original.getWorldEvents().choose(original, 1), copy.getWorldEvents().choose(copy, 1));
            run(original, 4000);
            run(copy, 4000);
            Assert.Null(WorldComparer.difference(original, copy, savedIds));
            Assert.Equal(WorldComparer.fingerprint(original), WorldComparer.fingerprint(copy));
        }

        [Fact]
        public void testTheVersion3Fixture_LoadsWithItsWorldEventClocks_AndPlaysOn() {
            byte[] file = File.ReadAllBytes(SaveFixtures.pathFor(3));
            Assert.Equal(3, SaveGame.check(file).FormatVersion);
            Simulation loaded = SaveGame.decode(file);
            // the same clocks the fixture world had when it was written
            Simulation fresh = SaveFixtures.fixtureWorld();
            Assert.Equal(fresh.getWorldEvents().getNextPlayerEventTick(), loaded.getWorldEvents().getNextPlayerEventTick());
            Assert.Equal(fresh.getWorldEvents().getNextWorldEventTick(), loaded.getWorldEvents().getNextWorldEventTick());
            run(loaded, 500);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void testASaveFromBeforeWorldEvents_LoadsWithTheirClocksStartingFromItsTick(int version) {
            Simulation loaded = SaveGame.decode(File.ReadAllBytes(SaveFixtures.pathFor(version)));
            WorldEvents events = loaded.getWorldEvents();
            Assert.Equal(WorldEvents.Unscheduled, events.getNextPlayerEventTick());
            int tick = loaded.getTickCounter().getTotalTicks();
            run(loaded, 1);
            Assert.Null(events.getPending());
            Assert.Empty(events.getEffects());
            Assert.InRange(events.getNextPlayerEventTick(), tick + 1 + WorldEvents.FIRST_PLAYER_EVENT_MIN_SECONDS * 50,
                tick + 1 + WorldEvents.FIRST_PLAYER_EVENT_MAX_SECONDS * 50);
            Assert.True(events.getNextWorldEventTick() > tick);
            // and loading it twice gives the same clocks
            Simulation again = SaveGame.decode(File.ReadAllBytes(SaveFixtures.pathFor(version)));
            run(again, 1);
            Assert.Equal(events.getNextPlayerEventTick(), again.getWorldEvents().getNextPlayerEventTick());
        }
    }
}
