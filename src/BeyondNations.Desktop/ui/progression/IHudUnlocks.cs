using System.Collections.Generic;

namespace beyondnations.desktop.ui.progression {

    /**
    * A part of the HUD, or a family of command-bar buttons, that a new game
    * does not show until play makes it useful (unlock as you go, like
    * Primordial Rise's unlocks.js and FishE's progression). What each one
    * covers, and when it comes, is HudUnlocks' table.
    *
    * Basics is never locked: walking, Use, Eat, the energy, food and coin
    * meters, the season chip, the inventory, the NEXT line, the Journal, the map and the
    * menu are there from the first second.
    */
    public enum UnlockId {
        Basics,
        Nation,
        Crafting,
        Founding,
        Settlement,
        Era,
        Settlers,
        Building,
        Market,
        Members,
        Treasury,
        Laws,
        Caravans,
        Diplomacy,
        Winter,
        Feeding,
        Saplings,
    }

    /** The one card an unlock shows the first time it comes: what appeared and why, with no keys named. */
    public sealed class UnlockCard {
        public readonly UnlockId id;
        public readonly string title;
        public readonly string text;
        /** A plain icon name ("nation", "coins", "caravan"...) for the card's picture. */
        public readonly string icon;

        public UnlockCard(UnlockId id, string title, string text, string icon) {
            this.id = id;
            this.title = title;
            this.text = text;
            this.icon = icon;
        }
    }

    /**
    * What the HUD asks about unlocks (the contextual HUD design, lane C).
    * Presentation only: nothing here changes what the simulation allows,
    * only whether the HUD shows it yet.
    */
    public interface IHudUnlocks {
        bool isUnlocked(UnlockId id);

        /** Whether a command-bar button, by its label as WorldCommandBar makes it, is unlocked. Unknown labels are. */
        bool commandUnlocked(string label);

        /** The cards for unlocks that came since the last call, oldest first; each card is handed out once. */
        IEnumerable<UnlockCard> drainNewCards();
    }

    /** Everything unlocked and nothing to announce: the stub before HudUnlocks, and --all-unlocked. */
    public sealed class EverythingUnlocked : IHudUnlocks {
        public bool isUnlocked(UnlockId id) { return true; }
        public bool commandUnlocked(string label) { return true; }
        public IEnumerable<UnlockCard> drainNewCards() { return new UnlockCard[0]; }
    }
}
