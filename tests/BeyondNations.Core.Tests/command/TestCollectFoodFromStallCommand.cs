using System.Numerics;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestCollectFoodFromStallCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private CollectFoodFromStallCommand command;
        private Player player;

        public TestCollectFoodFromStallCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            command = new CollectFoodFromStallCommand(nationRepository, entityRepository);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
        }

        private Nation joinNationAs(NationRole role) {
            Nation nation = new Nation("Testland", new EntityId(), random);
            nation.addMember(player.getId());
            nation.setRole(player.getId(), role);
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            return nation;
        }

        private Settlement enterSettlement(Nation nation) {
            Settlement settlement = new Settlement(Vector3.Zero, nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            player.setCurrentSettlementId(settlement.getId());
            return settlement;
        }

        private Stall giveStall(Settlement settlement) {
            Market market = settlement.getMarket();
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(player.getId());
            return stall;
        }

        private Stall enterOwnStall() {
            Nation nation = joinNationAs(NationRole.MERCHANT);
            Settlement settlement = enterSettlement(nation);
            return giveStall(settlement);
        }

        [Fact]
        public void testExecute_NotInNation_CollectsNothing() {
            // run
            command.execute(player);

            // check
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal("You are not in a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_Serf_CollectsNothing() {
            // prepare
            Nation nation = joinNationAs(NationRole.SERF);
            Settlement settlement = enterSettlement(nation);
            Stall stall = giveStall(settlement);
            stall.getInventory().addItem(ItemType.APPLE, 10);

            // run
            command.execute(player);

            // check
            Assert.Equal(10, stall.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal("You are not a merchant.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_MerchantOutsideSettlement_CollectsNothing() {
            // prepare
            joinNationAs(NationRole.MERCHANT);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal("You are not inside a settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_MerchantWithoutStall_CollectsNothing() {
            // prepare
            Nation nation = joinNationAs(NationRole.MERCHANT);
            enterSettlement(nation);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal("You do not own a stall.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_StallWithoutFood_CollectsNothing() {
            // prepare
            Stall stall = enterOwnStall();
            stall.getInventory().addItem(ItemType.WOOD, 8);

            // run
            command.execute(player);

            // check
            Assert.Equal(8, stall.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("Your stall has no food.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_EvenApples_CollectsHalf() {
            // prepare
            Stall stall = enterOwnStall();
            stall.getInventory().addItem(ItemType.APPLE, 10);

            // run
            command.execute(player);

            // check
            Assert.Equal(5, stall.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(5, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal("Collected 5 APPLE from stall.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_OddApples_CollectsHalfRoundedDown() {
            // prepare
            Stall stall = enterOwnStall();
            stall.getInventory().addItem(ItemType.APPLE, 7);

            // run
            command.execute(player);

            // check
            Assert.Equal(4, stall.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(3, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal("Collected 3 APPLE from stall.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_SingleApple_CollectsIt() {
            // prepare
            Stall stall = enterOwnStall();
            stall.getInventory().addItem(ItemType.APPLE, 1);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, stall.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal("Collected 1 APPLE from stall.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_MixedFood_CollectsOnlyTheMostNourishing() {
            // prepare (the host's button counts only apples, #419)
            Stall stall = enterOwnStall();
            stall.getInventory().addItem(ItemType.APPLE, 6);
            stall.getInventory().addItem(ItemType.CHICKEN_MEAT, 4);

            // run
            command.execute(player);

            // check
            Assert.Equal(2, stall.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Equal(2, player.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Equal(6, stall.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal("Collected 2 CHICKEN_MEAT from stall.", player.getStatus().getStatus());
        }
    }
}
