using System.Collections.Generic;

namespace beyondnations {

    public class Stall {
        private EntityId ownerId;
        private Inventory inventory;

        public static readonly int WOOD_COST_TO_BUILD = 20;
        public static readonly int STONE_COST_TO_BUILD = 5;
        public static readonly int COIN_COST_TO_PURCHASE = 100;

        public Stall() {
            ownerId = null;
            inventory = new Inventory(0);
        }

        /** Whether the inventory holds the wood and stone a new stall costs. */
        public static bool canAffordToBuild(Inventory inventory) {
            return inventory.getNumItems(ItemType.WOOD) >= WOOD_COST_TO_BUILD
                && inventory.getNumItems(ItemType.STONE) >= STONE_COST_TO_BUILD;
        }

        /** Removes the wood and stone a new stall costs. */
        public static void payBuildCost(Inventory inventory) {
            inventory.removeItem(ItemType.WOOD, WOOD_COST_TO_BUILD);
            inventory.removeItem(ItemType.STONE, STONE_COST_TO_BUILD);
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