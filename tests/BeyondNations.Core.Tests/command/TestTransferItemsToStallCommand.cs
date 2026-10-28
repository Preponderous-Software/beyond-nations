using System.Numerics;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestTransferItemsToStallCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private TransferItemsToStallCommand command;
        private Player player;

        public TestTransferItemsToStallCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            command = new TransferItemsToStallCommand(nationRepository, entityRepository);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().addItem(ItemType.WOOD, 12);
            player.getInventory().addItem(ItemType.APPLE, 3);
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

        [Fact]
        public void testExecute_NotInNation_TransfersNothing() {
            // run
            command.execute(player);

            // check
            Assert.Equal(12, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("You are not in a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_Serf_TransfersNothing() {
            // prepare
            Nation nation = joinNationAs(NationRole.SERF);
            Settlement settlement = enterSettlement(nation);
            Stall stall = giveStall(settlement);

            // run
            command.execute(player);

            // check
            Assert.Equal(12, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(0, stall.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("You are not a merchant.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_MerchantOutsideSettlement_TransfersNothing() {
            // prepare
            joinNationAs(NationRole.MERCHANT);

            // run
            command.execute(player);

            // check
            Assert.Equal(12, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("You are not inside a settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_MerchantWithoutStall_TransfersNothing() {
            // prepare
            Nation nation = joinNationAs(NationRole.MERCHANT);
            enterSettlement(nation);

            // run
            command.execute(player);

            // check
            Assert.Equal(12, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("You do not own a stall in this settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_MerchantWithStall_MovesEverythingIncludingCoins() {
            // prepare
            Nation nation = joinNationAs(NationRole.MERCHANT);
            Settlement settlement = enterSettlement(nation);
            Stall stall = giveStall(settlement);
            stall.getInventory().addItem(ItemType.WOOD, 5);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);
            Assert.True(coinsBefore > 0);

            // run
            command.execute(player);

            // check
            Assert.Equal(17, stall.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(3, stall.getInventory().getNumItems(ItemType.APPLE));
            // the player's purse goes into the stall as well (#418)
            Assert.Equal(coinsBefore, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(0, player.getInventory().getTotalNumItems());
            Assert.Equal("Transferred contents of inventory to stall.", player.getStatus().getStatus());
        }
    }
}
