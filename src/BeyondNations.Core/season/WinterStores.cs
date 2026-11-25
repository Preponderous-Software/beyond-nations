using System.Collections.Generic;

namespace beyondnations {

    /**
    * What the player's nation carries into winter, for the warning a day
    * ahead (#353): the HUD banner, the Season box and the chronicle line.
    * In the 0.5.0 playtest fifty pawns starved through one winter and the
    * first sign was the death notice; the only warning ahead was "Winter in
    * 1 day!", which said nothing of whether the nation was ready.
    *
    * The measure is the pawns' own: each wants SeasonalRules.WINTER_FOOD_STORE
    * food in its pack before winter (PawnBehaviorExecutor lays it in from
    * autumn), and a pawn's pack is the only larder the simulation keeps.
    * Nothing here is saved; it is counted when asked.
    */
    public static class WinterStores {

        /** A nation's members, the food they carry, how many carry less than they want for winter, and how many are hungry. */
        public readonly struct Count {
            public readonly int Members;
            public readonly int Food;
            public readonly int Short;
            /** Members who need food and carry none (Pawn.needsFood). */
            public readonly int Hungry;
            /**
            * Apples carried beyond the store a pawn keeps (SeasonalRules.PAWN_APPLES_KEPT),
            * which rot a sixth at a time: not in Food, since they will not last
            * the winter. A ruler who stocked to "101 of 72" watched 23 of it
            * vanish before winter with nothing to say why (playtest s03, #495).
            */
            public readonly int Spoiling;

            public Count(int members, int food, int shortOfStores) : this(members, food, shortOfStores, 0, 0) {
            }

            public Count(int members, int food, int shortOfStores, int hungry) : this(members, food, shortOfStores, hungry, 0) {
            }

            public Count(int members, int food, int shortOfStores, int hungry, int spoiling) {
                Members = members;
                Food = food;
                Short = shortOfStores;
                Hungry = hungry;
                Spoiling = spoiling;
            }

            /** The food the members want between them: WINTER_FOOD_STORE each. */
            public int Wanted { get { return Members * SeasonalRules.WINTER_FOOD_STORE; } }

            /**
            * The food the members want for what is left of the season: Wanted
            * before winter, and in winter Wanted times the part of winter
            * still to come (winterProgress in [0, 1)), rounded up. The famine
            * line came on by midday of winter's first day for a nation on
            * track: the stores fell as they were eaten while the target stayed
            * at the full winter's (playtest w2f).
            */
            public int wantedFor(Season season, float winterProgress) {
                if (season != Season.WINTER) {
                    return Wanted;
                }
                float left = 1f - System.Math.Clamp(winterProgress, 0f, 1f);
                return (int) System.MathF.Ceiling(Wanted * left - 0.0001f);
            }
        }

        public static Count count(EntityRepository entityRepository, NationId nationId) {
            int members = 0;
            int food = 0;
            int shortOfStores = 0;
            int hungry = 0;
            int spoiling = 0;
            if (nationId == null) {
                return new Count(0, 0, 0);
            }
            foreach (Pawn pawn in getMembers(entityRepository, nationId)) {
                members++;
                int rotting = spoilingIn(pawn);
                int kept = FoodItems.countFood(pawn.getInventory()) - rotting;
                food += kept;
                spoiling += rotting;
                if (kept < SeasonalRules.WINTER_FOOD_STORE) {
                    shortOfStores++;
                }
                if (pawn.needsFood()) {
                    hungry++;
                }
            }
            return new Count(members, food, shortOfStores, hungry, spoiling);
        }

        /** The nation's living member pawns, in repository order. */
        public static List<Pawn> getMembers(EntityRepository entityRepository, NationId nationId) {
            List<Pawn> members = new List<Pawn>();
            if (nationId == null) {
                return members;
            }
            int entityCount = entityRepository.getEntityCount();
            for (int i = 0; i < entityCount; i++) {
                Pawn pawn = entityRepository.getEntityAt(i) as Pawn;
                if (pawn != null && !pawn.isMarkedForDeletion() && nationId.Equals(pawn.getNationId())) {
                    members.Add(pawn);
                }
            }
            return members;
        }

        /** The apples a pawn carries beyond what it keeps from spoiling. */
        public static int spoilingIn(Pawn pawn) {
            return System.Math.Max(0, pawn.getInventory().getNumItems(ItemType.APPLE) - SeasonalRules.PAWN_APPLES_KEPT);
        }

        /** The food a pawn carries that will keep: everything but the apples beyond its kept store. */
        public static int keptBy(Pawn pawn) {
            return FoodItems.countFood(pawn.getInventory()) - spoilingIn(pawn);
        }

        /** The members carrying less than WINTER_FOOD_STORE food that keeps. */
        public static List<Pawn> getShort(EntityRepository entityRepository, NationId nationId) {
            return getMembers(entityRepository, nationId).FindAll(p => keptBy(p) < SeasonalRules.WINTER_FOOD_STORE);
        }

        /** The most short members the warning names before it says "and N more". */
        public const int MaxNamed = 3;

        /** "Ada", "Ada and Bo", "Ada, Bo and Cy", "Ada, Bo, Cy and 2 more". */
        public static string nameList(List<Pawn> pawns) {
            List<string> names = new List<string>();
            for (int i = 0; i < pawns.Count && i < MaxNamed; i++) {
                names.Add(pawns[i].getName());
            }
            if (pawns.Count > MaxNamed) {
                names.Add((pawns.Count - MaxNamed) + " more");
            }
            if (names.Count == 0) {
                return "";
            }
            if (names.Count == 1) {
                return names[0];
            }
            return string.Join(", ", names.GetRange(0, names.Count - 1)) + " and " + names[names.Count - 1];
        }

        /**
        * Whether a nation cannot feed its members, so that no child is born
        * in its settlements and no newcomer joins it (#489): a quarter or
        * more of its members hungry, in any season, or in winter its members
        * carrying less food between them than the winter wants (the Season
        * box's "Food: 0 of 44"). In the 0.8.0 playtest a nation at "Food: 0"
        * took in a child and three newcomers in one winter, each another
        * mouth, and the target rose from 28 to 44 while the stores ran out.
        */
        public static bool cannotFeed(Count stores, Season season) {
            return cannotFeed(stores, season, 0f);
        }

        /**
        * cannotFeed part way through winter: the stores are measured against
        * what the rest of winter needs (Count.wantedFor), not the whole
        * winter's, since the members have eaten the rest already.
        */
        public static bool cannotFeed(Count stores, Season season, float winterProgress) {
            if (stores.Members == 0) {
                return false;
            }
            if (stores.Hungry * FAMINE_HUNGRY_DIVISOR >= stores.Members) {
                return true;
            }
            return season == Season.WINTER && stores.Food < stores.wantedFor(season, winterProgress);
        }

        /** cannotFeed for a nation now, at the start of the season. */
        public static bool cannotFeed(EntityRepository entityRepository, NationId nationId, Season season) {
            return nationId != null && cannotFeed(count(entityRepository, nationId), season);
        }

        /** cannotFeed for a nation now, by the calendar; without one it is always spring. */
        public static bool cannotFeed(EntityRepository entityRepository, NationId nationId, SeasonCalendar calendar) {
            if (nationId == null) {
                return false;
            }
            Season season = calendar == null ? Season.SPRING : calendar.getSeason();
            float progress = calendar == null ? 0f : calendar.getSeasonProgress();
            return cannotFeed(count(entityRepository, nationId), season, progress);
        }

        /** A nation with one member in this many hungry cannot feed its members (cannotFeed): a quarter. */
        public const int FAMINE_HUNGRY_DIVISOR = 4;

        /** Whether the warning is due: on the last day before winter. */
        public static bool isWarningDue(SeasonCalendar calendar) {
            return calendar.getDaysUntilWinter() == 1;
        }

        /**
        * "Fortham's 5 members carry 9 food of the 60 they need for winter."
        * WINTER_FOOD_STORE each is what a member eats in a winter at home, so
        * the sum is what the winter will take, not a token (#488).
        */
        public static string describe(Nation nation, Count stores) {
            return nation.getName() + "'s " + stores.Members + (stores.Members == 1 ? " member carries " : " members carry ")
                + stores.Food + " food of the " + stores.Wanted + " " + (stores.Members == 1 ? "it needs" : "they need") + " for winter.";
        }

        /**
        * The HUD banner on the last day before winter while any member is
        * short, or null: "Winter comes tomorrow. Fortham's 5 members carry 9
        * food of the 60 they need for winter; 4 are short. Bring food home and
        * Feed Residents."
        */
        public static string describeWarning(EntityRepository entityRepository, Nation nation, SeasonCalendar calendar, Settlement here = null) {
            if (nation == null || !isWarningDue(calendar)) {
                return null;
            }
            Count stores = count(entityRepository, nation.getId());
            if (stores.Members == 0 || stores.Short == 0) {
                return null;
            }
            return "Winter comes tomorrow. " + describe(nation, stores).TrimEnd('.') + "; "
                + describeShort(getShort(entityRepository, nation.getId()), nation, here);
        }

        /**
        * Who is short and what to do, the way Feed Residents counts (#495
        * playtest s03): the banner said "1 is short ... Feed Residents" while
        * the button, which stocks only the members standing in the
        * settlement, read "(0 hungry)" and nothing named the one short.
        * here is the player's own settlement they stand in, or null.
        *
        *   "1 is short: Katie Williams. Bring food home and Feed Residents."
        *   "3 are short: Ada, Bo and Cy. Feed Residents stocks the 2 here; Cy must come home first."
        *   "1 is short: Katie Williams, who is not here. Feed Residents stocks only those here: they must come home."
        */
        public static string describeShort(List<Pawn> shortMembers, Nation nation, Settlement here) {
            if (here != null && !nation.getId().Equals(here.getNationId())) {
                here = null;
            }
            string head = shortMembers.Count + (shortMembers.Count == 1 ? " is" : " are") + " short: " + nameList(shortMembers);
            if (here == null) {
                return head + ". Bring food home and Feed Residents.";
            }
            List<Pawn> away = shortMembers.FindAll(p => !here.getId().Equals(p.getCurrentSettlementId()));
            int shortHere = shortMembers.Count - away.Count;
            if (shortHere == 0) {
                return head + (shortMembers.Count == 1 ? ", who is not here" : ", none of them here")
                    + ". Feed Residents stocks only those here: they must come home.";
            }
            if (away.Count == 0) {
                return head + ". Feed Residents stocks the " + shortHere + " here.";
            }
            return head + ". Feed Residents stocks the " + shortHere + " here; " + nameList(away) + " must come home first.";
        }

        /**
        * The chronicle's line as the last day before winter begins, or null
        * for a nation with no members: "Winter comes tomorrow, and Fortham's 5
        * members carry 9 food of the 60 they need for winter."
        */
        public static string describeForChronicle(EntityRepository entityRepository, Nation nation) {
            if (nation == null) {
                return null;
            }
            Count stores = count(entityRepository, nation.getId());
            if (stores.Members == 0) {
                return null;
            }
            return "Winter comes tomorrow, and " + describe(nation, stores);
        }
    }
}
