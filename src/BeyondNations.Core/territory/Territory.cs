using System;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * The land each settlement claims.
    *
    * A settlement claims a circle around itself whose radius grows with its
    * tier: a camp claims 70 units, a village 100, a town 130. Claims never
    * overlap. Settlements are founded at least 250 units apart, so two towns
    * would overlap; where two claims would meet, each is cut back in
    * proportion to its tier's radius so they share the gap between them, and
    * the borders touch. Touching borders are a source of friction between
    * the two nations (Diplomacy).
    *
    * No settlement may be founded inside another nation's claim.
    *
    * Distances are measured across the ground (x and z), ignoring height.
    */
    public static class Territory {
        public const float CAMP_CLAIM_RADIUS = 70f;
        public const float VILLAGE_CLAIM_RADIUS = 100f;
        public const float TOWN_CLAIM_RADIUS = 130f;

        /** Settlements of two nations within this distance make them neighbours. */
        public const float NEIGHBOUR_DISTANCE = 450f;

        /** Claims whose edges are this close count as touching. */
        public const float TOUCHING_GAP = 25f;

        public static float getBaseClaimRadius(SettlementTier tier) {
            switch (tier) {
                case SettlementTier.TOWN: return TOWN_CLAIM_RADIUS;
                case SettlementTier.VILLAGE: return VILLAGE_CLAIM_RADIUS;
                default: return CAMP_CLAIM_RADIUS;
            }
        }

        public static float groundDistance(Vector3 a, Vector3 b) {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        /**
        * Works out every settlement's claim afresh: its tier's radius, cut
        * back wherever it would overlap another's. Pure arithmetic on
        * positions and tiers, so the same world always draws the same borders.
        */
        public static void updateClaims(EntityRepository entityRepository) {
            List<Entity> settlements = entityRepository.getEntitiesOfType(EntityType.SETTLEMENT);
            foreach (Settlement settlement in settlements) {
                settlement.setClaimRadius(computeClaimRadius(settlement, settlements));
            }
        }

        public static float computeClaimRadius(Settlement settlement, List<Entity> settlements) {
            float radius = getBaseClaimRadius(settlement.getTier());
            float own = radius;
            foreach (Settlement other in settlements) {
                if (other == settlement || other.isMarkedForDeletion()) {
                    continue;
                }
                float otherBase = getBaseClaimRadius(other.getTier());
                float distance = groundDistance(settlement.getPosition(), other.getPosition());
                if (distance < own + otherBase) {
                    radius = MathF.Min(radius, distance * own / (own + otherBase));
                }
            }
            return radius;
        }

        /**
        * The settlement whose claim takes in this point, or null in the
        * wilds. Claims do not overlap, so there is at most one.
        */
        public static Settlement findClaimAt(EntityRepository entityRepository, Vector3 position) {
            foreach (Settlement settlement in entityRepository.getEntitiesOfType(EntityType.SETTLEMENT)) {
                if (settlement.isMarkedForDeletion()) {
                    continue;
                }
                if (groundDistance(settlement.getPosition(), position) <= settlement.getClaimRadius()) {
                    return settlement;
                }
            }
            return null;
        }

        /**
        * Why a nation may not found a settlement here, or null if it may: a
        * site inside another nation's claim is refused.
        */
        public static string getFoundingObjection(EntityRepository entityRepository, NationRepository nationRepository, Vector3 position, NationId founder) {
            Settlement claim = findClaimAt(entityRepository, position);
            if (claim != null && claim.getNationId() != founder) {
                return "This land is claimed by " + claim.getNationName() + ".";
            }
            return null;
        }

        /**
        * A new settlement founded near another nation's land is resented:
        * each nation with a settlement within neighbour distance takes it as
        * a provocation. Returns the nations that did.
        */
        public static List<Nation> recordFounding(Settlement founded, EntityRepository entityRepository, NationRepository nationRepository) {
            List<Nation> offended = new List<Nation>();
            Nation founder = nationRepository.getNation(founded.getNationId());
            if (founder == null) {
                return offended;
            }
            foreach (Settlement other in entityRepository.getEntitiesOfType(EntityType.SETTLEMENT)) {
                if (other == founded || other.getNationId() == founded.getNationId()) {
                    continue;
                }
                Nation neighbour = nationRepository.getNation(other.getNationId());
                if (neighbour == null || offended.Contains(neighbour)) {
                    continue;
                }
                if (groundDistance(founded.getPosition(), other.getPosition()) <= NEIGHBOUR_DISTANCE
                        && !nationRepository.getRelations().isAllied(founder, neighbour)) {
                    nationRepository.getRelations().adjust(founder, neighbour, Diplomacy.FOUNDING_NEAR_BORDER);
                    offended.Add(neighbour);
                }
            }
            updateClaims(entityRepository);
            return offended;
        }

        /**
        * What the player is standing in, for the HUD: "wilds", "your own
        * land", or "Xland (Rival)".
        */
        public static string describeFor(Player player, EntityRepository entityRepository, NationRepository nationRepository) {
            Settlement claim = findClaimAt(entityRepository, player.getPosition());
            if (claim == null) {
                return "wilds";
            }
            if (player.getNationId() != null && claim.getNationId() == player.getNationId()) {
                return "your own land";
            }
            Nation owner = nationRepository.getNation(claim.getNationId());
            Nation own = nationRepository.getNation(player.getNationId());
            if (owner == null) {
                return claim.getNationName();
            }
            if (own == null) {
                return owner.getName();
            }
            return owner.getName() + " (" + NationRelationStates.getName(nationRepository.getRelations().getState(own, owner)) + ")";
        }
    }
}
