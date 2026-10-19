using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace beyondnations {

    /**
    * Writes the body of a save (docs/SAVES.md) as JSON, with the handful of
    * shapes the simulation is made of -- vectors, colours, ids, inventories,
    * references to the world's random streams -- written one way everywhere.
    *
    * It is hand-written over Utf8JsonWriter rather than reflection-based
    * serialisation: the browser build is trimmed, which reflection does not
    * survive, and a format that follows private field names would break on
    * every rename. Each class writes its own fields (a save(SaveWriter) method
    * beside the fields themselves), so a new field is added next to the code
    * that must save it, and TestSaveCoverage fails until it is.
    */
    public class SaveWriter {
        private readonly Utf8JsonWriter json;
        private readonly SaveStreams streams;

        internal SaveWriter(Utf8JsonWriter json, SaveStreams streams) {
            this.json = json;
            this.streams = streams;
        }

        public Utf8JsonWriter getJson() {
            return json;
        }

        public void beginObject(string name) {
            json.WriteStartObject(name);
        }

        public void beginObject() {
            json.WriteStartObject();
        }

        public void endObject() {
            json.WriteEndObject();
        }

        public void beginArray(string name) {
            json.WriteStartArray(name);
        }

        public void beginArray() {
            json.WriteStartArray();
        }

        public void endArray() {
            json.WriteEndArray();
        }

        public void write(string name, int value) {
            json.WriteNumber(name, value);
        }

        public void write(string name, long value) {
            json.WriteNumber(name, value);
        }

        public void write(string name, bool value) {
            json.WriteBoolean(name, value);
        }

        public void write(string name, string value) {
            if (value == null) {
                json.WriteNull(name);
            } else {
                json.WriteString(name, value);
            }
        }

        /**
        * Floats are written in their shortest round-trip form, so the value
        * read back is bit-for-bit the one written. JSON has no NaN or
        * infinity, so those (which the simulation should never hold, but a
        * save must not fail over) are written as strings.
        */
        public void write(string name, float value) {
            json.WritePropertyName(name);
            writeValue(value);
        }

        public void writeValue(float value) {
            if (float.IsFinite(value)) {
                json.WriteRawValue(value.ToString("R", CultureInfo.InvariantCulture), true);
            } else {
                json.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void writeValue(int value) {
            json.WriteNumberValue(value);
        }

        public void writeValue(string value) {
            if (value == null) {
                json.WriteNullValue();
            } else {
                json.WriteStringValue(value);
            }
        }

        public void writeEnum<T>(string name, T value) where T : struct, Enum {
            json.WriteString(name, value.ToString());
        }

        public void write(string name, Vector3 value) {
            json.WriteStartArray(name);
            writeValue(value.X);
            writeValue(value.Y);
            writeValue(value.Z);
            json.WriteEndArray();
        }

        public void write(string name, Rgba value) {
            json.WriteStartArray(name);
            writeValue(value.r);
            writeValue(value.g);
            writeValue(value.b);
            writeValue(value.a);
            json.WriteEndArray();
        }

        public void write(string name, Guid value) {
            json.WriteString(name, value.ToString("N"));
        }

        public void write(string name, EntityId id) {
            write(name, id == null ? null : id.getGuid().ToString("N"));
        }

        public void writeValue(EntityId id) {
            writeValue(id == null ? null : id.getGuid().ToString("N"));
        }

        public void write(string name, NationId id) {
            write(name, id == null ? null : id.getGuid().ToString("N"));
        }

        /** Which of the world's random streams an object draws from. */
        public void writeStream(string name, RandomSource random) {
            json.WriteString(name, streams.nameOf(random));
        }

        public void writeIdList(string name, IEnumerable<EntityId> ids) {
            json.WriteStartArray(name);
            foreach (EntityId id in ids) {
                writeValue(id);
            }
            json.WriteEndArray();
        }

        /** A map keyed by entity, written as [[id, value], ...] in the map's own order. */
        public void writeIdIntMap(string name, Dictionary<EntityId, int> map) {
            json.WriteStartArray(name);
            foreach (KeyValuePair<EntityId, int> entry in map) {
                json.WriteStartArray();
                writeValue(entry.Key);
                json.WriteNumberValue(entry.Value);
                json.WriteEndArray();
            }
            json.WriteEndArray();
        }

        public void writeStringList(string name, IEnumerable<string> values) {
            json.WriteStartArray(name);
            foreach (string value in values) {
                writeValue(value);
            }
            json.WriteEndArray();
        }

        public void write(string name, Inventory inventory) {
            json.WritePropertyName(name);
            json.WriteStartObject();
            inventory.save(this);
            json.WriteEndObject();
        }

        public void write(string name, Appearance appearance) {
            json.WritePropertyName(name);
            if (appearance == null) {
                json.WriteNullValue();
                return;
            }
            json.WriteStartObject();
            json.WriteStartArray("parts");
            foreach (AppearancePart part in appearance.getParts()) {
                json.WriteStartObject();
                writeEnum("kind", part.getKind());
                write("offset", part.getOffset());
                write("scale", part.getScale());
                write("color", part.getColor());
                json.WriteEndObject();
            }
            json.WriteEndArray();
            write("label", appearance.getLabel());
            write("labelOffset", appearance.getLabelOffset());
            write("labelScale", appearance.getLabelScale());
            json.WriteEndObject();
        }
    }
}
