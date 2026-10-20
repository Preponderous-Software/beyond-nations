using System.Numerics;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestCollectProfitFromStallCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private CollectProfitFromStallCommand command;
        private Player player;

        public TestCollectProfitFromStallCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            command = new CollectProfitFromStallCommand(nationRepository, entityRepository);

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

        private Stall giveStall(Settlement settlement, int coins) {
            Market market = settlement.getMarket();
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(player.getId());
            if (coins > 0) {
                stall.getInventory().addItem(ItemType.COIN, coins);
            }
            return stall;
        }

        private Stall enterOwnStall(int coins) {
            Nation nation = joinNationAs(NationRole.MERCHANT);
            Settlement settlement = enterSettlement(nation);
            return giveStall(settlement, coins);
        }

        [Fact]
        public void testExecute_NotInNation_CollectsNothing() {
            // prepare
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("You are not in a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_Serf_CollectsNothing() {
            // prepare
            Nation nation = joinNationAs(NationRole.SERF);
            Settlement settlement = enterSettlement(nation);
            Stall stall = giveStall(settlement, 10);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(10, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("You are not a merchant.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_MerchantOutsideSettlement_CollectsNothing() {
            // prepare
            joinNationAs(NationRole.MERCHANT);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("You are not inside a settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_MerchantWithoutStall_CollectsNothing() {
            // prepare
            Nation nation = joinNationAs(NationRole.MERCHANT);
            enterSettlement(nation);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("You do not own a stall.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_EmptyStall_CollectsNothing() {
            // prepare
            Stall stall = enterOwnStall(0);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Your stall has no coins.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_EvenCoins_CollectsHalf() {
            // prepare
            Stall stall = enterOwnStall(10);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(5, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(coinsBefore + 5, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Collected 5 coins from stall.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_OddCoins_CollectsHalfRoundedDown() {
            // prepare
            Stall stall = enterOwnStall(7);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(4, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(coinsBefore + 3, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Collected 3 coins from stall.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_SingleCoin_CollectsIt() {
            // prepare
            Stall stall = enterOwnStall(1);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(coinsBefore + 1, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Collected 1 coins from stall.", player.getStatus().getStatus());
        }
    }
}
