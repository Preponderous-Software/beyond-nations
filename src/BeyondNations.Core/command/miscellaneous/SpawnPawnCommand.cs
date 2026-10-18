using System.Numerics;

namespace beyondnations {

    public class SpawnPawnCommand {
        private PawnNameGenerator pawnNameGenerator;
        private RandomSource random;
        private EventProducer eventProducer;
        private EntityRepository entityRepository;

        public SpawnPawnCommand(EventProducer eventProducer, EntityRepository entityRepository, RandomSource random, PawnNameGenerator pawnNameGenerator) {
            this.pawnNameGenerator = pawnNameGenerator;
            this.random = random;
            this.eventProducer = eventProducer;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            Vector3 position = player.getPosition();
            position += new Vector3(random.range(-5f, 5f), 0, random.range(-5f, 5f));
            Pawn pawn = new Pawn(position, pawnNameGenerator.generate(), random);
            eventProducer.producePawnSpawnEvent(position, pawn);
            entityRepository.addEntity(pawn);
        }
    }
}