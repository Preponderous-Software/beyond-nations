using System;
using System.Globalization;

namespace beyondnations {

    /**
    * What a save says about itself on its first line: enough to list it in a
    * menu (day, nation, when) and to decide whether it can be read at all
    * (format version), without reading the world that follows.
    */
    public class SaveHeader {
        public const string Magic = "beyond-nations-save";

        public int FormatVersion { get; internal set; }
        public string GameVersion { get; internal set; }
        public int Seed { get; internal set; }
        public int Tick { get; internal set; }
        /** The in-game day, counted from 1. */
        public int Day { get; internal set; }
        /** The player's nation, or null for a player with none. */
        public string Nation { get; internal set; }
        public DateTime SavedAtUtc { get; internal set; }
        public int BodyBytes { get; internal set; }
        public string Crc32 { get; internal set; }

        /** "Day 3 - Xland" or "Day 3 - no nation". */
        public string describe() {
            return "Day " + Day + " - " + (Nation ?? "no nation");
        }
    }
}
