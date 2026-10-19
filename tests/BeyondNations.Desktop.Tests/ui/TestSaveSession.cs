using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests {

    /** Saves held in memory, as localStorage holds them in the browser. */
    public class MemorySaveStorage : ISaveStorage {
        public readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
        public string FailWritesWith;

        public IReadOnlyList<string> listNames() {
            return new List<string>(Files.Keys);
        }

        public byte[] read(string name) {
            byte[] data;
            return Files.TryGetValue(name, out data) ? (byte[]) data.Clone() : null;
        }

        public void writeAtomically(string name, byte[] data) {
            if (FailWritesWith != null) {
                throw new IOException(FailWritesWith);
            }
            Files[name] = (byte[]) data.Clone();
        }

        public void remove(string name) {
            Files.Remove(name);
        }
    }

    /**
    * When the host saves, and what it tells the player (docs/SAVES.md).
    */
    public class TestSaveSession {
        private static Simulation world(int seed) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            return new Simulation(config);
        }

        private static void run(Simulation simulation, SaveSession saves, int ticks) {
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(1f / 50f);
                saves.onFixedStep(simulation);
            }
        }

        [Fact]
        public void testANewWorld_IsNotSavedUntilAsked_ThenAutosavesOnTheClock() {
            MemorySaveStorage storage = new MemorySaveStorage();
            SaveSession saves = new SaveSession(storage, "test", true);
            Simulation simulation = world(4);
            saves.beginNewWorld(simulation, new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc));
            Assert.Equal("world-4-20261018-120000", saves.getCurrentSlot());
            Assert.Empty(storage.Files);

            run(simulation, saves, SaveSession.AutosaveTicks - 1);
            Assert.Empty(storage.Files);
            run(simulation, saves, 1);
            Assert.True(storage.Files.ContainsKey("world-4-20261018-120000.bnsave"));
            Assert.StartsWith("Autosaved: Day 1", saves.getFeedback());
            Assert.Equal("Autosaved.", simulation.getPlayer().getStatus().getStatus());
        }

        [Fact]
        public void testSave_ThenContinue_GivesTheSameWorld() {
            MemorySaveStorage storage = new MemorySaveStorage();
            SaveSession saves = new SaveSession(storage, "test", true);
            Simulation simulation = world(4);
            saves.beginNewWorld(simulation, DateTime.UtcNow);
            run(simulation, saves, 500);
            Assert.True(saves.save(simulation, false));
            Assert.StartsWith("Saved: ", saves.getFeedback());
            Assert.False(saves.isFeedbackError());

            SaveSession nextRun = new SaveSession(storage, "test", true);
            SaveSlotInfo latest = nextRun.latest();
            Assert.NotNull(latest);
            Simulation loaded = nextRun.load(latest.Slot);
            Assert.NotNull(loaded);
            Assert.Equal(500, loaded.getTickCounter().getTotalTicks());
            Assert.StartsWith("Welcome back.", loaded.getPlayer().getStatus().getStatus());
            Assert.Equal(saves.getCurrentSlot(), nextRun.getCurrentSlot());
        }

        [Fact]
        public void testAFailedSave_IsReportedNotThrown() {
            MemorySaveStorage storage = new MemorySaveStorage { FailWritesWith = "the browser's storage for this site is full" };
            SaveSession saves = new SaveSession(storage, "test", true);
            Simulation simulation = world(4);
            saves.beginNewWorld(simulation, DateTime.UtcNow);
            Assert.False(saves.save(simulation, false));
            Assert.True(saves.isFeedbackError());
            Assert.Contains("Could not save", saves.getFeedback());
            Assert.Contains("Delete an old world", saves.getFeedback());
            Assert.True(StatusTone.isFailure(saves.getFeedback()));
        }

        [Fact]
        public void testAFailedLoad_ReturnsNullWithTheReason() {
            MemorySaveStorage storage = new MemorySaveStorage();
            storage.Files["world-1-20261018-120000.bnsave"] = System.Text.Encoding.UTF8.GetBytes("not a save");
            SaveSession saves = new SaveSession(storage, "test", true);
            Assert.Null(saves.latest());
            Assert.Null(saves.load("world-1-20261018-120000"));
            Assert.True(saves.isFeedbackError());
            Assert.Contains("not a Beyond Nations save", saves.getFeedback());
            Assert.Equal("Damaged, and no backup to fall back on", LoadScreen.shortReason(saves.list()[0]));
        }

        [Fact]
        public void testDisabled_NeverWritesOrLists() {
            MemorySaveStorage storage = new MemorySaveStorage();
            SaveSession enabled = new SaveSession(storage, "test", true);
            Simulation simulation = world(4);
            enabled.beginNewWorld(simulation, DateTime.UtcNow);
            enabled.save(simulation, false);

            SaveSession disabled = new SaveSession(storage, "test", false);
            Assert.Null(disabled.latest());
            disabled.beginNewWorld(simulation, DateTime.UtcNow.AddMinutes(1));
            int files = storage.Files.Count;
            run(simulation, disabled, SaveSession.AutosaveTicks + 1);
            Assert.False(disabled.save(simulation, false));
            Assert.Equal(files, storage.Files.Count);
        }

        [Fact]
        public void testDelete_RemovesTheSaveAndItsBackup() {
            MemorySaveStorage storage = new MemorySaveStorage();
            SaveSession saves = new SaveSession(storage, "test", true);
            Simulation simulation = world(4);
            saves.beginNewWorld(simulation, DateTime.UtcNow);
            saves.save(simulation, false);
            simulation.fixedUpdate(1f / 50f);
            saves.save(simulation, false);
            Assert.Equal(2, storage.Files.Count);
            saves.delete(saves.getCurrentSlot());
            Assert.Empty(storage.Files);
            Assert.Empty(saves.list());
        }

        [Fact]
        public void testDescribe_NamesDayNationAndTime() {
            MemorySaveStorage storage = new MemorySaveStorage();
            SaveSession saves = new SaveSession(storage, "test", true);
            Simulation simulation = world(4);
            saves.beginNewWorld(simulation, DateTime.UtcNow);
            saves.save(simulation, false);
            string line = SaveSession.describe(saves.latest());
            Assert.StartsWith("Day 1 - no nation   ", line);
        }
    }
}
