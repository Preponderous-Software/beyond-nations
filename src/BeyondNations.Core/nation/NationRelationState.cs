namespace beyondnations {

    /**
    * How two nations stand toward each other, read off their relation score
    * (NationRelations). ALLIED is the one state a score alone cannot give: it
    * needs an alliance to have been agreed, and it ends if the score falls
    * below zero.
    */
    public enum NationRelationState {
        HOSTILE,
        RIVAL,
        NEUTRAL,
        FRIENDLY,
        ALLIED
    }

    public static class NationRelationStates {
        public static string getName(NationRelationState state) {
            switch (state) {
                case NationRelationState.HOSTILE: return "Hostile";
                case NationRelationState.RIVAL: return "Rival";
                case NationRelationState.FRIENDLY: return "Friendly";
                case NationRelationState.ALLIED: return "Allied";
                default: return "Neutral";
            }
        }
    }
}
