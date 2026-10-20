using System;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * Remembers how the world in play has looked, so the run can be exported
    * as a replay (ReplayUsdaWriter) when the player asks.
    *
    * Every `interval` ticks it takes a small sample: where every visible pawn,
    * settlement and chicken is and the player stands, the colour of each
    * pawn's and settlement's nation, the day, and a few counts. Nothing about
    * the sample feeds back into the world: the recorder only reads, and draws
    * no random numbers, so recording never changes what happens.
    *
    * Memory stays bounded however long the run: when more than `maxSamples`
    * have been kept, every other one is dropped and the interval doubles, so
    * a long run is still covered end to end, just more coarsely.
    *
    * The recorder belongs to the host, not to the save. A loaded world starts
    * a fresh recording at the tick it was saved at; the chronicle, which is
    * saved, is the record of what came before (docs/SHARING.md).
    *
    * Entity ids in a sample are numbers the recorder hands out in the order
    * it first sees each entity (EntityId is a Guid, fresh every run), so the
    * same seed played the same way records the same ids, points and hashes.
    */
    public sealed class RunRecorder {
        public const int DefaultInterval = 250;
        public const int DefaultMaxSamples = 1000;

        // The entity sets every sample holds, in this order.
        public const int Pawns = 0;
        public const int Settlements = 1;
        public const int PlayerSet = 2;
        public const int Chickens = 3;
        public const int SetCount = 4;

        private static readonly byte[] NoNationColour = { 158, 163, 173 };

        /** One kind of entity in one sample: ids, Z-up points and (for nation-coloured sets) sRGB bytes. */
        public sealed class SetSample {
            public readonly int[] ids;
            public readonly Vector3[] points;
            public readonly byte[] colours;

            public SetSample(int[] ids, Vector3[] points, byte[] colours) {
                this.ids = ids;
                this.points = points;
                this.colours = colours;
            }
        }

        public sealed class Sample {
            public readonly int tick;
            public readonly int day;
            public readonly int nations;
            public readonly SetSample[] sets;
            public readonly string stateHash;

            public Sample(int tick, int day, int nations, SetSample[] sets, string stateHash) {
                this.tick = tick;
                this.day = day;
                this.nations = nations;
                this.sets = sets;
                this.stateHash = stateHash;
            }

            public int count(int set) { return sets[set].ids.Length; }
        }

        private readonly int maxSamples;
        private int interval;
        private readonly List<Sample> samples = new List<Sample>();
        private readonly Dictionary<EntityId, int> ids = new Dictionary<EntityId, int>();
        private int nextId = 1;
        private int nextTick;
        private int thinnings;

        public RunRecorder() : this(DefaultInterval, DefaultMaxSamples) {
        }

        public RunRecorder(int interval, int maxSamples) {
            if (interval <= 0) {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "interval must be positive");
            }
            if (maxSamples < 2) {
                throw new ArgumentOutOfRangeException(nameof(maxSamples), maxSamples, "keep at least two samples");
            }
            this.interval = interval;
            this.maxSamples = maxSamples;
        }

        /** Forgets everything and takes the first sample of a world, new or just loaded. */
        public void begin(Simulation simulation) {
            samples.Clear();
            ids.Clear();
            nextId = 1;
            thinnings = 0;
            nextTick = 0;
            recordNow(simulation);
        }

        /** Called after each fixed step: takes a sample when one is due. */
        public void observe(Simulation simulation) {
            if (simulation == null) {
                return;
            }
            if (simulation.getTickCounter().getTotalTicks() >= nextTick) {
                recordNow(simulation);
            }
        }

        /**
        * Takes a sample at the current tick unless one was already taken at
        * it; an export calls this so the replay ends where the player is.
        */
        public void recordNow(Simulation simulation) {
            if (simulation == null) {
                return;
            }
            int tick = simulation.getTickCounter().getTotalTicks();
            if (samples.Count > 0 && tick <= samples[samples.Count - 1].tick) {
                return;
            }
            samples.Add(capture(simulation, tick));
            nextTick = tick + interval;
            if (samples.Count > maxSamples) {
                thin();
            }
        }

        public IReadOnlyList<Sample> getSamples() { return samples; }
        public int getSampleCount() { return samples.Count; }
        public int getInterval() { return interval; }
        /** How many times the samples have been halved to stay within maxSamples. */
        public int getThinnings() { return thinnings; }
        public int getFirstTick() { return samples.Count > 0 ? samples[0].tick : 0; }
        public int getLastTick() { return samples.Count > 0 ? samples[samples.Count - 1].tick : 0; }

        private void thin() {
            List<Sample> kept = new List<Sample>(samples.Count / 2 + 1);
            for (int i = 0; i < samples.Count; i += 2) {
                kept.Add(samples[i]);
            }
            // The newest sample always stays, so the replay still ends now.
            Sample last = samples[samples.Count - 1];
            if (kept[kept.Count - 1] != last) {
                kept.Add(last);
            }
            samples.Clear();
            samples.AddRange(kept);
            interval *= 2;
            nextTick = last.tick + interval;
            thinnings++;
        }

        // --- capture --------------------------------------------------------

        private int idOf(EntityId id) {
            int n;
            if (!ids.TryGetValue(id, out n)) {
                n = nextId++;
                ids[id] = n;
            }
            return n;
        }

        private Sample capture(Simulation simulation, int tick) {
            List<int>[] setIds = new List<int>[SetCount];
            List<Vector3>[] setPoints = new List<Vector3>[SetCount];
            List<byte>[] setColours = new List<byte>[SetCount];
            for (int s = 0; s < SetCount; s++) {
                setIds[s] = new List<int>();
                setPoints[s] = new List<Vector3>();
                setColours[s] = new List<byte>();
            }

            NationRepository nations = simulation.getNationRepository();
            EntityRepository repository = simulation.getEntityRepository();
            int count = repository.getEntityCount();
            for (int i = 0; i < count; i++) {
                Entity entity = repository.getEntityAt(i);
                if (entity == null || entity.isMarkedForDeletion() || !entity.isVisible()) {
                    continue;
                }
                int set;
                NationId nationId = null;
                switch (entity.getType()) {
                    case EntityType.PAWN:
                        set = Pawns;
                        nationId = ((Pawn) entity).getNationId();
                        break;
                    case EntityType.SETTLEMENT:
                        set = Settlements;
                        nationId = ((Settlement) entity).getNationId();
                        break;
                    case EntityType.PLAYER:
                        set = PlayerSet;
                        break;
                    case EntityType.CHICKEN:
                        set = Chickens;
                        break;
                    default:
                        continue;
                }
                setIds[set].Add(idOf(entity.getId()));
                setPoints[set].Add(toReplaySpace(entity.getPosition(), ReplayUsdaWriter.widthOf(set)));
                if (set == Pawns || set == Settlements) {
                    Nation nation = nationId != null ? nations.getNation(nationId) : null;
                    if (nation != null) {
                        Rgba c = nation.getColor();
                        setColours[set].Add(toByte(c.r));
                        setColours[set].Add(toByte(c.g));
                        setColours[set].Add(toByte(c.b));
                    } else {
                        setColours[set].AddRange(NoNationColour);
                    }
                }
            }

            SetSample[] sets = new SetSample[SetCount];
            for (int s = 0; s < SetCount; s++) {
                bool coloured = s == Pawns || s == Settlements;
                sets[s] = new SetSample(setIds[s].ToArray(), setPoints[s].ToArray(), coloured ? setColours[s].ToArray() : null);
            }
            int day = simulation.getTimeOfDay().getDay();
            int nationCount = nations.getNumberOfNations();
            return new Sample(tick, day, nationCount, sets, hashOf(tick, day, nationCount, sets));
        }

        /**
        * Core is Y-up; a usd-replay/1 layer is Z-up. (x, y, z) becomes
        * (x, -z, y), lifted by half the drawn size so a marker sits on the
        * ground rather than in it (as tools/history-to-replay.py does).
        */
        public static Vector3 toReplaySpace(Vector3 position, float drawnWidth) {
            return new Vector3(position.X, -position.Z, position.Y + drawnWidth / 2f);
        }

        private static byte toByte(float channel) {
            float clamped = Math.Max(0f, Math.Min(1f, channel));
            return (byte) Math.Round(clamped * 255f);
        }

        // --- state hash -----------------------------------------------------

        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        /**
        * FNV-1a/64 over the sample as recorded: tick, day, nation count, then
        * per set its size, and per entity its id, position in thousandths and
        * colour bytes. Sixteen lowercase hex digits.
        */
        private static string hashOf(int tick, int day, int nations, SetSample[] sets) {
            ulong h = FnvOffset;
            h = mix(h, tick);
            h = mix(h, day);
            h = mix(h, nations);
            foreach (SetSample set in sets) {
                h = mix(h, set.ids.Length);
                for (int i = 0; i < set.ids.Length; i++) {
                    h = mix(h, set.ids[i]);
                    h = mix(h, (int) Math.Round(set.points[i].X * 1000f));
                    h = mix(h, (int) Math.Round(set.points[i].Y * 1000f));
                    h = mix(h, (int) Math.Round(set.points[i].Z * 1000f));
                }
                if (set.colours != null) {
                    foreach (byte b in set.colours) {
                        h = (h ^ b) * FnvPrime;
                    }
                }
            }
            return h.ToString("x16");
        }

        private static ulong mix(ulong h, int value) {
            unchecked {
                uint v = (uint) value;
                for (int i = 0; i < 4; i++) {
                    h = (h ^ (v & 0xFF)) * FnvPrime;
                    v >>= 8;
                }
            }
            return h;
        }
    }
}
