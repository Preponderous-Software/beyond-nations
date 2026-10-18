
namespace beyondnations {

    public class NationJoinCommand {
        private RandomSource random;
        private NationRepository nationRepository;
        private EventProducer eventProducer;

        public NationJoinCommand(NationRepository nationRepository, EventProducer eventProducer, RandomSource random) {
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
            if (nationRepository.getNumberOfNations() == 0) {
                player.getStatus().update("There are no nations to join.");
                return;
            }
            Nation nation = nationRepository.getRandomNation();
            nation.addMember(player.getId());
            player.setNationId(nation.getId());
            player.setColor(nation.getColor());
            eventProducer.produceNationJoinEvent(nation, player.getId());
            player.getStatus().update("You joined nation " + nation.getName() + ". Members: " + nation.getNumberOfMembers() + ".");

            // choose random nation settlement
            int numSettlements = nation.getSettlements().Count;
            if (numSettlements != 0) {
                int randomSettlementIndex = random.range(0, numSettlements);
                EntityId randomSettlementId = nation.getSettlements()[randomSettlementIndex];
                player.setHomeSettlementId(randomSettlementId);
            }
        }
    }
}