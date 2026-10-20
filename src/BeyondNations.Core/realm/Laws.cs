using System.Text.Json;
using System.Collections.Generic;

namespace beyondnations {

    /** The four laws a ruler can set for their nation. */
    public enum LawType {
        TAXES,
        CONSCRIPTION,
        OPEN_BORDERS,
        RATIONING
    }

    /**
    * What each law is called, the settings it can take and what each costs
    * and buys. The numbers live here, in one table, so the laws panel, the
    * docs and the tests all read the same thing.
    *
    * Every law starts at level 0, which is the world as it was before laws
    * existed: no tax, no levy, closed borders, no rationing. A nation whose
    * laws are all at 0 behaves exactly as it always did, so a seeded world
    * that never touches the laws takes the same course it always has.
    */
    public static class Laws {
        public static readonly LawType[] All = { LawType.TAXES, LawType.CONSCRIPTION, LawType.OPEN_BORDERS, LawType.RATIONING };

        // taxes: coins each member pawn pays per law round, by level
        public static readonly int[] TAX_COINS = { 0, 1, 3 };
        // taxes: law rounds between each point of loyalty lost, by level (0 = none lost)
        public static readonly int[] TAX_LOYALTY_EVERY = { 0, 3, 1 };
        // taxes: how much longer a settlement waits for an immigrant, in percent (-1 = none come)
        public static readonly int[] TAX_IMMIGRATION_PERCENT = { 100, 200, -1 };

        // conscription: one conscript defends for every this many member pawns, rounded up
        public const int CONSCRIPT_PER_MEMBERS = 2;
        // conscription: percent of each gather's wood and stone a conscript loses to drill
        public const int CONSCRIPT_GATHER_LOSS_PERCENT = 40;

        // open borders: immigrants come this many times as often
        public const int OPEN_BORDERS_IMMIGRATION_PERCENT = 50;
        // open borders: tolls, coins per settlement per law round
        public const int OPEN_BORDERS_TOLL = 2;
        // open borders: raiders carry off this many times the loot
        public const int OPEN_BORDERS_RAID_MULTIPLIER = 2;
        // open borders: every this many law rounds, thieves take from each settlement
        public const int OPEN_BORDERS_THEFT_EVERY = 3;
        public const int OPEN_BORDERS_THEFT_AMOUNT = 2;
        // open borders: every this many law rounds, trade warms the nation to every nation it is not at war with
        public const int OPEN_BORDERS_TRADE_EVERY = 4;

        // rationing: percent of a round's hunger handed back to each member pawn
        public const int RATIONING_SAVED_PERCENT = 25;
        // rationing: law rounds between each point of loyalty lost
        public const int RATIONING_LOYALTY_EVERY = 2;

        public static int getNumLevels(LawType law) {
            return law == LawType.TAXES ? 3 : 2;
        }

        public static string getName(LawType law) {
            switch (law) {
                case LawType.TAXES: return "Taxes";
                case LawType.CONSCRIPTION: return "Conscription";
                case LawType.OPEN_BORDERS: return "Borders";
                case LawType.RATIONING: return "Rationing";
                default: return law.ToString();
            }
        }

        /** The name of one setting of a law: "None", "Low", "High"; "Off", "On"; "Closed", "Open". */
        public static string getLevelName(LawType law, int level) {
            switch (law) {
                case LawType.TAXES:
                    return level == 0 ? "None" : (level == 1 ? "Low" : "High");
                case LawType.OPEN_BORDERS:
                    return level == 0 ? "Closed" : "Open";
                default:
                    return level == 0 ? "Off" : "On";
            }
        }

        /** What a setting buys and costs, in a line short enough for a phone. */
        public static string describe(LawType law, int level) {
            switch (law) {
                case LawType.TAXES:
                    if (level == 0) {
                        return "No tax: members keep their coin.";
                    }
                    if (level == 1) {
                        return "Each member pays " + TAX_COINS[1] + " coin every 10 s; loyalty slips slowly; immigrants come half as often.";
                    }
                    return "Each member pays " + TAX_COINS[2] + " coins every 10 s; loyalty falls fast; no immigrants come.";
                case LawType.CONSCRIPTION:
                    return level == 0
                        ? "No levy: members gather freely."
                        : "One conscript per " + CONSCRIPT_PER_MEMBERS + " members defends against raids and sieges; members gather "
                            + CONSCRIPT_GATHER_LOSS_PERCENT + "% less.";
                case LawType.OPEN_BORDERS:
                    return level == 0
                        ? "Closed: strangers keep away."
                        : "Immigrants come twice as often, tolls bring " + OPEN_BORDERS_TOLL + " coins a settlement and trade warms neighbours; raids take double and thieves steal.";
                case LawType.RATIONING:
                    return level == 0
                        ? "No rationing: members eat freely."
                        : "Members' hunger drains a quarter slower; morale slips.";
                default:
                    return "";
            }
        }
    }

    /**
    * The laws of one nation: the setting each law is at, and any change the
    * ruler has proclaimed that has yet to take effect. A change takes
    * Realm.LAW_DELAY_TICKS to come in, so a ruler cannot flip taxes on for
    * one collection and off again before anyone notices.
    */
    public class NationLaws {
        private NationId nationId;
        private int[] levels = new int[Laws.All.Length];
        // -1: nothing pending
        private int[] pending = new int[] { -1, -1, -1, -1 };
        private int[] proclaimedAt = new int[Laws.All.Length];
        private int lastTaxCollected = 0;
        private int totalTaxCollected = 0;
        private int deserters = 0;

        public NationLaws(NationId nationId) {
            this.nationId = nationId;
        }

        public NationId getNationId() {
            return nationId;
        }

        public int getLevel(LawType law) {
            return levels[(int) law];
        }

        public void setLevel(LawType law, int level) {
            levels[(int) law] = level;
        }

        /** The setting a proclaimed change will bring in, or -1. */
        public int getPending(LawType law) {
            return pending[(int) law];
        }

        public int getProclaimedAt(LawType law) {
            return proclaimedAt[(int) law];
        }

        public void proclaim(LawType law, int level, int tick) {
            pending[(int) law] = level;
            proclaimedAt[(int) law] = tick;
        }

        public void clearPending(LawType law) {
            pending[(int) law] = -1;
        }

        public bool isOn(LawType law) {
            return getLevel(law) > 0;
        }

        public int getLastTaxCollected() { return lastTaxCollected; }
        public int getTotalTaxCollected() { return totalTaxCollected; }
        public int getDeserters() { return deserters; }

        public void recordTax(int coins) {
            lastTaxCollected = coins;
            totalTaxCollected += coins;
        }

        public void recordDeserter() {
            deserters++;
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.write("nationId", nationId);
            writeInts(writer, "levels", levels);
            writeInts(writer, "pending", pending);
            writeInts(writer, "proclaimedAt", proclaimedAt);
            writer.write("lastTaxCollected", lastTaxCollected);
            writer.write("totalTaxCollected", totalTaxCollected);
            writer.write("deserters", deserters);
        }

        internal static NationLaws load(JsonElement element, SaveReader reader) {
            NationLaws laws = new NationLaws(reader.getNationId(element, "nationId"));
            readInts(element, "levels", laws.levels);
            readInts(element, "pending", laws.pending);
            readInts(element, "proclaimedAt", laws.proclaimedAt);
            for (int i = 0; i < Laws.All.Length; i++) {
                int count = Laws.getNumLevels(Laws.All[i]);
                if (laws.levels[i] < 0 || laws.levels[i] >= count || laws.pending[i] < -1 || laws.pending[i] >= count) {
                    throw new SaveFormatException("a law is set to a level that does not exist");
                }
            }
            laws.lastTaxCollected = SaveReader.getInt(element, "lastTaxCollected");
            laws.totalTaxCollected = SaveReader.getInt(element, "totalTaxCollected");
            laws.deserters = SaveReader.getInt(element, "deserters");
            return laws;
        }

        private static void writeInts(SaveWriter writer, string name, int[] values) {
            writer.beginArray(name);
            foreach (int value in values) {
                writer.writeValue(value);
            }
            writer.endArray();
        }

        private static void readInts(JsonElement element, string name, int[] into) {
            List<int> values = new List<int>();
            foreach (JsonElement value in SaveReader.property(element, name).EnumerateArray()) {
                values.Add(value.GetInt32());
            }
            if (values.Count != into.Length) {
                throw new SaveFormatException("a nation's laws hold " + values.Count + " " + name + ", not " + into.Length);
            }
            values.CopyTo(into);
        }
    }
}
