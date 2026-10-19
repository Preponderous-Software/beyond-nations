using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using beyondnations;

namespace beyondnationstests {

    /**
    * Walks two object graphs side by side -- every field of every object
    * reachable from two Simulations -- and reports the first place they
    * differ. This is what holds a loaded world to "identical": not a summary,
    * but every float bit for bit, every list in order, and the same shape of
    * shared references (if two fields point at one object in the original,
    * they point at one object in the copy).
    *
    * Ids are Guids from Guid.NewGuid(), the one thing in the simulation no
    * seed reproduces: two runs of one seed give their pawns different ids.
    * So in exact mode (straight after a load) every id must be equal, and in
    * matched mode (after both worlds have run on) ids that existed when the
    * world was saved must still be equal, while ids made since need only
    * correspond one to one -- the nth entity in one repository has the id of
    * the nth in the other, and every reference agrees.
    *
    * Dictionaries and sets are compared as maps, not in enumeration order:
    * a Dictionary's order depends on its history of removals, and nothing in
    * the simulation lets that order decide anything (lists, which do, are
    * compared in order). The only fields not compared at all are the ones that
    * are not world state, listed as Transient in SaveCoverage. The event
    * repository is compared by count and by the events a save keeps.
    */
    public class WorldComparer {
        private readonly Dictionary<object, object> forward = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<object, object> backward = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<Guid, Guid> idForward = new Dictionary<Guid, Guid>();
        private readonly Dictionary<Guid, Guid> idBackward = new Dictionary<Guid, Guid>();
        private readonly HashSet<Guid> savedIds;
        private int objectsCompared;

        private WorldComparer(HashSet<Guid> savedIds) {
            this.savedIds = savedIds;
        }

        /** Every id in a world, for matched mode: these must stay equal however long the worlds run. */
        public static HashSet<Guid> idsOf(Simulation simulation) {
            HashSet<Guid> ids = new HashSet<Guid>();
            collectIds(simulation, ids, new HashSet<object>(ReferenceEqualityComparer.Instance));
            return ids;
        }

        private static void collectIds(object o, HashSet<Guid> ids, HashSet<object> seen) {
            if (o == null || o is string || o.GetType().IsPrimitive || o.GetType().IsEnum || !seen.Add(o)) {
                return;
            }
            if (isId(o)) {
                ids.Add(guidOf(o));
                return;
            }
            if (o is IEnumerable) {
                foreach (object item in (IEnumerable) o) {
                    collectIds(item, ids, seen);
                }
                if (o is IDictionary) {
                    foreach (object key in ((IDictionary) o).Keys) {
                        collectIds(key, ids, seen);
                    }
                }
                return;
            }
            if (o.GetType().Assembly != typeof(Simulation).Assembly) {
                return;
            }
            for (Type t = o.GetType(); t != null && t != typeof(object); t = t.BaseType) {
                foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                    if (!field.FieldType.IsValueType || field.FieldType == typeof(Guid)) {
                        collectIds(field.GetValue(o), ids, seen);
                    }
                }
            }
        }

        /** Exact mode: null when the two are the same, otherwise where and how they differ. */
        public static string difference(Simulation a, Simulation b) {
            return run(a, b, null);
        }

        /** Matched mode: ids in savedIds must be equal, newer ones need only correspond. */
        public static string difference(Simulation a, Simulation b, HashSet<Guid> savedIds) {
            return run(a, b, savedIds);
        }

        private static string run(Simulation a, Simulation b, HashSet<Guid> savedIds) {
            WorldComparer comparer = new WorldComparer(savedIds);
            try {
                comparer.matchIds(a, b);
                comparer.compare(a, b, "simulation");
                return null;
            } catch (Difference d) {
                return d.Message;
            }
        }

        public static int countObjects(Simulation a, Simulation b, HashSet<Guid> savedIds) {
            WorldComparer comparer = new WorldComparer(savedIds);
            comparer.matchIds(a, b);
            comparer.compare(a, b, "simulation");
            return comparer.objectsCompared;
        }

        /**
        * Pairs up the ids of everything that lives in a repository, position
        * by position, so a dictionary keyed by an id made after the save can
        * be matched to its counterpart before the walk reaches it.
        */
        private void matchIds(Simulation a, Simulation b) {
            EntityRepository ea = a.getEntityRepository();
            EntityRepository eb = b.getEntityRepository();
            if (ea.getEntityCount() != eb.getEntityCount()) {
                fail("simulation.entityRepository", "count " + ea.getEntityCount() + " vs " + eb.getEntityCount());
            }
            for (int i = 0; i < ea.getEntityCount(); i++) {
                mapId(guidOf(ea.getEntityAt(i).getId()), guidOf(eb.getEntityAt(i).getId()), "simulation.entityRepository[" + i + "].id");
            }
            List<Nation> na = a.getNationRepository().getNations();
            List<Nation> nb = b.getNationRepository().getNations();
            if (na.Count != nb.Count) {
                fail("simulation.nationRepository", "count " + na.Count + " vs " + nb.Count);
            }
            for (int i = 0; i < na.Count; i++) {
                mapId(guidOf(na[i].getId()), guidOf(nb[i].getId()), "simulation.nationRepository[" + i + "].id");
            }
        }

        private class Difference : Exception {
            public Difference(string message) : base(message) {
            }
        }

        private static void fail(string path, string message) {
            throw new Difference(path + ": " + message);
        }

        private static bool isId(object o) {
            return o is EntityId || o is NationId || o is ChunkId || o is LocationId || o is EnvironmentId;
        }

        private static Guid guidOf(object id) {
            return Guid.Parse(id.ToString());
        }

        private void mapId(Guid a, Guid b, string path) {
            if (savedIds == null || savedIds.Contains(a) || savedIds.Contains(b)) {
                if (a != b) {
                    fail(path, "id " + a + " vs " + b + (savedIds == null ? "" : " (saved: " + savedIds.Contains(a) + ", " + savedIds.Contains(b) + ")"));
                }
            }
            Guid mapped;
            if (idForward.TryGetValue(a, out mapped)) {
                if (mapped != b) {
                    fail(path, "id " + a + " corresponds to " + mapped + " elsewhere, here to " + b);
                }
                return;
            }
            if (idBackward.TryGetValue(b, out mapped)) {
                fail(path, "id " + b + " corresponds to " + mapped + " elsewhere, here to " + a);
            }
            idForward[a] = b;
            idBackward[b] = a;
        }

        private void compare(object a, object b, string path) {
            if (a == null || b == null) {
                if (a != null || b != null) {
                    fail(path, (a == null ? "null" : describe(a)) + " vs " + (b == null ? "null" : describe(b)));
                }
                return;
            }

            Type type = a.GetType();

            // events: what a save keeps of them
            if (a is Event && b is Event) {
                Event ea = (Event) a;
                Event eb = (Event) b;
                // an event made after the save may name an id made after it
                string described = GuidPattern.Replace(ea.getDescription() ?? "", m => translate(m.Value));
                if (ea.getType() != eb.getType() || described != (eb.getDescription() ?? "")) {
                    fail(path, "event '" + ea.getDescription() + "' vs '" + eb.getDescription() + "'");
                }
                return;
            }

            if (type != b.GetType()) {
                fail(path, "type " + type.Name + " vs " + b.GetType().Name);
            }

            if (type.IsPrimitive || type.IsEnum || a is string || a is decimal || a is Guid || a is DateTime) {
                if (a is float) {
                    if (BitConverter.SingleToInt32Bits((float) a) != BitConverter.SingleToInt32Bits((float) b)) {
                        fail(path, ((float) a).ToString("R") + " vs " + ((float) b).ToString("R"));
                    }
                } else if (a is double) {
                    if (BitConverter.DoubleToInt64Bits((double) a) != BitConverter.DoubleToInt64Bits((double) b)) {
                        fail(path, ((double) a).ToString("R") + " vs " + ((double) b).ToString("R"));
                    }
                } else if (!a.Equals(b)) {
                    fail(path, a + " vs " + b);
                }
                return;
            }

            if (!type.IsValueType) {
                object mapped;
                if (forward.TryGetValue(a, out mapped)) {
                    if (!ReferenceEquals(mapped, b)) {
                        fail(path, "shared reference differs: the original points at an object seen before, the copy at a different one");
                    }
                    return;
                }
                if (backward.TryGetValue(b, out mapped)) {
                    fail(path, "shared reference differs: the copy points at an object seen before, the original at a different one");
                }
                forward[a] = b;
                backward[b] = a;
                objectsCompared++;
            }

            if (isId(a)) {
                mapId(guidOf(a), guidOf(b), path);
                return;
            }

            if (a is Delegate) {
                // the same code, over the same captured values (an objective's target, say)
                Delegate da = (Delegate) a;
                Delegate db = (Delegate) b;
                if (da.Method != db.Method) {
                    fail(path, "delegate " + da.Method.Name + " vs " + db.Method.Name);
                }
                compare(da.Target, db.Target, path + ".target");
                return;
            }

            if (a is NationRelations) {
                compareRelations(a, b, path);
                return;
            }

            if (a is EventRepository) {
                compareEvents((EventRepository) a, (EventRepository) b, path);
                return;
            }

            if (a is Array) {
                Array aa = (Array) a;
                Array ab = (Array) b;
                if (aa.Rank != ab.Rank) {
                    fail(path, "array rank");
                }
                for (int d = 0; d < aa.Rank; d++) {
                    if (aa.GetLength(d) != ab.GetLength(d)) {
                        fail(path, "array length " + aa.GetLength(d) + " vs " + ab.GetLength(d));
                    }
                }
                IEnumerator ia = aa.GetEnumerator();
                IEnumerator ib = ab.GetEnumerator();
                int i = 0;
                while (ia.MoveNext() && ib.MoveNext()) {
                    compare(ia.Current, ib.Current, path + "[" + i++ + "]");
                }
                return;
            }

            if (a is IDictionary) {
                compareMaps((IDictionary) a, (IDictionary) b, path);
                return;
            }

            if (a is IEnumerable && type.IsGenericType && type.Namespace == "System.Collections.Generic") {
                List<object> la = new List<object>();
                List<object> lb = new List<object>();
                foreach (object o in (IEnumerable) a) {
                    la.Add(o);
                }
                foreach (object o in (IEnumerable) b) {
                    lb.Add(o);
                }
                if (la.Count != lb.Count) {
                    fail(path, "count " + la.Count + " vs " + lb.Count);
                }
                if (type.GetGenericTypeDefinition() == typeof(HashSet<>)) {
                    // a set: the same members, in any order
                    HashSet<object> members = new HashSet<object>(lb);
                    foreach (object o in la) {
                        if (!members.Contains(o)) {
                            fail(path, "the copy's set lacks " + o);
                        }
                    }
                    return;
                }
                for (int i = 0; i < la.Count; i++) {
                    compare(la[i], lb[i], path + "[" + i + "]");
                }
                return;
            }

            if (type.Assembly != typeof(Simulation).Assembly && !type.IsValueType) {
                fail(path, "an object of type " + type.FullName + " the comparer does not know");
            }

            for (Type t = type; t != null && t != typeof(object) && t != typeof(ValueType); t = t.BaseType) {
                foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                    if (SaveCoverage.isTransient(t, field.Name)) {
                        continue;
                    }
                    compare(field.GetValue(a), field.GetValue(b), path + "." + field.Name);
                }
            }
        }

        private void compareMaps(IDictionary a, IDictionary b, string path) {
            if (a.Count != b.Count) {
                fail(path, "count " + a.Count + " vs " + b.Count);
            }
            // the copy's keys, found by id (translated across the id matching) or by value
            Dictionary<Guid, object> byId = new Dictionary<Guid, object>();
            foreach (object key in b.Keys) {
                if (isId(key)) {
                    byId[guidOf(key)] = key;
                }
            }
            foreach (DictionaryEntry entry in a) {
                object keyB;
                if (isId(entry.Key)) {
                    Guid guid = guidOf(entry.Key);
                    Guid mapped;
                    if (!idForward.TryGetValue(guid, out mapped)) {
                        mapped = guid;
                    }
                    if (!byId.TryGetValue(mapped, out keyB)) {
                        fail(path, "the copy has no entry for " + guid);
                    }
                } else {
                    if (!b.Contains(entry.Key)) {
                        fail(path, "the copy has no entry for " + entry.Key);
                    }
                    keyB = entry.Key;
                    foreach (object candidate in b.Keys) {
                        if (candidate.Equals(entry.Key)) {
                            keyB = candidate;
                            break;
                        }
                    }
                }
                compare(entry.Key, keyB, path + ".key(" + entry.Key + ")");
                compare(entry.Value, b[keyB], path + "[" + entry.Key + "]");
            }
        }

        /**
        * NationRelations keys its maps by "guid|guid" strings, so a pair of
        * nations founded after the save has a different key in each world:
        * the keys are translated across the id matching before comparing.
        */
        private void compareRelations(object a, object b, string path) {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Dictionary<string, int> scoresA = (Dictionary<string, int>) typeof(NationRelations).GetField("scores", flags).GetValue(a);
            Dictionary<string, int> scoresB = (Dictionary<string, int>) typeof(NationRelations).GetField("scores", flags).GetValue(b);
            if (scoresA.Count != scoresB.Count) {
                fail(path + ".scores", "count " + scoresA.Count + " vs " + scoresB.Count);
            }
            foreach (KeyValuePair<string, int> entry in scoresA) {
                string key = translatePair(entry.Key);
                int score;
                if (!scoresB.TryGetValue(key, out score) || score != entry.Value) {
                    fail(path + ".scores[" + entry.Key + "]", entry.Value + " vs " + (scoresB.ContainsKey(key) ? score.ToString() : "missing"));
                }
            }
            foreach (string name in new[] { "alliances", "denounced" }) {
                HashSet<string> setA = (HashSet<string>) typeof(NationRelations).GetField(name, flags).GetValue(a);
                HashSet<string> setB = (HashSet<string>) typeof(NationRelations).GetField(name, flags).GetValue(b);
                if (setA.Count != setB.Count) {
                    fail(path + "." + name, "count " + setA.Count + " vs " + setB.Count);
                }
                foreach (string pair in setA) {
                    if (!setB.Contains(translatePair(pair))) {
                        fail(path + "." + name, "the copy lacks " + pair);
                    }
                }
            }
        }

        private static readonly System.Text.RegularExpressions.Regex GuidPattern =
            new System.Text.RegularExpressions.Regex("[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}");

        private string translatePair(string pair) {
            string[] parts = pair.Split('|');
            string x = translate(parts[0]);
            string y = translate(parts[1]);
            return string.CompareOrdinal(x, y) <= 0 ? x + "|" + y : y + "|" + x;
        }

        private string translate(string id) {
            Guid mapped;
            return idForward.TryGetValue(Guid.Parse(id), out mapped) ? mapped.ToString() : id;
        }

        private void compareEvents(EventRepository a, EventRepository b, string path) {
            foreach (EventType type in Enum.GetValues(typeof(EventType))) {
                int countA = count(a, type);
                int countB = count(b, type);
                if (countA != countB) {
                    fail(path, type + " count " + countA + " vs " + countB);
                }
                if (countA == 0) {
                    continue;
                }
                List<Event> la = a.getEvents(type);
                List<Event> lb = b.getEvents(type);
                int tail = Math.Min(Math.Min(la.Count, lb.Count), EventRepository.SavedEventsPerType);
                for (int i = 1; i <= tail; i++) {
                    compare(la[la.Count - i], lb[lb.Count - i], path + "." + type + "[-" + i + "]");
                }
            }
        }

        private static int count(EventRepository repository, EventType type) {
            try {
                return repository.getNumberOfEvents(type);
            } catch (KeyNotFoundException) {
                return 0;
            }
        }

        private static string describe(object o) {
            Entity entity = o as Entity;
            return entity != null ? entity.getType() + " " + entity.getName() : o.GetType().Name;
        }

        /**
        * The kind of fingerprint TestSimulationDeterminism takes: every
        * entity's type, name, position, behaviour, energy and goods, every
        * nation, and the player. A second, independent check beside the full
        * comparison, and the "determinism hash" a run reports.
        */
        public static string fingerprint(Simulation simulation) {
            StringBuilder text = new StringBuilder();
            text.Append("tick ").Append(simulation.getTickCounter().getTotalTicks()).Append('\n');
            foreach (Entity entity in simulation.getEntityRepository().getEntities()) {
                text.Append(entity.getType()).Append(' ').Append(entity.getName()).Append(' ')
                    .Append(entity.getPosition().X.ToString("R")).Append(',')
                    .Append(entity.getPosition().Y.ToString("R")).Append(',')
                    .Append(entity.getPosition().Z.ToString("R"));
                Pawn pawn = entity as Pawn;
                if (pawn != null) {
                    text.Append(' ').Append(pawn.getCurrentBehaviorType()).Append(' ').Append(pawn.getEnergy().ToString("R"));
                }
                text.Append(' ').Append(entity.getInventory().getTotalNumItems()).Append('\n');
            }
            foreach (Nation nation in simulation.getNationRepository().getNations()) {
                text.Append("nation ").Append(nation.getName()).Append(' ').Append(nation.getNumberOfMembers()).Append('\n');
            }
            text.Append("player ").Append(simulation.getPlayer().getEnergy().ToString("R")).Append(' ')
                .Append(simulation.getPlayer().getStatus().getStatus()).Append('\n');
            return text.ToString();
        }

        /** A short hash of the fingerprint, for a log line. */
        public static string hash(Simulation simulation) {
            byte[] bytes = Encoding.UTF8.GetBytes(fingerprint(simulation));
            return Crc32.toHex(Crc32.compute(bytes));
        }
    }
}
