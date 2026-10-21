namespace beyondnations {

    /**
    * How a pawn feels about someone, named. The raw relationship number keeps
    * climbing; the tier is what the player sees and what recruitment and
    * orders are gated on.
    */
    public enum RelationshipTier {
        STRANGER,
        ACQUAINTANCE,
        FRIEND,
        LOYAL,
    }

    public static class RelationshipTiers {
        public const int ACQUAINTANCE_THRESHOLD = 5;
        public const int FRIEND_THRESHOLD = 15;
        public const int LOYAL_THRESHOLD = 30;

        public static RelationshipTier fromValue(int value) {
            if (value >= LOYAL_THRESHOLD) {
                return RelationshipTier.LOYAL;
            }
            if (value >= FRIEND_THRESHOLD) {
                return RelationshipTier.FRIEND;
            }
            if (value >= ACQUAINTANCE_THRESHOLD) {
                return RelationshipTier.ACQUAINTANCE;
            }
            return RelationshipTier.STRANGER;
        }

        public static string getName(RelationshipTier tier) {
            switch (tier) {
                case RelationshipTier.ACQUAINTANCE: return "Acquaintance";
                case RelationshipTier.FRIEND: return "Friend";
                case RelationshipTier.LOYAL: return "Loyal";
                default: return "Stranger";
            }
        }
    }
}
