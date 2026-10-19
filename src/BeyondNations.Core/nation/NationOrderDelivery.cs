namespace beyondnations {

    /**
    * Hands over what a pawn has gathered under a NationOrder. Called by the
    * simulation every tick for every pawn, before behaviours run, so a pawn
    * that has just walked into one of its nation's settlements gives up the
    * ordered item before it has a chance to sell it at the market.
    *
    * Deliveries go into the settlement's own inventory (its stockpile), which
    * the leader collects with CollectSettlementStockpileCommand. Finishing an
    * order makes the pawn proud of its work: its relationship with a player
    * leader rises.
    */
    public static class NationOrderDelivery {
        public const int COMPLETION_RELATIONSHIP_GAIN = 3;

        /** Returns how many items were delivered this call. */
        public static int deliver(Pawn pawn, EntityRepository entityRepository, NationRepository nationRepository, Player player, EventProducer eventProducer) {
            NationOrder order = pawn.getOrder();
            if (order == null) {
                return 0;
            }
            if (pawn.getNationId() != order.getNationId() || nationRepository.getNation(order.getNationId()) == null) {
                pawn.setOrder(null);
                return 0;
            }
            if (!pawn.isCurrentlyInSettlement()) {
                return 0;
            }
            Settlement settlement = entityRepository.getEntity(pawn.getCurrentSettlementId()) as Settlement;
            if (settlement == null || settlement.getNationId() != order.getNationId()) {
                return 0;
            }
            int carried = pawn.getInventory().getNumItems(order.getItemType());
            int amount = System.Math.Min(carried, order.getRemaining());
            if (amount <= 0) {
                return 0;
            }

            pawn.getInventory().removeItem(order.getItemType(), amount);
            settlement.getInventory().addItem(order.getItemType(), amount);
            order.recordDelivery(amount);

            Nation nation = nationRepository.getNation(order.getNationId());
            bool playerLeads = nation.getLeaderId() == player.getId();
            string item = order.getItemType().ToString().ToLower();
            if (order.isComplete()) {
                pawn.setOrder(null);
                if (playerLeads) {
                    bool tierUp = PawnRelations.raise(pawn, player, COMPLETION_RELATIONSHIP_GAIN, eventProducer);
                    player.getStatus().update(pawn.getName() + " finished your order: " + order.getQuantity() + " " + item + " delivered to the stockpile." + PawnRelations.tierUpSuffix(tierUp, pawn, player));
                }
            }
            else if (playerLeads) {
                player.getStatus().update(pawn.getName() + " delivered " + amount + " " + item + " to the stockpile (" + order.getDelivered() + "/" + order.getQuantity() + ").");
            }
            return amount;
        }
    }
}
