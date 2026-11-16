using System.Collections.Generic;

namespace beyondnations {

    /**
    * The leader -- or the member the settlement was given to (#176) -- takes
    * the goods out of the settlement's stockpile: the wood and stone member
    * pawns delivered under orders, and whatever the leader's own caravans
    * brought back unsold. Coins are left alone: those are the settlement's
    * funds, withdrawn with WithdrawSettlementFundsCommand.
    *
    * It used to take only wood and stone, so the 15 saplings a caravan
    * brought home unsold sat on the stockpile where nothing could reach them
    * and the player took them for lost (0.8.0 re-run s05, #485). Now it
    * takes every good but coin.
    */
    public class CollectSettlementStockpileCommand {
        private readonly NationRepository nationRepository;
        private readonly EntityRepository entityRepository;

        public CollectSettlementStockpileCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        /** Every good on the stockpile, coins aside: what Collect Stockpile would hand over. */
        public static int getStockpileSize(Settlement settlement) {
            int total = 0;
            foreach (ItemStack stack in getGoods(settlement)) {
                total += stack.getQuantity();
            }
            return total;
        }

        /** The goods on the stockpile, coins aside, in the inventory's order (wood and stone first). */
        public static List<ItemStack> getGoods(Settlement settlement) {
            List<ItemStack> goods = new List<ItemStack>();
            foreach (ItemSlot slot in settlement.getInventory().getSlots()) {
                if (slot.isEmpty() || slot.getItemStack().getItemType() == ItemType.COIN || slot.getQuantity() <= 0) {
                    continue;
                }
                goods.Add(new ItemStack(slot.getItemStack().getItemType(), slot.getQuantity()));
            }
            return goods;
        }

        public void execute(Player player) {
            if (player.getNationId() == null) {
                player.getStatus().update("You are not in a nation.");
                return;
            }
            Nation nation = nationRepository.getNation(player.getNationId());
            if (player.getCurrentSettlementId() == null) {
                player.getStatus().update("You are not inside a settlement.");
                return;
            }
            Settlement settlement = (Settlement) entityRepository.getEntity(player.getCurrentSettlementId());
            // the leader, or the member it was given to (#176)
            string refusal = SettlementOwnership.refusal(player.getId(), nation, settlement, entityRepository);
            if (refusal != null) {
                player.getStatus().update(refusal);
                return;
            }
            List<ItemStack> goods = getGoods(settlement);
            if (goods.Count == 0) {
                player.getStatus().update("The stockpile is empty. Order members to gather for the nation to fill it.");
                return;
            }

            List<string> parts = new List<string>();
            foreach (ItemStack stack in goods) {
                settlement.getInventory().removeItem(stack.getItemType(), stack.getQuantity());
                player.getInventory().addItem(stack.getItemType(), stack.getQuantity());
                parts.Add(ItemNames.describeQuantity(stack.getItemType(), stack.getQuantity()));
            }
            player.getStatus().update("Collected " + describeList(parts) + " from the stockpile.");
        }

        /** "221 wood, 67 stone and 15 saplings" */
        private static string describeList(List<string> parts) {
            if (parts.Count == 1) {
                return parts[0];
            }
            return string.Join(", ", parts.GetRange(0, parts.Count - 1)) + " and " + parts[parts.Count - 1];
        }
    }
}
