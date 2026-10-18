using System.Numerics;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestWithdrawSettlementFundsCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private WithdrawSettlementFundsCommand command;
        private Player player;

        public TestWithdrawSettlementFundsCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            command = new WithdrawSettlementFundsCommand(nationRepository, entityRepository);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
        }

        private Nation makePlayerLeader() {
            Nation nation = new Nation("Testland", player.getId(), random);
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            return nation;
        }

        private Settlement addSettlement(Nation nation, int funds) {
            Settlement settlement = new Settlement(Vector3.Zero, nation.getId(), nation.getColor(), nation.getName(), random);
            if (funds > 0) {
                settlement.addFunds(funds);
            }
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return settlement;
        }

        private Settlement enterOwnSettlement(int funds) {
            Nation nation = makePlayerLeader();
            Settlement settlement = addSettlement(nation, funds);
            player.setCurrentSettlementId(settlement.getId());
            return settlement;
        }

        [Fact]
        public void testExecute_NotInNation_WithdrawsNothing() {
            // prepare
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("You are not in a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_SerfInOwnNationsSettlement_WithdrawsNothing() {
            // prepare
            Nation nation = new Nation("Testland", new EntityId(), random);
            nation.addMember(player.getId());
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            Settlement settlement = addSettlement(nation, 10);
            player.setCurrentSettlementId(settlement.getId());
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(10, settlement.getFunds());
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("You are not a leader.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_LeaderOutsideSettlement_WithdrawsNothing() {
            // prepare
            Nation nation = makePlayerLeader();
            Settlement settlement = addSettlement(nation, 10);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(10, settlement.getFunds());
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("You are not inside a settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_LeaderInAnotherNationsSettlement_WithdrawsNothing() {
            // prepare
            makePlayerLeader();
            Nation otherNation = new Nation("Otherland", new EntityId(), random);
            nationRepository.addNation(otherNation);
            Settlement settlement = addSettlement(otherNation, 10);
            player.setCurrentSettlementId(settlement.getId());
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(10, settlement.getFunds());
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("You do not own this settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_SettlementHasNoFunds_WithdrawsNothing() {
            // prepare
            Settlement settlement = enterOwnSettlement(0);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, settlement.getFunds());
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Settlement has no funds.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_EvenFunds_WithdrawsHalf() {
            // prepare
            Settlement settlement = enterOwnSettlement(10);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(5, settlement.getFunds());
            Assert.Equal(coinsBefore + 5, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Withdrew 5 coins from settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_OddFunds_WithdrawsHalfRoundedDown() {
            // prepare
            Settlement settlement = enterOwnSettlement(7);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(4, settlement.getFunds());
            Assert.Equal(coinsBefore + 3, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Withdrew 3 coins from settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_SingleCoin_WithdrawsIt() {
            // prepare
            Settlement settlement = enterOwnSettlement(1);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, settlement.getFunds());
            Assert.Equal(coinsBefore + 1, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Withdrew 1 coins from settlement.", player.getStatus().getStatus());
        }
    }
}
