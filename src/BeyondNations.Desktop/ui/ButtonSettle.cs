using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace beyondnations.desktop.ui {

    /**
    * Holds a command bar button off for a moment after it appears or moves,
    * so a click aimed at whatever was there before does not land on it.
    *
    * The bar lays out what the moment offers, and some of that comes and
    * goes: a pawn walks out of reach and the buttons after its row drop into
    * its place; entering a settlement swaps Enter Settlement for Withdraw
    * Funds. In the 0.6.3 playtest (s03) a click on Talk bought a pickaxe for
    * 299 coins when the pawn left, and a double click on Enter Settlement
    * withdrew the settlement's funds. A button only takes clicks once it has
    * stood where it is for SettleSeconds; until then it is drawn disabled,
    * so the wait shows.
    *
    * Buttons are known by their label with the numbers taken out, so
    * "Collect Stockpile (12)" becoming "(13)" is the same button and does
    * not wait again. Time is passed in, as ConfirmGate's is, so the rule can
    * be tested without a clock.
    */
    public class ButtonSettle {
        public const double SettleSeconds = 0.5;

        private static readonly Regex Numbers = new Regex("[0-9]+");

        private struct Placement {
            public float X;
            public float Y;
            public double Since;
            public bool Seen;
        }

        private readonly Dictionary<string, Placement> placements = new Dictionary<string, Placement>();

        /** The key a label is known by: its numbers taken out. */
        public static string keyOf(string label) {
            return Numbers.Replace(label, "#");
        }

        /**
        * Records that the button is drawn at (x, y) this frame, and says
        * whether it has stood there long enough to take a click.
        */
        public bool isSettled(string label, float x, float y, double now) {
            string key = keyOf(label);
            Placement placement;
            if (!placements.TryGetValue(key, out placement) || placement.X != x || placement.Y != y) {
                placement = new Placement { X = x, Y = y, Since = now };
            }
            placement.Seen = true;
            placements[key] = placement;
            return now - placement.Since >= SettleSeconds;
        }

        /** Forgets the buttons not drawn this frame, so one that comes back waits again. */
        public void endFrame() {
            List<string> gone = new List<string>();
            List<string> drawn = new List<string>();
            foreach (KeyValuePair<string, Placement> entry in placements) {
                (entry.Value.Seen ? drawn : gone).Add(entry.Key);
            }
            foreach (string key in gone) {
                placements.Remove(key);
            }
            foreach (string key in drawn) {
                Placement placement = placements[key];
                placement.Seen = false;
                placements[key] = placement;
            }
        }
    }
}
