using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Input;
using Xunit;

using beyondnations;
using beyondnations.desktop.input;
using beyondnations.desktop.ui;
using beyondnationstests.desktop.input;

namespace beyondnationstests {

    /**
    * The world command bar offers Talk/Gift/Invite for the pawn within reach,
    * and the leader's levers for the player's own members; G and R reach the
    * same commands from the keyboard.
    */
    public class TestPawnCommandBar {

        private static Simulation createSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(1234);
            return new Simulation(config);
        }

        private static Pawn addPawnNextToPlayer(Simulation simulation) {
            Vector3 position = simulation.getPlayer().getPosition() + new Vector3(0.5f, 0, 0);
            Pawn pawn = new Pawn(position, "Ada", simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            return pawn;
        }

        private static List<string> labels(Simulation simulation) {
            List<string> result = new List<string>();
            foreach (WorldCommand command in WorldCommandBar.getCommands(simulation)) {
                result.Add(command.getLabel());
            }
            return result;
        }

        private static WorldCommand find(Simulation simulation, string label) {
            foreach (WorldCommand command in WorldCommandBar.getCommands(simulation)) {
                if (command.getLabel() == label) {
                    return command;
                }
            }
            return null;
        }

        private static Nation playerNation(Simulation simulation) {
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(simulation.getPlayer());
            return simulation.getNationRepository().getNation(simulation.getPlayer().getNationId());
        }

        [Fact]
        public void testPawnInReach_TalkOfferedWithTier_AndTalkingRaisesIt() {
            Simulation simulation = createSimulation();
            simulation.getPlayer().getInventory().clear();
            Pawn pawn = addPawnNextToPlayer(simulation);

            List<string> before = labels(simulation);
            Assert.Contains("Talk to Ada (Stranger)", before);
            // Gift and Invite are there, greyed with the reason (0.8.0 playtest s02)
            Assert.False(find(simulation, "Gift Ada").isEnabled());
            Assert.Equal("You have nothing to give. " + GiftPawnCommand.describeWhatMakesAGift(), find(simulation, "Gift Ada").getTooltip());
            Assert.False(find(simulation, "Invite Ada").isEnabled());
            Assert.Equal("Create or join a nation first, then invite people into it.", find(simulation, "Invite Ada").getTooltip());

            find(simulation, "Talk to Ada (Stranger)").execute();
            Assert.Equal(PawnRelations.TALK_RELATIONSHIP_GAIN, PawnRelations.getRelationship(pawn, simulation.getPlayer()));
        }

        [Fact]
        public void testOwnMemberNeverMet_NameplateAndTalkButtonShowTheSameTier() {
            // playtest 0.7.1 winter s03: a member who joined on their own read
            // "Loyal" on the nameplate (the trait) and "(Stranger)" on Talk
            Simulation simulation = createSimulation();
            Nation nation = playerNation(simulation);
            Pawn pawn = addPawnNextToPlayer(simulation);
            pawn.setTraits(new[] { PawnTrait.INDUSTRIOUS, PawnTrait.LOYAL });
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());
            Assert.False(PawnRelations.hasMet(pawn, simulation.getPlayer()));

            simulation.fixedUpdate(0.02f);

            string firstLine = pawn.getNameTag().Split('\n')[0];
            Assert.Equal("Ada (Stranger)", firstLine);
            Assert.Contains("Talk to " + firstLine, labels(simulation));
            // the trait cannot be read as the tier
            Assert.Contains("Industrious, Loyal by nature", pawn.getNameTag());

            PawnRelations.raise(pawn, simulation.getPlayer(), RelationshipTiers.LOYAL_THRESHOLD, null);
            simulation.fixedUpdate(0.02f);
            Assert.Equal("Ada (Loyal)", pawn.getNameTag().Split('\n')[0]);
            Assert.Contains("Talk to Ada (Loyal)", labels(simulation));
        }

        [Fact]
        public void testNoPawnInReach_NoPawnCommands() {
            Simulation simulation = createSimulation();
            simulation.getPlayer().setPosition(new Vector3(100000, 2, 100000));

            foreach (string label in labels(simulation)) {
                Assert.False(label.StartsWith("Talk to"), label);
            }
        }

        [Fact]
        public void testGiftAndInvite_OfferedWhenTheyApply() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            player.getInventory().addItem(ItemType.COIN, 50);
            playerNation(simulation);
            addPawnNextToPlayer(simulation);

            List<string> commands = labels(simulation);
            Assert.Contains("Gift Ada", commands);
            Assert.Contains("Invite Ada", commands);
            Assert.True(find(simulation, "Gift Ada").isEnabled());
        }

        /**
        * Playtest 0.8.0 s02: next to a stranger the bar showed only the
        * nation's buttons; Invite was a key a phone does not have. Now it
        * is always on the bar for the pawn in reach, greyed with the reason
        * the pawn would give, and a touch on it runs the same command.
        */
        [Fact]
        public void testInvite_OnTheBar_GreyedWithTheReason_UntilThePawnWouldJoin() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            Nation nation = playerNation(simulation);
            Pawn pawn = addPawnNextToPlayer(simulation);

            WorldCommand invite = find(simulation, "Invite Ada");
            Assert.False(invite.isEnabled());
            Assert.Equal("Ada doesn't know you yet. Talk to them or give a gift until they are an Acquaintance.", invite.getTooltip());

            pawn.increaseRelationship(player, RelationshipTiers.ACQUAINTANCE_THRESHOLD);
            invite = find(simulation, "Invite Ada");
            Assert.True(invite.isEnabled());
            invite.execute();
            Assert.Equal(nation.getId(), pawn.getNationId());
        }

        [Fact]
        public void testInvite_AnotherNationsLeader_WillNeverJoin() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            playerNation(simulation);
            Pawn pawn = addPawnNextToPlayer(simulation);
            Nation lowerham = new Nation("Lowerham", pawn.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(lowerham);
            pawn.setNationId(lowerham.getId());
            pawn.increaseRelationship(player, 1000);

            WorldCommand invite = find(simulation, "Invite Ada");
            Assert.False(invite.isEnabled());
            Assert.Equal("Ada leads Lowerham; will never join.", invite.getTooltip());
        }

        [Fact]
        public void testInvite_AnotherNationsMember_NeedsLoyal() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            playerNation(simulation);
            Pawn leader = new Pawn(new Vector3(9000, 0, 9000), "Bo", simulation.getRandom());
            simulation.getEntityRepository().addEntity(leader);
            Nation lowerham = new Nation("Lowerham", leader.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(lowerham);
            leader.setNationId(lowerham.getId());
            Pawn pawn = addPawnNextToPlayer(simulation);
            lowerham.addMember(pawn.getId());
            pawn.setNationId(lowerham.getId());

            WorldCommand invite = find(simulation, "Invite Ada");
            Assert.False(invite.isEnabled());
            Assert.Equal("Ada belongs to Lowerham and would leave it only for a Loyal friend.", invite.getTooltip());
        }

        [Fact]
        public void testLeaderLevers_OfferedForOwnMember() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            Nation nation = playerNation(simulation);
            Settlement settlement = new Settlement(new Vector3(500, 0, 500), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
            settlement.getMarket().createStall();

            Pawn pawn = addPawnNextToPlayer(simulation);
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());
            pawn.setHomeSettlementId(settlement.getId());
            pawn.increaseRelationship(player, RelationshipTiers.ACQUAINTANCE_THRESHOLD);

            List<string> commands = labels(simulation);
            Assert.False(find(simulation, "Invite Ada").isEnabled());
            Assert.Equal("Ada is already one of " + nation.getName() + ".", find(simulation, "Invite Ada").getTooltip());
            Assert.Contains("Order Ada: Gather Wood", commands);
            Assert.Contains("Appoint Ada Merchant", commands);

            find(simulation, "Order Ada: Gather Wood").execute();
            Assert.NotNull(pawn.getOrder());
            Assert.DoesNotContain("Order Ada: Gather Wood", labels(simulation));
        }

        [Fact]
        public void testAppointTrader_StaysOnTheBar_GreyedWithWhoHoldsTheSlot() {
            // #487: with the one trader slot taken the button vanished
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            Nation nation = playerNation(simulation);
            Settlement settlement = new Settlement(new Vector3(500, 0, 500), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());

            Pawn pawn = addPawnNextToPlayer(simulation);
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());
            pawn.setHomeSettlementId(settlement.getId());
            player.getInventory().clear();
            player.getInventory().addItem(ItemType.COIN, Traders.TRADER_STAKE);

            Assert.True(find(simulation, "Appoint Ada Trader").isEnabled());

            Pawn charlotte = new Pawn(new Vector3(500, 0, 500), "Charlotte Evans", simulation.getRandom());
            simulation.getEntityRepository().addEntity(charlotte);
            nation.addMember(charlotte.getId());
            charlotte.setNationId(nation.getId());
            charlotte.setHomeSettlementId(settlement.getId());
            nation.setRole(charlotte.getId(), NationRole.TRADER);

            WorldCommand appoint = find(simulation, "Appoint Ada Trader");
            Assert.NotNull(appoint);
            Assert.False(appoint.isEnabled());
            Assert.StartsWith("One trader per settlement: Charlotte Evans", appoint.getTooltip());
            // a tap on the greyed button puts the reason on the status line (touch path)
            beyondnations.desktop.ui.screens.WorldHud.explainDisabled(appoint, player.getStatus());
            Assert.StartsWith("One trader per settlement: Charlotte Evans", player.getStatus().getLatest());
        }

        [Fact]
        public void testGKey_GiftsNearestPawn() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            player.getInventory().clear();
            player.getInventory().addItem(ItemType.COIN, 50);
            Pawn pawn = addPawnNextToPlayer(simulation);
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            source.press(Key.G);
            inputService.update();
            controller.update(simulation, inputService);

            Assert.Equal(50 - GiftPawnCommand.COIN_GIFT_AMOUNT, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(GiftPawnCommand.COIN_GIFT_GAIN, PawnRelations.getRelationship(pawn, player));
        }

        /**
        * Playtest 0.5.0 (s01, s03, s06): inside their own settlement, with a
        * resident strolling a few metres away, the bar offered nothing for it
        * and G said nobody was in reach, so a leader could not befriend or
        * feed their own people. The same holds for the touch bar, which
        * shows these commands.
        */
        [Fact]
        public void testInsideOwnSettlement_ResidentOnPlaza_BarOffersItAndGKeyGifts() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            player.getInventory().clear();
            player.getInventory().addItem(ItemType.COIN, 50);
            Nation nation = playerNation(simulation);
            Settlement settlement = new Settlement(player.getPosition(), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
            simulation.getEntityRepository().addEntity(settlement);
            nation.addSettlement(settlement.getId());
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            Pawn pawn = addPawnNextToPlayer(simulation);
            pawn.setPosition(player.getPosition() + new Vector3(2.9f, 0, 0));
            SettlementLife.admit(pawn, settlement);

            List<string> commands = labels(simulation);
            Assert.Contains("Talk to Ada (Stranger)", commands);
            Assert.Contains("Gift Ada", commands);
            Assert.Contains("Exit Settlement", commands);

            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            source.press(Key.G);
            inputService.update();
            new PlayerInputController().update(simulation, inputService);

            Assert.Equal(GiftPawnCommand.COIN_GIFT_GAIN, PawnRelations.getRelationship(pawn, player));
        }

        [Fact]
        public void testRKey_InvitesNearestPawn() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            Nation nation = playerNation(simulation);
            Pawn pawn = addPawnNextToPlayer(simulation);
            pawn.increaseRelationship(player, RelationshipTiers.ACQUAINTANCE_THRESHOLD);
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            source.press(Key.R);
            inputService.update();
            controller.update(simulation, inputService);

            Assert.Equal(nation.getId(), pawn.getNationId());
        }
    }
}
