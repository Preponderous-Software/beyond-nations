using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestNationLeaveCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private NationRepository nationRepository;
        private EventRepository eventRepository;
        private EntityRepository entityRepository;
        private NationLeaveCommand command;
        private Player player;

        public TestNationLeaveCommand() {
            nationRepository = new NationRepository(random);
            eventRepository = new EventRepository();
            entityRepository = new EntityRepository(random);
            EventProducer eventProducer = new EventProducer(eventRepository);
            command = new NationLeaveCommand(nationRepository, eventProducer, entityRepository);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().clear();
            entityRepository.addEntity(player);
        }

        /**
        * Puts the player in the nation, coloured as its members are, the way
        * NationJoinCommand and NationCreateCommand leave them.
        */
        private void enter(Nation nation) {
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            player.setColor(nation.getColor());
        }

        private Nation createNationLedByOther(string name) {
            Nation nation = new Nation(name, new EntityId(), random);
            nation.addMember(player.getId());
            return nation;
        }

        private Settlement addSettlement(Nation nation) {
            Settlement settlement = new Settlement(Vector3.Zero, nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return settlement;
        }

        private Stall giveStall(Settlement settlement, ItemType itemType, int quantity) {
            settlement.getMarket().createStall();
            Stall stall = settlement.getMarket().getStallForSale();
            stall.setOwnerId(player.getId());
            stall.getInventory().addItem(itemType, quantity);
            return stall;
        }

        [Fact]
        public void testExecute_NotInNation_NothingHappens() {
            // run
            command.execute(player);

            // check
            Assert.Null(player.getNationId());
            Assert.Equal(0, eventRepository.getTotalNumberOfEvents());
            Assert.Equal("You are not a member of a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_Member_LeavesNation() {
            // prepare
            Nation nation = createNationLedByOther("Testland");
            enter(nation);

            // run
            command.execute(player);

            // check
            Assert.Null(player.getNationId());
            Assert.False(nation.isMember(player.getId()));
            Assert.Equal(1, nation.getNumberOfMembers());
            Assert.Equal(1, nationRepository.getNumberOfNations());
            Assert.Equal(Rgba.White, player.getAppearance().getColor());
            Assert.Equal(1, eventRepository.getNumberOfEvents(EventType.NationLeave));
            Assert.Equal("You left nation Testland. Members: 1.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_Member_HomeSettlementCleared() {
            // prepare
            Nation nation = createNationLedByOther("Testland");
            enter(nation);
            Settlement settlement = addSettlement(nation);
            player.setHomeSettlementId(settlement.getId());

            // run
            command.execute(player);

            // check
            Assert.Null(player.getHomeSettlementId());
        }

        [Fact]
        public void testExecute_MemberWithStall_StallReleasedAndStockReturned() {
            // prepare
            Nation nation = createNationLedByOther("Testland");
            enter(nation);
            Stall stall = giveStall(addSettlement(nation), ItemType.WOOD, 7);

            // run
            command.execute(player);

            // check
            Assert.Null(stall.getOwnerId());
            Assert.Equal(0, stall.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(7, player.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testExecute_MemberWithStallInAnotherNation_ThatStallKept() {
            // prepare
            Nation nation = createNationLedByOther("Testland");
            enter(nation);
            Nation other = new Nation("Otherland", new EntityId(), random);
            nationRepository.addNation(other);
            Stall stall = giveStall(addSettlement(other), ItemType.WOOD, 7);

            // run
            command.execute(player);

            // check
            Assert.Equal(player.getId(), stall.getOwnerId());
            Assert.Equal(7, stall.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(0, player.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testExecute_SoleLeader_DisbandsNation() {
            // prepare
            Nation nation = new Nation("Testland", player.getId(), random);
            enter(nation);
            Settlement settlement = addSettlement(nation);
            player.setHomeSettlementId(settlement.getId());

            // run
            command.execute(player);

            // check
            Assert.Null(player.getNationId());
            Assert.Equal(0, nationRepository.getNumberOfNations());
            Assert.True(settlement.isMarkedForDeletion());
            Assert.Equal(0, nation.getNumberOfSettlements());
            Assert.Null(player.getHomeSettlementId());
            Assert.Equal(Rgba.White, player.getAppearance().getColor());
            Assert.Equal(1, eventRepository.getNumberOfEvents(EventType.NationDisband));
            Assert.Equal("You disbanded nation Testland.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_SoleLeaderInSettlement_Refused() {
            // prepare
            Nation nation = new Nation("Testland", player.getId(), random);
            enter(nation);
            Settlement settlement = addSettlement(nation);
            player.setCurrentSettlementId(settlement.getId());

            // run
            command.execute(player);

            // check
            Assert.Equal(nation.getId(), player.getNationId());
            Assert.Equal(1, nationRepository.getNumberOfNations());
            Assert.False(settlement.isMarkedForDeletion());
            Assert.Equal(0, eventRepository.getTotalNumberOfEvents());
            Assert.Equal("You cannot disband your nation while in a settlement.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_LeaderWithMembers_LeadershipPassesOn() {
            // prepare
            Nation nation = new Nation("Testland", player.getId(), random);
            EntityId successorId = new EntityId();
            nation.addMember(successorId);
            enter(nation);

            // run
            command.execute(player);

            // check
            Assert.Equal(successorId, nation.getLeaderId());
            Assert.Equal(NationRole.LEADER, nation.getRole(successorId));
            Assert.False(nation.isMember(player.getId()));
            Assert.Equal(1, nationRepository.getNumberOfNations());
            Assert.Null(player.getNationId());
            Assert.Equal(Rgba.White, player.getAppearance().getColor());
            Assert.Equal(1, eventRepository.getNumberOfEvents(EventType.NationLeave));
            Assert.Equal("You left nation Testland. Members: 1.", player.getStatus().getStatus());
        }
    }
}
