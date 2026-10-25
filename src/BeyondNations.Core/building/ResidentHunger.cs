using System.Collections.Generic;

namespace beyondnations {

    /**
    * Which of a nation's pawns are going hungry, for the HUD's warning and
    * for Feed Residents (#353). In the 0.5.0 playtest fifty pawns starved
    * through one winter, most "within the walls", and the first sign the
    * player had was the death notice.
    */
    public static class ResidentHunger {

        /** The nation's living pawns that need food (Pawn.needsFood), the starving first. */
        public static List<Pawn> getHungryMembers(EntityRepository entityRepository, NationId nationId) {
            List<Pawn> starving = new List<Pawn>();
            List<Pawn> hungry = new List<Pawn>();
            if (nationId == null) {
                return hungry;
            }
            int entityCount = entityRepository.getEntityCount();
            for (int i = 0; i < entityCount; i++) {
                Pawn pawn = entityRepository.getEntityAt(i) as Pawn;
                if (pawn == null || pawn.isMarkedForDeletion() || !nationId.Equals(pawn.getNationId()) || !pawn.needsFood()) {
                    continue;
                }
                (pawn.isStarving() ? starving : hungry).Add(pawn);
            }
            starving.AddRange(hungry);
            return starving;
        }

        /** The hungry pawns standing in this settlement, the starving first: the ones Feed Residents can reach. */
        public static List<Pawn> getHungryPresent(EntityRepository entityRepository, Settlement settlement) {
            List<Pawn> present = new List<Pawn>();
            foreach (Pawn pawn in getHungryMembers(entityRepository, settlement.getNationId())) {
                if (settlement.getId().Equals(pawn.getCurrentSettlementId())) {
                    present.Add(pawn);
                }
            }
            return present;
        }

        /**
        * The banner over the HUD -- or null when there is nothing to warn
        * of -- naming how many of the nation's members are hungry and where
        * they are: "Fortham: 6 members hungry, 1 starving: 5 here, 1
        * outside the walls. Feed Residents feeds the 5 here."
        *
        * here is the player's own settlement they stand in (null elsewhere).
        * Its count is ResidentHunger.getHungryPresent, the same list Feed
        * Residents counts on its button and feeds, so the two numbers agree.
        * Playtest 0.6.2 s02 (finding 8): the banner said 6 and the button 5,
        * because the banner counted every member and the button only those
        * present, and nothing said so; after feeding everyone present the
        * banner still said "1 member hungry" with no hint where.
        *
        * Starving pawns are always worth a line, and merely hungry ones in
        * winter, when no harvest will feed them -- and whenever any are
        * standing here, since the Feed Residents button is then offering to
        * feed them (s02: the button said "1 hungry" with no banner, in spring).
        */
        public static string describeWarning(EntityRepository entityRepository, Nation nation, bool winter, Settlement here = null) {
            if (nation == null) {
                return null;
            }
            if (here != null && !nation.getId().Equals(here.getNationId())) {
                here = null;
            }
            List<Pawn> hungry = getHungryMembers(entityRepository, nation.getId());
            int starving = hungry.FindAll(p => p.isStarving()).Count;
            int hungryHere = here == null ? 0 : hungry.FindAll(p => here.getId().Equals(p.getCurrentSettlementId())).Count;
            if (starving == 0 && hungryHere == 0 && (!winter || hungry.Count == 0)) {
                return null;
            }
            string counts = nation.getName() + ": " + hungry.Count + (hungry.Count == 1 ? " member" : " members") + " hungry"
                + (starving > 0 ? ", " + starving + " starving" : "");
            string advice;
            if (here == null) {
                advice = "Bring food home and Feed Residents.";
            }
            else if (hungryHere > 0) {
                advice = "Feed Residents feeds the " + hungryHere + " here.";
            }
            else {
                advice = "Only those here can be fed: they must come home.";
            }
            return counts + ": " + describeWhere(entityRepository, hungry, here, hungryHere) + ". " + advice;
        }

        /** The most places the banner names before it says "elsewhere". */
        private const int MaxPlacesNamed = 2;

        /**
        * "5 here, 1 in the Camp of Oakford, 2 outside the walls": where the
        * hungry members are, here first, then the settlements with the most,
        * then those out in the world.
        */
        private static string describeWhere(EntityRepository entityRepository, List<Pawn> hungry, Settlement here, int hungryHere) {
            if (hungryHere == hungry.Count) {
                return hungryHere == 1 ? "here" : "all here";
            }
            List<EntityId> places = new List<EntityId>();
            Dictionary<EntityId, int> countByPlace = new Dictionary<EntityId, int>();
            int outside = 0;
            foreach (Pawn pawn in hungry) {
                EntityId place = pawn.getCurrentSettlementId();
                if (place == null) {
                    outside++;
                }
                else if (here == null || !here.getId().Equals(place)) {
                    if (!countByPlace.ContainsKey(place)) {
                        countByPlace[place] = 0;
                        places.Add(place);
                    }
                    countByPlace[place]++;
                }
            }
            // the most first; List.Sort is not stable, so ties keep first-seen order by index
            List<EntityId> byCount = new List<EntityId>(places);
            byCount.Sort((a, b) => countByPlace[a] != countByPlace[b] ? countByPlace[b] - countByPlace[a] : places.IndexOf(a) - places.IndexOf(b));

            List<string> parts = new List<string>();
            if (hungryHere > 0) {
                parts.Add(hungryHere + " here");
            }
            int elsewhere = 0;
            for (int i = 0; i < byCount.Count; i++) {
                Settlement settlement = entityRepository.getEntity(byCount[i]) as Settlement;
                if (i < MaxPlacesNamed && settlement != null) {
                    parts.Add(countByPlace[byCount[i]] + " in the " + settlement.getDisplayName());
                }
                else {
                    elsewhere += countByPlace[byCount[i]];
                }
            }
            if (outside > 0) {
                parts.Add(outside + " outside the walls");
            }
            if (elsewhere > 0) {
                parts.Add(elsewhere + " elsewhere");
            }
            if (hungryHere == 0 && parts.Count == 1) {
                // one place, and not here: "all in the Camp of Oakford"
                string only = parts[0];
                string count = hungry.Count.ToString();
                return hungry.Count == 1 ? only.Substring(count.Length + 1) : "all " + only.Substring(count.Length + 1);
            }
            return string.Join(", ", parts);
        }
    }
}
