using System.Numerics;

namespace beyondnations {

    /**
    * The player's side of a relationship with a pawn: reading it as a number
    * and a tier, raising it on both sides at once, and finding the pawn the
    * player is close enough to talk to. The pawn's own map is the source of
    * truth for how it feels; the player's map is kept in step so the
    * "Relationships" count on the HUD stays honest.
    */
    public static class PawnRelations {

        /** How close a pawn has to be to talk to, gift or invite -- the same reach E has always used. */
        public const float INTERACTION_RANGE = 5f;

        /** How long a pawn needs between conversations before talking warms it again: 30 seconds at the default tick rate. */
        public const int TALK_COOLDOWN_TICKS = 30 * TickCounter.DefaultTicksPerSecond;

        public const int TALK_RELATIONSHIP_GAIN = 2;

        public static int getRelationship(Pawn pawn, Entity other) {
            int value;
            return pawn.getRelationships().TryGetValue(other.getId(), out value) ? value : 0;
        }

        public static bool hasMet(Pawn pawn, Entity other) {
            return pawn.getRelationships().ContainsKey(other.getId());
        }

        public static RelationshipTier getTier(Pawn pawn, Entity other) {
            return RelationshipTiers.fromValue(getRelationship(pawn, other));
        }

        public static string getTierName(Pawn pawn, Entity other) {
            return RelationshipTiers.getName(getTier(pawn, other));
        }

        /**
        * Raises the relationship on both sides and records it as an event.
        * Returns true when the pawn moved up a tier, so callers can say so.
        */
        public static bool raise(Pawn pawn, Player player, int amount, EventProducer eventProducer) {
            RelationshipTier before = getTier(pawn, player);
            pawn.increaseRelationship(player, amount);
            player.increaseRelationship(pawn, amount);
            if (eventProducer != null) {
                eventProducer.producePawnRelationshipIncreaseEvent(pawn, player, amount);
            }
            return getTier(pawn, player) > before;
        }

        /**
        * The nearest pawn out in the world within reach of the player, or null.
        * Nobody can be talked to from inside a settlement, and pawns inside one
        * are hidden, so both cases return null.
        */
        public static Pawn findPawnInRange(Player player, Environment environment) {
            if (player.isCurrentlyInSettlement()) {
                return null;
            }
            Pawn pawn = (Pawn) environment.getNearestEntityOfType(player.getPosition(), EntityType.PAWN);
            if (pawn == null || pawn.isMarkedForDeletion()) {
                return null;
            }
            if (Vector3.Distance(player.getPosition(), pawn.getPosition()) >= INTERACTION_RANGE) {
                return null;
            }
            return pawn;
        }

        /** "Alice (Friend)" -- how status lines and buttons name a pawn. */
        public static string describe(Pawn pawn, Player player) {
            return pawn.getName() + " (" + getTierName(pawn, player) + ")";
        }

        /** The suffix appended when a tier is reached, or "" when it was not. */
        public static string tierUpSuffix(bool tierUp, Pawn pawn, Player player) {
            if (!tierUp) {
                return "";
            }
            switch (getTier(pawn, player)) {
                case RelationshipTier.LOYAL: return " " + pawn.getName() + " is now loyal to you!";
                case RelationshipTier.FRIEND: return " " + pawn.getName() + " now counts you as a friend!";
                default: return " " + pawn.getName() + " now knows you by name.";
            }
        }
    }
}
