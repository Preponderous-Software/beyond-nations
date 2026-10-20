using System.Collections.Generic;

namespace beyondnations.desktop.ui {

    /**
    * The command bar's trade buttons (TradeCaravans).
    *
    * Out on the road, standing by a caravan: buy one of each good it carries
    * (up to three), sell it what it deals in, ride guard on it, or rob it.
    * Each label names the price, so the bar is the caravan's price list.
    *
    * In one of the player's own settlements with a Storehouse, as its
    * leader: Send Caravan, which opens the dispatch panel (CaravanDispatchPanel).
    *
    * Kept apart from WorldCommandBar so the trade lane touches that list with
    * one call.
    */
    public static class CaravanCommands {
        public const string SendCaravanLabel = "Send Caravan";
        public const int MaxBuyButtons = 3;

        public static void addCommands(Simulation simulation, List<WorldCommand> commands) {
            TradeCaravans trade = simulation.getTradeCaravans();
            Player player = simulation.getPlayer();

            if (player.isCurrentlyInSettlement()) {
                if (trade.getDispatchRefusal() == null) {
                    commands.Add(new WorldCommand(SendCaravanLabel, () => CaravanDispatchPanel.open(simulation)));
                }
                return;
            }

            Caravan caravan = trade.findCaravanInReach();
            if (caravan == null) {
                return;
            }
            string name = caravan.isPlayerOwned() ? "Your Caravan" : "Caravan";
            int offered = 0;
            foreach (ItemStack stack in caravan.getCargo()) {
                if (offered >= MaxBuyButtons) {
                    break;
                }
                ItemType itemType = stack.getItemType();
                int price = trade.getPriceToBuyFrom(caravan, itemType);
                commands.Add(new WorldCommand("Buy " + ItemNames.getSingularName(itemType) + " (" + price + ")", () =>
                    trade.buyFromCaravan(caravan, itemType)));
                offered++;
            }
            if (trade.getGoodsToSellTo(caravan).Count > 0) {
                commands.Add(new WorldCommand("Sell to " + name, () => trade.sellToCaravan(caravan)));
            }
            if (trade.canEscort(caravan)) {
                commands.Add(new WorldCommand("Escort " + name, () => trade.escort(caravan)));
            }
            if (trade.canRaid(caravan)) {
                commands.Add(new WorldCommand("Raid " + name, () => trade.raid(caravan)));
            }
        }
    }
}
