namespace beyondnations {

    /**
    * One building standing in a settlement. Like a Stall it is part of the
    * settlement rather than an entity of its own: it has no position of its
    * own, only the slot it was given when it went up, which is where the
    * settlement's layout draws it.
    */
    public class Building {
        private readonly BuildingType type;
        private readonly int slot;
        private int level;

        public Building(BuildingType type, int slot) : this(type, slot, 0) {
        }

        public Building(BuildingType type, int slot, int level) {
            this.type = type;
            this.slot = slot;
            this.level = level;
        }

        public BuildingType getType() {
            return type;
        }

        public int getSlot() {
            return slot;
        }

        /** How many times it has been upgraded (BuildingUpgrades): 0 as built, at most BuildingUpgrades.getMaxLevel. */
        public int getLevel() {
            return level;
        }

        /** Raises the level by one. The caller has checked the upgrade is allowed and taken the coins. */
        internal void raiseLevel() {
            level++;
        }
    }
}
