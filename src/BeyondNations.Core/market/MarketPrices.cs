using System;
using System.Text.Json;

namespace beyondnations {

    /**
    * How a market's prices lean, item by item, beyond what is on its stalls
    * this instant.
    *
    * A price is the item's supply curve (ItemCostCalculator.calculateCost)
    * times a pressure, in percent: 100 is the plain curve, 200 a market
    * crying out for the thing, 50 one drowning in it. Pressure is not set
    * outright; MarketPricing works out where it ought to be from what the
    * settlement holds and how many live there, and every pricing round it
    * moves a few points toward that, so prices drift rather than jump.
    *
    * The reference price is what the item cost at the last round, which is
    * what the market box's trend arrows compare against.
    *
    * Everything is a whole number, and nothing here draws from a random
    * stream, so a seeded world prices the same way every time.
    */
    public class MarketPrices {
        public const int DEFAULT_PRESSURE = 100;
        public const int MIN_PRESSURE = 50;
        public const int MAX_PRESSURE = 200;

        /** -1 means "no price seen yet", which shows no trend. */
        public const int NO_REFERENCE = -1;

        private static readonly int NumItemTypes = Enum.GetValues(typeof(ItemType)).Length;

        private int[] pressure;
        private int[] reference;

        public MarketPrices() {
            pressure = new int[NumItemTypes];
            reference = new int[NumItemTypes];
            for (int i = 0; i < NumItemTypes; i++) {
                pressure[i] = DEFAULT_PRESSURE;
                reference[i] = NO_REFERENCE;
            }
        }

        public int getPressure(ItemType itemType) {
            return pressure[(int) itemType];
        }

        public void setPressure(ItemType itemType, int value) {
            pressure[(int) itemType] = clampPressure(value);
        }

        public int getReference(ItemType itemType) {
            return reference[(int) itemType];
        }

        public void setReference(ItemType itemType, int price) {
            reference[(int) itemType] = price;
        }

        public static int clampPressure(int value) {
            return Math.Clamp(value, MIN_PRESSURE, MAX_PRESSURE);
        }

        /**
        * Moves the pressure at most maxStep points toward target. Returns the
        * new pressure.
        */
        public int driftToward(ItemType itemType, int target, int maxStep) {
            int current = getPressure(itemType);
            int clampedTarget = clampPressure(target);
            int step = Math.Clamp(clampedTarget - current, -maxStep, maxStep);
            setPressure(itemType, current + step);
            return getPressure(itemType);
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.beginArray("items");
            foreach (ItemType itemType in Enum.GetValues(typeof(ItemType))) {
                if (getPressure(itemType) == DEFAULT_PRESSURE && getReference(itemType) == NO_REFERENCE) {
                    continue;
                }
                writer.beginObject();
                writer.writeEnum("item", itemType);
                writer.write("pressure", getPressure(itemType));
                writer.write("reference", getReference(itemType));
                writer.endObject();
            }
            writer.endArray();
        }

        internal void load(JsonElement element) {
            for (int i = 0; i < NumItemTypes; i++) {
                pressure[i] = DEFAULT_PRESSURE;
                reference[i] = NO_REFERENCE;
            }
            foreach (JsonElement item in SaveReader.property(element, "items").EnumerateArray()) {
                ItemType itemType = SaveReader.getEnum<ItemType>(item, "item");
                int saved = SaveReader.getInt(item, "pressure");
                if (saved < MIN_PRESSURE || saved > MAX_PRESSURE) {
                    throw new SaveFormatException("a market price pressure is out of range");
                }
                pressure[(int) itemType] = saved;
                reference[(int) itemType] = SaveReader.getInt(item, "reference");
            }
        }
    }
}
