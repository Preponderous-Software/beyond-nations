namespace beyondnations {

    /**
    * Two nations moved from one named state to another: an alliance agreed or
    * broken, a rivalry hardening into hostility, a feud cooling to neutral.
    */
    public class NationRelationChangeEvent : Event {
        private Nation nation1;
        private Nation nation2;
        private NationRelationState before;
        private NationRelationState after;

        public NationRelationChangeEvent(Nation nation1, Nation nation2, NationRelationState before, NationRelationState after)
                : this(nation1, nation2, before, after, null) {
        }

        /** why, when known, is told at the end of the line: "... have become rivals over their border." */
        public NationRelationChangeEvent(Nation nation1, Nation nation2, NationRelationState before, NationRelationState after, string why)
                : base(EventType.NationRelationChange, withWhy(describe(nation1, nation2, before, after), why)) {
            this.nation1 = nation1;
            this.nation2 = nation2;
            this.before = before;
            this.after = after;
        }

        private static string withWhy(string description, string why) {
            if (string.IsNullOrEmpty(why)) {
                return description;
            }
            return description.TrimEnd('.') + " " + why + ".";
        }

        public static string describe(Nation nation1, Nation nation2, NationRelationState before, NationRelationState after) {
            string pair = nation1.getName() + " and " + nation2.getName();
            if (after == NationRelationState.ALLIED) {
                return pair + " have formed an alliance.";
            }
            if (before == NationRelationState.ALLIED) {
                return "The alliance between " + pair + " has ended.";
            }
            if (after == NationRelationState.HOSTILE) {
                return pair + " are now hostile.";
            }
            if (after == NationRelationState.RIVAL && before > after) {
                return pair + " have become rivals.";
            }
            return "Relations between " + pair + " are now " + NationRelationStates.getName(after).ToLowerInvariant() + ".";
        }

        public Nation getNation1() { return nation1; }
        public Nation getNation2() { return nation2; }
        public NationRelationState getBefore() { return before; }
        public NationRelationState getAfter() { return after; }

        public override string ToString() {
            return "NationRelationChangeEvent [" + "nation1=" + nation1.getName() + ", nation2=" + nation2.getName() + ", before=" + before + ", after=" + after + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}
