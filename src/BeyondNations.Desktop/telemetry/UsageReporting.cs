using System;
using System.IO;
using System.Text.Json;
using beyondnations;
using beyondnations.desktop.ui;
using StephensonSoftware.Trace;

namespace beyondnations.desktop.telemetry {

    /**
    * Tells trace (https://trace.danielstephenson.dev) that Beyond Nations was
    * started, and nothing else: one `startup` event, tagged with the game's
    * version and a random installation ID (the tag `install`). Nothing about
    * the player, the machine or the world is sent.
    *
    * The installation ID is a random UUID the client keeps in
    * trace-install-id, beside settings.json in the game's data directory; it
    * lets trace count installations rather than startups. Deleting the file
    * resets it. TRACE_INSTALL_ID, when set and not blank, is sent instead and
    * the file is not touched. The client only reads or writes the file when
    * reporting is on, so every opt-out below also stops the ID.
    *
    * Reporting is on by default and the player has the last word. First match
    * turns it off, and is the reason the startup line gives:
    *   1. the environment, TRACE_USAGE_REPORTING=off or DO_NOT_TRACK=1
    *      (checked inside the vendored TraceClient) -- "environment";
    *   2. --no-usage-reporting on the command line, or
    *      "usageReporting": { "enabled": false } in settings.json -- "config";
    *   3. no key -- "no key".
    *
    * The bundled usage-reporting.json carries the defaults (enabled, endpoint,
    * key). settings.json lives in the game's data directory and is the
    * player's; it is created on first run with reporting shown as enabled, so
    * the switch is visible on disk, and it is never rewritten after that.
    * Its "endpoint" and "key" entries, when present, override the bundled
    * ones -- which is how a run is pointed at a local server instead of the
    * real one.
    *
    * Nothing here draws or needs a window, so all of it is tested headlessly.
    */
    public static class UsageReporting {
        public const string Application = "beyond-nations";
        public const string DefaultsFileName = "usage-reporting.json";
        public const string SettingsFileName = "settings.json";
        public const string InstallIdFileName = "trace-install-id";
        public const string InstallIdVariable = "TRACE_INSTALL_ID";
        public const string DetailsUrl = "https://github.com/Stephenson-Software/trace#usage-reporting";

        private static readonly JsonDocumentOptions JsonOptions = new JsonDocumentOptions {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /** What was decided before a client is built. */
        public class Settings {
            public bool Enabled = true;
            public string Endpoint;
            public string Key;
            public bool FirstRun;
        }

        /** The bundled defaults, beside the executable. */
        public static string defaultDefaultsPath() {
            return Path.Combine(AppContext.BaseDirectory, DefaultsFileName);
        }

        /** The player's settings, in the game's data directory. */
        public static string defaultSettingsPath() {
            return Path.Combine(AppDataPaths.getBaseDirectory(), SettingsFileName);
        }

        /** The installation ID's file: beside settings.json, in the game's data directory. */
        public static string installIdPath(string settingsPath) {
            return Path.Combine(Path.GetDirectoryName(settingsPath), InstallIdFileName);
        }

        /**
        * TRACE_INSTALL_ID when it is set and not blank, else null. Passed to the
        * client as its explicit ID, which wins over the file.
        */
        public static string installIdOverride() {
            string id = System.Environment.GetEnvironmentVariable(InstallIdVariable);
            return string.IsNullOrWhiteSpace(id) ? null : id;
        }

        /**
        * Reads the bundled defaults, then the player's settings over them.
        * Creates settings.json when it does not exist yet, which is what makes
        * a run the first one. Never throws: a missing or unreadable defaults
        * file leaves no key (so nothing is sent), and an unreadable
        * settings.json counts as reporting off, since a player who edited it
        * by hand was most likely turning it off.
        */
        public static Settings load(string defaultsPath, string settingsPath, Action<string> warn) {
            Settings settings = new Settings();
            try {
                using (JsonDocument defaults = JsonDocument.Parse(File.ReadAllText(defaultsPath), JsonOptions)) {
                    readInto(defaults.RootElement, settings);
                }
            } catch (Exception e) {
                warn("usage reporting defaults could not be read from " + defaultsPath + ": " + e.Message);
                settings.Key = null;
            }

            try {
                if (!File.Exists(settingsPath)) {
                    settings.FirstRun = true;
                    Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                    File.WriteAllText(settingsPath,
                        "{" + System.Environment.NewLine
                        + "    \"usageReporting\": {" + System.Environment.NewLine
                        + "        \"enabled\": true" + System.Environment.NewLine
                        + "    }" + System.Environment.NewLine
                        + "}" + System.Environment.NewLine);
                    return settings;
                }
                using (JsonDocument player = JsonDocument.Parse(File.ReadAllText(settingsPath), JsonOptions)) {
                    JsonElement block;
                    if (player.RootElement.ValueKind == JsonValueKind.Object
                        && player.RootElement.TryGetProperty("usageReporting", out block)
                        && block.ValueKind == JsonValueKind.Object) {
                        readInto(block, settings);
                    }
                }
            } catch (Exception e) {
                warn("could not read " + settingsPath + " (" + e.Message + "); usage reporting stays off until it is fixed");
                settings.Enabled = false;
            }
            return settings;
        }

        private static void readInto(JsonElement block, Settings settings) {
            JsonElement value;
            if (block.TryGetProperty("enabled", out value)) {
                if (value.ValueKind == JsonValueKind.False) {
                    settings.Enabled = false;
                } else if (value.ValueKind == JsonValueKind.True) {
                    settings.Enabled = true;
                }
            }
            if (block.TryGetProperty("endpoint", out value) && value.ValueKind == JsonValueKind.String) {
                settings.Endpoint = value.GetString();
            }
            if (block.TryGetProperty("key", out value) && value.ValueKind == JsonValueKind.String) {
                settings.Key = value.GetString();
            }
        }

        /**
        * What to print about reporting at startup, or null for nothing: the
        * full notice the first time the game runs with reporting on, and the
        * reason on every run with it off.
        */
        public static string notice(TraceClient client, bool firstRun, string settingsPath) {
            if (!client.IsEnabled) {
                return "Usage reporting is off (" + client.DisabledReason + ").";
            }
            if (!firstRun) {
                return null;
            }
            return "Usage reporting is on: Beyond Nations sends its name, its version and a random "
                + "installation ID (kept in " + installIdPath(settingsPath) + "; delete it to reset) at startup to "
                + "https://trace.danielstephenson.dev - nothing about you, your machine or your world. "
                + "Turn it off with \"usageReporting\": { \"enabled\": false } in " + settingsPath
                + ", with --no-usage-reporting, or for every program that reports to trace with "
                + "TRACE_USAGE_REPORTING=off or DO_NOT_TRACK=1. Details: " + DetailsUrl;
        }

        /**
        * The game's version, which the client adds to every event as the tag
        * `version`. Never blank, since the client refuses a blank one.
        */
        public static string programVersion() {
            return string.IsNullOrWhiteSpace(GameVersion.Version) ? "unknown" : GameVersion.Version;
        }

        /**
        * Builds the client, says whether reporting is on, and reports
        * `startup`. The caller disposes the client on exit, which gives the
        * event up to five seconds to be sent. Never throws.
        */
        public static TraceClient start(bool switchedOff, string defaultsPath, string settingsPath,
                                        Action<string> info, Action<string> warn) {
            try {
                Settings settings = load(defaultsPath, settingsPath, warn);
                string endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
                    ? "https://trace.danielstephenson.dev"
                    : settings.Endpoint;
                TraceClient client = new TraceClient(endpoint, Application, programVersion(),
                    key: settings.Key,
                    enabled: settings.Enabled && !switchedOff,
                    installId: installIdOverride(),
                    installIdFile: installIdPath(settingsPath));
                string line = notice(client, settings.FirstRun, settingsPath);
                if (line != null) {
                    info(line);
                }
                client.Report("startup");
                return client;
            } catch (Exception e) {
                warn("usage reporting could not start: " + e.Message);
                return TraceClient.Disabled();
            }
        }
    }
}
