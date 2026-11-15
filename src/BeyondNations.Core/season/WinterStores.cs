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

        /** A nation's members, the food they carry, and how many carry less than they want for winter. */
        public readonly struct Count {
            public readonly int Members;
            public readonly int Food;
            public readonly int Short;

            public Count(int members, int food, int shortOfStores) {
                Members = members;
                Food = food;
                Short = shortOfStores;
            }

            /** The food the members want between them: WINTER_FOOD_STORE each. */
            public int Wanted { get { return Members * SeasonalRules.WINTER_FOOD_STORE; } }
        }

        public static Count count(EntityRepository entityRepository, NationId nationId) {
            int members = 0;
            int food = 0;
            int shortOfStores = 0;
            if (nationId == null) {
                return new Count(0, 0, 0);
            }
            int entityCount = entityRepository.getEntityCount();
            for (int i = 0; i < entityCount; i++) {
                Pawn pawn = entityRepository.getEntityAt(i) as Pawn;
                if (pawn == null || pawn.isMarkedForDeletion() || !nationId.Equals(pawn.getNationId())) {
                    continue;
                }
                members++;
                int carried = FoodItems.countFood(pawn.getInventory());
                food += carried;
                if (carried < SeasonalRules.WINTER_FOOD_STORE) {
                    shortOfStores++;
                }
            }
            return new Count(members, food, shortOfStores);
        }

        /** Whether the warning is due: on the last day before winter. */
        public static bool isWarningDue(SeasonCalendar calendar) {
            return calendar.getDaysUntilWinter() == 1;
        }

        /** "Fortham's 5 members carry 9 food of the 20 they want for winter." */
        public static string describe(Nation nation, Count stores) {
            return nation.getName() + "'s " + stores.Members + (stores.Members == 1 ? " member carries " : " members carry ")
                + stores.Food + " food of the " + stores.Wanted + " " + (stores.Members == 1 ? "it wants" : "they want") + " for winter.";
        }

        /**
        * The HUD banner on the last day before winter while any member is
        * short, or null: "Winter comes tomorrow. Fortham's 5 members carry 9
        * food of the 20 they want for winter; 4 are short. Bring food home and
        * Feed Residents."
        */
        public static string describeWarning(EntityRepository entityRepository, Nation nation, SeasonCalendar calendar) {
            if (nation == null || !isWarningDue(calendar)) {
                return null;
            }
            Count stores = count(entityRepository, nation.getId());
            if (stores.Members == 0 || stores.Short == 0) {
                return null;
            }
            return "Winter comes tomorrow. " + describe(nation, stores).TrimEnd('.') + "; " + stores.Short
                + (stores.Short == 1 ? " is" : " are") + " short. Bring food home and Feed Residents.";
        }

        /**
        * The chronicle's line as the last day before winter begins, or null
        * for a nation with no members: "Winter comes tomorrow, and Fortham's 5
        * members carry 9 food of the 20 they want for winter."
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
