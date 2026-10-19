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

        public Building(BuildingType type, int slot) {
            this.type = type;
            this.slot = slot;
        }

        public BuildingType getType() {
            return type;
        }

        public int getSlot() {
            return slot;
        }
    }
}
