namespace beyondnations {

    /**
    * Upgrading a building with coins: the sink for a purse that has filled
    * every building slot. The fun pass found a player who does not court
    * with 3,000 to 3,700 coins at 56 minutes and nothing left to buy once a
    * settlement had its twelve buildings.
    *
    * Each upgrade raises one building's level by one and makes the number it
    * already stands for bigger:
    *
    *   House       level 1  1,500 coins (Town era)     room for 6 residents, was 4
    *               level 2  3,000 coins (Kingdom era)  room for 8
    *   Farm        level 1  1,200 coins (Town era)     2 apples a harvest, was 1
    *               level 2  2,400 coins (Kingdom era)  3 apples a harvest
    *   Storehouse  level 1  2,000 coins (Town era)     3 more market stalls, was 2
    *               level 2  4,000 coins (Kingdom era)  4 more
    *   Workshop    no upgrades: it only lets recipes be crafted
    *
    * Coins only, from the player's purse. The upgrade goes to the building
    * of that type with the lowest level (the first such slot on a tie), so
    * every house reaches level 1 before any reaches level 2. A settlement of
    * a nation led by a pawn (no building era) is not gated by era, as for
    * building; pawns do not upgrade.
    *
    * The level is saved with the building (save format 8).
    */
    public static class BuildingUpgrades {

        /** How many more residents each level of a house makes room for. */
        public const int HOUSE_RESIDENTS_PER_LEVEL = 2;
        /** How many more apples a farm stocks each harvest per level. */
        public const int FARM_APPLES_PER_LEVEL = 1;
        /** How many more market stalls each level of a storehouse allows. */
        public const int STOREHOUSE_STALLS_PER_LEVEL = 1;

        /** The highest level a building of this type can reach. */
        public static int getMaxLevel(BuildingType type) {
            switch (type) {
                case BuildingType.HOUSE:
                case BuildingType.FARM:
                case BuildingType.STOREHOUSE:
                    return 2;
            }
            return 0;
        }

        /** Whether this type can be upgraded at all. */
        public static bool isUpgradable(BuildingType type) {
            return getMaxLevel(type) > 0;
        }

        /** The coins to raise a building of this type to this level (1 or 2). */
        public static int getPrice(BuildingType type, int toLevel) {
            int first;
            switch (type) {
                case BuildingType.HOUSE: first = 1500; break;
                case BuildingType.FARM: first = 1200; break;
                case BuildingType.STOREHOUSE: first = 2000; break;
                default: return 0;
            }
            return first * toLevel;
        }

        /** The era that lets a building be raised to this level: the Town for level 1, the Kingdom for level 2. */
        public static Era eraFor(int toLevel) {
            return toLevel <= 1 ? Era.TOWN : Era.KINGDOM;
        }

        /** Whether this settlement's era allows raising a building to this level. */
        public static bool isLevelUnlocked(Settlement settlement, int toLevel) {
            Era? era = settlement.getBuildingEra();
            return era == null || era.Value >= eraFor(toLevel);
        }

        /** Whether the settlement's era allows any upgrade yet (the Town era). */
        public static bool areUpgradesUnlocked(Settlement settlement) {
            return isLevelUnlocked(settlement, 1);
        }

        /** Residents one house at this level makes room for. */
        public static int houseResidents(int level) {
            return BuildingCatalog.HOUSE_RESIDENTS + HOUSE_RESIDENTS_PER_LEVEL * level;
        }

        /** Apples one farm at this level stocks each harvest round. */
        public static int farmApples(int level) {
            return 1 + FARM_APPLES_PER_LEVEL * level;
        }

        /** Market stalls one storehouse at this level allows. */
        public static int storehouseStalls(int level) {
            return BuildingCatalog.STOREHOUSE_STALLS + STOREHOUSE_STALLS_PER_LEVEL * level;
        }

        /**
        * The building of this type the next upgrade goes to: the lowest
        * level below the maximum, the first slot on a tie. Null when the
        * settlement has none, or every one is fully upgraded.
        */
        public static Building findNext(Settlement settlement, BuildingType type) {
            Building next = null;
            foreach (Building building in settlement.getBuildings()) {
                if (building.getType() != type || building.getLevel() >= getMaxLevel(type)) {
                    continue;
                }
                if (next == null || building.getLevel() < next.getLevel()) {
                    next = building;
                }
            }
            return next;
        }

        /** The price of the next upgrade of this type here, or 0 when there is none to make. */
        public static int getNextPrice(Settlement settlement, BuildingType type) {
            Building next = findNext(settlement, type);
            return next == null ? 0 : getPrice(type, next.getLevel() + 1);
        }

        /**
        * Why the player cannot upgrade a building of this type here, or null
        * when they can: "No farm here to upgrade.", "Every house here is
        * fully upgraded.", "Level 2 comes with the Kingdom era." or "Needs
        * 3,000 coins; you have 1,200."
        */
        public static string describeWhyNot(Settlement settlement, Inventory purse, BuildingType type) {
            string name = BuildingCatalog.getDisplayName(type).ToLowerInvariant();
            if (!isUpgradable(type)) {
                return "A " + name + " cannot be upgraded.";
            }
            if (settlement.getNumBuildings(type) == 0) {
                return "No " + name + " here to upgrade.";
            }
            Building next = findNext(settlement, type);
            if (next == null) {
                return "Every " + name + " here is fully upgraded.";
            }
            int toLevel = next.getLevel() + 1;
            if (!isLevelUnlocked(settlement, toLevel)) {
                return "Level " + toLevel + " comes with the " + Eras.getDisplayName(eraFor(toLevel)) + " era.";
            }
            int price = getPrice(type, toLevel);
            int coins = purse.getNumItems(ItemType.COIN);
            if (coins < price) {
                return "Needs " + BuildingPurchase.formatCoins(price) + " coins; you have " + BuildingPurchase.formatCoins(coins) + ".";
            }
            return null;
        }

        /**
        * What a building of this type does at this level, against level 0:
        * "room for 6 residents (was 4)", "2 apples a harvest (was 1)",
        * "3 more market stalls (was 2)".
        */
        public static string describeEffect(BuildingType type, int level) {
            switch (type) {
                case BuildingType.HOUSE:
                    return "room for " + houseResidents(level) + " residents (was " + houseResidents(level - 1) + ")";
                case BuildingType.FARM:
                    return farmApples(level) + " apples a harvest (was " + farmApples(level - 1) + ")";
                case BuildingType.STOREHOUSE:
                    return storehouseStalls(level) + " more market stalls (was " + storehouseStalls(level - 1) + ")";
            }
            return "no change";
        }

        /**
        * Upgrades the next building of this type: takes the coins from the
        * purse, raises its level and applies what it does. Returns the
        * building upgraded, or null when describeWhyNot gives a reason (none
        * to upgrade, a later era, or too few coins), so a short purse is never
        * emptied for an upgrade it cannot pay for.
        */
        public static Building upgrade(Settlement settlement, Inventory purse, BuildingType type) {
            if (describeWhyNot(settlement, purse, type) != null) {
                return null;
            }
            Building next = findNext(settlement, type);
            purse.removeItem(ItemType.COIN, getPrice(type, next.getLevel() + 1));
            settlement.upgradeBuilding(next);
            return next;
        }
    }
}
