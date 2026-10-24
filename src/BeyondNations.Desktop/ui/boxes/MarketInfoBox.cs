using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

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
    *
    * Last come the living prices (MarketPricing): what each good costs to buy
    * here and what a stall pays for one, with an arrow for which way the
    * price has moved since the last pricing round -- the readout a player
    * needs to buy low here and sell high somewhere else.
    */
    public class MarketInfoBox : InfoBox {
        public const string PricesHeading = "Prices (buy/sell):";

        /** Always listed: the goods every settlement deals in. */
        private static readonly ItemType[] StapleGoods = { ItemType.APPLE, ItemType.WOOD, ItemType.STONE };

        /** Listed once a stall has some, or the price has moved off the plain curve. */
        private static readonly ItemType[] OtherGoods = { ItemType.CHICKEN_MEAT, ItemType.COOKED_MEAT, ItemType.PLANKS, ItemType.BRICKS };

        private readonly Market market;
        private readonly EntityRepository entityRepository;

        public MarketInfoBox(Market market, EntityRepository entityRepository) : base("Market Info") {
            this.market = market;
            this.entityRepository = entityRepository;
        }

        /** The goods the price list shows right now, in order. */
        public List<ItemType> getListedGoods() {
            List<ItemType> goods = new List<ItemType>(StapleGoods);
            foreach (ItemType itemType in OtherGoods) {
                if (market.getQuantityAvailable(itemType) > 0
                        || market.getPrices().getPressure(itemType) != MarketPrices.DEFAULT_PRESSURE) {
                    goods.Add(itemType);
                }
            }
            return goods;
        }

        /** "^", "v" or "-": the trend as text, for tests and for anything that cannot draw the arrow. */
        public static string trendMark(int trend) {
            return trend > 0 ? "^" : trend < 0 ? "v" : "-";
        }

        /** "Wood 54/49" -- buy, then sell. */
        public string describePrice(ItemType itemType) {
            return ItemNames.getDisplayName(itemType) + " " + market.getBuyPrice(itemType) + "/" + market.getSellPrice(itemType);
        }

        public override List<string> getLines() {
            List<string> lines = new List<string>();
            if (isCompact()) {
                // the totals in one short line; the merchants and prices are what a trader reads
                lines.Add(market.getNumStalls() + "/" + market.getMaxNumStalls() + " stalls, " + market.getTotalCoins() + " coins");
            }
            else {
                lines.Add("Stalls: " + market.getNumStalls() + "/" + market.getMaxNumStalls());
                lines.Add("Stalls for Sale: " + market.getNumStallsForSale());
                lines.Add("Total Coins: " + market.getTotalCoins());
                lines.Add("Items Bought: " + market.getTotalNumItemsBought());
                lines.Add("Items Sold: " + market.getTotalNumItemsSold());
            }

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
                else if (owner != null) {
                    merchantNames.Add(owner.getType().ToString());
                }
            }

            if (merchantNames.Count > 0 && isCompact()) {
                // one line, not one a merchant: six merchants ran the column
                // off the bottom of a 960x540 screen (playtest 0.6.1, #350)
                lines.Add(describeMerchantsCompact(merchantNames));
            }
            else if (merchantNames.Count > 0) {
                lines.Add("Merchants:");
                foreach (string name in merchantNames) {
                    lines.Add("- " + name);
                }
            }

            lines.Add(PricesHeading);
            foreach (ItemType itemType in getListedGoods()) {
                lines.Add(describePrice(itemType) + " " + trendMark(MarketPricing.getTrend(market, itemType)));
            }

            return lines;
        }

        /** "Merchants: Ann, Bob +4", naming at most two. */
        public static string describeMerchantsCompact(List<string> names) {
            int shown = System.Math.Min(2, names.Count);
            string line = "Merchants: " + string.Join(", ", names.GetRange(0, shown));
            return names.Count > shown ? line + " +" + (names.Count - shown) : line;
        }

        /**
        * The text lines as usual, except that each price line ends in a drawn
        * arrow -- green and pointing up for a rising price, red and down for a
        * falling one -- instead of its text mark. The arrow is stacked
        * rectangles, which keeps to the fixed-signature draw calls the
        * browser build can make.
        */
        protected override void drawBody() {
            List<string> lines = getLines();
            List<ItemType> goods = getListedGoods();
            int firstPrice = lines.Count - goods.Count;
            float width = System.MathF.Max(40f, ImGui.GetContentRegionAvail().X);
            for (int i = 0; i < lines.Count; i++) {
                if (i < firstPrice) {
                    foreach (string part in TextWrap.wrap(lines[i], width, text => ImGui.CalcTextSize(text).X)) {
                        ImGui.TextUnformatted(part);
                    }
                    continue;
                }
                ItemType itemType = goods[i - firstPrice];
                int trend = MarketPricing.getTrend(market, itemType);
                ImGui.TextUnformatted(describePrice(itemType));
                ImGui.SameLine();
                drawTrendArrow(trend);
            }
        }

        private static void drawTrendArrow(int trend) {
            Vector2 at = ImGui.GetCursorScreenPos();
            float size = ImGui.GetTextLineHeight();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            if (trend == 0) {
                uint grey = ImGui.GetColorU32(new Vector4(0.7f, 0.7f, 0.7f, 1f));
                drawList.AddRectFilled(new Vector2(at.X + 1f, at.Y + size * 0.45f), new Vector2(at.X + size * 0.7f, at.Y + size * 0.55f), grey);
            }
            else {
                uint colour = ImGui.GetColorU32(trend > 0 ? new Vector4(0.45f, 0.95f, 0.45f, 1f) : new Vector4(1f, 0.45f, 0.4f, 1f));
                const int rows = 4;
                float rowHeight = size * 0.7f / rows;
                float top = at.Y + size * 0.15f;
                float centre = at.X + size * 0.35f;
                for (int r = 0; r < rows; r++) {
                    // the point is the narrow row: at the top for up, the bottom for down
                    int widthStep = trend > 0 ? r + 1 : rows - r;
                    float half = size * 0.35f * widthStep / rows;
                    float y = top + r * rowHeight;
                    drawList.AddRectFilled(new Vector2(centre - half, y), new Vector2(centre + half, y + rowHeight + 0.5f), colour);
                }
            }
            ImGui.Dummy(new Vector2(size * 0.75f, size));
        }
    }
}
