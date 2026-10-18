using System.Collections.Generic;

namespace beyondnations {

    public class Stall {
        private EntityId ownerId;
        private Inventory inventory;

        public static readonly int WOOD_COST_TO_BUILD = 50;
        public static readonly int COIN_COST_TO_PURCHASE = 100;

        public Stall() {
            ownerId = null;
            inventory = new Inventory(0);
        }

        public EntityId getOwnerId() {
            return ownerId;
        }

        public void setOwnerId(EntityId ownerId) {
            this.ownerId = ownerId;
        }

        public Inventory getInventory() {
            return inventory;
        }

        public void transferContentsToEntity(Entity entity) {
            entity.getInventory().transferContentsOfInventory(inventory);
        }

        public void transferContentsFromEntity(Entity entity) {
            inventory.transferContentsOfInventory(entity.getInventory());
        }
    }
}