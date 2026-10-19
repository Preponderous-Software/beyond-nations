using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace beyondnations {

    /**
    * The world's random streams by name, so an object that holds one (a pawn,
    * a chicken, a market) can say which in a save and get the same stream
    * back on load, not a copy of it.
    */
    public class SaveStreams {
        public const string Main = "main";
        public const string SettlementLife = "settlementLife";
        public const string Traits = "traits";
        public const string Population = "population";

        private readonly List<KeyValuePair<string, RandomSource>> streams = new List<KeyValuePair<string, RandomSource>>();

        public void add(string name, RandomSource random) {
            streams.Add(new KeyValuePair<string, RandomSource>(name, random));
        }

        public IReadOnlyList<KeyValuePair<string, RandomSource>> all() {
            return streams;
        }

        public string nameOf(RandomSource random) {
            foreach (KeyValuePair<string, RandomSource> stream in streams) {
                if (ReferenceEquals(stream.Value, random)) {
                    return stream.Key;
                }
            }
            // Something built outside the simulation (a test fixture, a
            // debug command) with a stream of its own. It is saved as drawing
            // from the main stream rather than failing the save.
            Log.warning("save: an object draws from a random stream the world does not own; it will load drawing from the main stream");
            return Main;
        }

        public RandomSource get(string name) {
            foreach (KeyValuePair<string, RandomSource> stream in streams) {
                if (stream.Key == name) {
                    return stream.Value;
                }
            }
            throw new SaveFormatException("unknown random stream '" + name + "'");
        }
    }

    /**
    * A save that cannot be loaded: corrupt, from a newer version, or not a
    * save at all. The message is meant for the player.
    */
    public class SaveFormatException : Exception {
        public SaveFormatException(string message) : base(message) {
        }

        public SaveFormatException(string message, Exception inner) : base(message, inner) {
        }
    }

    /**
    * Reads what SaveWriter wrote, and keeps the one thing a load needs beyond
    * the JSON: identity. The simulation compares ids and nations by reference
    * in places (stall.getOwnerId() == pawn.getId()), so every reference to an
    * entity id resolves to the one EntityId instance the entity itself holds,
    * and every reference to an entity or nation to the one object.
    */
    public class SaveReader {
        private readonly SaveStreams streams;
        private readonly TickCounter tickCounter;
        private readonly Dictionary<Guid, EntityId> entityIds = new Dictionary<Guid, EntityId>();
        private readonly Dictionary<Guid, NationId> nationIds = new Dictionary<Guid, NationId>();
        private readonly Dictionary<EntityId, Entity> entities = new Dictionary<EntityId, Entity>();
        private readonly Dictionary<NationId, Nation> nations = new Dictionary<NationId, Nation>();

        internal SaveReader(SaveStreams streams, TickCounter tickCounter) {
            this.streams = streams;
            this.tickCounter = tickCounter;
        }

        public TickCounter getTickCounter() {
            return tickCounter;
        }

        // --- identity ---

        public EntityId entityId(JsonElement element) {
            if (element.ValueKind == JsonValueKind.Null) {
                return null;
            }
            Guid guid = parseGuid(element.GetString());
            EntityId id;
            if (!entityIds.TryGetValue(guid, out id)) {
                id = new EntityId(guid);
                entityIds[guid] = id;
            }
            return id;
        }

        public NationId nationId(JsonElement element) {
            if (element.ValueKind == JsonValueKind.Null) {
                return null;
            }
            Guid guid = parseGuid(element.GetString());
            NationId id;
            if (!nationIds.TryGetValue(guid, out id)) {
                id = new NationId(guid);
                nationIds[guid] = id;
            }
            return id;
        }

        public void registerEntity(Entity entity) {
            entities[entity.getId()] = entity;
        }

        public Entity entity(JsonElement element) {
            EntityId id = entityId(element);
            if (id == null) {
                return null;
            }
            Entity entity;
            if (!entities.TryGetValue(id, out entity)) {
                throw new SaveFormatException("a reference to entity " + id + " that the save does not contain");
            }
            return entity;
        }

        public void registerNation(Nation nation) {
            nations[nation.getId()] = nation;
        }

        public Nation nation(JsonElement element) {
            NationId id = nationId(element);
            if (id == null) {
                return null;
            }
            Nation nation;
            if (!nations.TryGetValue(id, out nation)) {
                throw new SaveFormatException("a reference to nation " + id + " that the save does not contain");
            }
            return nation;
        }

        public RandomSource stream(JsonElement element) {
            return streams.get(element.GetString());
        }

        // --- values ---

        public static JsonElement property(JsonElement obj, string name) {
            JsonElement value;
            if (obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(name, out value)) {
                throw new SaveFormatException("the save is missing '" + name + "'");
            }
            return value;
        }

        public static bool has(JsonElement obj, string name) {
            JsonElement ignored;
            return obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out ignored);
        }

        public static int getInt(JsonElement obj, string name) {
            return property(obj, name).GetInt32();
        }

        public static long getLong(JsonElement obj, string name) {
            return property(obj, name).GetInt64();
        }

        public static bool getBool(JsonElement obj, string name) {
            return property(obj, name).GetBoolean();
        }

        public static string getString(JsonElement obj, string name) {
            JsonElement value = property(obj, name);
            return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
        }

        public static float getFloat(JsonElement obj, string name) {
            return toFloat(property(obj, name));
        }

        public static float toFloat(JsonElement value) {
            if (value.ValueKind == JsonValueKind.String) {
                return float.Parse(value.GetString(), CultureInfo.InvariantCulture);
            }
            return float.Parse(value.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public static T getEnum<T>(JsonElement obj, string name) where T : struct, Enum {
            return toEnum<T>(property(obj, name));
        }

        public static T toEnum<T>(JsonElement value) where T : struct, Enum {
            string text = value.GetString();
            T result;
            if (text == null || !Enum.TryParse<T>(text, false, out result) || !Enum.IsDefined(typeof(T), result)) {
                throw new SaveFormatException("'" + text + "' is not a known " + typeof(T).Name);
            }
            return result;
        }

        public static Vector3 getVector3(JsonElement obj, string name) {
            JsonElement value = property(obj, name);
            return new Vector3(toFloat(value[0]), toFloat(value[1]), toFloat(value[2]));
        }

        public static Rgba toRgba(JsonElement value) {
            return new Rgba(toFloat(value[0]), toFloat(value[1]), toFloat(value[2]), toFloat(value[3]));
        }

        public static Rgba getRgba(JsonElement obj, string name) {
            return toRgba(property(obj, name));
        }

        public static Guid getGuid(JsonElement obj, string name) {
            return parseGuid(getString(obj, name));
        }

        public static Guid parseGuid(string text) {
            Guid guid;
            if (text == null || !Guid.TryParseExact(text, "N", out guid)) {
                throw new SaveFormatException("'" + text + "' is not an id");
            }
            return guid;
        }

        public EntityId getEntityId(JsonElement obj, string name) {
            return entityId(property(obj, name));
        }

        public NationId getNationId(JsonElement obj, string name) {
            return nationId(property(obj, name));
        }

        public List<EntityId> getIdList(JsonElement obj, string name) {
            List<EntityId> ids = new List<EntityId>();
            foreach (JsonElement element in property(obj, name).EnumerateArray()) {
                ids.Add(entityId(element));
            }
            return ids;
        }

        public void readIdIntMap(JsonElement obj, string name, Dictionary<EntityId, int> into) {
            into.Clear();
            foreach (JsonElement entry in property(obj, name).EnumerateArray()) {
                into[entityId(entry[0])] = entry[1].GetInt32();
            }
        }

        public static List<string> getStringList(JsonElement obj, string name) {
            List<string> values = new List<string>();
            foreach (JsonElement element in property(obj, name).EnumerateArray()) {
                values.Add(element.ValueKind == JsonValueKind.Null ? null : element.GetString());
            }
            return values;
        }

        public Inventory getInventory(JsonElement obj, string name) {
            Inventory inventory = new Inventory(0);
            inventory.load(property(obj, name));
            return inventory;
        }

        public static Appearance getAppearance(JsonElement obj, string name) {
            JsonElement value = property(obj, name);
            if (value.ValueKind == JsonValueKind.Null) {
                return null;
            }
            Appearance appearance = new Appearance();
            foreach (JsonElement part in property(value, "parts").EnumerateArray()) {
                appearance.addPart(getEnum<PrimitiveKind>(part, "kind"), getVector3(part, "offset"),
                    getVector3(part, "scale"), getRgba(part, "color"));
            }
            appearance.setLabel(getString(value, "label"));
            appearance.setLabelOffset(getVector3(value, "labelOffset"));
            appearance.setLabelScale(getFloat(value, "labelScale"));
            return appearance;
        }
    }
}
