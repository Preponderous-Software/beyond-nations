using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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

    /**
    * Encodes a Simulation as a save file and decodes one back (docs/SAVES.md).
    *
    * A save is UTF-8 text in two parts. The first line is the header, a small
    * JSON object (SaveHeader). Everything after it is the body: the whole
    * world as one JSON object, with the CRC-32 of its bytes in the header.
    * Decoding refuses, with a SaveFormatException a player can read, anything
    * that is not a save, is damaged, or was written by a newer format than
    * this build knows; it never returns a half-loaded world.
    */
    public static class SaveGame {
        /**
        * The format this build writes. Bump it for any change to what a save
        * holds, and add the step from the previous version to SaveMigrations.
        */
        public const int CurrentFormatVersion = 6;

        // --- writing ---

        public static byte[] encode(Simulation simulation, string gameVersion) {
            return encode(simulation, gameVersion, DateTime.UtcNow);
        }

        public static byte[] encode(Simulation simulation, string gameVersion, DateTime savedAtUtc) {
            byte[] body = encodeBody(simulation, gameVersion);
            Nation nation = simulation.getNationRepository().getNation(simulation.getPlayer().getNationId());

            using (MemoryStream stream = new MemoryStream()) {
                using (Utf8JsonWriter json = new Utf8JsonWriter(stream)) {
                    json.WriteStartObject();
                    json.WriteString("magic", SaveHeader.Magic);
                    json.WriteNumber("formatVersion", CurrentFormatVersion);
                    json.WriteString("gameVersion", gameVersion ?? "");
                    json.WriteNumber("seed", simulation.getRandom().getSeed());
                    json.WriteNumber("tick", simulation.getTickCounter().getTotalTicks());
                    json.WriteNumber("day", simulation.getTimeOfDay().getDay() + 1);
                    if (nation == null) {
                        json.WriteNull("nation");
                    } else {
                        json.WriteString("nation", nation.getName());
                    }
                    json.WriteString("savedAt", savedAtUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
                    json.WriteNumber("bodyBytes", body.Length);
                    json.WriteString("crc32", Crc32.toHex(Crc32.compute(body)));
                    json.WriteEndObject();
                }
                stream.WriteByte((byte) '\n');
                stream.Write(body, 0, body.Length);
                return stream.ToArray();
            }
        }

        private static byte[] encodeBody(Simulation simulation, string gameVersion) {
            SaveStreams streams = simulation.getStreams();
            using (MemoryStream stream = new MemoryStream()) {
                using (Utf8JsonWriter json = new Utf8JsonWriter(stream)) {
                    SaveWriter writer = new SaveWriter(json, streams);
                    json.WriteStartObject();
                    json.WriteNumber("formatVersion", CurrentFormatVersion);
                    json.WriteString("gameVersion", gameVersion ?? "");
                    simulation.save(writer, streams);
                    json.WriteEndObject();
                }
                return stream.ToArray();
            }
        }

        // --- reading ---

        /**
        * Reads and checks the header only: whether this is a save, which
        * format, and whether this build can read it. Does not check the body;
        * decode() does.
        */
        public static SaveHeader readHeader(byte[] file) {
            if (file == null || file.Length == 0) {
                throw new SaveFormatException("The save file is empty.");
            }
            int newline = Array.IndexOf(file, (byte) '\n');
            if (newline <= 0) {
                throw new SaveFormatException("This is not a Beyond Nations save.");
            }
            SaveHeader header = new SaveHeader();
            try {
                using (JsonDocument document = JsonDocument.Parse(new ReadOnlyMemory<byte>(file, 0, newline))) {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object || !SaveReader.has(root, "magic")
                            || SaveReader.getString(root, "magic") != SaveHeader.Magic) {
                        throw new SaveFormatException("This is not a Beyond Nations save.");
                    }
                    header.FormatVersion = SaveReader.getInt(root, "formatVersion");
                    header.GameVersion = SaveReader.getString(root, "gameVersion");
                    if (header.FormatVersion > CurrentFormatVersion) {
                        throw new SaveFormatException("This save was made by a newer version of Beyond Nations ("
                            + header.GameVersion + ", save format " + header.FormatVersion + "). This version reads save formats up to "
                            + CurrentFormatVersion + ". Update the game to load it; the save has not been changed.");
                    }
                    if (header.FormatVersion < 1) {
                        throw new SaveFormatException("This save has an invalid format version (" + header.FormatVersion + ").");
                    }
                    header.Seed = SaveReader.getInt(root, "seed");
                    header.Tick = SaveReader.getInt(root, "tick");
                    header.Day = SaveReader.getInt(root, "day");
                    header.Nation = SaveReader.getString(root, "nation");
                    header.SavedAtUtc = DateTime.Parse(SaveReader.getString(root, "savedAt"), CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                    header.BodyBytes = SaveReader.getInt(root, "bodyBytes");
                    header.Crc32 = SaveReader.getString(root, "crc32");
                }
            } catch (SaveFormatException) {
                throw;
            } catch (Exception e) when (e is JsonException || e is FormatException || e is InvalidOperationException) {
                throw new SaveFormatException("The save's header is damaged.", e);
            }
            return header;
        }

        /** Whether the body is all there and unchanged: its length and checksum match the header. */
        public static void verify(byte[] file, SaveHeader header) {
            int start = Array.IndexOf(file, (byte) '\n') + 1;
            int length = file.Length - start;
            if (length != header.BodyBytes) {
                throw new SaveFormatException("The save is damaged: it is " + length + " bytes long where " + header.BodyBytes
                    + " were written. It may have been cut short.");
            }
            string actual = Crc32.toHex(Crc32.compute(file, start, length));
            if (actual != header.Crc32) {
                throw new SaveFormatException("The save is damaged: its checksum does not match (" + actual + ", expected " + header.Crc32 + ").");
            }
        }

        /** Checks a whole file -- header, version, length and checksum -- without building the world. */
        public static SaveHeader check(byte[] file) {
            SaveHeader header = readHeader(file);
            verify(file, header);
            return header;
        }

        /**
        * Builds the world a save describes. Throws SaveFormatException, and
        * nothing else, for a save it cannot load.
        */
        public static Simulation decode(byte[] file) {
            SaveHeader header = check(file);
            int start = Array.IndexOf(file, (byte) '\n') + 1;
            ReadOnlyMemory<byte> body = new ReadOnlyMemory<byte>(file, start, file.Length - start);
            try {
                if (header.FormatVersion == CurrentFormatVersion) {
                    using (JsonDocument document = JsonDocument.Parse(body)) {
                        return decodeBody(document.RootElement, header);
                    }
                }
                JsonObject node = JsonNode.Parse(body.Span) as JsonObject;
                if (node == null) {
                    throw new SaveFormatException("The save's world is damaged.");
                }
                JsonObject migrated = SaveMigrations.migrate(node, header.FormatVersion, CurrentFormatVersion);
                using (JsonDocument document = JsonDocument.Parse(migrated.ToJsonString())) {
                    return decodeBody(document.RootElement, header);
                }
            } catch (SaveFormatException) {
                throw;
            } catch (Exception e) {
                throw new SaveFormatException("The save could not be loaded: " + e.Message, e);
            }
        }

        private static Simulation decodeBody(JsonElement root, SaveHeader header) {
            if (root.ValueKind != JsonValueKind.Object) {
                throw new SaveFormatException("The save's world is damaged.");
            }
            int bodyVersion = SaveReader.getInt(root, "formatVersion");
            if (bodyVersion != CurrentFormatVersion) {
                throw new SaveFormatException("The save's world is format " + bodyVersion + ", not " + CurrentFormatVersion + ".");
            }
            if (SaveReader.getInt(root, "seed") != header.Seed) {
                throw new SaveFormatException("The save's header and world disagree about the seed.");
            }
            return Simulation.load(root);
        }
    }
}
