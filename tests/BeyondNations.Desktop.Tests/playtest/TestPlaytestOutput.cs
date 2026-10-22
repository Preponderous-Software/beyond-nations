using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Xunit;

using beyondnations;
using beyondnations.desktop;
using beyondnations.desktop.playtest;

namespace beyondnationstests.desktop.playtest {

    /**
    * --playtest-out (docs/PLAYTESTING.md): the bearing maths a harness steers
    * by, the shape of state.json and events.jsonl, and that nothing at all is
    * written without the flag. None of it needs a window.
    */
    public class TestPlaytestOutput {

        private static Simulation newSimulation() {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(42);
            Simulation simulation = new Simulation(gameConfig);
            // the first step generates the land around the player, trees and all
            simulation.fixedUpdate(0.02f);
            return simulation;
        }

        private static string newTemporaryDirectory() {
            string path = Path.Combine(Path.GetTempPath(), "bn-playtest-test-" + Guid.NewGuid().ToString("N"));
            return path;
        }

        // --- bearings ---

        [Fact]
        public void testBearing_StraightAheadIsZero_BehindIs180() {
            // yaw 0 faces +z (Player.getForward)
            Assert.Equal(0f, PlaytestBearing.relativeDegrees(Vector3.Zero, 0f, new Vector3(0, 0, 10)), 3);
            Assert.Equal(180f, PlaytestBearing.relativeDegrees(Vector3.Zero, 0f, new Vector3(0, 0, -10)), 3);
        }

        [Fact]
        public void testBearing_PositiveIsRight_TheWayARisingYawDoesNotTurn() {
            // a rising yaw turns left, so at yaw 0 the left is +x and the right -x
            Assert.Equal(90f, PlaytestBearing.relativeDegrees(Vector3.Zero, 0f, new Vector3(-10, 0, 0)), 3);
            Assert.Equal(-90f, PlaytestBearing.relativeDegrees(Vector3.Zero, 0f, new Vector3(10, 0, 0)), 3);

            // turning left by the bearing of something on the left brings it dead ahead
            Vector3 target = new Vector3(10, 0, 0);
            float bearing = PlaytestBearing.relativeDegrees(Vector3.Zero, 0f, target);
            float turnedYaw = -bearing * MathF.PI / 180f;
            Assert.Equal(0f, PlaytestBearing.relativeDegrees(Vector3.Zero, turnedYaw, target), 3);
        }

        [Fact]
        public void testBearing_IsMeasuredFromThePlayer_AndIgnoresHeight() {
            Vector3 from = new Vector3(100, 5, 100);
            Assert.Equal(0f, PlaytestBearing.relativeDegrees(from, 0f, new Vector3(100, 50, 120)), 3);
            Assert.Equal(0f, PlaytestBearing.relativeDegrees(from, 0f, from));
        }

        [Fact]
        public void testNormalise_WrapsIntoMinus180To180() {
            Assert.Equal(-170f, PlaytestBearing.normalise(190f), 3);
            Assert.Equal(170f, PlaytestBearing.normalise(-190f), 3);
            Assert.Equal(180f, PlaytestBearing.normalise(-180f), 3);
            Assert.Equal(10f, PlaytestBearing.normalise(730f), 3);
        }

        [Fact]
        public void testHeading_IsAlwaysFrom0To360() {
            Assert.Equal(270f, PlaytestBearing.headingDegrees(-MathF.PI / 2f), 3);
            Assert.Equal(90f, PlaytestBearing.headingDegrees(MathF.PI / 2f + 4f * MathF.PI), 2);
        }

        // --- state.json ---

        [Fact]
        public void testState_DescribesTheWorldAndTheNearestOfEachKind() {
            Simulation simulation = newSimulation();
            PlaytestView view = new PlaytestView { Screen = ScreenType.WORLD, FramesPerSecond = 30 };
            view.OpenPanels.Add("tip");

            PlaytestState state = PlaytestState.capture(simulation, view, 1234);
            JsonElement root = JsonDocument.Parse(state.toJson()).RootElement;

            Assert.Equal(1234, root.GetProperty("wallTimeMs").GetInt64());
            Assert.Equal("WORLD", root.GetProperty("screen").GetString());
            Assert.Equal(30, root.GetProperty("fps").GetInt32());
            Assert.Equal(42, root.GetProperty("seed").GetInt32());
            Assert.Equal("Camp", root.GetProperty("era").GetString());
            Assert.Equal("tip", root.GetProperty("openPanels")[0].GetString());
            Assert.Equal("Gather wood", root.GetProperty("objective").GetProperty("title").GetString());
            Assert.Equal(1, root.GetProperty("objective").GetProperty("number").GetInt32());
            JsonElement player = root.GetProperty("player");
            Assert.True(player.GetProperty("alive").GetBoolean());
            Assert.Equal(JsonValueKind.Null, player.GetProperty("nation").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("worldEvent").ValueKind);

            JsonElement nearest = root.GetProperty("nearest");
            foreach (string kind in PlaytestState.NearestKinds) {
                JsonElement list = nearest.GetProperty(kind);
                Assert.True(list.GetArrayLength() <= PlaytestState.NearestPerKind);
                double previous = -1;
                foreach (JsonElement target in list.EnumerateArray()) {
                    double distance = target.GetProperty("distance").GetDouble();
                    double bearing = target.GetProperty("bearing").GetDouble();
                    Assert.True(distance >= previous, kind + " is listed nearest first");
                    Assert.InRange(bearing, -180.0, 180.0);
                    previous = distance;
                }
            }
            // a new world has trees about
            Assert.Equal(PlaytestState.NearestPerKind, nearest.GetProperty("tree").GetArrayLength());
        }

        [Fact]
        public void testState_TheNearestTreeIsTheOneInteractWouldReach() {
            Simulation simulation = newSimulation();
            Player player = simulation.getPlayer();
            AppleTree tree = simulation.getEnvironment().getNearestTree(player.getPosition());

            PlaytestState state = PlaytestState.capture(simulation, new PlaytestView { Screen = ScreenType.WORLD }, 0);

            PlaytestTarget first = state.Nearest["tree"][0];
            Assert.Equal(PlaytestState.shortId(tree.getId()), first.Id);
            Assert.Equal(Vector3.Distance(player.getPosition(), tree.getPosition()), first.Distance, 3);
        }

        [Fact]
        public void testState_OffTheWorld_HasOnlyTheScreen() {
            PlaytestState state = PlaytestState.capture(null, new PlaytestView { Screen = ScreenType.TITLE }, 5);
            JsonElement root = JsonDocument.Parse(state.toJson()).RootElement;

            Assert.Equal("TITLE", root.GetProperty("screen").GetString());
            Assert.False(root.GetProperty("world").GetBoolean());
            Assert.False(root.TryGetProperty("player", out _));
        }

        [Fact]
        public void testState_ListsInventoryAndAPendingWorldEventsChoices() {
            Simulation simulation = newSimulation();
            simulation.getPlayer().getInventory().addItem(ItemType.WOOD, 7);
            simulation.getWorldEvents().startPlayerEventNow(simulation, WanderersEvent.Id);

            JsonElement root = JsonDocument.Parse(
                PlaytestState.capture(simulation, new PlaytestView { Screen = ScreenType.WORLD }, 0).toJson()).RootElement;

            Assert.Equal(7, root.GetProperty("player").GetProperty("inventory").GetProperty("wood").GetInt32());
            JsonElement worldEvent = root.GetProperty("worldEvent");
            Assert.False(string.IsNullOrEmpty(worldEvent.GetProperty("title").GetString()));
            Assert.True(worldEvent.GetProperty("options").GetArrayLength() > 0);
        }

        // --- events.jsonl and the recorder ---

        [Fact]
        public void testFormatEvent_IsOneJsonObjectWithTypeTickAndTime() {
            string line = PlaytestRecorder.formatEvent("status", 50, 99, new Dictionary<string, object> {
                { "text", "He said \"hi\"" }, { "options", new List<string> { "a", "b" } }, { "none", null } });

            Assert.DoesNotContain("\n", line);
            JsonElement root = JsonDocument.Parse(line).RootElement;
            Assert.Equal("status", root.GetProperty("type").GetString());
            Assert.Equal(50, root.GetProperty("tick").GetInt32());
            Assert.Equal(99, root.GetProperty("wallTimeMs").GetInt64());
            Assert.Equal("He said \"hi\"", root.GetProperty("text").GetString());
            Assert.Equal(2, root.GetProperty("options").GetArrayLength());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("none").ValueKind);
        }

        [Fact]
        public void testRecorder_WritesStateAndAppendsWhatChanged() {
            string dir = newTemporaryDirectory();
            try {
                long now = 1000;
                PlaytestRecorder recorder = new PlaytestRecorder(dir, () => now, 500);
                Simulation simulation = newSimulation();
                PlaytestView view = new PlaytestView { Screen = ScreenType.WORLD };

                recorder.worldStarted(simulation, "created", "slot-1");
                recorder.update(simulation, view, 0);

                // an objective completes, the status line changes, a save lands
                simulation.getPlayer().getInventory().addItem(ItemType.WOOD, 50);
                Assert.True(simulation.getObjectiveTracker().evaluate(simulation));
                now += 100;
                recorder.update(simulation, view, 1);
                view.Screen = ScreenType.PAUSE;
                now += 100;
                recorder.update(simulation, view, 1);

                List<JsonElement> events = new List<JsonElement>();
                foreach (string line in File.ReadAllLines(Path.Combine(dir, PlaytestRecorder.EventsFileName))) {
                    events.Add(JsonDocument.Parse(line).RootElement);
                }
                List<string> types = events.ConvertAll(e => e.GetProperty("type").GetString());
                Assert.Equal("world", types[0]);
                Assert.Equal("slot-1", events[0].GetProperty("slot").GetString());
                Assert.Contains("screen", types);
                Assert.Contains("save", types);
                JsonElement objective = events.Find(e => e.GetProperty("type").GetString() == "objective_completed");
                Assert.Equal("Gather wood", objective.GetProperty("title").GetString());
                Assert.Equal(1, objective.GetProperty("number").GetInt32());
                JsonElement status = events.Find(e => e.GetProperty("type").GetString() == "status");
                Assert.StartsWith("Objective complete: Gather wood", status.GetProperty("text").GetString());
                Assert.Equal("PAUSE", events[events.Count - 1].GetProperty("to").GetString());

                // state.json is whole JSON, and no temporary file is left behind
                JsonElement state = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, PlaytestRecorder.StateFileName))).RootElement;
                Assert.Equal("WORLD", state.GetProperty("screen").GetString());
                Assert.False(File.Exists(Path.Combine(dir, PlaytestRecorder.StateFileName + ".tmp")));
            } finally {
                if (Directory.Exists(dir)) {
                    Directory.Delete(dir, true);
                }
            }
        }

        [Fact]
        public void testRecorder_RewritesStateOnlyOnceAnInterval() {
            string dir = newTemporaryDirectory();
            try {
                long now = 0;
                PlaytestRecorder recorder = new PlaytestRecorder(dir, () => now, 500);
                string statePath = Path.Combine(dir, PlaytestRecorder.StateFileName);

                recorder.update(null, new PlaytestView { Screen = ScreenType.TITLE }, 0);
                Assert.Equal(0, JsonDocument.Parse(File.ReadAllText(statePath)).RootElement.GetProperty("wallTimeMs").GetInt64());

                now = 400;
                recorder.update(null, new PlaytestView { Screen = ScreenType.MAIN_MENU }, 0);
                Assert.Equal(0, JsonDocument.Parse(File.ReadAllText(statePath)).RootElement.GetProperty("wallTimeMs").GetInt64());

                now = 500;
                recorder.update(null, new PlaytestView { Screen = ScreenType.MAIN_MENU }, 0);
                Assert.Equal("MAIN_MENU", JsonDocument.Parse(File.ReadAllText(statePath)).RootElement.GetProperty("screen").GetString());
            } finally {
                if (Directory.Exists(dir)) {
                    Directory.Delete(dir, true);
                }
            }
        }

        // --- without the flag ---

        [Fact]
        public void testWithoutTheFlag_ThereIsNoRecorderAndNothingIsWritten() {
            GameOptions options = GameOptions.parse(new string[] { });
            Assert.Null(options.PlaytestOut);
            Assert.Null(PlaytestRecorder.create(options.PlaytestOut));
            Assert.Null(PlaytestRecorder.create(""));
        }

        [Fact]
        public void testTheFlag_NamesTheDirectory_AndIsInTheUsageText() {
            GameOptions options = GameOptions.parse(new string[] { "--playtest-out", "/tmp/somewhere" });
            Assert.Equal("/tmp/somewhere", options.PlaytestOut);
            Assert.Contains("--playtest-out DIR", GameOptions.getUsageText());
        }
    }
}
