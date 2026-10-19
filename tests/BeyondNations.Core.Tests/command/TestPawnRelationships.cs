using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Talking, gifting, inviting and the leader's orders: the commands that
    * make the player's relationships with pawns matter.
    */
    public class TestPawnRelationships {
        private readonly RandomSource random = new RandomSource(20261018);

        private readonly EntityRepository entityRepository;
        private readonly Environment environment;
        private readonly NationRepository nationRepository;
        private readonly EventRepository eventRepository;
        private readonly EventProducer eventProducer;
        private readonly TickCounter tickCounter;
        private readonly Player player;

        public TestPawnRelationships() {
            entityRepository = new EntityRepository(random);
            environment = new Environment(5, 5, entityRepository, random);
            nationRepository = new NationRepository(random);
            eventRepository = new EventRepository();
            eventProducer = new EventProducer(eventRepository);
            tickCounter = new TickCounter();

            player = new Player(5, 10, tickCounter, 500, 200, random);
            player.setPosition(Vector3.Zero);
            player.getInventory().clear();
            entityRepository.addEntity(player);
        }

        private Pawn addPawn(string name, Vector3 position) {
            Pawn pawn = new Pawn(position, name, random);
            entityRepository.addEntity(pawn);
            return pawn;
        }

        private Pawn addPawn(string name) {
            return addPawn(name, new Vector3(2, 0, 0));
        }

        private Nation playerNation() {
            Nation nation = new Nation("Homeland", player.getId(), random);
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            return nation;
        }

        private Nation pawnNation(string name, Pawn leader) {
            Nation nation = new Nation(name, leader.getId(), random);
            nationRepository.addNation(nation);
            leader.setNationId(nation.getId());
            return nation;
        }

        private void join(Nation nation, Pawn pawn) {
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());
        }

        private Settlement addSettlement(Nation nation) {
            Settlement settlement = new Settlement(new Vector3(100, 0, 100), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return settlement;
        }

        private void setRelationship(Pawn pawn, int value) {
            pawn.increaseRelationship(player, value);
            player.increaseRelationship(pawn, value);
        }

        private void advanceTicks(int ticks) {
            for (int i = 0; i < ticks; i++) {
                tickCounter.increment();
            }
        }

        private string status() {
            return player.getStatus().getStatus();
        }

        // --- tiers ---

        [Theory]
        [InlineData(-3, RelationshipTier.STRANGER)]
        [InlineData(0, RelationshipTier.STRANGER)]
        [InlineData(4, RelationshipTier.STRANGER)]
        [InlineData(5, RelationshipTier.ACQUAINTANCE)]
        [InlineData(14, RelationshipTier.ACQUAINTANCE)]
        [InlineData(15, RelationshipTier.FRIEND)]
        [InlineData(29, RelationshipTier.FRIEND)]
        [InlineData(30, RelationshipTier.LOYAL)]
        [InlineData(500, RelationshipTier.LOYAL)]
        public void testTierFromValue(int value, RelationshipTier expected) {
            Assert.Equal(expected, RelationshipTiers.fromValue(value));
        }

        [Fact]
        public void testTierNames() {
            Assert.Equal("Stranger", RelationshipTiers.getName(RelationshipTier.STRANGER));
            Assert.Equal("Acquaintance", RelationshipTiers.getName(RelationshipTier.ACQUAINTANCE));
            Assert.Equal("Friend", RelationshipTiers.getName(RelationshipTier.FRIEND));
            Assert.Equal("Loyal", RelationshipTiers.getName(RelationshipTier.LOYAL));
        }

        [Fact]
        public void testFindPawnInRange_OnlyWithinReachAndOutsideSettlements() {
            Pawn far = addPawn("Far", new Vector3(20, 0, 0));
            Assert.Null(PawnRelations.findPawnInRange(player, environment));

            Pawn near = addPawn("Near", new Vector3(3, 0, 0));
            Assert.Same(near, PawnRelations.findPawnInRange(player, environment));

            player.setCurrentSettlementId(new EntityId());
            Assert.Null(PawnRelations.findPawnInRange(player, environment));
        }

        // --- talking ---

        [Fact]
        public void testTalk_FirstConversation_RaisesRelationshipOnBothSides() {
            Pawn pawn = addPawn("Ada");

            new TalkToPawnCommand(nationRepository, eventProducer, tickCounter).execute(player, pawn);

            Assert.Equal(PawnRelations.TALK_RELATIONSHIP_GAIN, PawnRelations.getRelationship(pawn, player));
            Assert.Equal(PawnRelations.TALK_RELATIONSHIP_GAIN, player.getRelationships()[pawn.getId()]);
            Assert.Equal(1, eventRepository.getNumberOfEvents(EventType.PawnRelationshipIncrease));
            Assert.StartsWith("Ada (Stranger): \"Who are you?", status());
        }

        [Fact]
        public void testTalk_WithinCooldown_DoesNotRaiseAgain() {
            Pawn pawn = addPawn("Ada");
            TalkToPawnCommand talk = new TalkToPawnCommand(nationRepository, eventProducer, tickCounter);

            talk.execute(player, pawn);
            advanceTicks(PawnRelations.TALK_COOLDOWN_TICKS - 1);
            talk.execute(player, pawn);

            Assert.Equal(PawnRelations.TALK_RELATIONSHIP_GAIN, PawnRelations.getRelationship(pawn, player));
        }

        [Fact]
        public void testTalk_AfterCooldown_RaisesAgain() {
            Pawn pawn = addPawn("Ada");
            TalkToPawnCommand talk = new TalkToPawnCommand(nationRepository, eventProducer, tickCounter);

            talk.execute(player, pawn);
            advanceTicks(PawnRelations.TALK_COOLDOWN_TICKS);
            talk.execute(player, pawn);

            Assert.Equal(2 * PawnRelations.TALK_RELATIONSHIP_GAIN, PawnRelations.getRelationship(pawn, player));
        }

        [Fact]
        public void testTalk_CooldownIsPerPawn() {
            Pawn ada = addPawn("Ada");
            Pawn bo = addPawn("Bo", new Vector3(3, 0, 0));
            TalkToPawnCommand talk = new TalkToPawnCommand(nationRepository, eventProducer, tickCounter);

            talk.execute(player, ada);
            talk.execute(player, bo);

            Assert.Equal(PawnRelations.TALK_RELATIONSHIP_GAIN, PawnRelations.getRelationship(bo, player));
        }

        [Fact]
        public void testTalk_CrossingATier_IsAnnounced() {
            Pawn pawn = addPawn("Ada");
            setRelationship(pawn, RelationshipTiers.ACQUAINTANCE_THRESHOLD - 1);

            new TalkToPawnCommand(nationRepository, eventProducer, tickCounter).execute(player, pawn);

            Assert.EndsWith("Ada now knows you by name.", status());
        }

        [Fact]
        public void testInteract_PawnInRange_TalksAndRaisesRelationship() {
            Pawn pawn = addPawn("Ada");
            InteractCommand interact = new InteractCommand(environment, nationRepository, eventProducer, entityRepository, random, tickCounter);

            interact.execute(player);

            Assert.Equal(PawnRelations.TALK_RELATIONSHIP_GAIN, PawnRelations.getRelationship(pawn, player));
            Assert.StartsWith("Ada (Stranger): ", status());
        }

        // --- dialogue ---

        [Fact]
        public void testDialogue_HungryPawn_TalksAboutHunger() {
            Pawn pawn = addPawn("Ada");
            pawn.getInventory().clear();
            pawn.setEnergy(10);

            Assert.Contains("hungry", PawnDialogue.compose(pawn, player, nationRepository));

            setRelationship(pawn, RelationshipTiers.FRIEND_THRESHOLD);
            Assert.Equal("Hello, friend! I'm starving -- could you spare some food?", PawnDialogue.compose(pawn, player, nationRepository));
        }

        [Fact]
        public void testDialogue_NationlessAcquaintance_HintsAtRecruitment() {
            Nation home = playerNation();
            Pawn pawn = addPawn("Ada");

            Assert.Equal("Who are you? I don't belong to any nation.", PawnDialogue.compose(pawn, player, nationRepository));

            setRelationship(pawn, RelationshipTiers.ACQUAINTANCE_THRESHOLD);
            Assert.Equal("Oh, hello again. I don't belong to any nation. I'd join " + home.getName() + " if you asked.", PawnDialogue.compose(pawn, player, nationRepository));
        }

        [Fact]
        public void testDialogue_RivalPawn_LoyaltyDependsOnTier() {
            playerNation();
            Pawn leader = addPawn("Rex", new Vector3(50, 0, 0));
            Nation rival = pawnNation("Rivalia", leader);
            Pawn pawn = addPawn("Ada");
            join(rival, pawn);

            Assert.EndsWith("I'm loyal to Rivalia.", PawnDialogue.compose(pawn, player, nationRepository));

            setRelationship(pawn, RelationshipTiers.LOYAL_THRESHOLD);
            Assert.EndsWith("I'd leave Rivalia for Homeland if you asked.", PawnDialogue.compose(pawn, player, nationRepository));

            Assert.EndsWith("I lead Rivalia. We are 2 strong.", PawnDialogue.compose(leader, player, nationRepository));
        }

        [Fact]
        public void testDialogue_OwnMember_CallsPlayerBossAndTalksAboutWork() {
            Nation home = playerNation();
            Pawn pawn = addPawn("Ada");
            join(home, pawn);
            setRelationship(pawn, RelationshipTiers.FRIEND_THRESHOLD);
            pawn.setCurrentBehaviorType(BehaviorType.GATHER_RESOURCES);

            Assert.Equal("Good to see you, boss! I'm out gathering for Homeland.", PawnDialogue.compose(pawn, player, nationRepository));
        }

        [Fact]
        public void testDialogue_PawnWithOrder_ReportsProgress() {
            Nation home = playerNation();
            Pawn pawn = addPawn("Ada");
            join(home, pawn);
            NationOrder order = new NationOrder(home.getId(), ItemType.WOOD, 10);
            order.recordDelivery(4);
            pawn.setOrder(order);

            Assert.EndsWith("I'm gathering 6 more wood for Homeland.", PawnDialogue.compose(pawn, player, nationRepository));
        }

        // --- gifting ---

        [Fact]
        public void testGift_HungryPawn_GetsFoodAndValuesItHighly() {
            Pawn pawn = addPawn("Ada");
            pawn.getInventory().clear();
            pawn.setEnergy(10);
            player.getInventory().addItem(ItemType.APPLE, 3);
            player.getInventory().addItem(ItemType.COIN, 100);

            new GiftPawnCommand(eventProducer).execute(player, pawn);

            Assert.Equal(2, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(100, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(1, pawn.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(GiftPawnCommand.HUNGRY_FOOD_GIFT_GAIN, PawnRelations.getRelationship(pawn, player));
        }

        [Fact]
        public void testGift_FedPawn_GetsCoinsFirst() {
            Pawn pawn = addPawn("Ada");
            int pawnCoins = pawn.getInventory().getNumItems(ItemType.COIN);
            player.getInventory().addItem(ItemType.APPLE, 3);
            player.getInventory().addItem(ItemType.COIN, 100);

            new GiftPawnCommand(eventProducer).execute(player, pawn);

            Assert.Equal(3, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(100 - GiftPawnCommand.COIN_GIFT_AMOUNT, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(pawnCoins + GiftPawnCommand.COIN_GIFT_AMOUNT, pawn.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(GiftPawnCommand.COIN_GIFT_GAIN, PawnRelations.getRelationship(pawn, player));
        }

        [Fact]
        public void testGift_FoodToFedPawn_WorthLessThanToHungryPawn() {
            Pawn pawn = addPawn("Ada");
            player.getInventory().addItem(ItemType.APPLE, 1);

            new GiftPawnCommand(eventProducer).execute(player, pawn);

            Assert.Equal(GiftPawnCommand.FOOD_GIFT_GAIN, PawnRelations.getRelationship(pawn, player));
            Assert.True(GiftPawnCommand.FOOD_GIFT_GAIN < GiftPawnCommand.HUNGRY_FOOD_GIFT_GAIN);
        }

        [Fact]
        public void testGift_OnlyWood_GivesWood() {
            Pawn pawn = addPawn("Ada");
            player.getInventory().addItem(ItemType.WOOD, 7);

            new GiftPawnCommand(eventProducer).execute(player, pawn);

            Assert.Equal(7 - GiftPawnCommand.RESOURCE_GIFT_AMOUNT, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(GiftPawnCommand.RESOURCE_GIFT_GAIN, PawnRelations.getRelationship(pawn, player));
        }

        [Fact]
        public void testGift_NothingToGive_ChangesNothing() {
            Pawn pawn = addPawn("Ada");
            player.getInventory().addItem(ItemType.COIN, GiftPawnCommand.COIN_GIFT_AMOUNT - 1);

            Assert.False(GiftPawnCommand.canGift(player));
            new GiftPawnCommand(eventProducer).execute(player, pawn);

            Assert.False(PawnRelations.hasMet(pawn, player));
            Assert.StartsWith("You have nothing to give Ada.", status());
        }

        // --- inviting ---

        private InvitePawnToNationCommand invite() {
            return new InvitePawnToNationCommand(nationRepository, eventProducer, entityRepository);
        }

        [Fact]
        public void testInvite_PlayerWithoutNation_Refused() {
            Pawn pawn = addPawn("Ada");
            setRelationship(pawn, 100);

            invite().execute(player, pawn);

            Assert.Null(pawn.getNationId());
            Assert.False(InvitePawnToNationCommand.isApplicable(player, pawn));
        }

        [Fact]
        public void testInvite_NationlessStranger_Refuses() {
            Nation home = playerNation();
            Pawn pawn = addPawn("Ada");

            invite().execute(player, pawn);

            Assert.Null(pawn.getNationId());
            Assert.Equal(1, home.getNumberOfMembers());
            Assert.Contains("needs Acquaintance", status());
        }

        [Fact]
        public void testInvite_NationlessAcquaintance_JoinsAsSerf() {
            Nation home = playerNation();
            Pawn pawn = addPawn("Ada");
            setRelationship(pawn, RelationshipTiers.ACQUAINTANCE_THRESHOLD);

            invite().execute(player, pawn);

            Assert.Equal(home.getId(), pawn.getNationId());
            Assert.True(home.isMember(pawn.getId()));
            Assert.Equal(NationRole.SERF, home.getRole(pawn.getId()));
            Assert.Equal(1, eventRepository.getNumberOfEvents(EventType.NationJoin));
            Assert.Equal("Ada joined Homeland! Members: 2.", status());
        }

        [Fact]
        public void testInvite_RivalFriend_StaysLoyal() {
            Nation home = playerNation();
            Pawn leader = addPawn("Rex", new Vector3(50, 0, 0));
            Nation rival = pawnNation("Rivalia", leader);
            Pawn pawn = addPawn("Ada");
            join(rival, pawn);
            setRelationship(pawn, RelationshipTiers.LOYAL_THRESHOLD - 1);

            invite().execute(player, pawn);

            Assert.Equal(rival.getId(), pawn.getNationId());
            Assert.Equal(2, rival.getNumberOfMembers());
            Assert.Equal(1, home.getNumberOfMembers());
            Assert.Contains("needs Loyal", status());
        }

        [Fact]
        public void testInvite_RivalLoyal_DefectsAndOldNationLosesMemberAndStall() {
            Nation home = playerNation();
            Pawn leader = addPawn("Rex", new Vector3(50, 0, 0));
            Nation rival = pawnNation("Rivalia", leader);
            Settlement rivalSettlement = addSettlement(rival);
            rivalSettlement.getMarket().createStall();
            Stall stall = rivalSettlement.getMarket().getStallForSale();
            Pawn pawn = addPawn("Ada");
            join(rival, pawn);
            stall.setOwnerId(pawn.getId());
            rival.setRole(pawn.getId(), NationRole.MERCHANT);
            pawn.setHomeSettlementId(rivalSettlement.getId());
            pawn.setOrder(new NationOrder(rival.getId(), ItemType.WOOD, 10));
            setRelationship(pawn, RelationshipTiers.LOYAL_THRESHOLD);

            invite().execute(player, pawn);

            Assert.Equal(home.getId(), pawn.getNationId());
            Assert.False(rival.isMember(pawn.getId()));
            Assert.Equal(1, rival.getNumberOfMembers());
            Assert.Equal(leader.getId(), rival.getLeaderId());
            Assert.Equal(NationRole.SERF, home.getRole(pawn.getId()));
            Assert.Null(stall.getOwnerId());
            Assert.Null(pawn.getHomeSettlementId());
            Assert.Null(pawn.getOrder());
            Assert.Equal(1, eventRepository.getNumberOfEvents(EventType.NationLeave));
            Assert.Equal("Ada left Rivalia and joined Homeland! Members: 2.", status());
        }

        [Fact]
        public void testInvite_RivalLeader_NeverDefects() {
            playerNation();
            Pawn leader = addPawn("Rex");
            Nation rival = pawnNation("Rivalia", leader);
            setRelationship(leader, 1000);

            invite().execute(player, leader);

            Assert.Equal(rival.getId(), leader.getNationId());
            Assert.Equal(leader.getId(), rival.getLeaderId());
            Assert.Equal(1, nationRepository.getNumberOfNations() - 1);
            Assert.Contains("I won't abandon my people", status());
        }

        [Fact]
        public void testInvite_AlreadyMember_NoChange() {
            Nation home = playerNation();
            Pawn pawn = addPawn("Ada");
            join(home, pawn);

            Assert.False(InvitePawnToNationCommand.isApplicable(player, pawn));
            invite().execute(player, pawn);

            Assert.Equal(2, home.getNumberOfMembers());
            Assert.Equal("Ada is already a member of Homeland.", status());
        }

        // --- leader orders ---

        [Fact]
        public void testOrder_NotLeader_Refused() {
            Pawn leader = addPawn("Rex", new Vector3(50, 0, 0));
            Nation nation = pawnNation("Rivalia", leader);
            addSettlement(nation);
            join(nation, addPawn("Unused", new Vector3(60, 0, 0)));
            nation.addMember(player.getId());
            player.setNationId(nation.getId());
            Pawn pawn = addPawn("Ada");
            join(nation, pawn);
            setRelationship(pawn, 100);

            new OrderPawnToGatherCommand(nationRepository).execute(player, pawn);

            Assert.Null(pawn.getOrder());
        }

        [Fact]
        public void testOrder_StrangerMember_RefusesOrders() {
            Nation home = playerNation();
            addSettlement(home);
            Pawn pawn = addPawn("Ada");
            join(home, pawn);

            new OrderPawnToGatherCommand(nationRepository).execute(player, pawn);

            Assert.Null(pawn.getOrder());
            Assert.Contains("needs Acquaintance", status());
        }

        [Fact]
        public void testOrder_NoSettlement_Refused() {
            Nation home = playerNation();
            Pawn pawn = addPawn("Ada");
            join(home, pawn);
            setRelationship(pawn, RelationshipTiers.ACQUAINTANCE_THRESHOLD);

            new OrderPawnToGatherCommand(nationRepository).execute(player, pawn);

            Assert.Null(pawn.getOrder());
        }

        [Fact]
        public void testOrder_AcquaintanceMember_TakesOrder() {
            Nation home = playerNation();
            addSettlement(home);
            Pawn pawn = addPawn("Ada");
            join(home, pawn);
            setRelationship(pawn, RelationshipTiers.ACQUAINTANCE_THRESHOLD);

            new OrderPawnToGatherCommand(nationRepository).execute(player, pawn);

            Assert.NotNull(pawn.getOrder());
            Assert.Equal(ItemType.WOOD, pawn.getOrder().getItemType());
            Assert.Equal(NationOrder.DEFAULT_QUANTITY, pawn.getOrder().getQuantity());
            Assert.Equal(home.getId(), pawn.getOrder().getNationId());
            Assert.Contains("I'll gather 10 wood for Homeland.", status());
        }

        // --- order delivery ---

        private int deliver(Pawn pawn) {
            return NationOrderDelivery.deliver(pawn, entityRepository, nationRepository, player, eventProducer);
        }

        [Fact]
        public void testDelivery_OutsideSettlement_NothingHappens() {
            Nation home = playerNation();
            addSettlement(home);
            Pawn pawn = addPawn("Ada");
            join(home, pawn);
            pawn.setOrder(new NationOrder(home.getId(), ItemType.WOOD, 10));
            pawn.getInventory().addItem(ItemType.WOOD, 20);

            Assert.Equal(0, deliver(pawn));
            Assert.Equal(20, pawn.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testDelivery_PartialThenComplete_FillsStockpileAndRewards() {
            Nation home = playerNation();
            Settlement settlement = addSettlement(home);
            Pawn pawn = addPawn("Ada");
            join(home, pawn);
            pawn.setOrder(new NationOrder(home.getId(), ItemType.WOOD, 10));
            pawn.setCurrentSettlementId(settlement.getId());
            pawn.getInventory().addItem(ItemType.WOOD, 4);

            Assert.Equal(4, deliver(pawn));
            Assert.Equal(4, settlement.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(6, pawn.getOrder().getRemaining());
            Assert.Equal("Ada delivered 4 wood to the stockpile (4/10).", status());

            pawn.getInventory().addItem(ItemType.WOOD, 20);
            Assert.Equal(6, deliver(pawn));
            Assert.Equal(10, settlement.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(14, pawn.getInventory().getNumItems(ItemType.WOOD));
            Assert.Null(pawn.getOrder());
            Assert.Equal(NationOrderDelivery.COMPLETION_RELATIONSHIP_GAIN, PawnRelations.getRelationship(pawn, player));
            Assert.StartsWith("Ada finished your order: 10 wood delivered to the stockpile.", status());
        }

        [Fact]
        public void testDelivery_RivalSettlement_NotDelivered() {
            Nation home = playerNation();
            addSettlement(home);
            Pawn leader = addPawn("Rex", new Vector3(50, 0, 0));
            Settlement rivalSettlement = addSettlement(pawnNation("Rivalia", leader));
            Pawn pawn = addPawn("Ada");
            join(home, pawn);
            pawn.setOrder(new NationOrder(home.getId(), ItemType.WOOD, 10));
            pawn.setCurrentSettlementId(rivalSettlement.getId());
            pawn.getInventory().addItem(ItemType.WOOD, 20);

            Assert.Equal(0, deliver(pawn));
            Assert.Equal(0, rivalSettlement.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testDelivery_PawnChangedNation_OrderDropped() {
            Nation home = playerNation();
            Pawn pawn = addPawn("Ada");
            pawn.setOrder(new NationOrder(home.getId(), ItemType.WOOD, 10));

            deliver(pawn);

            Assert.Null(pawn.getOrder());
        }

        [Fact]
        public void testCollectStockpile_LeaderInOwnSettlement_TakesWoodAndStoneButNotFunds() {
            Nation home = playerNation();
            Settlement settlement = addSettlement(home);
            settlement.getInventory().addItem(ItemType.WOOD, 10);
            settlement.getInventory().addItem(ItemType.STONE, 3);
            settlement.addFunds(50);
            player.setCurrentSettlementId(settlement.getId());

            Assert.Equal(13, CollectSettlementStockpileCommand.getStockpileSize(settlement));
            new CollectSettlementStockpileCommand(nationRepository, entityRepository).execute(player);

            Assert.Equal(10, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(3, player.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal(50, settlement.getFunds());
            Assert.Equal(0, CollectSettlementStockpileCommand.getStockpileSize(settlement));
        }

        // --- appoint merchant ---

        [Fact]
        public void testAppointMerchant_FreeStallInHome_PawnBecomesMerchant() {
            Nation home = playerNation();
            Settlement settlement = addSettlement(home);
            settlement.getMarket().createStall();
            Pawn pawn = addPawn("Ada");
            join(home, pawn);
            pawn.setHomeSettlementId(settlement.getId());
            int coins = pawn.getInventory().getNumItems(ItemType.COIN);

            Assert.True(LeaderPawnCommands.canAppointMerchant(player, pawn, nationRepository, entityRepository));
            new AppointMerchantCommand(nationRepository, entityRepository, eventProducer).execute(player, pawn);

            Assert.Equal(NationRole.MERCHANT, home.getRole(pawn.getId()));
            Assert.NotNull(settlement.getMarket().getStall(pawn.getId()));
            Assert.Equal(coins, pawn.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(AppointMerchantCommand.RELATIONSHIP_GAIN, PawnRelations.getRelationship(pawn, player));
            Assert.False(LeaderPawnCommands.canAppointMerchant(player, pawn, nationRepository, entityRepository));
        }

        [Fact]
        public void testAppointMerchant_NoFreeStall_Refused() {
            Nation home = playerNation();
            Settlement settlement = addSettlement(home);
            Pawn pawn = addPawn("Ada");
            join(home, pawn);
            pawn.setHomeSettlementId(settlement.getId());

            Assert.False(LeaderPawnCommands.canAppointMerchant(player, pawn, nationRepository, entityRepository));
            new AppointMerchantCommand(nationRepository, entityRepository, eventProducer).execute(player, pawn);

            Assert.Equal(NationRole.SERF, home.getRole(pawn.getId()));
        }

        // --- wired into the simulation ---

        [Fact]
        public void testSimulation_NameTagShowsTier_AndOrderedWoodIsDelivered() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(20261018);
            Simulation simulation = new Simulation(config);
            Player simPlayer = simulation.getPlayer();
            Pawn pawn = new Pawn(new Vector3(0, 1.5f, 0), "Ada", simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            Nation nation = new Nation("Homeland", simPlayer.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            simPlayer.setNationId(nation.getId());
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());
            Settlement settlement = new Settlement(new Vector3(30, 0, 30), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
            pawn.setHomeSettlementId(settlement.getId());
            pawn.increaseRelationship(simPlayer, RelationshipTiers.FRIEND_THRESHOLD);
            pawn.setOrder(new NationOrder(nation.getId(), ItemType.WOOD, 5));
            pawn.getInventory().addItem(ItemType.WOOD, 5);

            simulation.fixedUpdate(1f / 50f);
            Assert.StartsWith("Ada (Friend)\n", pawn.getNameTag());

            pawn.setCurrentSettlementId(settlement.getId());
            simulation.fixedUpdate(1f / 50f);
            Assert.Null(pawn.getOrder());
            Assert.Equal(5, settlement.getInventory().getNumItems(ItemType.WOOD));
        }
    }
}
