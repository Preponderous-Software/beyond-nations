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
    * Version 1 is the first format, so there are no steps yet. The next
    * change to what a save holds adds the step from 1 to 2 here; for a new
    * section that is only "add it, empty".
    */
    public static class SaveMigrations {
        /** steps[i] migrates a body of version i + 1 to version i + 2. */
        private static readonly List<Func<JsonObject, JsonObject>> steps = new List<Func<JsonObject, JsonObject>>();

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
