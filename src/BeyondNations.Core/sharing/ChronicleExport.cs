using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace beyondnations {

    /**
    * The chronicle as a document to keep: "The History of Ironvale", in
    * Markdown, so it reads as plain text and renders anywhere Markdown does.
    *
    * It opens with the world's seed (and the link that rebuilds it), the day
    * the story has reached, and the nations standing now, then tells every
    * entry the chronicle still holds, oldest first, under a heading per day.
    * Entries about the player or the player's nation are in bold, as the HUD
    * draws them in gold. The chronicle keeps its most recent
    * Chronicle.DefaultCapacity entries (and they are saved with the world);
    * when older ones have been dropped the document says how many.
    *
    * It only reads: exporting never changes the world or the chronicle.
    */
    public static class ChronicleExport {

        /** "beyond-nations-seed-42-history.md". */
        public static string fileName(int seed) {
            return "beyond-nations-seed-" + seed.ToString(CultureInfo.InvariantCulture).Replace('-', 'm') + "-history.md";
        }

        /** The player's nation's name, or null when the player has none. */
        public static string playerNationName(Simulation simulation) {
            NationId id = simulation.getPlayer().getNationId();
            Nation nation = id != null ? simulation.getNationRepository().getNation(id) : null;
            return nation != null ? nation.getName() : null;
        }

        /** The document's title: "The History of Ironvale", or of a wanderer's world. */
        public static string title(Simulation simulation) {
            string nation = playerNationName(simulation);
            return nation != null ? "The History of " + nation : "The History of a Wanderer's World";
        }

        /** shareLink is the link that rebuilds this world (SeedLink), or null to leave it out. */
        public static string write(Simulation simulation, string shareLink) {
            Chronicle chronicle = simulation.getChronicle();
            Player player = simulation.getPlayer();
            int seed = simulation.getRandom().getSeed();
            int day = simulation.getTimeOfDay().getDay() + 1;

            StringBuilder md = new StringBuilder(8 * 1024);
            md.Append("# ").Append(title(simulation)).Append("\n\n");
            md.Append("A chronicle of Beyond Nations, told to day ").Append(day)
                .Append(" (tick ").Append(simulation.getTickCounter().getTotalTicks().ToString(CultureInfo.InvariantCulture)).Append(").\n\n");
            md.Append("- World seed: `").Append(WorldSeed.format(seed)).Append("`\n");
            if (shareLink != null) {
                md.Append("- Play this world: ").Append(shareLink).Append('\n');
            }
            string own = playerNationName(simulation);
            md.Append("- You: ").Append(own != null ? "of " + own : "a wanderer, sworn to no nation").Append('\n');
            md.Append('\n');

            md.Append("## The nations today\n\n");
            List<Nation> nations = simulation.getNationRepository().getNations();
            if (nations.Count == 0) {
                md.Append("No nation stands yet.\n\n");
            } else {
                foreach (Nation nation in nations) {
                    bool yours = player.getNationId() != null && nation.getId().Equals(player.getNationId());
                    md.Append("- **").Append(nation.getName()).Append("**: ")
                        .Append(plural(nation.getNumberOfMembers(), "member")).Append(", ")
                        .Append(plural(nation.getNumberOfSettlements(), "settlement"))
                        .Append(yours ? " (your nation)" : "").Append('\n');
                }
                md.Append('\n');
            }

            md.Append("## The chronicle\n\n");
            IReadOnlyList<ChronicleEntry> entries = chronicle.getEntries();
            int forgotten = chronicle.getTotalRecorded() - entries.Count;
            if (forgotten > 0) {
                md.Append("*The chronicle keeps its last ").Append(chronicle.getCapacity()).Append(" entries; ")
                    .Append(plural(forgotten, "older entry", "older entries")).Append(" have passed out of memory.*\n\n");
            }
            if (entries.Count == 0) {
                md.Append("Nothing worth telling has happened yet.\n");
                return md.ToString();
            }
            for (int i = 0; i < entries.Count; i++) {
                ChronicleEntry entry = entries[i];
                if (i == 0 || entry.getDay() != entries[i - 1].getDay()) {
                    if (i > 0) {
                        md.Append('\n');
                    }
                    md.Append("### Day ").Append(entry.getDay()).Append("\n\n");
                }
                md.Append("- *").Append(entry.getPartOfDay()).Append("*: ");
                md.Append(entry.concerns(player) ? "**" + entry.getText() + "**" : entry.getText());
                md.Append('\n');
            }
            return md.ToString();
        }

        private static string plural(int n, string one) {
            return plural(n, one, one + "s");
        }

        private static string plural(int n, string one, string many) {
            return n.ToString(CultureInfo.InvariantCulture) + " " + (n == 1 ? one : many);
        }
    }
}
