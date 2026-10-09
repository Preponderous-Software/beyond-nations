using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestNationCreateCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private NationRepository nationRepository;
        private EventRepository eventRepository;
        private NationCreateCommand command;
        private Player player;

        public TestNationCreateCommand() {
            nationRepository = new NationRepository(random);
            eventRepository = new EventRepository();
            EventProducer eventProducer = new EventProducer(eventRepository);
            command = new NationCreateCommand(nationRepository, eventProducer, random, new NationNameGenerator(random));

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
        }

        [Fact]
        public void testExecute_NotInNation_CreatesNationLedByPlayer() {
            // run
            command.execute(player);

            // check
            Assert.Equal(1, nationRepository.getNumberOfNations());
            Nation nation = nationRepository.getNation(player.getNationId());
            Assert.NotNull(nation);
            Assert.Equal(player.getId(), nation.getLeaderId());
            Assert.Equal(1, nation.getNumberOfMembers());
            Assert.Equal(NationRole.LEADER, nation.getRole(player.getId()));
            Assert.Equal(nation.getColor(), player.getAppearance().getColor());
            Assert.Equal(1, eventRepository.getNumberOfEvents(EventType.NationCreation));
            Assert.Equal("Created nation " + nation.getName() + ".", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_AlreadyLeader_NoSecondNation() {
            // prepare
            command.execute(player);
            Nation nation = nationRepository.getNation(player.getNationId());

            // run
            command.execute(player);

            // check
            Assert.Equal(1, nationRepository.getNumberOfNations());
            Assert.Equal(nation.getId(), player.getNationId());
            Assert.Equal(1, eventRepository.getNumberOfEvents(EventType.NationCreation));
            Assert.Equal("You are already the leader of " + nation.getName() + ".", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_AlreadyMember_NoSecondNation() {
            // prepare
            Nation nation = new Nation("Testland", new EntityId(), random);
            nation.addMember(player.getId());
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());

            // run
            command.execute(player);

            // check
            Assert.Equal(1, nationRepository.getNumberOfNations());
            Assert.Equal(nation.getId(), player.getNationId());
            Assert.Equal(0, eventRepository.getTotalNumberOfEvents());
            Assert.Equal("You are already a member of Testland.", player.getStatus().getStatus());
        }
    }
}
