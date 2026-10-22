namespace beyondnations {

    /**
    * Why two nations' relations moved, so that a change the player had no
    * hand in is never unexplained: the chronicle's line gets a phrase
    * ("Aland and Bshire have become rivals over their border."), and the
    * player's status line says it to them ("Aland resents your border:
    * relations are now Rival (-20).").
    */
    public sealed class RelationCause {
        /** Two settlements within neighbour distance grate on each other (Diplomacy). */
        public static readonly RelationCause Border = new RelationCause("over their border", "{0} resents your border");
        /** Allies warm toward each other. */
        public static readonly RelationCause Alliance = new RelationCause("as allies", "Your alliance with {0} is warming relations");
        /** Two nations at odds with the same third draw together. */
        public static readonly RelationCause SharedEnemy = new RelationCause("against a common enemy", "{0} shares an enemy with you");
        /** With nothing pushing it, a score eases toward zero. */
        public static readonly RelationCause Easing = new RelationCause("as old feelings faded", "Old feelings between you and {0} are fading");

        private readonly string between;
        private readonly string toPlayer;

        public RelationCause(string between, string toPlayer) {
            this.between = between;
            this.toPlayer = toPlayer;
        }

        /** The phrase the chronicle adds to a change between two nations: "over their border". */
        public string getBetween() {
            return between;
        }

        /** What the player is told, about the other nation: "Aland resents your border". */
        public string tellPlayer(Nation other) {
            return string.Format(toPlayer, other.getName());
        }
    }
}
