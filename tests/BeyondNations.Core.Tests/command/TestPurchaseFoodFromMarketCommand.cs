using System.Numerics;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestPurchaseFoodFromMarketCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private PurchaseFoodFromMarketCommand command;
        private Player player;
        private Nation hostNation;
        private Settlement settlement;

        public TestPurchaseFoodFromMarketCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            command = new PurchaseFoodFromMarketCommand(nationRepository, entityRepository);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().setNumItems(ItemType.COIN, 1000);

            hostNation = new Nation("Hostland", new EntityId(), random);
            nationRepository.addNation(hostNation);
            settlement = new Settlement(Vector3.Zero, hostNation.getId(), hostNation.getColor(), hostNation.getName(), random);
            entityRepository.addEntity(settlement);
            hostNation.addSettlement(settlement.getId());
        }

        private Stall openStall(Entity owner, int numApples) {
            Market market = settlement.getMarket();
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(owner.getId());
            if (numApples > 0) {
                stall.getInventory().addItem(ItemType.APPLE, numApples);
            }
            return stall;
        }

        private Pawn addMerchantPawn() {
            Pawn merchant = new Pawn(Vector3.Zero, "Merchant", random);
            entityRepository.addEntity(merchant);
            return merchant;
        }

        [Fact]
        public void testExecute_OutsideSettlement_BuysNothing() {
            // run
            command.execute(player);

            // check
            Assert.Equal(1000, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("You are not inside a settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_NoStalls_BuysNothing() {
            // prepare
            player.setCurrentSettlementId(settlement.getId());

            // run
            command.execute(player);

            // check
            Assert.Equal(1000, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("This settlement does not have any stalls.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_NoFoodForSale_BuysNothing() {
            // prepare
            openStall(addMerchantPawn(), 0);
            player.setCurrentSettlementId(settlement.getId());

            // run
            command.execute(player);

            // check
            Assert.Equal(1000, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal("No one is selling food at this market or you do not have enough money to purchase food.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_NoCoins_BuysNothing() {
            // prepare
            Stall stall = openStall(addMerchantPawn(), 5);
            player.getInventory().setNumItems(ItemType.COIN, 0);
            player.setCurrentSettlementId(settlement.getId());

            // run
            command.execute(player);

            // check
            Assert.Equal(5, stall.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal("No one is selling food at this market or you do not have enough money to purchase food.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_PawnStall_BuysOneAndBothRelationshipsRise() {
            // prepare
            Pawn merchant = addMerchantPawn();
            Stall stall = openStall(merchant, 5);
            player.setCurrentSettlementId(settlement.getId());
            int price = settlement.getMarket().getBuyPrice(ItemType.APPLE);

            // run
            command.execute(player);

            // check
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(4, stall.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(1000 - price, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(price, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(1, player.getRelationships()[merchant.getId()]);
            Assert.Equal(1, merchant.getRelationships()[player.getId()]);
            Assert.Equal("You purchased food from Merchant. Relationship increased to 1.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_PlayerStall_RaisesTheSellersRelationshipToo() {
            // prepare
            Player seller = new Player(5, 10, new TickCounter(), 500, 200, random);
            entityRepository.addEntity(seller);
            openStall(seller, 5);
            player.setCurrentSettlementId(settlement.getId());

            // run
            command.execute(player);

            // check
            Assert.Equal(1, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(1, player.getRelationships()[seller.getId()]);
            Assert.Equal(1, seller.getRelationships()[player.getId()]);
        }

        [Fact]
        public void testExecute_ForeignBuyer_RecordsATradeBetweenTheNations() {
            // prepare
            Nation buyerNation = new Nation("Buyerland", new EntityId(), random);
            buyerNation.addMember(player.getId());
            nationRepository.addNation(buyerNation);
            player.setNationId(buyerNation.getId());
            openStall(addMerchantPawn(), 5);
            player.setCurrentSettlementId(settlement.getId());
            int scoreBefore = nationRepository.getRelations().getScore(buyerNation, hostNation);

            // run
            command.execute(player);

            // check
            Assert.Equal(scoreBefore + Diplomacy.TRADE, nationRepository.getRelations().getScore(buyerNation, hostNation));
        }

        [Fact]
        public void testExecute_RepeatedPurchases_RelationshipKeepsRising() {
            // prepare
            Pawn merchant = addMerchantPawn();
            openStall(merchant, 5);
            player.setCurrentSettlementId(settlement.getId());

            // run
            command.execute(player);
            command.execute(player);

            // check
            Assert.Equal(2, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(2, player.getRelationships()[merchant.getId()]);
            Assert.Equal(2, merchant.getRelationships()[player.getId()]);
        }
    }
}
