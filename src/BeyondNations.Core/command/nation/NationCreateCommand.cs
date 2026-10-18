namespace beyondnations {

    public class NationCreateCommand {
        private NationNameGenerator nationNameGenerator;
        private RandomSource random;
        private NationRepository nationRepository;
        private EventProducer eventProducer;

        public NationCreateCommand(NationRepository nationRepository, EventProducer eventProducer, RandomSource random, NationNameGenerator nationNameGenerator) {
            this.nationNameGenerator = nationNameGenerator;
            this.random = random;
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
        }

        public void execute(Player player) {
            if (player.getNationId() != null) {
                Nation playerNation = nationRepository.getNation(player.getNationId());
                if (playerNation.getLeaderId() == player.getId()) {
                    player.getStatus().update("You are already the leader of " + playerNation.getName() + ".");
                }
                else {
                    player.getStatus().update("You are already a member of " + playerNation.getName() + ".");
                }
                return;
            }
            Nation nation = new Nation(nationNameGenerator.generate(), player.getId(), random);
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            player.setColor(nation.getColor());
            eventProducer.produceNationCreationEvent(nation);
            player.getStatus().update("Created nation " + nation.getName() + ".");
        }
    }
}