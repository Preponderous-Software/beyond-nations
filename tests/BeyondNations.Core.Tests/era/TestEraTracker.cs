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

        /** Runs the clock (and only the clock) on to the given day, counted from 1. */
        private void passToDay(int day) {
            while (EraTracker.dayOf(simulation) < day) {
                simulation.getTickCounter().increment();
            }
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
            first.getMarket().createStall();
            first.addFunds(EraRules.KingdomFunds);
            addMembers(nation, EraRules.KingdomMembers);
            passToDay(EraRules.KingdomDay);
            return nation;
        }

        [Fact]
        public void testANewRun_IsACampWithNoOutcome() {
            Assert.Equal(Era.CAMP, eras.getEra());
            Assert.Equal(RunOutcome.NONE, eras.getOutcome());
            Assert.Equal(0, eras.getReachedTick(Era.CAMP));
            Assert.Equal(-1, eras.getReachedTick(Era.VILLAGE));
            Assert.Equal(4, eras.getRequirements(simulation).Count);
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
            settlement.getMarket().createStall();
            settlement.getMarket().createStall();
            eras.evaluate(simulation);
            Assert.Equal(Era.CAMP, eras.getEra()); // the first night has not passed
            passToDay(EraRules.VillageDay);

            settlement.getMarket().createStall();
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
        public void testAYoungNationThePlayerLeads_CannotBeBesieged_UntilTheTownEra() {
            Nation nation = makeKingdomReadyNation();
            Settlement target = simulation.getEntityRepository().getEntity(nation.getSettlement(EraRules.KingdomTowns)) as Settlement;
            Assert.NotEqual(SettlementTier.TOWN, target.getTier());
            Nation attacker = makePawnNation("Ironhold");
            Settlement base_ = addSettlement(attacker, new Vector3(0, 1, 6000));
            addMembers(attacker, 60);
            Diplomacy diplomacy = simulation.getDiplomacy();

            eras.applyRules(simulation);
            Assert.True(nation.isSheltered());
            Assert.False(diplomacy.canBesiege(attacker, nation, base_, target));
            Assert.False(attacker.isSheltered()); // a pawn-led nation never is
            eras.evaluate(simulation); // village
            Assert.True(nation.isSheltered());
            eras.evaluate(simulation); // town: its towns have palisades, the rest can fall
            Assert.Equal(Era.TOWN, eras.getEra());
            Assert.False(nation.isSheltered());
            Assert.True(diplomacy.canBesiege(attacker, nation, base_, target));
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
            // the shelter is the player's, led or not: it goes only when the player does
            Assert.True(nation.isSheltered());
            nation.removeMember(player.getId());
            player.setNationId(null);
            eras.applyRules(simulation);
            Assert.False(nation.isSheltered());
        }

        [Fact]
        public void testANationThePlayerHasJoined_IsShelteredUntilTheTownEra_ButNotLimited() {
            Nation nation = makePawnNation("Joinland");
            Settlement settlement = addSettlement(nation, Vector3.Zero);
            Nation stranger = makePawnNation("Farland");
            nation.addMember(player.getId());
            player.setNationId(nation.getId());
            eras.applyRules(simulation);
            Assert.True(nation.isSheltered());
            Assert.False(stranger.isSheltered()); // a nation without the player is untouched
            Assert.Equal(0, nation.getSettlementLimit());
            Assert.Null(settlement.getBuildingEra());
        }

        /** A nation at the Town era with a village beside a far stronger pawn-led neighbour that is hostile to it. */
        private Settlement[] makeTownEraNationBesideAHostileGiant(out Nation nation, out Nation attacker) {
            nation = makeKingdomReadyNation();
            Settlement target = simulation.getEntityRepository().getEntity(nation.getSettlement(EraRules.KingdomTowns)) as Settlement;
            Assert.NotEqual(SettlementTier.TOWN, target.getTier());
            attacker = makePawnNation("Ironhold");
            Settlement base_ = addSettlement(attacker, target.getPosition() + new Vector3(0, 0, 150));
            build(base_, SettlementTiers.TOWN_BUILDINGS);
            addMembers(attacker, 120);
            simulation.getNationRepository().getRelations().adjust(nation, attacker, -100);
            eras.evaluate(simulation); // village
            eras.evaluate(simulation); // town
            Assert.Equal(Era.TOWN, eras.getEra());
            return new[] { target, base_ };
        }

        /** Runs the clock on by a diplomacy round and runs the round, as the simulation would. */
        private void passARound() {
            for (int i = 0; i < Diplomacy.ROUND_TICKS; i++) {
                simulation.getTickCounter().increment();
                eras.update(simulation);
            }
            simulation.getDiplomacy().runRound();
        }

        [Fact]
        public void testWhenTheTownEraComes_NoSiegeMayBeginUntilTheWarningHasRunOut() {
            Nation nation;
            Nation attacker;
            Settlement[] sites = makeTownEraNationBesideAHostileGiant(out nation, out attacker);
            Settlement target = sites[0];
            Diplomacy diplomacy = simulation.getDiplomacy();
            Assert.False(nation.isSheltered());
            Assert.True(nation.isForewarned());
            Assert.Equal(EraRules.SiegeWarningTicks, eras.getSiegeWarningTicksLeft(simulation));
            // strong enough, but the player has been given warning
            Assert.True(diplomacy.canBesiege(attacker, nation, sites[1], target));
            Assert.False(diplomacy.mayBeginSiege(attacker, nation, target));

            int rounds = 0;
            while (eras.getSiegeWarningTicksLeft(simulation) > 0) {
                passARound();
                rounds++;
                Assert.True(eras.getSiegeWarningTicksLeft(simulation) <= 0 || diplomacy.getBesieger(target) == null,
                    "a siege began with " + eras.getSiegeWarningTicksLeft(simulation) + " ticks of warning left");
            }
            Assert.Equal(EraRules.SiegeWarningTicks / Diplomacy.ROUND_TICKS, rounds);
            Assert.False(nation.isForewarned());
            Assert.Equal(-1, eras.getSiegeWarningTicksLeft(simulation));
            Assert.True(diplomacy.mayBeginSiege(attacker, nation, target));
            passARound();
            Assert.Equal(attacker, diplomacy.getBesieger(target));
        }

        [Fact]
        public void testTheEraAnnouncement_IsNotLost_WhenTheReachTheEraObjectiveCompletesOnTheSameTick() {
            // The Simulation evaluates the era and then the objective on the
            // same tick, so "Reach the Town era" completes the moment the era
            // is announced, and its message used to replace the announcement
            // (and the siege warning in it) before a frame was drawn.
            makeKingdomReadyNation();
            eras.evaluate(simulation);
            Assert.Equal(Era.VILLAGE, eras.getEra());
            simulation.getTickCounter().increment();
            ObjectiveTracker objectives = new ObjectiveTracker(new[] { ObjectiveCatalog.ambition(3, Era.VILLAGE) }, null);
            Assert.Equal("Reach the Town era", objectives.getCurrent().getTitle());
            Assert.False(objectives.evaluate(simulation));

            // one step, as Simulation.fixedUpdate runs it
            simulation.getTickCounter().increment();
            player.getStatus().beginTick();
            eras.evaluate(simulation);
            Assert.True(objectives.evaluate(simulation));
            player.getStatus().endTick();

            string status = player.getStatus().getStatus();
            Assert.StartsWith("A new era: Testland is now a Town.", status);
            Assert.Contains("hostile neighbours may besiege its villages in 5 minutes", status);
            Assert.Contains("Objective complete: Reach the Town era.", status);
            Assert.DoesNotContain("Village.", status); // last second's announcement is not repeated
        }

        [Fact]
        public void testTheSiegeWarning_IsOnTheStatusLine_AtTheTownEra_AMinuteBefore_AndAtItsEnd() {
            Nation nation;
            Nation attacker;
            makeTownEraNationBesideAHostileGiant(out nation, out attacker);
            Assert.StartsWith("A new era: Testland is now a Town.", player.getStatus().getStatus());
            Assert.Contains("hostile neighbours may besiege its villages in 5 minutes", player.getStatus().getStatus());

            int ticksPerSecond = simulation.getTickCounter().getTicksPerSecond();
            while (eras.getSiegeWarningTicksLeft(simulation) > 60 * ticksPerSecond) {
                simulation.getTickCounter().increment();
                eras.update(simulation);
            }
            Assert.StartsWith("In 1 minute hostile neighbours may besiege your villages.", player.getStatus().getStatus());
            while (eras.getSiegeWarningTicksLeft(simulation) > 0) {
                simulation.getTickCounter().increment();
                eras.update(simulation);
            }
            Assert.StartsWith("Your nation's shelter is over: hostile neighbours may now besiege its villages.", player.getStatus().getStatus());
        }

        [Fact]
        public void testASiegeAlreadyUnderWay_GoesOn_DuringTheWarning() {
            // the diplomacy showcase stages its siege, then the Town era
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation staged = new Simulation(config);
            DiplomacyShowcase.Staged s = DiplomacyShowcase.stage(staged);
            Assert.True(s.playerNation.isForewarned());
            Assert.Equal(s.hostile, staged.getDiplomacy().getBesieger(s.playerSettlement));
            int progress = staged.getDiplomacy().getSiegeProgress(s.playerSettlement);
            staged.getDiplomacy().runRound();
            Assert.Equal(progress + 1, staged.getDiplomacy().getSiegeProgress(s.playerSettlement));
        }

        [Fact]
        public void testTheSiegeWarning_SurvivesASave() {
            Nation nation;
            Nation attacker;
            makeTownEraNationBesideAHostileGiant(out nation, out attacker);
            for (int i = 0; i < 1000; i++) {
                simulation.getTickCounter().increment();
            }
            int left = eras.getSiegeWarningTicksLeft(simulation);
            Assert.Equal(EraRules.SiegeWarningTicks - 1000, left);
            Simulation loaded = SaveGame.decode(SaveGame.encode(simulation, "test"));
            Assert.Equal(left, loaded.getEraTracker().getSiegeWarningTicksLeft(loaded));
            Nation loadedNation = loaded.getNationRepository().getNation(loaded.getPlayer().getNationId());
            Assert.True(loadedNation.isForewarned());
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
