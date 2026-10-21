namespace beyondnations {

    /**
    * Talking to a pawn. The pawn answers with a line that reflects how it
    * feels about the player and what is on its mind (PawnDialogue), and the
    * first conversation in each cooldown window warms the relationship.
    * Talking again sooner still gets an answer, it just does not count.
    */
    public class TalkToPawnCommand {
        private readonly NationRepository nationRepository;
        private readonly EventProducer eventProducer;
        private readonly TickCounter tickCounter;

        public TalkToPawnCommand(NationRepository nationRepository, EventProducer eventProducer, TickCounter tickCounter) {
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
            this.tickCounter = tickCounter;
        }

        public void execute(Player player, Pawn pawn) {
            int tick = tickCounter == null ? 0 : tickCounter.getTotalTicks();
            bool tierUp = false;
            if (pawn.isConversationWarm(player.getId(), tick, PawnRelations.TALK_COOLDOWN_TICKS)) {
                tierUp = PawnRelations.raise(pawn, player, PawnRelations.TALK_RELATIONSHIP_GAIN, eventProducer);
                pawn.recordConversation(player.getId(), tick);
            }

            string line = PawnDialogue.compose(pawn, player, nationRepository);
            player.getStatus().update(PawnRelations.describe(pawn, player) + ": \"" + line + "\"" + PawnRelations.tierUpSuffix(tierUp, pawn, player));
        }
    }
}
