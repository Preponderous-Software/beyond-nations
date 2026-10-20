using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Eras, their unlocks, the victories and the collapse, judged against
    * real simulation states. Members are added as bare ids where only the
    * count matters, and the tracker is evaluated directly rather than by
    * waiting for the second to come round.
    */
    public class TestEraTracker {
        private readonly Simulation simulation;
        private readonly Player player;
        private readonly EraTracker eras;

        public TestEraTracker() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(20261020);
            simulation = new Simulation(config);
            player = simulation.getPlayer();
            eras = simulation.getEraTracker();
        }

        private Nation makePlayerLeader() {
            Nation nation = new Nation("Testland", player.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            player.setNationId(nation.getId());
            return nation;
        }

        private Nation makePawnNation(string name) {
            Pawn leader = new Pawn(new Vector3(5000, 1, 5000), "Leader of " + name, simulation.getRandom());
            simulation.getEntityRepository().addEntity(leader);
            Nation nation = new Nation(name, leader.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            leader.setNationId(nation.getId());
            return nation;
        }

        private Settlement addSettlement(Nation nation, Vector3 position) {
            Settlement settlement = new Settlement(position, nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return settlement;
        }

        private static void addMembers(Nation nation, int total) {
            while (nation.getNumberOfMembers() < total) {
                nation.addMember(new EntityId());
            }
        }

        private static void build(Settlement settlement, int count) {
            BuildingType[] order = { BuildingType.HOUSE, BuildingType.FARM };
            for (int i = 0; settlement.getNumBuildings() < count; i++) {
                Assert.True(settlement.addBuilding(order[i % 2]));
            }
        }

        /** A nation that meets every requirement up to the Kingdom. */
        private Nation makeKingdomReadyNation() {
            Nation nation = makePlayerLeader();
            Settlement first = null;
            for (int i = 0; i < EraRules.KingdomSettlements; i++) {
                Settlement settlement = addSettlement(nation, new Vector3(i * 1000, 1, 0));
                first = first ?? settlement;
                if (i < EraRules.KingdomTowns) {
                    build(settlement, SettlementTiers.TOWN_BUILDINGS);
                }
            }
            first.getMarket().createStall();
            first.addFunds(EraRules.KingdomFunds);
            addMembers(nation, EraRules.KingdomMembers);
            return nation;
        }

        [Fact]
        public void testANewRun_IsACampWithNoOutcome() {
            Assert.Equal(Era.CAMP, eras.getEra());
            Assert.Equal(RunOutcome.NONE, eras.getOutcome());
            Assert.Equal(0, eras.getReachedTick(Era.CAMP));
            Assert.Equal(-1, eras.getReachedTick(Era.VILLAGE));
            Assert.Equal(3, eras.getRequirements(simulation).Count);
            eras.evaluate(simulation);
            Assert.Equal(Era.CAMP, eras.getEra());
            Assert.False(eras.shouldShowOutcome());
        }

        [Fact]
        public void testVillage_NeedsAVillageMembersAndAStall() {
            Nation nation = makePlayerLeader();
            Settlement settlement = addSettlement(nation, Vector3.Zero);
            addMembers(nation, EraRules.VillageMembers);
            build(settlement, SettlementTiers.VILLAGE_BUILDINGS);
            eras.evaluate(simulation);
            Assert.Equal(Era.CAMP, eras.getEra()); // no stall yet

            settlement.getMarket().createStall();
            eras.evaluate(simulation);
            Assert.Equal(Era.VILLAGE, eras.getEra());
            Assert.StartsWith("A new era: Testland is now a Village.", player.getStatus().getStatus());
            Assert.Contains("Workshop", player.getStatus().getStatus());
            Assert.Contains(eras.getHistory(), entry => entry.isMilestone() && entry.getText() == "Testland entered the Village era.");
        }

        [Fact]
        public void testEras_AdvanceOneAtATime_AndNeverGoBack() {
            Nation nation = makeKingdomReadyNation();
            eras.evaluate(simulation);
            Assert.Equal(Era.VILLAGE, eras.getEra());
            eras.evaluate(simulation);
            Assert.Equal(Era.TOWN, eras.getEra());
            eras.evaluate(simulation);
            Assert.Equal(Era.KINGDOM, eras.getEra());
            Assert.Empty(eras.getRequirements(simulation));

            // losing the lot does not undo an era
            foreach (EntityId member in nation.getMemberIds()) {
                if (member != player.getId()) {
                    nation.removeMember(member);
                }
            }
            eras.evaluate(simulation);
            Assert.Equal(Era.KINGDOM, eras.getEra());
        }

        [Fact]
        public void testCamp_LocksTheWorkshopAndStorehouse_ForTheNationThePlayerLeads() {
            Nation nation = makePlayerLeader();
            Settlement own = addSettlement(nation, Vector3.Zero);
            Settlement theirs = addSettlement(makePawnNation("Pawnland"), new Vector3(3000, 1, 0));
            eras.applyRules(simulation);

            Assert.False(own.canAddBuilding(BuildingType.WORKSHOP));
            Assert.False(own.canAddBuilding(BuildingType.STOREHOUSE));
            Assert.True(own.canAddBuilding(BuildingType.HOUSE));
            Assert.True(own.canAddBuilding(BuildingType.FARM));
            // a pawn-led nation is never held back
            Assert.True(theirs.canAddBuilding(BuildingType.WORKSHOP));
            Assert.True(theirs.canAddBuilding(BuildingType.STOREHOUSE));
            Assert.True(simulation.getNationRepository().getNation(theirs.getNationId()).canFoundSettlement());

            // the build command says why
            own.addCurrentlyPresentEntity(player.getId());
            player.setCurrentSettlementId(own.getId());
            player.getInventory().addItem(ItemType.WOOD, 100);
            player.getInventory().addItem(ItemType.STONE, 100);
            Assert.False(new BuildBuildingCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player, BuildingType.WORKSHOP));
            Assert.Equal("The Workshop is unlocked in the Village era.", player.getStatus().getStatus());
            // and the build key builds something it may
            Assert.True(new BuildBuildingCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player, null));
        }

        [Fact]
        public void testEachEra_UnlocksItsBuildings() {
            Nation nation = makeKingdomReadyNation();
            Settlement settlement = simulation.getEntityRepository().getEntity(nation.getSettlement(0)) as Settlement;
            eras.evaluate(simulation);
            Assert.Equal(Era.VILLAGE, eras.getEra());
            Assert.True(settlement.canAddBuilding(BuildingType.WORKSHOP));
            Assert.False(settlement.canAddBuilding(BuildingType.STOREHOUSE));
            eras.evaluate(simulation);
            Assert.Equal(Era.TOWN, eras.getEra());
            Assert.True(settlement.canAddBuilding(BuildingType.STOREHOUSE));
        }

        [Fact]
        public void testTheSettlementLimit_RisesWithTheEra_AndStopsTheFoundCommand() {
            Nation nation = makePlayerLeader();
            addSettlement(nation, Vector3.Zero);
            eras.applyRules(simulation);
            Assert.Equal(1, nation.getSettlementLimit());
            Assert.False(nation.canFoundSettlement());

            player.setPosition(new Vector3(4000, 1, 4000));
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom()).execute(player);
            Assert.Equal(1, nation.getNumberOfSettlements());
            Assert.StartsWith("Your nation may hold only 1 settlement in this era.", player.getStatus().getStatus());
            Assert.Equal(Settlement.WOOD_COST_TO_BUILD, player.getInventory().getNumItems(ItemType.WOOD));

            Assert.Equal(3, EraRules.settlementLimit(Era.VILLAGE));
            Assert.Equal(6, EraRules.settlementLimit(Era.TOWN));
            Assert.Equal(0, EraRules.settlementLimit(Era.KINGDOM));
        }

        [Fact]
        public void testALimitIsLifted_WhenThePlayerNoLongerLeads() {
            Nation nation = makePlayerLeader();
            Settlement settlement = addSettlement(nation, Vector3.Zero);
            eras.applyRules(simulation);
            Assert.Equal(Era.CAMP, settlement.getBuildingEra());
            Pawn heir = new Pawn(Vector3.Zero, "Heir", simulation.getRandom());
            simulation.getEntityRepository().addEntity(heir);
            nation.addMember(heir.getId());
            nation.setLeaderId(heir.getId());
            eras.applyRules(simulation);
            Assert.Null(settlement.getBuildingEra());
            Assert.Equal(0, nation.getSettlementLimit());
        }

        [Fact]
        public void testProsperity_IsAKingdomVictory() {
            Nation nation = makeKingdomReadyNation();
            Settlement first = simulation.getEntityRepository().getEntity(nation.getSettlement(0)) as Settlement;
            first.addFunds(VictoryRules.ProsperityFunds);
            addMembers(nation, VictoryRules.ProsperityMembers);
            eras.evaluate(simulation);
            eras.evaluate(simulation);
            Assert.Equal(RunOutcome.NONE, eras.getOutcome()); // a town, however rich, is not yet a kingdom
            eras.evaluate(simulation);
            Assert.Equal(Era.KINGDOM, eras.getEra());
            Assert.Equal(RunOutcome.PROSPERITY, eras.getOutcome());
            Assert.True(eras.shouldShowOutcome());
            Assert.Contains(eras.getHistory(), entry => entry.getText() == "Testland won a Prosperity victory.");
        }

        [Fact]
        public void testConcord_AlliedWithEveryNationLeft() {
            Nation nation = makeKingdomReadyNation();
            Nation a = makePawnNation("Avalon");
            Nation b = makePawnNation("Brightmoor");
            eras.evaluate(simulation);
            eras.evaluate(simulation);
            Assert.Equal(Era.TOWN, eras.getEra());
            NationRelations relations = simulation.getNationRepository().getRelations();
            relations.formAlliance(nation, a);
            eras.evaluate(simulation);
            Assert.NotEqual(RunOutcome.CONCORD, eras.getOutcome());
            relations.formAlliance(nation, b);
            // a fresh tracker at the Town era, so Prosperity does not get there first
            Assert.True(VictoryRules.concordProgress(simulation, Era.TOWN, NationStanding.of(simulation)).met);
            Assert.False(VictoryRules.concordProgress(simulation, Era.VILLAGE, NationStanding.of(simulation)).met);
        }

        [Fact]
        public void testConcord_NeedsSomeoneToBeAlliedWith() {
            makeKingdomReadyNation();
            Assert.False(VictoryRules.concordProgress(simulation, Era.TOWN, NationStanding.of(simulation)).met);
        }

        [Fact]
        public void testDominion_MostOfTheWorldsSettlements() {
            Nation nation = makePlayerLeader();
            Nation rival = makePawnNation("Rivalia");
            for (int i = 0; i < VictoryRules.DominionMinSettlements; i++) {
                addSettlement(nation, new Vector3(i * 1000, 1, 0));
            }
            for (int i = 0; i < VictoryRules.DominionMinSettlements; i++) {
                addSettlement(rival, new Vector3(i * 1000, 1, 3000));
            }
            NationStanding standing = NationStanding.of(simulation);
            Assert.False(VictoryRules.dominionProgress(simulation, Era.TOWN, standing).met); // half is not most
            addSettlement(nation, new Vector3(9000, 1, 0));
            standing = NationStanding.of(simulation);
            Assert.True(VictoryRules.dominionProgress(simulation, Era.TOWN, standing).met);
            Assert.False(VictoryRules.dominionProgress(simulation, Era.VILLAGE, standing).met);
            Assert.Equal(RunOutcome.DOMINION, VictoryRules.checkVictory(simulation, Era.TOWN, standing));
        }

        [Fact]
        public void testDefeat_AfterTheNationHasStoodEmptyForTheGracePeriod() {
            Nation nation = makePlayerLeader();
            Settlement settlement = addSettlement(nation, Vector3.Zero);
            eras.evaluate(simulation);
            Assert.Equal(-1, eras.getCollapseSinceTick());

            nation.removeSettlement(settlement.getId());
            eras.evaluate(simulation);
            Assert.True(eras.getCollapseSinceTick() >= 0);
            Assert.StartsWith("Your nation is collapsing!", player.getStatus().getStatus());
            Assert.Equal(VictoryRules.CollapseGraceTicks, eras.getCollapseTicksLeft(simulation));

            for (int i = 0; i < VictoryRules.CollapseGraceTicks - 1; i++) {
                simulation.getTickCounter().increment();
            }
            eras.evaluate(simulation);
            Assert.Equal(RunOutcome.NONE, eras.getOutcome());
            simulation.getTickCounter().increment();
            eras.evaluate(simulation);
            Assert.Equal(RunOutcome.DEFEAT, eras.getOutcome());
            Assert.True(eras.shouldShowOutcome());
            // a defeat cannot be played past
            eras.acknowledgeOutcome();
            Assert.True(eras.shouldShowOutcome());
        }

        [Fact]
        public void testACollapse_IsCalledOff_WhenTheNationStandsAgain() {
            Nation nation = makePlayerLeader();
            Settlement settlement = addSettlement(nation, Vector3.Zero);
            eras.evaluate(simulation);
            nation.removeSettlement(settlement.getId());
            eras.evaluate(simulation);
            Assert.True(eras.getCollapseSinceTick() >= 0);
            addMembers(nation, 2); // a follower keeps it alive
            eras.evaluate(simulation);
            Assert.Equal(-1, eras.getCollapseSinceTick());
            Assert.Equal("Your nation stands again.", player.getStatus().getStatus());
        }

        [Fact]
        public void testNoDefeat_BeforeThePlayerHasEverHeldASettlement() {
            for (int i = 0; i < VictoryRules.CollapseGraceTicks * 2; i += 50) {
                for (int t = 0; t < 50; t++) {
                    simulation.getTickCounter().increment();
                }
                eras.evaluate(simulation);
            }
            Assert.Equal(RunOutcome.NONE, eras.getOutcome());
            Assert.Equal(-1, eras.getCollapseSinceTick());
        }

        [Fact]
        public void testAVictory_CanBePlayedOn_AndNoOtherOutcomeFollows() {
            Nation nation = makeKingdomReadyNation();
            Settlement first = simulation.getEntityRepository().getEntity(nation.getSettlement(0)) as Settlement;
            first.addFunds(VictoryRules.ProsperityFunds);
            addMembers(nation, VictoryRules.ProsperityMembers);
            for (int i = 0; i < 3; i++) {
                eras.evaluate(simulation);
            }
            Assert.Equal(RunOutcome.PROSPERITY, eras.getOutcome());
            eras.acknowledgeOutcome();
            Assert.False(eras.shouldShowOutcome());
            foreach (EntityId id in new List<EntityId>(nation.getSettlements())) {
                nation.removeSettlement(id);
            }
            for (int i = 0; i < VictoryRules.CollapseGraceTicks + 10; i++) {
                simulation.getTickCounter().increment();
            }
            eras.evaluate(simulation);
            eras.evaluate(simulation);
            Assert.Equal(RunOutcome.PROSPERITY, eras.getOutcome());
        }

        [Fact]
        public void testHistory_KeepsTheChroniclesLinesAboutThePlayer() {
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getRandom(), simulation.getNationNameGenerator()).execute(player);
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            eras.evaluate(simulation);
            Assert.Contains(eras.getHistory(), entry => !entry.isMilestone() && entry.getText().Contains(nation.getName()));
            int count = eras.getHistory().Count;
            eras.evaluate(simulation); // nothing new: nothing copied twice
            Assert.Equal(count, eras.getHistory().Count);
            Assert.Equal(nation.getName(), eras.getNationName());
        }

        [Fact]
        public void testAmbitions_RememberTheEraTheyWereMadeIn() {
            Objective first = eras.makeAmbition(0);
            makeKingdomReadyNation();
            eras.evaluate(simulation);
            Objective second = eras.makeAmbition(1);
            Assert.Equal(ObjectiveCatalog.ambition(0, Era.CAMP).getTitle(), first.getTitle());
            Assert.Equal(ObjectiveCatalog.ambition(1, Era.VILLAGE).getTitle(), second.getTitle());
            // asked again later, the first is the same goal it was
            eras.evaluate(simulation);
            Assert.Equal(first.getTitle(), eras.makeAmbition(0).getTitle());
        }

        [Fact]
        public void testTheEraState_SurvivesASave() {
            Nation nation = makeKingdomReadyNation();
            eras.evaluate(simulation);
            eras.evaluate(simulation);
            Settlement first = simulation.getEntityRepository().getEntity(nation.getSettlement(0)) as Settlement;
            first.addFunds(VictoryRules.ProsperityFunds);
            addMembers(nation, VictoryRules.ProsperityMembers);
            Assert.Equal(Era.TOWN, eras.getEra());

            Simulation loaded = SaveGame.decode(SaveGame.encode(simulation, "test"));
            EraTracker copy = loaded.getEraTracker();
            Assert.Equal(Era.TOWN, copy.getEra());
            Assert.Equal(eras.getReachedTick(Era.TOWN), copy.getReachedTick(Era.TOWN));
            Assert.Equal(eras.getHistory().Count, copy.getHistory().Count);
            Assert.Equal(eras.getPeakMembers(), copy.getPeakMembers());
            // the limits are back on the loaded nation
            Nation loadedNation = loaded.getNationRepository().getNation(loaded.getPlayer().getNationId());
            Assert.Equal(EraRules.settlementLimit(Era.TOWN), loadedNation.getSettlementLimit());
            copy.evaluate(loaded);
            Assert.Equal(RunOutcome.PROSPERITY, copy.getOutcome());
        }
    }
}
