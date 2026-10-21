using System;
using System.Numerics;

namespace beyondnations {

    /**
    * What makes a settlement look lived in.
    *
    * A pawn inside a settlement used to be hidden, so a busy town looked
    * deserted. It now stays visible and mills about the plaza and the market
    * while it is inside: it is still "in the settlement" for every rule that
    * cares (it cannot be gathered, talked to as if outside, or chosen as a
    * target, and it moves by this rather than by physics), it is simply drawn
    * there. Its nametag is hidden while inside, so a crowded market is not a
    * wall of names; the settlement's own nametag counts who is there.
    *
    * Milling draws from its own RandomSource rather than the simulation's, so
    * that where pawns stroll never changes what the rest of a seeded world
    * does.
    */
    public class SettlementLife {

        /** How fast a pawn strolls about town, in units per second. */
        public const float MillSpeed = 3f;

        /** Chance per tick that a pawn standing at its spot sets off for another. */
        public const int RestOneInTicks = 90;

        private readonly RandomSource random;

        public SettlementLife(RandomSource random) {
            this.random = random;
        }

        /**
        * Takes a pawn into a settlement: counted as present, inside for every
        * rule, standing where it arrived and with its nametag hidden.
        */
        public static void admit(Pawn pawn, Settlement settlement) {
            settlement.addCurrentlyPresentEntity(pawn.getId());
            pawn.setCurrentSettlementId(settlement.getId());
            pawn.setTargetEntity(null);
            pawn.setVelocity(Vector3.Zero);
            pawn.clearMillTarget();
            pawn.setNameTag(null);
            pawn.setVisible(true);
        }

        /**
        * One tick of strolling for a pawn inside a settlement: walk toward its
        * spot, and once there, now and then pick another. groundHeight is the
        * height the pawn's origin rests at.
        */
        public void mill(Pawn pawn, Settlement settlement, float deltaTime, float groundHeight) {
            Vector3 centre = settlement.getPosition();
            Vector3 position = pawn.getPosition();
            position.Y = groundHeight;

            if (!pawn.hasMillTarget()) {
                if (pawn.getPosition().Y != groundHeight || random.range(0, RestOneInTicks) == 0) {
                    pawn.setMillTarget(pickSpot(centre, groundHeight));
                }
                pawn.setPosition(position);
                return;
            }

            Vector3 toTarget = pawn.getMillTarget() - position;
            toTarget.Y = 0f;
            float distance = toTarget.Length();
            float step = MillSpeed * deltaTime;
            if (distance <= step) {
                pawn.setPosition(pawn.getMillTarget());
                pawn.clearMillTarget();
                return;
            }
            pawn.setPosition(position + toTarget / distance * step);
        }

        private Vector3 pickSpot(Vector3 centre, float groundHeight) {
            float angle = random.range(0f, 2f * MathF.PI);
            float radius = random.range(SettlementLayout.MillMinRadius, SettlementLayout.MillMaxRadius);
            return new Vector3(centre.X + MathF.Cos(angle) * radius, groundHeight, centre.Z + MathF.Sin(angle) * radius);
        }

        /**
        * How many pawns, and the player, call this settlement home.
        */
        public static int countResidents(EntityRepository entityRepository, EntityId settlementId) {
            int count = 0;
            int entityCount = entityRepository.getEntityCount();
            for (int i = 0; i < entityCount; i++) {
                Entity entity = entityRepository.getEntityAt(i);
                if (entity.isMarkedForDeletion()) {
                    continue;
                }
                EntityId home = null;
                if (entity is Pawn) {
                    home = ((Pawn) entity).getHomeSettlementId();
                }
                else if (entity is Player) {
                    home = ((Player) entity).getHomeSettlementId();
                }
                if (home != null && home.Equals(settlementId)) {
                    count++;
                }
            }
            return count;
        }

        // --- saving (docs/SAVES.md) ---

        internal RandomSource getRandom() {
            return random;
        }
    }
}
