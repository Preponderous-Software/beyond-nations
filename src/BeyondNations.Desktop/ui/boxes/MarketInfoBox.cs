using System.Collections.Generic;

namespace beyondnations.desktop.ui.boxes {

    /**
    * The market of the settlement the player is standing in, ported from
    * Assets/Scripts/ui/boxes/MarketInfoBox.cs.
    *
    * The five totals are followed by the merchant roster, which is only shown
    * when there is one. The Unity version grew its own box to fit that list by
    * incrementing a counter before drawing the frame; ImGui fits the window to
    * its contents, so the counter is gone -- and with it the bug where the
    * counter was a field and so grew again on every single frame.
    */
    public class MarketInfoBox : InfoBox {
        private readonly Market market;
        private readonly EntityRepository entityRepository;

        public MarketInfoBox(Market market, EntityRepository entityRepository) : base("Market Info") {
            this.market = market;
            this.entityRepository = entityRepository;
        }

        public override List<string> getLines() {
            List<string> lines = new List<string>();
            lines.Add("Stalls: " + market.getNumStalls() + "/" + market.getMaxNumStalls());
            lines.Add("Stalls for Sale: " + market.getNumStallsForSale());
            lines.Add("Total Coins: " + market.getTotalCoins());
            lines.Add("Items Bought: " + market.getTotalNumItemsBought());
            lines.Add("Items Sold: " + market.getTotalNumItemsSold());

            List<string> merchantNames = new List<string>();
            foreach (Stall stall in market.getStalls()) {
                if (stall.getOwnerId() == null) {
                    continue;
                }
                Entity owner = entityRepository.getEntity(stall.getOwnerId());
                if (owner is Player) {
                    merchantNames.Add("You");
                }
                else if (owner is Pawn) {
                    merchantNames.Add(((Pawn) owner).getName());
                }
                else {
                    merchantNames.Add(owner.getType().ToString());
                }
            }

            if (merchantNames.Count > 0) {
                lines.Add("Merchants:");
                foreach (string name in merchantNames) {
                    lines.Add("- " + name);
                }
            }

            return lines;
        }
    }
}
