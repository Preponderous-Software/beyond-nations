using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Saves outlive the build that wrote them. Every format version has a
    * committed fixture that must keep loading through SaveMigrations, and the
    * migration chain itself is held to running each step in order.
    */
    public class TestSaveMigrations {

        [Fact]
        public void testEveryFormatVersion_HasACommittedFixture() {
            for (int version = 1; version <= SaveGame.CurrentFormatVersion; version++) {
                Assert.True(File.Exists(SaveFixtures.pathFor(version)),
                    "no fixture for save format " + version + "; run the tests once with BN_WRITE_SAVE_FIXTURE=1 and commit it");
            }
        }

        [Fact]
        public void testTheVersion1Fixture_StillLoadsAndPlaysOn() {
            byte[] file = File.ReadAllBytes(SaveFixtures.pathFor(1));
            SaveHeader header = SaveGame.check(file);
            Assert.Equal(1, header.FormatVersion);
            Assert.Equal(SaveFixtures.FixtureSeed, header.Seed);
            Assert.Equal(SaveFixtures.FixtureTicks, header.Tick);

            Simulation loaded = SaveGame.decode(file);
            Assert.Equal(SaveFixtures.FixtureTicks, loaded.getTickCounter().getTotalTicks());
            Assert.Equal(SaveFixtures.FixtureSeed, loaded.getRandom().getSeed());
            Assert.True(loaded.getEntityRepository().getNumEntitiesOfType(EntityType.PAWN) > 0);
            Assert.NotEmpty(loaded.getNationRepository().getNations());

            // a loaded old save is a live world: it runs, and saves again in today's format
            for (int i = 0; i < 1500; i++) {
                loaded.fixedUpdate(1f / 50f);
            }
            byte[] resaved = SaveGame.encode(loaded, "test");
            Assert.Equal(SaveGame.CurrentFormatVersion, SaveGame.check(resaved).FormatVersion);
            Assert.Null(WorldComparer.difference(loaded, SaveGame.decode(resaved)));
        }

        [Fact]
        public void testTheVersion1Fixture_ThroughTheMigrationChain_IsTheCurrentFormat() {
            byte[] file = File.ReadAllBytes(SaveFixtures.pathFor(1));
            int start = Array.IndexOf(file, (byte) '\n') + 1;
            JsonObject body = (JsonObject) JsonNode.Parse(new ReadOnlySpan<byte>(file, start, file.Length - start));
            JsonObject migrated = SaveMigrations.migrate(body, 1, SaveGame.CurrentFormatVersion);
            Assert.Equal(SaveGame.CurrentFormatVersion, (int) migrated["formatVersion"]);
        }

        [Fact]
        public void testTheChain_RunsEachStepInOrder_AndChecksEachStampsItsVersion() {
            // two hypothetical future steps: 1 -> 2 adds an objectives section, 2 -> 3 renames it
            List<Func<JsonObject, JsonObject>> steps = new List<Func<JsonObject, JsonObject>> {
                body => {
                    body["objectives"] = new JsonObject { ["completed"] = new JsonArray() };
                    body["formatVersion"] = 2;
                    return body;
                },
                body => {
                    JsonNode objectives = body["objectives"];
                    body.Remove("objectives");
                    body["ambitions"] = objectives;
                    body["formatVersion"] = 3;
                    return body;
                },
            };
            JsonObject v1 = new JsonObject { ["formatVersion"] = 1, ["seed"] = 42 };
            JsonObject v3 = SaveMigrations.migrate(v1, 1, 3, steps);
            Assert.Equal(3, (int) v3["formatVersion"]);
            Assert.NotNull(v3["ambitions"]);
            Assert.Null(v3["objectives"]);

            JsonObject fromV2 = SaveMigrations.migrate(new JsonObject { ["formatVersion"] = 2, ["objectives"] = new JsonObject() }, 2, 3, steps);
            Assert.Equal(3, (int) fromV2["formatVersion"]);
        }

        [Fact]
        public void testAStepThatForgetsToStampItsVersion_IsCaught() {
            List<Func<JsonObject, JsonObject>> steps = new List<Func<JsonObject, JsonObject>> { body => body };
            Assert.Throws<SaveFormatException>(() => SaveMigrations.migrate(new JsonObject { ["formatVersion"] = 1 }, 1, 2, steps));
        }

        [Fact]
        public void testAMissingStep_IsAClearError() {
            Assert.Throws<SaveFormatException>(() =>
                SaveMigrations.migrate(new JsonObject { ["formatVersion"] = 1 }, 1, 2, new List<Func<JsonObject, JsonObject>>()));
            Assert.Throws<SaveFormatException>(() => SaveMigrations.migrate(new JsonObject(), 0, 1));
        }

        [Fact]
        public void testRealSteps_CoverEveryVersionBeforeTheCurrentOne() {
            Assert.True(SaveMigrations.getSteps().Count == SaveGame.CurrentFormatVersion - 1,
                "save format " + SaveGame.CurrentFormatVersion + " needs " + (SaveGame.CurrentFormatVersion - 1) + " migration steps; there are " + SaveMigrations.getSteps().Count);
        }
    }
}
