using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestInteractCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private EntityRepository entityRepository;
        private InteractCommand command;
        private Player player;

        public TestInteractCommand() {
            entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);
            EventProducer eventProducer = new EventProducer(new EventRepository());
            command = new InteractCommand(environment, nationRepository, eventProducer, entityRepository, random);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.setPosition(Vector3.Zero);
            player.getInventory().clear();
            entityRepository.addEntity(player);
        }

        private ItemDrop addDrop(Vector3 position, ItemType itemType, int quantity) {
            ItemDrop drop = new ItemDrop(position, new ItemStack(itemType, quantity));
            entityRepository.addEntity(drop);
            return drop;
        }

        [Fact]
        public void testExecute_DropInRange_PickedUpAndMarkedForDeletion() {
            // prepare
            ItemDrop drop = addDrop(new Vector3(2, 0, 0), ItemType.WOOD, 7);

            // run
            command.execute(player);

            // check
            Assert.Equal(7, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.True(drop.isMarkedForDeletion());
            Assert.Equal("Picked up WOOD x7.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_SeveralDropsInRange_AllPickedUpAtOnce() {
            // prepare
            addDrop(new Vector3(1, 0, 0), ItemType.COIN, 50);
            addDrop(new Vector3(1, 0, 0), ItemType.STONE, 3);

            // run
            command.execute(player);

            // check
            Assert.Equal(50, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(3, player.getInventory().getNumItems(ItemType.STONE));
        }

        [Fact]
        public void testExecute_DropOutOfRange_NotPickedUp() {
            // prepare
            ItemDrop drop = addDrop(new Vector3(20, 0, 0), ItemType.WOOD, 7);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.False(drop.isMarkedForDeletion());
            Assert.Equal("No entities within range to interact with.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_PressedTwiceBeforeDeletion_StackCollectedOnce() {
            // prepare
            addDrop(new Vector3(2, 0, 0), ItemType.WOOD, 7);

            // run: no tick runs between the two presses, so the drop is marked
            // but still in the repository for the second one
            command.execute(player);
            command.execute(player);

            // check
            Assert.Equal(7, player.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testExecute_DropAndRockInRange_DropTakenFirst() {
            // prepare
            addDrop(new Vector3(2, 0, 0), ItemType.APPLE, 2);
            Rock rock = new Rock(new Vector3(1, 0, 0));
            entityRepository.addEntity(rock);

            // run
            command.execute(player);

            // check
            Assert.Equal(2, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.False(rock.isMarkedForDeletion());
        }

        [Fact]
        public void testExecute_DropNextToSettlement_DropTakenBeforeEntering() {
            // prepare: a pawn can die within reach of its settlement
            addDrop(new Vector3(2, 0, 0), ItemType.COIN, 9);
            Settlement settlement = new Settlement(new Vector3(1, 0, 0), new NationId(), Rgba.White, "Testland", random);
            entityRepository.addEntity(settlement);

            // run
            command.execute(player);

            // check: picked up, and the player is still outside
            Assert.Equal(9, player.getInventory().getNumItems(ItemType.COIN));
            Assert.False(player.isCurrentlyInSettlement());
        }
    }
}
