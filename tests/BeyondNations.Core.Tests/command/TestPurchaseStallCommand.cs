using System.Numerics;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestPurchaseStallCommand {
        private readonly RandomSource random = new RandomSource(20261115);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private PurchaseStallCommand command;
        private Player player;

        public TestPurchaseStallCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            command = new PurchaseStallCommand(nationRepository, entityRepository);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
        }

        private Nation makePlayerLeader() {
            Nation nation = new Nation("Testland", player.getId(), random);
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            return nation;
        }

        private Nation joinNationAs(NationRole role) {
            Nation nation = new Nation("Testland", new EntityId(), random);
            nation.addMember(player.getId());
            nation.setRole(player.getId(), role);
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            return nation;
        }

        private Settlement enterSettlementWithStallForSale(Nation nation) {
            Settlement settlement = new Settlement(Vector3.Zero, nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            settlement.getMarket().createStall();
            player.setCurrentSettlementId(settlement.getId());
            return settlement;
        }

        [Fact]
        public void testExecute_Serf_BuysStallAndBecomesMerchant() {
            // prepare
            Nation nation = joinNationAs(NationRole.SERF);
            Settlement settlement = enterSettlementWithStallForSale(nation);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check
            Assert.NotNull(settlement.getMarket().getStall(player.getId()));
            Assert.Equal(NationRole.MERCHANT, nation.getRole(player.getId()));
            Assert.Equal(coinsBefore - Stall.COIN_COST_TO_PURCHASE, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("Stall purchased. You are now a merchant.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_Leader_IsRefusedAndKeepsLeadership() {
            // prepare
            Nation nation = makePlayerLeader();
            Assert.Equal(NationRole.LEADER, nation.getRole(player.getId()));
            Settlement settlement = enterSettlementWithStallForSale(nation);
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);

            // run
            command.execute(player);

            // check: a leader who bought a stall used to be demoted to merchant
            // and lose building stalls and withdrawing funds (#315)
            Assert.Equal(NationRole.LEADER, nation.getRole(player.getId()));
            Assert.Equal(player.getId(), nation.getLeaderId());
            Assert.Null(settlement.getMarket().getStall(player.getId()));
            Assert.Equal(1, settlement.getMarket().getNumStallsForSale());
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("A nation's leader cannot keep a stall. Stalls are for your people to buy.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_NotEnoughCoin_BuysNothing() {
            // prepare
            Nation nation = joinNationAs(NationRole.SERF);
            Settlement settlement = enterSettlementWithStallForSale(nation);
            player.getInventory().removeItem(ItemType.COIN, player.getInventory().getNumItems(ItemType.COIN) - 40);

            // run
            command.execute(player);

            // check
            Assert.Null(settlement.getMarket().getStall(player.getId()));
            Assert.Equal(NationRole.SERF, nation.getRole(player.getId()));
            Assert.Equal("Not enough coin. Need 60 more.", player.getStatus().getStatus());
        }
    }
}
