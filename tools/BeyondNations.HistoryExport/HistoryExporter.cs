using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace beyondnations.historyexport {

    public sealed class ExportOptions {
        public int Seed = 42;
        public int Ticks = 30000;
        // Write an entity frame every this many ticks. Events are checked on
        // every tick regardless, so none is lost between frames.
        public int Every = 25;
        public int TicksPerSecond = 50;
        // Entity kinds to include in frames; null means all of them.
        public HashSet<EntityType> Kinds = null;
    }

    /**
    * Runs a seeded simulation headless and writes its history as JSON Lines.
    *
    * Format "beyond-nations-history/1", one JSON object per line, each with a
    * "t" (record type):
    *
    *   header  once, first: seed, ticks, every, ticksPerSecond, chunkSize,
    *           locationScale, settlement ranges, ticksPerDay.
    *   chunk   when a chunk of ground is generated: chunk x/z and its tiles as
    *           [x, z, size, r, g, b] (tile centre, edge length, colour).
    *   event   on the tick it happened. Kinds and where they come from:
    *             nation_founded, nation_joined, nation_left, nation_disbanded,
    *             pawn_spawned, pawn_died -- read from the simulation's own
    *             EventRepository (src/BeyondNations.Core/event/EventType.cs);
    *             leader_changed, settlement_founded, settlement_removed --
    *             derived by comparing each live nation's leader and settlement
    *             list with the previous tick, because Core changes those
    *             without producing an event (Simulation.cs #267,
    *             PawnBehaviorExecutor.executeConstructSettlementBehavior).
    *   frame   every `Every` ticks: the day (TimeOfDay.getDay()), each entity present (id, kind, nation,
    *           home settlement, visibility; position only when it moved since
    *           the entity's previous frame, and name plus full appearance only
    *           when it first appears or changes) and each nation (id, name,
    *           colour, leader, member count, settlements). An entity missing
    *           from a frame is gone.
    *
    * Identity: EntityId and NationId wrap Guid.NewGuid(), so they differ on
    * every run. The exporter numbers entities and nations itself, in the order
    * the simulation first exposes them (repository insertion order and event
    * order, both deterministic), so the same seed gives byte-identical output.
    * The numbers are first-seen order in this export, so they are stable for a
    * given seed, tick count and `Every`, not across different `Every` values.
    *
    * Determinism also relies on Core measuring time in ticks, not on the wall
    * clock: the pawn behaviour cadence and sapling growth both follow the tick
    * count and GameConfig's ticks per second (#272), which the exporter sets
    * from TicksPerSecond.
    */
    public sealed class HistoryExporter {
        public const string Format = "beyond-nations-history/1";

        private static readonly EventType[] EventOrder = {
            EventType.NationCreation,
            EventType.NationJoin,
            EventType.NationLeave,
            EventType.NationDisband,
            EventType.PawnSpawn,
            EventType.PawnDeath,
        };

        private readonly ExportOptions options;
        private readonly Simulation simulation;
        private readonly Utf8JsonWriter json;
        private readonly Stream sink;

        private readonly Dictionary<EntityId, int> entityIds = new Dictionary<EntityId, int>();
        private readonly Dictionary<NationId, int> nationIds = new Dictionary<NationId, int>();
        // Nations in the order they were founded, including disbanded ones.
        private readonly List<Nation> nations = new List<Nation>();
        private readonly Dictionary<int, int> lastLeader = new Dictionary<int, int>();
        private readonly Dictionary<int, List<int>> lastSettlements = new Dictionary<int, List<int>>();
        private readonly Dictionary<EventType, int> eventsSeen = new Dictionary<EventType, int>();
        private readonly Dictionary<int, string> lastAppearance = new Dictionary<int, string>();
        private readonly Dictionary<int, Vector3> lastPosition = new Dictionary<int, Vector3>();
        private readonly HashSet<string> chunksSeen = new HashSet<string>();
        private int nextEntityId = 1;
        private int nextNationId = 1;

        public HistoryExporter(ExportOptions options, Stream output) {
            this.options = options;
            GameConfig config = new GameConfig();
            config.setWorldSeed(options.Seed);
            config.setTicksPerSecond(options.TicksPerSecond);
            simulation = new Simulation(config);
            sink = output;
            json = new Utf8JsonWriter(output, new JsonWriterOptions {
                Indented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                SkipValidation = true,
            });
            foreach (EventType type in EventOrder) {
                eventsSeen[type] = 0;
            }
        }

        public Simulation getSimulation() { return simulation; }

        /**
        * Writes the whole history: the header, then for each tick its chunks,
        * events and (every `Every` ticks, and on the last) a frame.
        */
        public void run() {
            float dt = 1f / options.TicksPerSecond;
            writeHeader();
            for (int i = 0; i < options.Ticks; i++) {
                simulation.fixedUpdate(dt);
                int tick = simulation.getTickCounter().getTotalTicks();
                writeNewChunks(tick);
                writeEvents(tick);
                writeDerivedNationEvents(tick);
                if (tick % options.Every == 0 || i == options.Ticks - 1) {
                    writeFrame(tick);
                }
            }
            json.Flush();
        }

        /** Runs an export into memory and returns it, for tests and small runs. */
        public static string exportToString(ExportOptions options) {
            using (MemoryStream stream = new MemoryStream()) {
                new HistoryExporter(options, stream).run();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        // --- identity -------------------------------------------------------

        private int idOf(EntityId id) {
            if (id == null) {
                return 0;
            }
            int n;
            if (!entityIds.TryGetValue(id, out n)) {
                n = nextEntityId++;
                entityIds[id] = n;
            }
            return n;
        }

        private int idOf(Nation nation) {
            int n;
            if (!nationIds.TryGetValue(nation.getId(), out n)) {
                n = nextNationId++;
                nationIds[nation.getId()] = n;
                nations.Add(nation);
            }
            return n;
        }

        private int idOf(NationId id) {
            if (id == null) {
                return 0;
            }
            int n;
            return nationIds.TryGetValue(id, out n) ? n : 0;
        }

        private string nameOf(EntityId id) {
            Entity entity = simulation.getEntityRepository().getEntity(id);
            return entity != null ? entity.getName() : "Entity " + idOf(id);
        }

        // --- records --------------------------------------------------------

        private void endLine() {
            json.Flush();
            sink.WriteByte((byte) '\n');
            json.Reset();
        }

        private void writeHeader() {
            GameConfig config = simulation.getGameConfig();
            json.WriteStartObject();
            json.WriteString("t", "header");
            json.WriteString("format", Format);
            json.WriteNumber("seed", options.Seed);
            json.WriteNumber("ticks", options.Ticks);
            json.WriteNumber("every", options.Every);
            json.WriteNumber("ticksPerSecond", options.TicksPerSecond);
            json.WriteNumber("chunkSize", config.getChunkSize());
            json.WriteNumber("locationScale", config.getLocationScale());
            json.WriteNumber("settlementJoinRange", config.getSettlementJoinRange());
            json.WriteNumber("minDistanceBetweenSettlements", config.getMinDistanceBetweenSettlements());
            json.WriteNumber("ticksPerDay", simulation.getTimeOfDay().getTicksPerDay());
            json.WriteEndObject();
            endLine();
        }

        private void writeNewChunks(int tick) {
            List<Chunk> chunks = simulation.getEnvironment().getChunks();
            if (chunks.Count == chunksSeen.Count) {
                return;
            }
            foreach (Chunk chunk in chunks) {
                string key = chunk.getX() + "," + chunk.getZ();
                if (!chunksSeen.Add(key)) {
                    continue;
                }
                json.WriteStartObject();
                json.WriteString("t", "chunk");
                json.WriteNumber("tick", tick);
                json.WriteNumber("x", chunk.getX());
                json.WriteNumber("z", chunk.getZ());
                json.WriteStartArray("tiles");
                Location[,] locations = chunk.getLocations();
                for (int x = 0; x < locations.GetLength(0); x++) {
                    for (int z = 0; z < locations.GetLength(1); z++) {
                        Location location = locations[x, z];
                        if (location == null) {
                            continue;
                        }
                        Vector3 p = location.getPosition();
                        Rgba c = location.getColor();
                        json.WriteStartArray();
                        writeNumber(p.X);
                        writeNumber(p.Z);
                        json.WriteNumberValue(location.getScale());
                        writeNumber(c.r);
                        writeNumber(c.g);
                        writeNumber(c.b);
                        json.WriteEndArray();
                    }
                }
                json.WriteEndArray();
                json.WriteEndObject();
                endLine();
            }
        }

        private void writeEvents(int tick) {
            EventRepository repository = simulation.getEventRepository();
            foreach (EventType type in EventOrder) {
                List<Event> events;
                try {
                    events = repository.getEvents(type);
                } catch (KeyNotFoundException) {
                    continue; // none of this type yet
                }
                for (int i = eventsSeen[type]; i < events.Count; i++) {
                    writeEvent(tick, events[i]);
                }
                eventsSeen[type] = events.Count;
            }
        }

        private void writeEvent(int tick, Event e) {
            json.WriteStartObject();
            json.WriteString("t", "event");
            json.WriteNumber("tick", tick);
            // Core's own descriptions are kept, except where they print an
            // EntityId: that is a fresh Guid every run (see the class comment).
            string description = e.getDescription();
            switch (e.getType()) {
                case EventType.NationCreation: {
                    Nation nation = ((NationCreationEvent) e).getNation();
                    int id = idOf(nation);
                    json.WriteString("kind", "nation_founded");
                    json.WriteNumber("nation", id);
                    json.WriteString("name", nation.getName());
                    writeColor("color", nation.getColor());
                    json.WriteNumber("leader", idOf(nation.getLeaderId()));
                    lastLeader[id] = idOf(nation.getLeaderId());
                    lastSettlements[id] = new List<int>();
                    break;
                }
                case EventType.NationJoin: {
                    NationJoinEvent join = (NationJoinEvent) e;
                    json.WriteString("kind", "nation_joined");
                    json.WriteNumber("nation", idOf(join.getNation()));
                    json.WriteNumber("entity", idOf(join.getEntityId()));
                    description = nameOf(join.getEntityId()) + " has joined the nation " + join.getNation().getName() + ".";
                    break;
                }
                case EventType.NationLeave: {
                    NationLeaveEvent leave = (NationLeaveEvent) e;
                    json.WriteString("kind", "nation_left");
                    json.WriteNumber("nation", idOf(leave.getNation()));
                    json.WriteNumber("entity", idOf(leave.getEntityId()));
                    description = nameOf(leave.getEntityId()) + " has left the nation " + leave.getNation().getName() + ".";
                    break;
                }
                case EventType.NationDisband: {
                    Nation nation = ((NationDisbandEvent) e).getNation();
                    json.WriteString("kind", "nation_disbanded");
                    json.WriteNumber("nation", idOf(nation));
                    json.WriteString("name", nation.getName());
                    break;
                }
                case EventType.PawnSpawn: {
                    Pawn pawn = ((PawnSpawnEvent) e).getPawn();
                    json.WriteString("kind", "pawn_spawned");
                    json.WriteNumber("entity", idOf(pawn.getId()));
                    json.WriteString("name", pawn.getName());
                    break;
                }
                case EventType.PawnDeath: {
                    Pawn pawn = ((PawnDeathEvent) e).getPawn();
                    json.WriteString("kind", "pawn_died");
                    json.WriteNumber("entity", idOf(pawn.getId()));
                    json.WriteString("name", pawn.getName());
                    break;
                }
            }
            json.WriteString("description", description);
            json.WriteEndObject();
            endLine();
        }

        private void writeDerivedNationEvents(int tick) {
            NationRepository repository = simulation.getNationRepository();
            EntityRepository entities = simulation.getEntityRepository();
            foreach (Nation nation in nations) {
                if (repository.getNation(nation.getId()) == null) {
                    continue; // disbanded; its nation_disbanded event is already out
                }
                int id = nationIds[nation.getId()];

                int leader = idOf(nation.getLeaderId());
                if (lastLeader[id] != leader) {
                    json.WriteStartObject();
                    json.WriteString("t", "event");
                    json.WriteNumber("tick", tick);
                    json.WriteString("kind", "leader_changed");
                    json.WriteNumber("nation", id);
                    json.WriteNumber("from", lastLeader[id]);
                    json.WriteNumber("to", leader);
                    Entity newLeader = entities.getEntity(nation.getLeaderId());
                    json.WriteString("description", (newLeader != null ? newLeader.getName() : "Someone")
                        + " is now the leader of " + nation.getName() + ".");
                    json.WriteEndObject();
                    endLine();
                    lastLeader[id] = leader;
                }

                List<int> previous = lastSettlements[id];
                List<int> current = new List<int>();
                foreach (EntityId settlementId in nation.getSettlements()) {
                    current.Add(idOf(settlementId));
                }
                foreach (int s in current) {
                    if (!previous.Contains(s)) {
                        Entity settlement = entities.getEntity(nation.getSettlements()[current.IndexOf(s)]);
                        json.WriteStartObject();
                        json.WriteString("t", "event");
                        json.WriteNumber("tick", tick);
                        json.WriteString("kind", "settlement_founded");
                        json.WriteNumber("nation", id);
                        json.WriteNumber("settlement", s);
                        if (settlement != null) {
                            writeVector("pos", settlement.getPosition());
                        }
                        json.WriteString("description", "A settlement of " + nation.getName() + " has been founded.");
                        json.WriteEndObject();
                        endLine();
                    }
                }
                foreach (int s in previous) {
                    if (!current.Contains(s)) {
                        json.WriteStartObject();
                        json.WriteString("t", "event");
                        json.WriteNumber("tick", tick);
                        json.WriteString("kind", "settlement_removed");
                        json.WriteNumber("nation", id);
                        json.WriteNumber("settlement", s);
                        json.WriteString("description", "A settlement of " + nation.getName() + " has been removed.");
                        json.WriteEndObject();
                        endLine();
                    }
                }
                lastSettlements[id] = current;
            }
        }

        private void writeFrame(int tick) {
            EntityRepository repository = simulation.getEntityRepository();
            json.WriteStartObject();
            json.WriteString("t", "frame");
            json.WriteNumber("tick", tick);
            // Core's calendar (TimeOfDay, #279): days completed since the world began.
            json.WriteNumber("day", simulation.getTimeOfDay().getDay());

            json.WriteStartArray("entities");
            int count = repository.getEntityCount();
            for (int i = 0; i < count; i++) {
                Entity entity = repository.getEntityAt(i);
                if (entity.isMarkedForDeletion()) {
                    continue;
                }
                if (options.Kinds != null && !options.Kinds.Contains(entity.getType())) {
                    continue;
                }
                writeEntity(entity);
            }
            json.WriteEndArray();

            json.WriteStartArray("nations");
            NationRepository nationRepository = simulation.getNationRepository();
            foreach (Nation nation in nations) {
                if (nationRepository.getNation(nation.getId()) == null) {
                    continue;
                }
                json.WriteStartObject();
                json.WriteNumber("id", nationIds[nation.getId()]);
                json.WriteString("name", nation.getName());
                writeColor("color", nation.getColor());
                json.WriteNumber("leader", idOf(nation.getLeaderId()));
                json.WriteNumber("members", nation.getNumberOfMembers());
                json.WriteStartArray("settlements");
                foreach (EntityId settlementId in nation.getSettlements()) {
                    json.WriteNumberValue(idOf(settlementId));
                }
                json.WriteEndArray();
                json.WriteEndObject();
            }
            json.WriteEndArray();

            json.WriteEndObject();
            endLine();
        }

        private void writeEntity(Entity entity) {
            int id = idOf(entity.getId());
            json.WriteStartObject();
            json.WriteNumber("id", id);
            json.WriteString("kind", entity.getType().ToString().ToLowerInvariant());
            // Position only when it moved since this entity's last frame: most
            // entities are trees and rocks, which never do. A reader holds the
            // last position it saw.
            Vector3 position = entity.getPosition();
            Vector3 previousPosition;
            if (!lastPosition.TryGetValue(id, out previousPosition) || previousPosition != position) {
                lastPosition[id] = position;
                writeVector("pos", position);
            }
            if (!entity.isVisible()) {
                json.WriteBoolean("visible", false);
            }

            NationId nationId = null;
            EntityId home = null;
            if (entity is Pawn pawn) {
                nationId = pawn.getNationId();
                home = pawn.getHomeSettlementId();
            } else if (entity is Player player) {
                nationId = player.getNationId();
                home = player.getHomeSettlementId();
            } else if (entity is Settlement settlement) {
                nationId = settlement.getNationId();
            }
            if (nationId != null) {
                json.WriteNumber("nation", idOf(nationId));
            }
            if (home != null) {
                json.WriteNumber("home", idOf(home));
            }

            // Appearance only when new or changed: trees and rocks never change,
            // and repeating them every frame would be most of the file.
            string appearance = describeAppearance(entity);
            string previous;
            if (!lastAppearance.TryGetValue(id, out previous) || previous != appearance) {
                lastAppearance[id] = appearance;
                json.WriteString("name", entity.getName());
                json.WriteStartArray("parts");
                foreach (AppearancePart part in entity.getAppearance().getParts()) {
                    json.WriteStartObject();
                    json.WriteString("prim", part.getKind().ToString().ToLowerInvariant());
                    writeVector("offset", part.getOffset());
                    writeVector("scale", part.getScale());
                    writeColor("color", part.getColor());
                    json.WriteEndObject();
                }
                json.WriteEndArray();
            }
            json.WriteEndObject();
        }

        private static string describeAppearance(Entity entity) {
            StringBuilder builder = new StringBuilder();
            foreach (AppearancePart part in entity.getAppearance().getParts()) {
                builder.Append(part.getKind()).Append(part.getOffset()).Append(part.getScale())
                    .Append(part.getColor().r).Append(',').Append(part.getColor().g).Append(',')
                    .Append(part.getColor().b).Append(';');
            }
            return builder.ToString();
        }

        // --- numbers --------------------------------------------------------

        // Three decimals: positions are in world units (tiles are 15 wide) and
        // colours in 0..1, so this loses nothing visible and halves the file.
        private void writeNumber(float value) {
            json.WriteNumberValue(Math.Round((double) value, 3));
        }

        private void writeVector(string name, Vector3 v) {
            json.WriteStartArray(name);
            writeNumber(v.X);
            writeNumber(v.Y);
            writeNumber(v.Z);
            json.WriteEndArray();
        }

        private void writeColor(string name, Rgba c) {
            json.WriteStartArray(name);
            writeNumber(c.r);
            writeNumber(c.g);
            writeNumber(c.b);
            json.WriteEndArray();
        }
    }
}
