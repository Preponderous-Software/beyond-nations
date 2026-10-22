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
    * one call. Raid is a Drastic command: on its own row and confirmed by a
    * second click (CommandBarLayout), since a playtester raided a caravan
    * that had just come into range by clicking where Enter Settlement was.
    */
    public static class CaravanCommands {
        public const string SendCaravanLabel = "Send Caravan";
        public const int MaxBuyButtons = 3;

        public static void addCommands(Simulation simulation, List<WorldCommand> commands) {
            TradeCaravans trade = simulation.getTradeCaravans();
            Player player = simulation.getPlayer();

            if (player.isCurrentlyInSettlement()) {
                if (trade.getDispatchRefusal() == null) {
                    commands.Add(new WorldCommand(SendCaravanLabel, CommandGroup.Trade, () => CaravanDispatchPanel.open(simulation)));
                }
                else {
                    // in their own settlement a leader still sees the button, saying what
                    // stands in the way; pressing it puts the whole reason on the status line
                    // (playtest 0.4.1 s05: the refusal was hidden with the button)
                    string blocked = getBlockedSendLabel(simulation);
                    if (blocked != null) {
                        commands.Add(new WorldCommand(blocked, CommandGroup.Trade, () => CaravanDispatchPanel.open(simulation)));
                    }
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
                commands.Add(new WorldCommand("Buy " + ItemNames.getSingularName(itemType) + " (" + price + ")", CommandGroup.Trade, () =>
                    trade.buyFromCaravan(caravan, itemType)));
                offered++;
            }
            if (trade.getGoodsToSellTo(caravan).Count > 0) {
                commands.Add(new WorldCommand("Sell to " + name, CommandGroup.Trade, () => trade.sellToCaravan(caravan)));
            }
            if (trade.canEscort(caravan)) {
                commands.Add(new WorldCommand("Escort " + name, CommandGroup.Trade, () => trade.escort(caravan)));
            }
            if (trade.canRaid(caravan)) {
                commands.Add(new WorldCommand("Raid " + name, CommandGroup.Drastic, () => trade.raid(caravan)));
            }
        }

        /**
        * For a leader in one of their own settlements who cannot send a
        * caravan from it yet, the button's label with the short reason:
        * "Send Caravan (Town era)", "Send Caravan (needs Storehouse)" or
        * "Send Caravan (2 on the road)". Null anywhere else, or when a caravan
        * can be sent.
        */
        public static string getBlockedSendLabel(Simulation simulation) {
            TradeCaravans trade = simulation.getTradeCaravans();
            Player player = simulation.getPlayer();
            Settlement here = trade.getPlayerSettlement();
            if (here == null || trade.getDispatchRefusal() == null
                    || !NationDiplomacy.playerLeads(player, simulation.getNationRepository())
                    || !here.getNationId().Equals(player.getNationId())) {
                return null;
            }
            if (here.getNumBuildings(BuildingType.STOREHOUSE) == 0) {
                return SendCaravanLabel + (here.isBuildingUnlocked(BuildingType.STOREHOUSE)
                    ? " (needs Storehouse)"
                    : " (" + Eras.getDisplayName(EraRules.unlockedIn(BuildingType.STOREHOUSE)) + " era)");
            }
            return SendCaravanLabel + " (" + trade.getNumPlayerCaravans() + " on the road)";
        }
    }
}
