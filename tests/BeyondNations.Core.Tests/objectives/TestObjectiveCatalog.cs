using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The objectives' predicates against real simulation states, and the
    * guarantees the catalog makes: costs come from the constants the commands
    * check, and ambitions keep rising.
    */
    public class TestObjectiveCatalog {
        private readonly Simulation simulation;
        private readonly Player player;

        public TestObjectiveCatalog() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(20261018);
            simulation = new Simulation(config);
            player = simulation.getPlayer();
        }

        private Objective arc(string id) {
            foreach (Objective objective in ObjectiveCatalog.firstArc()) {
                if (objective.getId() == id) {
                    return objective;
                }
            }
            throw new KeyNotFoundException(id);
        }

        private Nation makePlayerLeader() {
            Nation nation = new Nation("Testland", player.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            player.setNationId(nation.getId());
            return nation;
        }

        private Settlement addSettlement(Nation nation, Vector3 position) {
            Settlement settlement = new Settlement(position, nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return settlement;
        }

        [Fact]
        public void testFirstArc_IdsAreUniqueAndInTeachingOrder() {
            List<string> ids = new List<string>();
            foreach (Objective objective in ObjectiveCatalog.firstArc()) {
                Assert.DoesNotContain(objective.getId(), ids);
                Assert.False(objective.isAmbition());
                ids.Add(objective.getId());
            }
            Assert.Equal(new[] {
                "gather-wood", "eat", "join-nation", "found-settlement", "enter-settlement",
                "build-stall", "grow-nation", "sell-resources", "second-settlement",
            }, ids);
        }

        [Fact]
        public void testCosts_ReadFromTheConstantsTheCommandsCheck() {
            Assert.Equal(Settlement.WOOD_COST_TO_BUILD, arc("found-settlement").evaluate(simulation).getTarget());
            ObjectiveProgress stall = arc("build-stall").evaluate(simulation);
            Assert.Equal(Stall.WOOD_COST_TO_BUILD, stall.getTarget());
            Assert.Equal(Stall.STONE_COST_TO_BUILD, stall.getSecondTarget());
            Assert.Equal("Wood 0/" + Stall.WOOD_COST_TO_BUILD + " \u00b7 Stone 0/" + Stall.STONE_COST_TO_BUILD, stall.describe());
            Assert.Equal(Settlement.WOOD_COST_TO_BUILD / 5, ObjectiveCatalog.firstWoodTarget());
            Assert.Contains(Settlement.WOOD_COST_TO_BUILD.ToString(), arc("second-settlement").getHint(simulation));
        }

        [Fact]
        public void testNoneOfTheArcIsMetAtTheStart() {
            foreach (Objective objective in ObjectiveCatalog.firstArc()) {
                Assert.False(objective.evaluate(simulation).isComplete(), objective.getId());
            }
        }

        [Fact]
        public void testGatherWood_CountsCarriedWood() {
            player.getInventory().addItem(ItemType.WOOD, ObjectiveCatalog.firstWoodTarget() - 1);
            Assert.False(arc("gather-wood").evaluate(simulation).isComplete());
            player.getInventory().addItem(ItemType.WOOD, 1);
            Assert.True(arc("gather-wood").evaluate(simulation).isComplete());
        }

        [Fact]
        public void testEat_MetOncePlayerHasEaten() {
            player.getInventory().addItem(ItemType.APPLE, 1);
            player.setEnergy(50);
            player.fixedUpdate();
            Assert.Equal(1, player.getMealsEaten());
            Assert.True(arc("eat").evaluate(simulation).isComplete());
        }

        [Fact]
        public void testJoinNation_MetByCreatingOrJoining() {
            makePlayerLeader();
            Assert.True(arc("join-nation").evaluate(simulation).isComplete());
        }

        [Fact]
        public void testFoundSettlement_ShowsWoodButIsMetByTheSettlement() {
            Nation nation = makePlayerLeader();
            player.getInventory().addItem(ItemType.WOOD, 30);
            ObjectiveProgress before = arc("found-settlement").evaluate(simulation);
            Assert.False(before.isComplete());
            Assert.Equal("Wood 30/" + Settlement.WOOD_COST_TO_BUILD, before.describe());

            addSettlement(nation, Vector3.Zero);
            Assert.True(arc("found-settlement").evaluate(simulation).isComplete());
        }

        [Fact]
        public void testFoundSettlement_HintDependsOnLeadership() {
            Nation leaderNation = new Nation("Elsewhere", new EntityId(), simulation.getRandom());
            simulation.getNationRepository().addNation(leaderNation);
            leaderNation.addMember(player.getId());
            player.setNationId(leaderNation.getId());
            Assert.Contains("{LeaveNation}", arc("found-settlement").getHint(simulation));

            player.setNationId(null);
            makePlayerLeader();
            Assert.Contains("{FoundSettlement}", arc("found-settlement").getHint(simulation));
        }

        [Fact]
        public void testEnterSettlement_OnlyYourOwnNationsCounts() {
            Nation nation = makePlayerLeader();
            Nation other = new Nation("Otherland", new EntityId(), simulation.getRandom());
            simulation.getNationRepository().addNation(other);
            Settlement foreign = addSettlement(other, new Vector3(500, 0, 0));
            Settlement own = addSettlement(nation, Vector3.Zero);

            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, foreign);
            Assert.False(arc("enter-settlement").evaluate(simulation).isComplete());

            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, own);
            Assert.True(arc("enter-settlement").evaluate(simulation).isComplete());
        }

        [Fact]
        public void testBuildStall_MetByAStallInAnySettlementOfTheNation() {
            Nation nation = makePlayerLeader();
            Settlement settlement = addSettlement(nation, Vector3.Zero);
            Assert.False(arc("build-stall").evaluate(simulation).isComplete());
            settlement.getMarket().createStall();
            Assert.True(arc("build-stall").evaluate(simulation).isComplete());
        }

        [Fact]
        public void testBuildStall_HintAsksForStoneUntilAffordable() {
            makePlayerLeader();
            string hint = arc("build-stall").getHint(simulation);
            Assert.Contains(Stall.STONE_COST_TO_BUILD + " stone", hint);
            Assert.Contains("{BuildStall}", hint);

            player.getInventory().addItem(ItemType.WOOD, Stall.WOOD_COST_TO_BUILD);
            player.getInventory().addItem(ItemType.STONE, Stall.STONE_COST_TO_BUILD);
            Assert.True(Stall.canAffordToBuild(player.getInventory()));
            Assert.Equal("Inside your settlement, press {BuildStall}.", arc("build-stall").getHint(simulation));
        }

        [Fact]
        public void testGrowNation_CountsMembers() {
            Nation nation = makePlayerLeader();
            for (int i = 1; i < ObjectiveCatalog.NationGrowthTarget; i++) {
                Assert.False(arc("grow-nation").evaluate(simulation).isComplete());
                nation.addMember(new EntityId());
            }
            ObjectiveProgress progress = arc("grow-nation").evaluate(simulation);
            Assert.True(progress.isComplete());
            Assert.Equal("Members " + ObjectiveCatalog.NationGrowthTarget + "/" + ObjectiveCatalog.NationGrowthTarget, progress.describe());
        }

        [Fact]
        public void testSellResources_MetByASuccessfulSale() {
            Nation nation = makePlayerLeader();
            Settlement settlement = addSettlement(nation, Vector3.Zero);
            settlement.getMarket().createStall();
            Pawn merchant = new Pawn(Vector3.Zero, "Merchant", simulation.getRandom());
            simulation.getEntityRepository().addEntity(merchant);
            Stall stall = settlement.getMarket().getStallForSale();
            stall.setOwnerId(merchant.getId());
            stall.getInventory().addItem(ItemType.COIN, 500);
            player.getInventory().addItem(ItemType.STONE, 5);
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);

            SellResourcesAtMarketCommand sell = new SellResourcesAtMarketCommand(simulation.getNationRepository(), simulation.getEntityRepository());
            Assert.False(arc("sell-resources").evaluate(simulation).isComplete());
            sell.execute(player);

            Assert.Equal(1, player.getMarketSales());
            Assert.True(arc("sell-resources").evaluate(simulation).isComplete());
        }

        [Fact]
        public void testSellResources_FailedSale_NotCounted() {
            Nation nation = makePlayerLeader();
            Settlement settlement = addSettlement(nation, Vector3.Zero);
            settlement.getMarket().createStall(); // unowned: nobody to sell to
            player.getInventory().addItem(ItemType.STONE, 5);
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            new SellResourcesAtMarketCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player);
            Assert.Equal(0, player.getMarketSales());
        }

        [Fact]
        public void testSecondSettlement_CountsSettlements() {
            Nation nation = makePlayerLeader();
            addSettlement(nation, Vector3.Zero);
            Assert.False(arc("second-settlement").evaluate(simulation).isComplete());
            addSettlement(nation, new Vector3(1000, 0, 0));
            Assert.True(arc("second-settlement").evaluate(simulation).isComplete());
        }

        [Fact]
        public void testFunds_IncludesSettlementTreasuries() {
            Nation nation = makePlayerLeader();
            Settlement settlement = addSettlement(nation, Vector3.Zero);
            int coins = player.getInventory().getNumItems(ItemType.COIN);
            settlement.addFunds(75);
            Assert.Equal(coins + 75, ObjectiveCatalog.funds(simulation));
        }

        [Fact]
        public void testAmbitions_CycleCategoriesAndRiseEachLap() {
            for (int index = 0; index < ObjectiveCatalog.AmbitionCategoryCount * 5; index++) {
                Objective ambition = ObjectiveCatalog.ambition(index);
                Objective nextLap = ObjectiveCatalog.ambition(index + ObjectiveCatalog.AmbitionCategoryCount);
                Assert.True(ambition.isAmbition());
                Assert.NotEqual(ambition.getId(), nextLap.getId());
                Assert.True(nextLap.evaluate(simulation).getTarget() > ambition.evaluate(simulation).getTarget(), ambition.getTitle());
                Assert.False(ambition.getReward().isEmpty());
            }
        }

        [Fact]
        public void testAmbitions_StartBeyondTheArc() {
            Assert.Equal(ObjectiveCatalog.NationGrowthTarget + ObjectiveCatalog.NationGrowthStep, ObjectiveCatalog.ambition(0).evaluate(simulation).getTarget());
            Assert.Equal(Stall.COIN_COST_TO_PURCHASE * 5, ObjectiveCatalog.ambition(1).evaluate(simulation).getTarget());
            Assert.Equal(ObjectiveCatalog.ArcSettlementTarget + 1, ObjectiveCatalog.ambition(2).evaluate(simulation).getTarget());
            Assert.Equal("Survive to day 2", ObjectiveCatalog.ambition(3).getTitle());
        }

        [Fact]
        public void testDaysAmbition_TracksTimeOfDay() {
            Objective days = ObjectiveCatalog.ambition(3);
            Assert.False(days.evaluate(simulation).isComplete());
            int ticksPerDay = simulation.getTimeOfDay().getTicksPerDay();
            for (int i = 0; i < ticksPerDay * 2; i++) {
                simulation.getTickCounter().increment();
            }
            Assert.True(days.evaluate(simulation).isComplete());
        }

        [Fact]
        public void testPredicates_PlayerWithoutNation_ReadZero() {
            Assert.Equal(0, ObjectiveCatalog.members(simulation));
            Assert.Equal(0, ObjectiveCatalog.settlements(simulation));
            Assert.Equal(0, ObjectiveCatalog.nationStalls(simulation));
            Assert.False(ObjectiveCatalog.isLeader(simulation));
            Assert.False(ObjectiveCatalog.isInOwnSettlement(simulation));
        }
    }
}
