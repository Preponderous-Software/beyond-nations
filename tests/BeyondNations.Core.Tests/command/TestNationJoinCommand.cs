using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestNationJoinCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private NationRepository nationRepository;
        private EventRepository eventRepository;
        private NationJoinCommand command;
        private Player player;

        public TestNationJoinCommand() {
            nationRepository = new NationRepository(random);
            eventRepository = new EventRepository();
            EventProducer eventProducer = new EventProducer(eventRepository);
            command = new NationJoinCommand(nationRepository, eventProducer, random);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
        }

        private Nation addNation(string name) {
            Nation nation = new Nation(name, new EntityId(), random);
            nationRepository.addNation(nation);
            return nation;
        }

        [Fact]
        public void testExecute_OneNation_JoinsItAsSerf() {
            // prepare
            Nation nation = addNation("Testland");

            // run
            command.execute(player);

            // check
            Assert.Equal(nation.getId(), player.getNationId());
            Assert.True(nation.isMember(player.getId()));
            Assert.Equal(2, nation.getNumberOfMembers());
            Assert.Equal(NationRole.SERF, nation.getRole(player.getId()));
            Assert.Equal(nation.getColor(), player.getAppearance().getPrimaryPart().getColor());
            Assert.Equal(1, eventRepository.getNumberOfEvents(EventType.NationJoin));
            Assert.Equal("You joined nation Testland. Members: 2.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_NationWithoutSettlements_NoHomeSettlement() {
            // prepare
            addNation("Testland");

            // run
            command.execute(player);

            // check
            Assert.Null(player.getHomeSettlementId());
        }

        [Fact]
        public void testExecute_NationWithSettlement_ItBecomesHome() {
            // prepare
            Nation nation = addNation("Testland");
            EntityId settlementId = new EntityId();
            nation.addSettlement(settlementId);

            // run
            command.execute(player);

            // check
            Assert.Equal(settlementId, player.getHomeSettlementId());
        }

        [Fact]
        public void testExecute_NoNations_NothingJoined() {
            // run
            command.execute(player);

            // check
            Assert.Null(player.getNationId());
            Assert.Equal(0, eventRepository.getTotalNumberOfEvents());
            Assert.Equal("There are no nations to join.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_AlreadyMember_StaysInOwnNation() {
            // prepare
            Nation own = addNation("Ownland");
            own.addMember(player.getId());
            player.setNationId(own.getId());
            Nation other = addNation("Otherland");

            // run
            command.execute(player);

            // check
            Assert.Equal(own.getId(), player.getNationId());
            Assert.False(other.isMember(player.getId()));
            Assert.Equal(0, eventRepository.getTotalNumberOfEvents());
            Assert.Equal("You are already a member of Ownland.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_AlreadyLeader_StaysInOwnNation() {
            // prepare
            Nation own = new Nation("Ownland", player.getId(), random);
            nationRepository.addNation(own);
            player.setNationId(own.getId());

            // run
            command.execute(player);

            // check
            Assert.Equal(own.getId(), player.getNationId());
            Assert.Equal(1, own.getNumberOfMembers());
            Assert.Equal("You are already the leader of Ownland.", player.getStatus().getStatus());
        }
    }
}
