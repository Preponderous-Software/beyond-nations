using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

using beyondnations;
using beyondnations.historyexport;

namespace beyondnations.historyexport.tests {

    /**
    * The history exporter's promise is that a seed is a recording: the same
    * seed and tick count give byte-identical JSON Lines on every run, so a
    * film made from it can be remade. These tests hold it to that and to the
    * format the USD converter (tools/history-to-usd.py) reads.
    */
    public class TestHistoryExporter {
        // Seed 42 founds its first nation at tick 2000 and that nation's
        // first settlement at tick 2050.
        private const int SeedWithPawns = 42;
        // Most chunks have pawns since pawns spawn more densely; this seed's opening chunks have none
        // (78 until regions (RegionMap) changed how many trees and rocks each chunk rolls).
        private const int SeedWithoutPawns = 15;
        private const int TicksPastFirstSettlement = 2400;

        private static string export(int seed, int ticks, int every = 50) {
            ExportOptions options = new ExportOptions();
            options.Seed = seed;
            options.Ticks = ticks;
            options.Every = every;
            return HistoryExporter.exportToString(options);
        }

        private static List<JsonElement> records(string jsonl) {
            List<JsonElement> list = new List<JsonElement>();
            foreach (string line in jsonl.Split('\n')) {
                if (line.Length > 0) {
                    list.Add(JsonDocument.Parse(line).RootElement.Clone());
                }
            }
            return list;
        }

        private static IEnumerable<JsonElement> ofType(List<JsonElement> all, string t) {
            return all.Where(r => r.GetProperty("t").GetString() == t);
        }

        [Fact]
        public void testSameSeed_ExportsTwice_OutputIsByteIdentical() {
            string first = export(SeedWithPawns, TicksPastFirstSettlement);
            string second = export(SeedWithPawns, TicksPastFirstSettlement);

            Assert.True(first.Length > 0);
            Assert.Equal(first, second);
        }

        [Fact]
        public void testDifferentSeeds_Export_OutputsDiffer() {
            Assert.NotEqual(export(SeedWithPawns, 200), export(SeedWithoutPawns, 200));
        }

        [Fact]
        public void testExport_EveryLine_IsJsonWithARecordType_HeaderFirst() {
            List<JsonElement> all = records(export(SeedWithPawns, 300));

            JsonElement header = all[0];
            Assert.Equal("header", header.GetProperty("t").GetString());
            Assert.Equal(HistoryExporter.Format, header.GetProperty("format").GetString());
            Assert.Equal(SeedWithPawns, header.GetProperty("seed").GetInt32());
            Assert.Single(ofType(all, "header"));
            Assert.All(all, r => Assert.Contains(r.GetProperty("t").GetString(), new[] { "header", "chunk", "event", "frame" }));
        }

        [Fact]
        public void testFrames_WrittenEveryNTicksAndOnTheLastTick() {
            List<JsonElement> all = records(export(SeedWithPawns, 130, 50));

            int[] ticks = ofType(all, "frame").Select(f => f.GetProperty("tick").GetInt32()).ToArray();

            Assert.Equal(new[] { 50, 100, 130 }, ticks);
            // Every frame carries Core's day (TimeOfDay); 130 ticks is still the first.
            Assert.All(ofType(all, "frame"), f => Assert.Equal(0, f.GetProperty("day").GetInt32()));
        }

        [Fact]
        public void testEntities_PositionAndAppearanceOnlyWhenNewOrChanged() {
            List<JsonElement> frames = ofType(records(export(SeedWithPawns, 200, 50)), "frame").ToList();

            // Every entity's first frame carries its position and its parts.
            HashSet<int> seen = new HashSet<int>();
            int repeatsWithoutPosition = 0;
            foreach (JsonElement frame in frames) {
                foreach (JsonElement entity in frame.GetProperty("entities").EnumerateArray()) {
                    int id = entity.GetProperty("id").GetInt32();
                    if (seen.Add(id)) {
                        Assert.True(entity.TryGetProperty("pos", out _), "first frame of " + id + " has no pos");
                        Assert.True(entity.TryGetProperty("parts", out _), "first frame of " + id + " has no parts");
                    } else if (!entity.TryGetProperty("pos", out _)) {
                        repeatsWithoutPosition++;
                    }
                }
            }
            // Trees and rocks stand still, so later frames leave their position out.
            Assert.True(repeatsWithoutPosition > 0);
        }

        [Fact]
        public void testSeedWithPawns_FoundsANationAndItsSettlement_FramesAgree() {
            List<JsonElement> all = records(export(SeedWithPawns, TicksPastFirstSettlement));

            JsonElement founded = ofType(all, "event").First(e => e.GetProperty("kind").GetString() == "nation_founded");
            JsonElement settled = ofType(all, "event").First(e => e.GetProperty("kind").GetString() == "settlement_founded");
            int nation = founded.GetProperty("nation").GetInt32();
            int leader = founded.GetProperty("leader").GetInt32();
            int settlement = settled.GetProperty("settlement").GetInt32();
            Assert.Equal(nation, settled.GetProperty("nation").GetInt32());
            Assert.True(settled.GetProperty("tick").GetInt32() >= founded.GetProperty("tick").GetInt32());

            JsonElement last = ofType(all, "frame").Last();
            JsonElement nationRecord = last.GetProperty("nations").EnumerateArray().Single(n => n.GetProperty("id").GetInt32() == nation);
            Assert.Equal(leader, nationRecord.GetProperty("leader").GetInt32());
            Assert.Contains(settlement, nationRecord.GetProperty("settlements").EnumerateArray().Select(s => s.GetInt32()));

            JsonElement settlementEntity = last.GetProperty("entities").EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == settlement);
            Assert.Equal("settlement", settlementEntity.GetProperty("kind").GetString());
            Assert.Equal(nation, settlementEntity.GetProperty("nation").GetInt32());

            JsonElement leaderEntity = last.GetProperty("entities").EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == leader);
            Assert.Equal("pawn", leaderEntity.GetProperty("kind").GetString());
            Assert.Equal(nation, leaderEntity.GetProperty("nation").GetInt32());
        }

        [Fact]
        public void testSeedWithoutPawns_ExportsNoPawnsAndNoNations() {
            List<JsonElement> all = records(export(SeedWithoutPawns, 300));

            Assert.DoesNotContain(ofType(all, "frame").SelectMany(f => f.GetProperty("entities").EnumerateArray()),
                e => e.GetProperty("kind").GetString() == "pawn");
            Assert.Empty(ofType(all, "event"));
            Assert.NotEmpty(ofType(all, "chunk"));
        }

        [Fact]
        public void testKindsFilter_KeepsOnlyThoseKinds() {
            ExportOptions options = new ExportOptions();
            options.Seed = SeedWithPawns;
            options.Ticks = 100;
            options.Kinds = new HashSet<EntityType> { EntityType.PAWN };

            List<JsonElement> frames = ofType(records(HistoryExporter.exportToString(options)), "frame").ToList();

            Assert.NotEmpty(frames.SelectMany(f => f.GetProperty("entities").EnumerateArray()));
            Assert.All(frames.SelectMany(f => f.GetProperty("entities").EnumerateArray()),
                e => Assert.Equal("pawn", e.GetProperty("kind").GetString()));
        }

        [Fact]
        public void testTicksPerSecondOption_ReachesTheSimulation() {
            ExportOptions options = new ExportOptions();
            options.Seed = SeedWithoutPawns;
            options.Ticks = 1;
            options.TicksPerSecond = 20;
            HistoryExporter exporter = new HistoryExporter(options, new System.IO.MemoryStream());

            Assert.Equal(20, exporter.getSimulation().getTickCounter().getTicksPerSecond());
        }
    }
}
