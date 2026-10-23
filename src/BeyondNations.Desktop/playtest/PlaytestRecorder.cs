using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using beyondnations.desktop.ui;

namespace beyondnations.desktop.playtest {

    /**
    * --playtest-out DIR: what a playtesting agent reads instead of guessing
    * from screenshots (docs/PLAYTESTING.md; the format is the probe contract of the
    * playtester harness that drives the game, tools/playtest/).
    *
    *   DIR/state.json    the current PlaytestState, rewritten about twice a
    *                     second by writing a temporary file and renaming it
    *                     over the old one, so a reader never sees half a file
    *   DIR/events.jsonl  one JSON object per line, appended as things happen:
    *                     screen changes, objectives completed, era changes,
    *                     status messages, chronicle entries, world events
    *                     offered and resolved, deaths, saves and loads
    *
    * Events are found by comparing what the game shows each frame with what
    * it showed the frame before, so the simulation carries no hooks for
    * this and behaves exactly the same with the flag as without it. Without
    * the flag no recorder exists (create returns null) and nothing is
    * written. A file that cannot be written is reported once and otherwise
    * ignored: a playtest aid must never take the game down.
    */
    public class PlaytestRecorder {
        public const string StateFileName = "state.json";
        public const string EventsFileName = "events.jsonl";
        public const int DefaultStateIntervalMs = 500;

        private readonly string directory;
        private readonly string statePath;
        private readonly string eventsPath;
        private readonly Func<long> wallClockMs;
        private readonly int stateIntervalMs;
        private long nextStateAtMs = long.MinValue;
        // for the measured tick rate in state.json
        private long lastStateMs = -1;
        private int lastStateTick;
        private bool reportedWriteFailure;

        // what was seen last frame
        private bool primed;
        private ScreenType? lastScreen;
        private Simulation lastSimulation;
        private int lastObjectivesCompleted;
        private Era lastEra;
        private string lastStatus;
        private int lastChronicleTotal;
        private string lastEventTitle;
        private int lastPlayerDeaths;
        private int lastPawnDeaths;
        private int lastSavesWritten;
        private int lastObjectiveTick;

        /** A recorder for dir, or null when dir is null or empty: no flag, no files. */
        public static PlaytestRecorder create(string dir) {
            return string.IsNullOrEmpty(dir) ? null : new PlaytestRecorder(dir, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), DefaultStateIntervalMs);
        }

        public PlaytestRecorder(string directory, Func<long> wallClockMs, int stateIntervalMs) {
            this.directory = directory;
            this.wallClockMs = wallClockMs;
            this.stateIntervalMs = stateIntervalMs;
            Directory.CreateDirectory(directory);
            statePath = Path.Combine(directory, StateFileName);
            eventsPath = Path.Combine(directory, EventsFileName);
        }

        public string getDirectory() { return directory; }

        /**
        * Called once a frame, after the UI has drawn, with the world in play
        * (null off the world) and what the host shows.
        */
        public void update(Simulation simulation, PlaytestView view, int savesWritten) {
            long now = wallClockMs();
            detectEvents(simulation, view, savesWritten, now);
            if (now >= nextStateAtMs) {
                nextStateAtMs = now + stateIntervalMs;
                PlaytestState state = PlaytestState.capture(simulation, view, now);
                if (simulation != null && lastStateMs >= 0 && now > lastStateMs && state.Tick >= lastStateTick) {
                    state.TicksPerSecond = (state.Tick - lastStateTick) * 1000f / (now - lastStateMs);
                }
                lastStateMs = simulation != null ? now : -1;
                lastStateTick = state.Tick;
                writeState(state);
            }
        }

        /** The host built a new world (how = "created") or loaded one (how = "loaded"). */
        public void worldStarted(Simulation simulation, string how, string slot) {
            Dictionary<string, object> fields = new Dictionary<string, object> { { "how", how } };
            if (slot != null) {
                fields["slot"] = slot;
            }
            if (simulation != null) {
                fields["seed"] = ShareSession.seedOf(simulation);
            }
            append("world", simulation, wallClockMs(), fields);
            rebaseline(simulation);
        }

        private void rebaseline(Simulation simulation) {
            lastSimulation = simulation;
            if (simulation == null) {
                return;
            }
            lastObjectivesCompleted = simulation.getObjectiveTracker().getCompletedCount();
            lastEra = simulation.getEraTracker().getEra();
            lastStatus = simulation.getPlayer().getStatus().getStatus();
            lastChronicleTotal = simulation.getChronicle().getTotalRecorded();
            lastEventTitle = WorldEventSheet.getView(simulation)?.Title;
            lastPlayerDeaths = simulation.getNumPlayerDeaths();
            lastPawnDeaths = simulation.getNumPawnDeaths();
            lastObjectiveTick = simulation.getTickCounter().getTotalTicks();
        }

        private void detectEvents(Simulation simulation, PlaytestView view, int savesWritten, long now) {
            if (!primed) {
                primed = true;
                lastSavesWritten = savesWritten;
            }
            if (lastScreen != view.Screen) {
                append("screen", simulation, now, new Dictionary<string, object> {
                    { "from", lastScreen?.ToString() }, { "to", view.Screen.ToString() } });
                lastScreen = view.Screen;
            }
            if (savesWritten != lastSavesWritten) {
                append("save", simulation, now, new Dictionary<string, object> { { "count", savesWritten } });
                lastSavesWritten = savesWritten;
            }
            if (simulation == null) {
                return;
            }
            if (!ReferenceEquals(simulation, lastSimulation)) {
                // a world the host did not announce: start from where it is
                rebaseline(simulation);
                return;
            }

            int tick = simulation.getTickCounter().getTotalTicks();
            ObjectiveTracker objectives = simulation.getObjectiveTracker();
            while (lastObjectivesCompleted < objectives.getCompletedCount()) {
                Objective done = objectives.getObjectives()[lastObjectivesCompleted];
                lastObjectivesCompleted++;
                append("objective_completed", simulation, now, new Dictionary<string, object> {
                    { "number", lastObjectivesCompleted }, { "id", done.getId() }, { "title", done.getTitle() },
                    { "ticksTaken", tick - lastObjectiveTick } });
                lastObjectiveTick = tick;
            }

            Era era = simulation.getEraTracker().getEra();
            if (era != lastEra) {
                append("era_changed", simulation, now, new Dictionary<string, object> {
                    { "from", Eras.getDisplayName(lastEra) }, { "to", Eras.getDisplayName(era) } });
                lastEra = era;
            }

            string status = simulation.getPlayer().getStatus().getStatus();
            if (status != lastStatus) {
                if (!string.IsNullOrEmpty(status)) {
                    append("status", simulation, now, new Dictionary<string, object> { { "text", status } });
                }
                lastStatus = status;
            }

            Chronicle chronicle = simulation.getChronicle();
            int total = chronicle.getTotalRecorded();
            if (total > lastChronicleTotal) {
                int fresh = Math.Min(total - lastChronicleTotal, chronicle.getCount());
                foreach (ChronicleEntry entry in chronicle.getRecent(fresh)) {
                    append("chronicle", simulation, now, new Dictionary<string, object> {
                        { "kind", entry.getEventType().ToString() }, { "text", entry.getText() } });
                }
                lastChronicleTotal = total;
            }

            WorldEventSheet.View sheet = WorldEventSheet.getView(simulation);
            string eventTitle = sheet?.Title;
            if (eventTitle != lastEventTitle) {
                if (lastEventTitle != null) {
                    append("world_event_resolved", simulation, now, new Dictionary<string, object> {
                        { "title", lastEventTitle }, { "status", string.IsNullOrEmpty(status) ? null : status } });
                }
                if (sheet != null) {
                    List<string> options = new List<string>();
                    foreach (WorldEventSheet.Row row in sheet.Rows) {
                        options.Add(row.Available ? row.Label : row.Label + " (unavailable)");
                    }
                    append("world_event_offered", simulation, now, new Dictionary<string, object> {
                        { "title", sheet.Title }, { "body", sheet.Body }, { "options", options } });
                }
                lastEventTitle = eventTitle;
            }

            if (simulation.getNumPlayerDeaths() != lastPlayerDeaths) {
                append("player_death", simulation, now, new Dictionary<string, object> { { "total", simulation.getNumPlayerDeaths() } });
                lastPlayerDeaths = simulation.getNumPlayerDeaths();
            }
            if (simulation.getNumPawnDeaths() != lastPawnDeaths) {
                append("pawn_death", simulation, now, new Dictionary<string, object> {
                    { "count", simulation.getNumPawnDeaths() - lastPawnDeaths }, { "total", simulation.getNumPawnDeaths() } });
                lastPawnDeaths = simulation.getNumPawnDeaths();
            }
        }

        /** One line of events.jsonl. Public so the format can be tested on its own. */
        public static string formatEvent(string type, int tick, long wallTimeMs, IDictionary<string, object> fields) {
            using (MemoryStream stream = new MemoryStream()) {
                using (Utf8JsonWriter writer = new Utf8JsonWriter(stream)) {
                    writer.WriteStartObject();
                    writer.WriteString("type", type);
                    writer.WriteNumber("tick", tick);
                    writer.WriteNumber("wallTimeMs", wallTimeMs);
                    if (fields != null) {
                        foreach (KeyValuePair<string, object> field in fields) {
                            writer.WritePropertyName(field.Key);
                            writeValue(writer, field.Value);
                        }
                    }
                    writer.WriteEndObject();
                }
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static void writeValue(Utf8JsonWriter writer, object value) {
            switch (value) {
                case null: writer.WriteNullValue(); break;
                case string text: writer.WriteStringValue(text); break;
                case int number: writer.WriteNumberValue(number); break;
                case long number: writer.WriteNumberValue(number); break;
                case bool flag: writer.WriteBooleanValue(flag); break;
                case IEnumerable<string> list:
                    writer.WriteStartArray();
                    foreach (string item in list) {
                        writer.WriteStringValue(item);
                    }
                    writer.WriteEndArray();
                    break;
                default: writer.WriteStringValue(value.ToString()); break;
            }
        }

        private void append(string type, Simulation simulation, long now, IDictionary<string, object> fields) {
            int tick = simulation == null ? 0 : simulation.getTickCounter().getTotalTicks();
            try {
                File.AppendAllText(eventsPath, formatEvent(type, tick, now, fields) + "\n");
            } catch (Exception e) {
                reportFailure(e);
            }
        }

        private void writeState(PlaytestState state) {
            string temporary = statePath + ".tmp";
            try {
                File.WriteAllText(temporary, state.toJson() + "\n");
                File.Move(temporary, statePath, true);
            } catch (Exception e) {
                reportFailure(e);
            }
        }

        private void reportFailure(Exception e) {
            if (reportedWriteFailure) {
                return;
            }
            reportedWriteFailure = true;
            Log.warning("--playtest-out: could not write to " + directory + ": " + e.Message);
        }
    }
}
