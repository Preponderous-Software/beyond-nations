using System;
using System.Numerics;

namespace beyondnations {

    public class TeleportHomeCommand {

        /**
        * Where the player lands, measured from the settlement's centre: clear
        * of the well (1.3 across the middle) and inside the 5 units Interact
        * reaches, so the next press of E enters, as the "Enter your
        * settlement" objective says. It was anywhere up to 28 units out.
        */
        public const float LandMinRadius = 2.5f;
        public const float LandMaxRadius = 4f;

        private RandomSource random;
        private EntityRepository entityRepository;

        public TeleportHomeCommand(EntityRepository entityRepository, RandomSource random) {
            this.random = random;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            if (player.isCurrentlyInSettlement()) {
                player.getStatus().update("Cannot teleport while in settlement.");
                return;
            }
            
            EntityId homeSettlementId = player.getHomeSettlementId();
            if (homeSettlementId == null) {
                player.getStatus().update("No home found.");
                return;
            }
            Entity homeSettlement = entityRepository.getEntity(homeSettlementId);
            float angle = random.range(0f, 2f * MathF.PI);
            float radius = random.range(LandMinRadius, LandMaxRadius);
            player.setPosition(homeSettlement.getPosition() + new Vector3(MathF.Cos(angle) * radius, 0, MathF.Sin(angle) * radius));
            player.getStatus().update("Welcome home!");
        }
    }
}