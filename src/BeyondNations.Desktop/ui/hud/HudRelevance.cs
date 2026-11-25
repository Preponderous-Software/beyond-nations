namespace beyondnations.desktop.ui.hud {

    /**
    * When each contextual piece of the world HUD shows: only while it is
    * relevant (Sisyphus' and Atman's "lines shown only while true"). Pure
    * functions of plain values, checked by unit tests (TestHudRelevance).
    */
    public static class HudRelevance {
        /** The settlement box: only inside a settlement. */
        public static bool showSettlement(bool inSettlement) {
            return inSettlement;
        }

        /** The market box: only inside a settlement that has a market, once markets are unlocked. */
        public static bool showMarket(bool inSettlement, bool hasMarket, bool marketUnlocked) {
            return inSettlement && hasMarket && marketUnlocked;
        }

        /**
        * The winter countdown and the food-for-winter lines: only in autumn
        * (winter is near) and in winter, once winter is unlocked.
        */
        public static bool showWinterLines(Season season, bool winterUnlocked) {
            return winterUnlocked && (season == Season.AUTUMN || season == Season.WINTER);
        }

        /** The carrying chip: only while the player carries something (coins are a meter, not cargo). */
        public static bool showCarrying(int itemsCarried) {
            return itemsCarried > 0;
        }

        /** The carrying chip's line: "Carrying 12 wood, 3 apples +2 more". */
        public static string carryingLine(System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<string, int>> items, int shown = 2) {
            if (items == null || items.Count == 0) {
                return null;
            }
            System.Text.StringBuilder text = new System.Text.StringBuilder("Carrying ");
            int listed = System.Math.Min(shown, items.Count);
            for (int i = 0; i < listed; i++) {
                if (i > 0) {
                    text.Append(", ");
                }
                text.Append(items[i].Value).Append(' ').Append(items[i].Key);
            }
            if (items.Count > listed) {
                text.Append(" +").Append(items.Count - listed).Append(" more");
            }
            return text.ToString();
        }

        /** The coins meter: only once coins are unlocked or the player has some. */
        public static bool showCoins(bool coinsUnlocked, int coins) {
            return coinsUnlocked || coins > 0;
        }
    }

    /**
    * One icon meter on the top strip (energy, food, coins): dim while all is
    * well, lit while the value is low or has just changed, so the eye goes to
    * it only when it matters. Pure: the clock is passed in.
    */
    public class MeterTracker {
        /** How long a change keeps the meter lit, in seconds. */
        public const double ChangeSeconds = 3.0;

        private bool seen;
        private float last;
        private double changedAt = double.NegativeInfinity;

        /** Takes the value as it stands now; true when the meter is lit (low, or changed within ChangeSeconds). */
        public bool update(float value, bool low, double now, float threshold = 1f) {
            if (!seen) {
                seen = true;
                last = value;
            } else if (System.MathF.Abs(value - last) >= threshold) {
                last = value;
                changedAt = now;
            }
            return low || now - changedAt < ChangeSeconds;
        }

        /** Whether it changed within ChangeSeconds of now. */
        public bool isChanging(double now) {
            return now - changedAt < ChangeSeconds;
        }
    }
}
