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

        /** Whether a builder can pay, counting the settlement's stockpile as well as what it carries. */
        public static bool canAffordToBuild(Inventory builder, Inventory stockpile) {
            return available(builder, stockpile, ItemType.WOOD) >= WOOD_COST_TO_BUILD
                && available(builder, stockpile, ItemType.STONE) >= STONE_COST_TO_BUILD;
        }

        /** Takes the cost from what the builder carries first, then from the stockpile. The caller has checked. */
        public static void payBuildCost(Inventory builder, Inventory stockpile) {
            take(builder, stockpile, ItemType.WOOD, WOOD_COST_TO_BUILD);
            take(builder, stockpile, ItemType.STONE, STONE_COST_TO_BUILD);
        }

        private static int available(Inventory builder, Inventory stockpile, ItemType itemType) {
            return builder.getNumItems(itemType) + (stockpile == null ? 0 : stockpile.getNumItems(itemType));
        }

        private static void take(Inventory builder, Inventory stockpile, ItemType itemType, int amount) {
            int fromBuilder = System.Math.Min(amount, builder.getNumItems(itemType));
            if (fromBuilder > 0) {
                builder.removeItem(itemType, fromBuilder);
            }
            if (amount - fromBuilder > 0) {
                stockpile.removeItem(itemType, amount - fromBuilder);
            }
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