
namespace beyondnations {

    public class SellResourcesAtMarketCommand {
        private NationRepository nationRepository;
        private EntityRepository entityRepository;

        public SellResourcesAtMarketCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public void execute(Player player) {
            EntityId currentSettlementId = player.getCurrentSettlementId();
            if (currentSettlementId == null) {
                player.getStatus().update("You are not inside a settlement.");
                return;
            }

            Settlement settlement = (Settlement) entityRepository.getEntity(currentSettlementId);
            Market market = settlement.getMarket();
            if (market == null) {
                player.getStatus().update("This settlement does not have a market.");
                return;
            }
            if (market.getNumStalls() == 0) {
                player.getStatus().update("This settlement does not have any stalls.");
                return;
            }
            
            
            // what the bag held before, for the receipt (#182: the line used to say only "You sold resources")
            System.Collections.Generic.Dictionary<ItemType, int> before = new System.Collections.Generic.Dictionary<ItemType, int>();
            foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType))) {
                before[itemType] = player.getInventory().getNumItems(itemType);
            }
            EntityId stallOwnerId = market.sellResources(player);
            if (stallOwnerId == null) {
                player.getStatus().update(market.describeWhyNoSale(player) ?? "No one at this market is buying what you carry.");
                return;
            }
            player.recordMarketSale();

            // increase relationship with owner of stall
            Entity stallOwner = entityRepository.getEntity(stallOwnerId);
            if (stallOwner == null) {
                Log.error("stall owner is null");
                return;
            }
            player.increaseRelationship(stallOwner, 1);
            if (stallOwner is Player) {
                Player stallOwnerPlayer = (Player) stallOwner;
                stallOwnerPlayer.increaseRelationship(player, 1);
            }
            else if (stallOwner is Pawn) {
                Pawn stallOwnerPawn = (Pawn) stallOwner;
                stallOwnerPawn.increaseRelationship(player, 1);
            }
            else {
                Log.error("stall owner is not a player or pawn");
                return;
            }
            
            Diplomacy.recordTrade(nationRepository, player.getNationId(), settlement);
            player.getStatus().update(describeSale(before, player.getInventory(), stallOwner.getName())
                + " Relationship increased to " + player.getRelationships()[stallOwnerId] + ".");
        }

        /** "Sold 1 stone and 1 wood to Abigail Davies for 25 coins.", from the bag before and after. */
        public static string describeSale(System.Collections.Generic.Dictionary<ItemType, int> before, Inventory after, string buyer) {
            System.Collections.Generic.List<string> goods = new System.Collections.Generic.List<string>();
            foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType))) {
                int sold = before[itemType] - after.getNumItems(itemType);
                if (itemType != ItemType.COIN && sold > 0) {
                    goods.Add(ItemNames.describeQuantity(itemType, sold));
                }
            }
            int coins = after.getNumItems(ItemType.COIN) - before[ItemType.COIN];
            string list = goods.Count <= 1 ? string.Join("", goods)
                : string.Join(", ", goods.GetRange(0, goods.Count - 1)) + " and " + goods[goods.Count - 1];
            return "Sold " + list + " to " + buyer + " for " + coins + (coins == 1 ? " coin." : " coins.");
        }
    }
}