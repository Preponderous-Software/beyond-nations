using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace beyondnations {

    /**
    * Brings an old save's body up to the current format, one version at a
    * time (docs/SAVES.md). Each step takes the body of version N and returns
    * the body of version N + 1, setting its formatVersion; decode then reads
    * the result exactly as it reads a save written today.
    *
    * Steps are never edited or removed once released: a save written by any
    * past release must keep loading. Each step gets a fixture under
    * tests/BeyondNations.Core.Tests/save/fixtures that TestSaveMigrations
    * loads.
    *
    * Version 1 is the first format.
    *
    *   1 -> 2  adds the realm section (realm/Realm): no laws, no wars or
    *           truces, no past rulers, the reign begun at tick 0 and the
    *           run not over -- which is exactly the state of a world from
    *           before laws and succession existed.
    *
    *   2 -> 3  adds the worldEvents section (worldevents/WorldEvents),
    *           empty: nothing pending, nothing running, and the events'
    *           clocks start from the first tick the loaded world runs.
    *   3 -> 4  trade caravans and living prices: a "trade" section (no
    *           caravans sent yet), and every market's "prices" with every
    *           item at its plain supply-curve price and no trend yet.
    */
    public static class SaveMigrations {
        /** steps[i] migrates a body of version i + 1 to version i + 2. */
        private static readonly List<Func<JsonObject, JsonObject>> steps = new List<Func<JsonObject, JsonObject>> {
            addRealm,
            addWorldEvents,
            addTradeAndPrices,
        };

        /** 1 -> 2: a world from before the realm has no laws, wars, truces or lineage. */
        private static JsonObject addRealm(JsonObject body) {
            // the law rounds that would have run by the saved tick, as in a world played from the start
            // (500 is Realm.LAW_ROUND_TICKS as format 2 had it; a released step never changes)
            int lawRounds = 0;
            JsonObject time = body["time"] as JsonObject;
            if (time != null && time["totalTicks"] != null) {
                lawRounds = Math.Max(0, (int) time["totalTicks"]) / 500;
            }
            body["realm"] = new JsonObject {
                ["lawRound"] = lawRounds,
                ["laws"] = new JsonArray(),
                ["wars"] = new JsonArray(),
                ["truces"] = new JsonArray(),
                ["lineage"] = new JsonArray(),
                ["reignStartTick"] = 0,
                ["runOver"] = false,
                ["noticeShown"] = false,
                ["notice"] = null,
            };
            body["formatVersion"] = 2;
            return body;
        }

        /** 2 -> 3: a world from before world events has none pending, none running, and its clocks not yet started. */
        private static JsonObject addWorldEvents(JsonObject body) {
            body["worldEvents"] = new JsonObject();
            body["formatVersion"] = 3;
            return body;
        }

        /** 3 -> 4: caravans and living prices (TradeCaravans, MarketPrices). */
        private static JsonObject addTradeAndPrices(JsonObject body) {
            body["trade"] = new JsonObject {
                ["numDispatched"] = 0,
                ["numDelivered"] = 0,
                ["numRaided"] = 0,
                ["numEscorted"] = 0,
                ["lastDispatchTicks"] = new JsonArray(),
                ["lastNotableDay"] = -1,
            };
            foreach (string list in new[] { "entities", "orphanEntities" }) {
                JsonArray entities = body[list] as JsonArray;
                if (entities == null) {
                    continue;
                }
                foreach (JsonNode entity in entities) {
                    JsonObject saved = entity as JsonObject;
                    JsonObject market = saved == null ? null : saved["market"] as JsonObject;
                    if (market != null && !market.ContainsKey("prices")) {
                        market["prices"] = new JsonObject { ["items"] = new JsonArray() };
                    }
                }
            }
            body["formatVersion"] = 4;
            return body;
        }

        public static IReadOnlyList<Func<JsonObject, JsonObject>> getSteps() {
            return steps;
        }

        public static JsonObject migrate(JsonObject body, int fromVersion, int toVersion) {
            return migrate(body, fromVersion, toVersion, steps);
        }

        /**
        * Runs the steps from fromVersion up to toVersion. Public with the
        * steps as a parameter so the chaining itself can be tested with steps
        * that are not (yet) real.
        */
        public static JsonObject migrate(JsonObject body, int fromVersion, int toVersion, IReadOnlyList<Func<JsonObject, JsonObject>> withSteps) {
            if (fromVersion < 1 || fromVersion > toVersion) {
                throw new SaveFormatException("There is no way to read a save of format " + fromVersion + " as format " + toVersion + ".");
            }
            JsonObject current = body;
            for (int version = fromVersion; version < toVersion; version++) {
                int index = version - 1;
                if (index >= withSteps.Count) {
                    throw new SaveFormatException("There is no migration from save format " + version + " to " + (version + 1) + ".");
                }
                current = withSteps[index](current);
                JsonNode stamped;
                int stampedVersion;
                if (current == null || !current.TryGetPropertyValue("formatVersion", out stamped) || stamped == null
                        || !int.TryParse(stamped.ToJsonString(), out stampedVersion) || stampedVersion != version + 1) {
                    throw new SaveFormatException("The migration from save format " + version + " did not produce format " + (version + 1) + ".");
                }
            }
            return current;
        }
    }
}
