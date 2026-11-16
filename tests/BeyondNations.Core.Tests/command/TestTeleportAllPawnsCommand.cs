using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The debug command that gathers every pawn around the player (bound in
    * PlayerInputController): each pawn lands within 20 paces of the player
    * on either axis, forgets what it was walking towards, and one inside a
    * settlement steps out of it first, taking the settlement's colour.
    * Nothing but pawns moves.
    */
    public class TestTeleportAllPawnsCommand {
        private readonly EntityRepository entityRepository;
        private readonly Player player;
        private readonly RandomSource random = new RandomSource(20261111);

        public TestTeleportAllPawnsCommand() {
            entityRepository = new EntityRepository(random);
            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.setPosition(new Vector3(-250, 3, 410));
            entityRepository.addEntity(player);
        }

        private TeleportAllPawnsCommand command() {
            return new TeleportAllPawnsCommand(entityRepository, random);
        }

        private Pawn addPawn(Vector3 position) {
            Pawn pawn = new Pawn(position, "Pawn", random);
            entityRepository.addEntity(pawn);
            return pawn;
        }

        [Fact]
        public void testExecute_MovesEveryPawnWithinTwentyPacesOfThePlayer_AtThePlayersHeight() {
            for (int i = 0; i < 100; i++) {
                addPawn(new Vector3(1000 + i * 37, 0, -900 - i * 11));
            }

            command().execute(player);

            foreach (Entity pawn in entityRepository.getEntitiesOfType(EntityType.PAWN)) {
                Vector3 offset = pawn.getPosition() - player.getPosition();
                // RandomSource.range(int, int) excludes its upper bound, so -20 to 19
                Assert.InRange(offset.X, -20f, 19f);
                Assert.InRange(offset.Z, -20f, 19f);
                Assert.Equal(player.getPosition().Y, pawn.getPosition().Y);
            }
        }

        [Fact]
        public void testExecute_ClearsEachPawnsTarget() {
            Pawn pawn = addPawn(new Vector3(600, 0, 600));
            Pawn other = addPawn(new Vector3(700, 0, 700));
            pawn.setTargetEntity(other);

            command().execute(player);

            Assert.False(pawn.hasTargetEntity());
            Assert.Null(pawn.getTargetEntity());
        }

        [Fact]
        public void testExecute_TakesAPawnOutOfItsSettlement_InTheSettlementsColour() {
            Settlement settlement = new Settlement(new Vector3(800, 0, -300), new NationId(), Rgba.Magenta, "Testland", random);
            entityRepository.addEntity(settlement);
            Pawn pawn = addPawn(settlement.getPosition());
            pawn.setColor(Rgba.Gray);
            pawn.setCurrentSettlementId(settlement.getId());
            settlement.addCurrentlyPresentEntity(pawn.getId());

            command().execute(player);

            Assert.False(pawn.isCurrentlyInSettlement());
            Assert.Equal(0, settlement.getCurrentlyPresentEntitiesCount());
            Assert.Equal(Rgba.Magenta, pawn.getAppearance().getPrimaryPart().getColor());
            Assert.True((pawn.getPosition() - player.getPosition()).Length() < 30f);
        }

        [Fact]
        public void testExecute_LeavesAPawnOutsideASettlementItsOwnColour() {
            Pawn pawn = addPawn(new Vector3(600, 0, 600));
            pawn.setColor(Rgba.Cyan);

            command().execute(player);

            Assert.Equal(Rgba.Cyan, pawn.getAppearance().getPrimaryPart().getColor());
        }

        [Fact]
        public void testExecute_LeavesThePlayerAndOtherEntitiesWhereTheyWere() {
            Settlement settlement = new Settlement(new Vector3(800, 0, -300), new NationId(), Rgba.White, "Testland", random);
            entityRepository.addEntity(settlement);
            addPawn(new Vector3(600, 0, 600));

            command().execute(player);

            Assert.Equal(new Vector3(-250, 3, 410), player.getPosition());
            Assert.Equal(new Vector3(800, 0, -300), settlement.getPosition());
        }

        [Fact]
        public void testExecute_WithNoPawns_ChangesNothing() {
            command().execute(player);

            Assert.Equal(new Vector3(-250, 3, 410), player.getPosition());
            Assert.Equal(1, entityRepository.getEntityCount());
        }
    }
}
