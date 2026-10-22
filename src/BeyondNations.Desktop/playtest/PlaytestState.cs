using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;
using beyondnations.desktop.ui;

namespace beyondnations.desktop.playtest {

    /**
    * What the host knows that the simulation does not: the screen, the frame
    * rate and which panels are open. Game fills one in each frame for the
    * playtest recorder; nothing else reads it.
    */
    public class PlaytestView {
        public ScreenType Screen = ScreenType.TITLE;
        public int FramesPerSecond;
        public List<string> OpenPanels = new List<string>();
        public string Tip;
    }

    /** One entry in state.json's "nearest": something the harness can steer to. */
    public class PlaytestTarget {
        public string Name;
        public string Id;
        public float Distance;
        public float Bearing;
    }

    /**
    * One snapshot of the game as a playtesting agent sees it, written to
    * state.json by PlaytestRecorder (--playtest-out, docs/PLAYTESTING.md).
    * It is everything on the HUD plus where the nearest things are, so an
    * agent can steer by it instead of guessing distances from a screenshot.
    *
    * capture() reads; toJson() writes. Neither changes the simulation.
    */
    public class PlaytestState {
        /** How many of each kind "nearest" lists. */
        public const int NearestPerKind = 3;

        /** The kinds "nearest" reports, in the order they are written. */
        public static readonly string[] NearestKinds = { "tree", "rock", "pawn", "chicken", "settlement", "stall" };

        public long WallTimeMs;
        public string Screen;
        public int FramesPerSecond;
        /** Simulation ticks per wall-clock second since the last state; below 50 means the world runs slow. */
        public float TicksPerSecond;
        public bool HasWorld;
        public int Seed;
        public int Tick;
        public int Day;
        public float TimeOfDay;
        public string Season;
        public string Region;
        public string Era;

        public float X, Z, YawDeg, Energy;
        public int Coins;
        public int SpeedPerSecond;
        public SortedDictionary<string, int> Inventory = new SortedDictionary<string, int>();
        public string Nation;
        public string Role;
        public string Settlement;
        public bool Alive = true;

        public int ObjectiveNumber;
        public string ObjectiveTitle;
        public string ObjectiveProgress;
        public int ObjectivesCompleted;

        public string Status;
        public string Tip;
        public List<string> OpenPanels = new List<string>();

        public string EventTitle;
        public List<string> EventOptions = new List<string>();

        public Dictionary<string, List<PlaytestTarget>> Nearest = new Dictionary<string, List<PlaytestTarget>>();

        /** The state of simulation (which may be null, off the world) as view shows it. */
        public static PlaytestState capture(Simulation simulation, PlaytestView view, long wallTimeMs) {
            PlaytestState state = new PlaytestState();
            state.WallTimeMs = wallTimeMs;
            state.Screen = view.Screen.ToString();
            state.FramesPerSecond = view.FramesPerSecond;
            state.OpenPanels.AddRange(view.OpenPanels);
            state.Tip = view.Tip;
            state.HasWorld = simulation != null;
            if (simulation == null) {
                return state;
            }

            Player player = simulation.getPlayer();
            Vector3 position = player.getPosition();
            state.Seed = ShareSession.seedOf(simulation);
            state.Tick = simulation.getTickCounter().getTotalTicks();
            state.Day = simulation.getTimeOfDay().getDay();
            state.TimeOfDay = simulation.getTimeOfDay().getFraction();
            state.Season = SeasonCalendar.name(simulation.getSeasonCalendar().getSeason());
            state.Region = RegionCatalog.name(simulation.getRegionMap().regionAt(position));
            state.Era = Eras.getDisplayName(simulation.getEraTracker().getEra());

            state.X = position.X;
            state.Z = position.Z;
            state.YawDeg = PlaytestBearing.headingDegrees(player.getYaw());
            state.Energy = player.getEnergy();
            state.SpeedPerSecond = player.getCurrentSpeed();
            Inventory inventory = player.getInventory();
            state.Coins = inventory.getNumItems(ItemType.COIN);
            foreach (ItemType type in Enum.GetValues(typeof(ItemType))) {
                int count = inventory.getNumItems(type);
                if (count > 0 && type != ItemType.COIN) {
                    state.Inventory[type.ToString().ToLowerInvariant()] = count;
                }
            }
            if (player.getNationId() != null) {
                Nation nation = simulation.getNationRepository().getNation(player.getNationId());
                if (nation != null) {
                    state.Nation = nation.getName();
                    state.Role = nation.getRole(player.getId()).ToString();
                }
            }
            EntityId settlementId = player.isCurrentlyInSettlement() ? player.getCurrentSettlementId() : player.getHomeSettlementId();
            Entity settlement = settlementId == null ? null : simulation.getEntityRepository().getEntity(settlementId);
            state.Settlement = settlement?.getName();
            state.Alive = player.getEnergy() > 0f && !simulation.getRealm().isRunOver();

            ObjectiveTracker objectives = simulation.getObjectiveTracker();
            state.ObjectivesCompleted = objectives.getCompletedCount();
            Objective current = objectives.getCurrent();
            if (current != null) {
                state.ObjectiveNumber = objectives.getCompletedCount() + 1;
                state.ObjectiveTitle = current.getTitle();
                ObjectiveProgress progress = objectives.getCurrentProgress();
                state.ObjectiveProgress = progress.hasCounter() ? progress.describe() : null;
            }

            state.Status = player.getStatus().getStatus();

            WorldEventSheet.View sheet = WorldEventSheet.getView(simulation);
            if (sheet != null) {
                state.EventTitle = sheet.Title;
                foreach (WorldEventSheet.Row row in sheet.Rows) {
                    state.EventOptions.Add(row.Available ? row.Label : row.Label + " (unavailable)");
                }
            }

            captureNearest(state, simulation, position, player.getYaw(), player.getId());
            return state;
        }

        /** One pass over the entities, keeping the closest few of each kind. */
        private static void captureNearest(PlaytestState state, Simulation simulation, Vector3 position, float yaw, EntityId playerId) {
            Dictionary<string, List<KeyValuePair<float, Entity>>> found = new Dictionary<string, List<KeyValuePair<float, Entity>>>();
            foreach (string kind in NearestKinds) {
                found[kind] = new List<KeyValuePair<float, Entity>>();
            }
            foreach (Entity entity in simulation.getEntityRepository().getEntities()) {
                if (entity.isMarkedForDeletion() || entity.getId() == playerId) {
                    continue;
                }
                string kind = kindOf(entity.getType());
                if (kind == null) {
                    continue;
                }
                float distance = Vector3.Distance(position, entity.getPosition());
                found[kind].Add(new KeyValuePair<float, Entity>(distance, entity));
                if (entity is Settlement withMarket && withMarket.getMarket() != null && withMarket.getMarket().getNumStalls() > 0) {
                    found["stall"].Add(new KeyValuePair<float, Entity>(distance, entity));
                }
            }
            foreach (string kind in NearestKinds) {
                List<KeyValuePair<float, Entity>> list = found[kind];
                list.Sort((a, b) => a.Key.CompareTo(b.Key));
                List<PlaytestTarget> targets = new List<PlaytestTarget>();
                for (int i = 0; i < list.Count && i < NearestPerKind; i++) {
                    Entity entity = list[i].Value;
                    string name = entity.getName();
                    if (kind == "stall") {
                        Market market = ((Settlement) entity).getMarket();
                        name = name + " market, " + market.getNumStalls() + " stall" + (market.getNumStalls() == 1 ? "" : "s")
                            + (market.hasStall(simulation.getPlayer().getId()) ? ", one yours" : "");
                    }
                    targets.Add(new PlaytestTarget {
                        Name = name,
                        Id = shortId(entity.getId()),
                        Distance = list[i].Key,
                        Bearing = PlaytestBearing.relativeDegrees(position, yaw, entity.getPosition()),
                    });
                }
                state.Nearest[kind] = targets;
            }
        }

        /** The "nearest" kind an entity counts as, or null when it is not reported. */
        public static string kindOf(EntityType type) {
            switch (type) {
                case EntityType.TREE: return "tree";
                case EntityType.ROCK: return "rock";
                case EntityType.PAWN: return "pawn";
                case EntityType.CHICKEN: return "chicken";
                case EntityType.SETTLEMENT: return "settlement";
                default: return null;
            }
        }

        /** The first eight hex digits of an id: enough to tell entities apart in one session. */
        public static string shortId(EntityId id) {
            string text = id == null ? "" : id.getId().ToString("N");
            return text.Length > 8 ? text.Substring(0, 8) : text;
        }

        /** The state as one line of JSON, which is what state.json holds. */
        public string toJson() {
            using (MemoryStream stream = new MemoryStream()) {
                using (Utf8JsonWriter writer = new Utf8JsonWriter(stream)) {
                    write(writer);
                }
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private void write(Utf8JsonWriter w) {
            w.WriteStartObject();
            w.WriteNumber("wallTimeMs", WallTimeMs);
            w.WriteString("version", GameVersion.Version);
            w.WriteString("screen", Screen);
            w.WriteNumber("fps", FramesPerSecond);
            w.WriteBoolean("world", HasWorld);
            w.WriteNumber("tps", round(TicksPerSecond, 1));
            w.WriteStartArray("openPanels");
            foreach (string panel in OpenPanels) {
                w.WriteStringValue(panel);
            }
            w.WriteEndArray();
            writeNullable(w, "tip", Tip);
            if (!HasWorld) {
                w.WriteEndObject();
                return;
            }
            w.WriteNumber("seed", Seed);
            w.WriteNumber("tick", Tick);
            w.WriteNumber("day", Day);
            w.WriteNumber("timeOfDay", round(TimeOfDay, 3));
            w.WriteString("season", Season);
            w.WriteString("region", Region);
            w.WriteString("era", Era);

            w.WriteStartObject("player");
            w.WriteNumber("x", round(X, 2));
            w.WriteNumber("z", round(Z, 2));
            w.WriteNumber("yawDeg", round(YawDeg, 1));
            w.WriteNumber("speed", SpeedPerSecond);
            w.WriteNumber("energy", round(Energy, 1));
            w.WriteNumber("coins", Coins);
            w.WriteStartObject("inventory");
            foreach (KeyValuePair<string, int> item in Inventory) {
                w.WriteNumber(item.Key, item.Value);
            }
            w.WriteEndObject();
            writeNullable(w, "nation", Nation);
            writeNullable(w, "role", Role);
            writeNullable(w, "settlement", Settlement);
            w.WriteBoolean("alive", Alive);
            w.WriteEndObject();

            w.WriteStartObject("objective");
            w.WriteNumber("number", ObjectiveNumber);
            writeNullable(w, "title", ObjectiveTitle);
            writeNullable(w, "progress", ObjectiveProgress);
            w.WriteNumber("completed", ObjectivesCompleted);
            w.WriteEndObject();

            writeNullable(w, "status", string.IsNullOrEmpty(Status) ? null : Status);

            if (EventTitle == null) {
                w.WriteNull("worldEvent");
            } else {
                w.WriteStartObject("worldEvent");
                w.WriteString("title", EventTitle);
                w.WriteStartArray("options");
                foreach (string option in EventOptions) {
                    w.WriteStringValue(option);
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }

            w.WriteStartObject("nearest");
            foreach (string kind in NearestKinds) {
                w.WriteStartArray(kind);
                if (Nearest.TryGetValue(kind, out List<PlaytestTarget> targets)) {
                    foreach (PlaytestTarget target in targets) {
                        w.WriteStartObject();
                        w.WriteString("name", target.Name);
                        w.WriteString("id", target.Id);
                        w.WriteNumber("distance", round(target.Distance, 1));
                        w.WriteNumber("bearing", round(target.Bearing, 1));
                        w.WriteEndObject();
                    }
                }
                w.WriteEndArray();
            }
            w.WriteEndObject();
            w.WriteEndObject();
        }

        private static void writeNullable(Utf8JsonWriter w, string name, string value) {
            if (value == null) {
                w.WriteNull(name);
            } else {
                w.WriteString(name, value);
            }
        }

        private static double round(float value, int digits) {
            return Math.Round((double) value, digits);
        }
    }
}
