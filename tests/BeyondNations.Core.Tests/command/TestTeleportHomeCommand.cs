using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Teleporting home (H) has to leave the player where the next press of
    * Interact enters: the "Enter your settlement" objective says exactly
    * that. Playtest 0.5.0 (s01, s02, s04) landed 5.4 and 9 m from the well,
    * just out of the 5 m reach, so E said nothing was in range while the
    * command bar's Enter Settlement button worked.
    */
    public class TestTeleportHomeCommand {
        private readonly EntityRepository entityRepository;
        private readonly Environment environment;
        private readonly Player player;
        private readonly Settlement settlement;
        private readonly RandomSource random = new RandomSource(20261022);

        public TestTeleportHomeCommand() {
            entityRepository = new EntityRepository(random);
            environment = new Environment(5, 5, entityRepository, random);
            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.setPosition(new Vector3(300, 0, 300));
            player.getInventory().clear();
            entityRepository.addEntity(player);
            settlement = new Settlement(new Vector3(40, 0, -60), new NationId(), Rgba.White, "Testland", random);
            entityRepository.addEntity(settlement);
            player.setHomeSettlementId(settlement.getId());
        }

        [Fact]
        public void testExecute_LandsWithinInteractReachOfTheWell_AndClearOfIt() {
            for (int i = 0; i < 200; i++) {
                player.setPosition(new Vector3(300, 0, 300));
                new TeleportHomeCommand(entityRepository, random).execute(player);

                Vector3 offset = player.getPosition() - settlement.getPosition();
                offset.Y = 0;
                Assert.True(offset.Length() < PawnRelations.INTERACTION_RANGE, "landed " + offset.Length() + " from the well");
                Assert.True(offset.Length() >= TeleportHomeCommand.LandMinRadius, "landed in the well, " + offset.Length() + " from its centre");
            }
        }

        [Fact]
        public void testExecute_ThenInteract_EntersSettlement() {
            new TeleportHomeCommand(entityRepository, random).execute(player);
            new InteractCommand(environment, new NationRepository(random), new EventProducer(new EventRepository()), entityRepository, random)
                .execute(player);

            Assert.True(player.isCurrentlyInSettlement());
            Assert.Equal(settlement.getId(), player.getCurrentSettlementId());
        }
    }
}
