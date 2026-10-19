using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Slots on disk: atomic writes, the previous save kept as a backup, a
    * damaged save falling back to its backup, and nothing ever overwritten
    * by a load or by a save that does not check out.
    */
    public class TestSaveSlots : IDisposable {
        private readonly string directory;
        private readonly SaveSlots slots;

        public TestSaveSlots() {
            directory = Path.Combine(Path.GetTempPath(), "bn-save-tests-" + Guid.NewGuid().ToString("N"));
            slots = new SaveSlots(new FileSaveStorage(directory));
        }

        public void Dispose() {
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, true);
            }
        }

        private static Simulation world(int seed, int ticks) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            Simulation simulation = new Simulation(config);
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            return simulation;
        }

        private string pathOf(string name) {
            return Path.Combine(directory, name);
        }

        [Fact]
        public void testWrite_ThenLoad_GivesTheWorldBack() {
            Simulation simulation = world(5, 300);
            slots.write("world-a", SaveGame.encode(simulation, "test"));
            SaveLoadResult result = slots.load("world-a");
            Assert.False(result.FromBackup);
            Assert.Null(WorldComparer.difference(simulation, result.Simulation));
            Assert.False(File.Exists(pathOf("world-a.bnsave.tmp")));
        }

        [Fact]
        public void testASecondSave_KeepsTheFirstAsTheBackup() {
            Simulation simulation = world(5, 300);
            byte[] first = SaveGame.encode(simulation, "test");
            slots.write("world-a", first);
            simulation.fixedUpdate(1f / 50f);
            byte[] second = SaveGame.encode(simulation, "test");
            slots.write("world-a", second);
            Assert.Equal(second, File.ReadAllBytes(pathOf("world-a.bnsave")));
            Assert.Equal(first, File.ReadAllBytes(pathOf("world-a.bnsave.bak")));
        }

        [Fact]
        public void testADamagedSave_LoadsItsBackup_AndSaysSo() {
            Simulation simulation = world(5, 300);
            slots.write("world-a", SaveGame.encode(simulation, "test"));
            slots.write("world-a", SaveGame.encode(world(5, 301), "test"));
            byte[] damaged = File.ReadAllBytes(pathOf("world-a.bnsave"));
            damaged[damaged.Length - 10] ^= 0x10;
            File.WriteAllBytes(pathOf("world-a.bnsave"), damaged);

            SaveLoadResult result = slots.load("world-a");
            Assert.True(result.FromBackup);
            Assert.Contains("checksum", result.Problem);
            Assert.Equal(300, result.Simulation.getTickCounter().getTotalTicks());
            // loading wrote nothing
            Assert.Equal(damaged, File.ReadAllBytes(pathOf("world-a.bnsave")));
        }

        [Fact]
        public void testADamagedSave_NeverPushesOutAGoodBackup() {
            byte[] good = SaveGame.encode(world(5, 300), "test");
            slots.write("world-a", good);
            slots.write("world-a", SaveGame.encode(world(5, 301), "test"));
            byte[] backupBefore = File.ReadAllBytes(pathOf("world-a.bnsave.bak"));
            File.WriteAllBytes(pathOf("world-a.bnsave"), Encoding.UTF8.GetBytes("garbage"));

            slots.write("world-a", SaveGame.encode(world(5, 302), "test"));
            Assert.Equal(backupBefore, File.ReadAllBytes(pathOf("world-a.bnsave.bak")));
            Assert.Equal(302, slots.load("world-a").Simulation.getTickCounter().getTotalTicks());
        }

        [Fact]
        public void testASaveThatDoesNotCheckOut_IsNeverWritten() {
            byte[] good = SaveGame.encode(world(5, 300), "test");
            slots.write("world-a", good);
            byte[] bad = (byte[]) good.Clone();
            bad[bad.Length - 5] ^= 0x01;
            Assert.Throws<SaveFormatException>(() => slots.write("world-a", bad));
            Assert.Equal(good, File.ReadAllBytes(pathOf("world-a.bnsave")));
        }

        [Fact]
        public void testASlotHoldingANewerVersionsSave_IsLeftAlone() {
            byte[] good = SaveGame.encode(world(5, 300), "test");
            string newer = Encoding.UTF8.GetString(good).Replace("\"formatVersion\":" + SaveGame.CurrentFormatVersion + ",",
                "\"formatVersion\":" + (SaveGame.CurrentFormatVersion + 1) + ",");
            Directory.CreateDirectory(directory);
            File.WriteAllText(pathOf("world-a.bnsave"), newer);

            SaveFormatException refused = Assert.Throws<SaveFormatException>(() => slots.load("world-a"));
            Assert.Contains("newer version", refused.Message);
            Assert.Throws<SaveFormatException>(() => slots.write("world-a", good));
            Assert.Equal(newer, File.ReadAllText(pathOf("world-a.bnsave")));
            SaveSlotInfo info = slots.list()[0];
            Assert.False(info.isLoadable());
            Assert.Contains("newer version", info.Problem);
        }

        [Fact]
        public void testList_NewestFirst_WithDayAndNation_UnreadableLast() {
            slots.write("world-old", SaveGame.encode(world(1, 100), "test", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            slots.write("world-new", SaveGame.encode(world(2, 100), "test", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)));
            Directory.CreateDirectory(directory);
            File.WriteAllText(pathOf("world-broken.bnsave"), "nonsense");
            File.WriteAllText(pathOf("notes.txt"), "not a save");

            List<SaveSlotInfo> list = slots.list();
            Assert.Equal(new[] { "world-new", "world-old", "world-broken" }, list.ConvertAll(i => i.Slot).ToArray());
            Assert.Equal("Day 1 - no nation", list[0].Header.describe());
            Assert.False(list[2].isLoadable());
            Assert.Equal("world-new", slots.latest().Slot);
        }

        [Fact]
        public void testNoSaves_NoLatest() {
            Assert.Empty(slots.list());
            Assert.Null(slots.latest());
            Assert.Throws<SaveFormatException>(() => slots.load("world-a"));
        }

        [Fact]
        public void testSlotNames_AreSafeFileNames() {
            Assert.True(SaveSlots.isValidSlotName(SaveSlots.newSlotName(-42, new DateTime(2026, 10, 18, 14, 22, 33, DateTimeKind.Utc))));
            Assert.Equal("world-m42-20261018-142233", SaveSlots.newSlotName(-42, new DateTime(2026, 10, 18, 14, 22, 33, DateTimeKind.Utc)));
            Assert.False(SaveSlots.isValidSlotName("../etc/passwd"));
            Assert.False(SaveSlots.isValidSlotName(""));
            Assert.Throws<ArgumentException>(() => slots.load("../x"));
        }
    }
}
