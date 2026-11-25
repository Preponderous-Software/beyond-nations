using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.progression;

namespace beyondnationstests.desktop.ui {

    /**
    * Unlock as you go (HudUnlocks): each rule's condition, the objective
    * safety net, announcing once, never re-locking, a loaded world counting
    * what holds as announced, the command bar's labels, and that watching
    * the unlocks leaves the simulation stepping exactly as before.
    */
    public class TestHudUnlocks {

        private static UnlockSnapshot blank() {
            return new UnlockSnapshot();
        }

        private static bool holds(UnlockId id, UnlockSnapshot snapshot) {
            return HudUnlocks.ruleFor(id).holds(snapshot);
        }

        private static Simulation newWorld(int seed = 42) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            return new Simulation(config);
        }

        // --- the table ---

        [Fact]
        public void testEveryUnlockButBasicsHasOneRuleWithACard() {
            foreach (UnlockId id in Enum.GetValues(typeof(UnlockId))) {
                int rules = HudUnlocks.Rules.Count(r => r.id == id);
                Assert.Equal(id == UnlockId.Basics ? 0 : 1, rules);
            }
            foreach (HudUnlocks.Rule rule in HudUnlocks.Rules) {
                Assert.False(string.IsNullOrWhiteSpace(rule.title));
                Assert.False(string.IsNullOrWhiteSpace(rule.icon));
                Assert.InRange(rule.text.Split(". ").Length, 1, 2);
            }
        }

        [Fact]
        public void testCardsNameNoKeys() {
            foreach (HudUnlocks.Rule rule in HudUnlocks.Rules) {
                string text = rule.title + " " + rule.text;
                Assert.DoesNotContain("{", text);
                Assert.DoesNotContain("press", text.ToLowerInvariant());
                Assert.DoesNotContain(" key", text.ToLowerInvariant());
                Assert.DoesNotContain("click", text.ToLowerInvariant());
                Assert.DoesNotContain("tap ", text.ToLowerInvariant());
            }
        }

        [Fact]
        public void testNothingHoldsInABlankSnapshot() {
            foreach (HudUnlocks.Rule rule in HudUnlocks.Rules) {
                Assert.False(rule.holds(blank()), rule.id + " held with nothing done");
            }
        }

        [Fact]
        public void testNation_ComesWithTheFirstWoodStoneOrMeal() {
            UnlockSnapshot s = blank(); s.wood = 1;
            Assert.True(holds(UnlockId.Nation, s));
            s = blank(); s.stone = 1;
            Assert.True(holds(UnlockId.Nation, s));
            s = blank(); s.mealsEaten = 1;
            Assert.True(holds(UnlockId.Nation, s));
            s = blank(); s.inNation = true;
            Assert.True(holds(UnlockId.Nation, s));
        }

        [Fact]
        public void testCrafting_ComesWhenAToolCanBeMadeOrIsCarried() {
            UnlockSnapshot s = blank(); s.canCraftTool = true;
            Assert.True(holds(UnlockId.Crafting, s));
            s = blank(); s.hasTool = true;
            Assert.True(holds(UnlockId.Crafting, s));
        }

        [Fact]
        public void testFounding_ComesToALeader() {
            UnlockSnapshot s = blank(); s.inNation = true;
            Assert.False(holds(UnlockId.Founding, s));
            s.leads = true;
            Assert.True(holds(UnlockId.Founding, s));
        }

        [Fact]
        public void testSettlement_ComesWithAHomeOrOneInReach() {
            UnlockSnapshot s = blank(); s.nearSettlement = true;
            Assert.True(holds(UnlockId.Settlement, s));
            s = blank(); s.hasHome = true;
            Assert.True(holds(UnlockId.Settlement, s));
            s = blank(); s.inSettlement = true;
            Assert.True(holds(UnlockId.Settlement, s));
        }

        [Fact]
        public void testEra_ComesWithTheFirstSettlement() {
            UnlockSnapshot s = blank(); s.inNation = true; s.leads = true;
            Assert.False(holds(UnlockId.Era, s));
            s.settlements = 1;
            Assert.True(holds(UnlockId.Era, s));
            s = blank(); s.era = Era.VILLAGE;
            Assert.True(holds(UnlockId.Era, s));
        }

        [Fact]
        public void testSettlers_ComeToANationWithAHome() {
            UnlockSnapshot s = blank(); s.inNation = true;
            Assert.False(holds(UnlockId.Settlers, s));
            s.hasHome = true;
            Assert.True(holds(UnlockId.Settlers, s));
        }

        [Fact]
        public void testBuilding_ComesInsideASettlementThePlayerManages() {
            UnlockSnapshot s = blank(); s.inSettlement = true; s.inOwnSettlement = true;
            Assert.False(holds(UnlockId.Building, s));
            s.managesHere = true;
            Assert.True(holds(UnlockId.Building, s));
            s = blank(); s.nationStalls = 1;
            Assert.True(holds(UnlockId.Building, s));
        }

        [Fact]
        public void testMarket_ComesWithAStall() {
            UnlockSnapshot s = blank(); s.inSettlement = true;
            Assert.False(holds(UnlockId.Market, s));
            s.marketHere = true;
            Assert.True(holds(UnlockId.Market, s));
            s = blank(); s.nationStalls = 1;
            Assert.True(holds(UnlockId.Market, s));
        }

        [Fact]
        public void testMembers_ComeToALeaderWithAMember() {
            UnlockSnapshot s = blank(); s.inNation = true; s.leads = true; s.members = 1;
            Assert.False(holds(UnlockId.Members, s));
            s.members = 2;
            Assert.True(holds(UnlockId.Members, s));
            s.leads = false;
            Assert.False(holds(UnlockId.Members, s));
        }

        [Fact]
        public void testTreasury_ComesWithSomethingToCollect() {
            UnlockSnapshot s = blank(); s.treasuryToCollect = true;
            Assert.True(holds(UnlockId.Treasury, s));
        }

        [Fact]
        public void testLaws_ComeToTheLeaderOfAVillageOrOfFive() {
            UnlockSnapshot s = blank(); s.leads = true; s.members = 4;
            Assert.False(holds(UnlockId.Laws, s));
            s.members = 5;
            Assert.True(holds(UnlockId.Laws, s));
            s = blank(); s.leads = true; s.era = Era.VILLAGE;
            Assert.True(holds(UnlockId.Laws, s));
        }

        [Fact]
        public void testCaravans_ComeWithTheVillageEraOrACaravanInReach() {
            UnlockSnapshot s = blank(); s.era = Era.VILLAGE;
            Assert.True(holds(UnlockId.Caravans, s));
            s = blank(); s.caravanInReach = true;
            Assert.True(holds(UnlockId.Caravans, s));
            s = blank(); s.playerCaravans = 1;
            Assert.True(holds(UnlockId.Caravans, s));
        }

        [Fact]
        public void testDiplomacy_ComesToALeaderOnForeignLandOrWithAHostileNeighbour() {
            UnlockSnapshot s = blank(); s.onForeignClaim = true;
            Assert.False(holds(UnlockId.Diplomacy, s));
            s.leads = true;
            Assert.True(holds(UnlockId.Diplomacy, s));
            s = blank(); s.leads = true; s.hostileNeighbour = true;
            Assert.True(holds(UnlockId.Diplomacy, s));
        }

        [Fact]
        public void testWinter_ComesInAutumn() {
            UnlockSnapshot s = blank(); s.season = Season.SUMMER;
            Assert.False(holds(UnlockId.Winter, s));
            s.season = Season.AUTUMN;
            Assert.True(holds(UnlockId.Winter, s));
            s.season = Season.WINTER;
            Assert.True(holds(UnlockId.Winter, s));
        }

        [Fact]
        public void testFeeding_ComesWithHungryResidentsOrAutumnInANation() {
            UnlockSnapshot s = blank(); s.hungryHere = 1;
            Assert.True(holds(UnlockId.Feeding, s));
            s = blank(); s.season = Season.AUTUMN;
            Assert.False(holds(UnlockId.Feeding, s));
            s.inNation = true;
            Assert.True(holds(UnlockId.Feeding, s));
        }

        [Fact]
        public void testSaplings_ComeWithASaplingAndAHome() {
            UnlockSnapshot s = blank(); s.hasSapling = true;
            Assert.False(holds(UnlockId.Saplings, s));
            s.hasHome = true;
            Assert.True(holds(UnlockId.Saplings, s));
        }

        [Fact]
        public void testSafetyNet_AnObjectiveThatPointsAtAnUnlockUnlocksIt() {
            foreach (HudUnlocks.Rule rule in HudUnlocks.Rules) {
                foreach (string objective in rule.neededBy) {
                    UnlockSnapshot s = blank();
                    s.objectivesReached.Add(objective);
                    Assert.True(rule.holds(s), rule.id + " not unlocked by " + objective);
                    Assert.False(rule.when(s));
                }
            }
        }

        [Fact]
        public void testNeededBy_NamesRealObjectives() {
            HashSet<string> ids = new HashSet<string>(ObjectiveCatalog.firstArc().Select(o => o.getId()));
            foreach (HudUnlocks.Rule rule in HudUnlocks.Rules) {
                foreach (string objective in rule.neededBy) {
                    Assert.Contains(objective, ids);
                }
            }
        }

        // --- announcing ---

        [Fact]
        public void testAnnouncesOnce() {
            HudUnlocks unlocks = new HudUnlocks();
            UnlockSnapshot s = blank(); s.wood = 3;
            Assert.Equal(new[] { UnlockId.Nation }, unlocks.observe(s));
            Assert.Empty(unlocks.observe(s));
            List<UnlockCard> cards = unlocks.drainNewCards().ToList();
            Assert.Single(cards);
            Assert.Equal(UnlockId.Nation, cards[0].id);
            Assert.Equal(HudUnlocks.ruleFor(UnlockId.Nation).text, cards[0].text);
            Assert.Empty(unlocks.drainNewCards());
            unlocks.observe(s);
            Assert.Empty(unlocks.drainNewCards());
        }

        [Fact]
        public void testSeveralAtOnceComeInTableOrder() {
            HudUnlocks unlocks = new HudUnlocks();
            UnlockSnapshot s = blank(); s.wood = 3; s.canCraftTool = true; s.inNation = true; s.leads = true;
            unlocks.observe(s);
            Assert.Equal(new[] { UnlockId.Nation, UnlockId.Crafting, UnlockId.Founding }, unlocks.drainNewCards().Select(c => c.id).ToArray());
        }

        [Fact]
        public void testNeverRelocks() {
            HudUnlocks unlocks = new HudUnlocks();
            UnlockSnapshot s = blank(); s.hasSapling = true; s.hasHome = true;
            unlocks.observe(s);
            unlocks.observe(blank());
            Assert.True(unlocks.isUnlocked(UnlockId.Saplings));
        }

        [Fact]
        public void testBasicsAreAlwaysUnlocked() {
            HudUnlocks unlocks = new HudUnlocks();
            Assert.True(unlocks.isUnlocked(UnlockId.Basics));
            Assert.True(unlocks.commandUnlocked("Eat Apple (+10)"));
            Assert.True(unlocks.commandUnlocked("Deal with Bandits (Oakford)"));
            Assert.True(unlocks.commandUnlocked("Some Button Added Later"));
        }

        [Fact]
        public void testNewWorld_StartsBare() {
            Simulation simulation = newWorld();
            HudUnlocks unlocks = new HudUnlocks();
            unlocks.begin(simulation, true);
            unlocks.update(simulation);
            foreach (HudUnlocks.Rule rule in HudUnlocks.Rules) {
                Assert.False(unlocks.isUnlocked(rule.id), rule.id + " unlocked in a new world");
            }
            Assert.False(unlocks.commandUnlocked("Create Nation"));
            Assert.Empty(unlocks.drainNewCards());
        }

        [Fact]
        public void testNewWorld_FirstWoodUnlocksNationsWithACard() {
            Simulation simulation = newWorld();
            HudUnlocks unlocks = new HudUnlocks();
            unlocks.begin(simulation, true);
            simulation.getPlayer().getInventory().addItem(ItemType.WOOD, 1);
            unlocks.update(simulation);
            Assert.True(unlocks.commandUnlocked("Create Nation"));
            Assert.Equal(UnlockId.Nation, unlocks.drainNewCards().Single().id);
        }

        [Fact]
        public void testLoadedWorld_WhatHoldsCountsAsAnnounced() {
            Settlement settlement;
            Simulation simulation = newWorld();
            settlement = SettlementShowcase.stage(simulation);
            HudUnlocks unlocks = new HudUnlocks();
            unlocks.begin(simulation, false);
            Assert.True(unlocks.isUnlocked(UnlockId.Nation));
            Assert.True(unlocks.isUnlocked(UnlockId.Founding));
            Assert.True(unlocks.isUnlocked(UnlockId.Settlement));
            Assert.True(unlocks.isUnlocked(UnlockId.Market));
            Assert.Empty(unlocks.drainNewCards());

            // what comes later still comes with its card
            Assert.False(unlocks.isUnlocked(UnlockId.Saplings));
            simulation.getPlayer().getInventory().addItem(ItemType.SAPLING, 1);
            unlocks.update(simulation);
            Assert.Contains(UnlockId.Saplings, unlocks.drainNewCards().Select(c => c.id));
        }

        [Fact]
        public void testAllUnlocked_UnlocksEverythingAndAnnouncesNothing() {
            HudUnlocks unlocks = HudUnlocks.allUnlocked();
            foreach (UnlockId id in Enum.GetValues(typeof(UnlockId))) {
                Assert.True(unlocks.isUnlocked(id));
            }
            Assert.True(unlocks.commandUnlocked("Create Nation"));
            Assert.True(unlocks.commandUnlocked("Send Caravan"));
            unlocks.update(newWorld());
            Assert.Empty(unlocks.observe(blank()));
            Assert.Empty(unlocks.drainNewCards());
            Assert.True(unlocks.isEverythingUnlocked());
        }

        // --- the command bar ---

        [Fact]
        public void testLabelConstantsMatchTheCommandBar() {
            Assert.Equal(WorldCommandBar.SettlementLabel, HudUnlocks.SettlementLabel);
            Assert.Equal(MarketTradePanel.OpenLabel, HudUnlocks.TradeAtMarketLabel);
            Assert.Equal(CaravanCommands.SendCaravanLabel, HudUnlocks.SendCaravanLabel);
        }

        [Theory]
        [InlineData("Create Nation", UnlockId.Nation)]
        [InlineData("Join Nation", UnlockId.Nation)]
        [InlineData("Leave Nation", UnlockId.Nation)]
        [InlineData("Found Settlement", UnlockId.Founding)]
        [InlineData("Enter Settlement", UnlockId.Settlement)]
        [InlineData("Exit Settlement", UnlockId.Settlement)]
        [InlineData("Teleport Home", UnlockId.Settlement)]
        [InlineData("Settlement", UnlockId.Settlement)]
        [InlineData("Build Stall", UnlockId.Building)]
        [InlineData("Build Storehouse", UnlockId.Building)]
        [InlineData("Purchase Food", UnlockId.Market)]
        [InlineData("Sell Resources", UnlockId.Market)]
        [InlineData("Trade at Market", UnlockId.Market)]
        [InlineData("Purchase Stall", UnlockId.Market)]
        [InlineData("Transfer Items", UnlockId.Market)]
        [InlineData("Collect Coins (4)", UnlockId.Market)]
        [InlineData("Collect Food (2)", UnlockId.Market)]
        [InlineData("Withdraw Funds", UnlockId.Treasury)]
        [InlineData("Collect Stockpile (12)", UnlockId.Treasury)]
        [InlineData("Feed Residents (3 hungry)", UnlockId.Feeding)]
        [InlineData("Plant Sapling", UnlockId.Saplings)]
        [InlineData("Talk to Ada Smith (Stranger)", UnlockId.Settlers)]
        [InlineData("Gift Ada Smith", UnlockId.Settlers)]
        [InlineData("Invite Ada Smith", UnlockId.Settlers)]
        [InlineData("Order Ada Smith: Gather Wood", UnlockId.Members)]
        [InlineData("Appoint Ada Smith Merchant", UnlockId.Members)]
        [InlineData("Appoint Ada Smith Trader", UnlockId.Members)]
        [InlineData("Craft Axe", UnlockId.Crafting)]
        [InlineData("Buy Pickaxe (12)", UnlockId.Crafting)]
        [InlineData("Send Caravan", UnlockId.Caravans)]
        [InlineData("Send Caravan (Village era)", UnlockId.Caravans)]
        [InlineData("Buy Wood (3)", UnlockId.Caravans)]
        [InlineData("Sell to Your Caravan", UnlockId.Caravans)]
        [InlineData("Escort Caravan", UnlockId.Caravans)]
        [InlineData("Raid Caravan", UnlockId.Caravans)]
        [InlineData("Gift Rivermark 50 Coins", UnlockId.Diplomacy)]
        [InlineData("Propose Alliance to Rivermark", UnlockId.Diplomacy)]
        [InlineData("Offer Peace to Rivermark", UnlockId.Diplomacy)]
        [InlineData("Denounce Rivermark", UnlockId.Diplomacy)]
        [InlineData("Declare War on Rivermark", UnlockId.Diplomacy)]
        public void testCommandLabels(string label, UnlockId expected) {
            Assert.Equal(expected, HudUnlocks.unlockForCommand(label));
            HudUnlocks unlocks = new HudUnlocks();
            Assert.False(unlocks.commandUnlocked(label));
            Assert.True(HudUnlocks.allUnlocked().commandUnlocked(label));
        }

        [Fact]
        public void testEveryButtonOfAStagedTownBelongsToAnUnlock() {
            Simulation simulation = newWorld();
            Settlement settlement = SettlementShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            player.getInventory().addItem(ItemType.WOOD, 100);
            player.getInventory().addItem(ItemType.STONE, 100);
            player.getInventory().addItem(ItemType.APPLE, 3);
            player.getInventory().addItem(ItemType.SAPLING, 1);
            List<string> outside = WorldCommandBar.getCommands(simulation).Select(c => c.getLabel()).ToList();
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            List<string> inside = WorldCommandBar.getCommands(simulation).Select(c => c.getLabel()).ToList();
            foreach (string label in outside.Concat(inside)) {
                if (label.StartsWith("Eat")) {
                    continue;
                }
                Assert.True(HudUnlocks.unlockForCommand(label) != null, "no unlock for \"" + label + "\"");
            }
            Assert.Contains("Build House", inside);
        }

        // --- presentation only ---

        /**
        * Every entity in order with its exact position, and the player's
        * inventory, nations and era: ids are Guids no seed reproduces, so
        * two worlds are compared by everything else.
        */
        private static string fingerprint(Simulation simulation) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            EntityRepository entities = simulation.getEntityRepository();
            for (int i = 0; i < entities.getEntityCount(); i++) {
                Entity entity = entities.getEntityAt(i);
                System.Numerics.Vector3 p = entity.getPosition();
                sb.Append(entity.getType()).Append(' ').Append(BitConverter.SingleToInt32Bits(p.X)).Append(',')
                  .Append(BitConverter.SingleToInt32Bits(p.Y)).Append(',').Append(BitConverter.SingleToInt32Bits(p.Z)).Append('\n');
            }
            Inventory inventory = simulation.getPlayer().getInventory();
            foreach (ItemType type in Enum.GetValues(typeof(ItemType))) {
                sb.Append(type).Append('=').Append(inventory.getNumItems(type)).Append(' ');
            }
            foreach (Nation nation in simulation.getNationRepository().getNations()) {
                sb.Append(nation.getName()).Append(':').Append(nation.getNumberOfMembers()).Append('/').Append(nation.getNumberOfSettlements()).Append(' ');
            }
            sb.Append(simulation.getEraTracker().getEra()).Append(' ').Append(simulation.getTickCounter().getTotalTicks());
            return sb.ToString();
        }

        [Fact]
        public void testWatchingUnlocksLeavesTheSimulationUnchanged() {
            Simulation watched = newWorld(7);
            Simulation alone = newWorld(7);
            HudUnlocks unlocks = new HudUnlocks();
            unlocks.begin(watched, true);
            float dt = 1f / watched.getGameConfig().getTicksPerSecond();
            for (int i = 0; i < 3000; i++) {
                watched.fixedUpdate(dt);
                alone.fixedUpdate(dt);
                unlocks.update(watched);
                UnlockSnapshot.of(watched);
            }
            Assert.Equal(fingerprint(alone), fingerprint(watched));
        }
    
        [Fact]
        public void testYoungWorld_OnlyACampUnderTenMinutes() {
            Assert.True(HudUnlocks.isYoungWorld(Era.CAMP, 0, 50));
            Assert.True(HudUnlocks.isYoungWorld(Era.CAMP, 599 * 50, 50));
            Assert.False(HudUnlocks.isYoungWorld(Era.CAMP, 600 * 50, 50));
            Assert.False(HudUnlocks.isYoungWorld(Era.VILLAGE, 10, 50));
            Assert.False(HudUnlocks.isYoungWorld((Simulation) null));
        }
}
}
