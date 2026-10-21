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
            Assert.DoesNotContain("Gift Ada", before);
            Assert.DoesNotContain("Invite Ada", before);

            find(simulation, "Talk to Ada (Stranger)").execute();
            Assert.Equal(PawnRelations.TALK_RELATIONSHIP_GAIN, PawnRelations.getRelationship(pawn, simulation.getPlayer()));
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
            Assert.DoesNotContain("Invite Ada", commands);
            Assert.Contains("Order Ada: Gather Wood", commands);
            Assert.Contains("Appoint Ada Merchant", commands);

            find(simulation, "Order Ada: Gather Wood").execute();
            Assert.NotNull(pawn.getOrder());
            Assert.DoesNotContain("Order Ada: Gather Wood", labels(simulation));
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
