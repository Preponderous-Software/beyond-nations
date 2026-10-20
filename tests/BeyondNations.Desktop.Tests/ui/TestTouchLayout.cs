using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Input;
using Xunit;
using beyondnations;
using beyondnations.desktop.input;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;
using beyondnationstests.desktop.input;

namespace beyondnationstests.desktop.ui {

    /**
    * The touch layout (docs/WEB.md, "Touch controls"): the setting, the
    * analog stick through the shared input layer, and what the screens say
    * on a touch screen. TouchLayout is static, so the tests that set it run
    * in one collection and put it back.
    */
    [CollectionDefinition("TouchLayout", DisableParallelization = true)]
    public class TouchLayoutCollection {
    }

    [Collection("TouchLayout")]
    public class TestTouchLayout {

        [Fact]
        public void onTheDesktopItIsNeitherAvailableNorActive() {
            TouchLayout.reset();
            Assert.False(TouchLayout.isAvailable());
            Assert.False(TouchLayout.isActive());
            Assert.Equal(0f, TouchLayout.getBottomReserve());
        }

        [Fact]
        public void theHostsReportTurnsItOnWithItsBand() {
            try {
                TouchLayout.setState(TouchMode.Auto, true, 150f);
                Assert.True(TouchLayout.isAvailable());
                Assert.True(TouchLayout.isActive());
                Assert.Equal(150f, TouchLayout.getBottomReserve());
                Assert.Equal("Touch controls: Auto (on)", TouchLayout.describe());

                TouchLayout.setState(TouchMode.Off, false, 150f);
                Assert.False(TouchLayout.isActive());
                Assert.Equal(0f, TouchLayout.getBottomReserve());
                Assert.Equal("Touch controls: Off", TouchLayout.describe());
            } finally {
                TouchLayout.reset();
            }
        }

        [Fact]
        public void theSettingCyclesAndIsReportedOnceWhenThePlayerChangesIt() {
            try {
                TouchLayout.setState(TouchMode.Auto, false, 0f);
                TouchMode changed;
                Assert.False(TouchLayout.consumeModeChange(out changed));

                TouchLayout.setMode(TouchLayout.next(TouchLayout.getMode()));
                Assert.True(TouchLayout.consumeModeChange(out changed));
                Assert.Equal(TouchMode.On, changed);
                Assert.False(TouchLayout.consumeModeChange(out changed));

                Assert.Equal(TouchMode.Off, TouchLayout.next(TouchMode.On));
                Assert.Equal(TouchMode.Auto, TouchLayout.next(TouchMode.Off));
            } finally {
                TouchLayout.reset();
            }
        }

        [Fact]
        public void namesRoundTripAndAnythingElseIsAuto() {
            foreach (TouchMode mode in new[] { TouchMode.Auto, TouchMode.On, TouchMode.Off }) {
                Assert.Equal(mode, TouchLayout.parse(TouchLayout.toName(mode)));
            }
            Assert.Equal(TouchMode.Auto, TouchLayout.parse(null));
            Assert.Equal(TouchMode.Auto, TouchLayout.parse("sideways"));
            Assert.Equal(TouchMode.On, TouchLayout.parse(" ON "));
        }

        [Fact]
        public void theControlsListLeadsWithTheTouchControlsAndKeepsEveryBinding() {
            List<ControlsScreen.Row> keyboard = ControlsScreen.getRows();
            List<ControlsScreen.Row> touch = ControlsScreen.getRows(true);
            Assert.Equal(ControlsScreen.TouchSection, touch[0].Section);
            Assert.DoesNotContain(keyboard, row => row.Section == ControlsScreen.TouchSection);
            Assert.Equal(keyboard.Count, touch.FindAll(row => row.Section != ControlsScreen.TouchSection).Count);
        }

        [Fact]
        public void objectiveHintsNameTheButtonsOnATouchScreen() {
            Assert.Equal("Use", ObjectiveKeyNames.ofTouch(HintKey.Interact));
            Assert.Equal("Bag", ObjectiveKeyNames.ofTouch(HintKey.ToggleInventory));
            Assert.Equal(KeyBindings.Interact.ToString(), ObjectiveKeyNames.of(HintKey.Interact));
        }

        // --- the analog stick through InputService and PlayerInputController ---

        private class StickSource : IInputSource {
            public readonly HashSet<Key> Held = new HashSet<Key>();
            public Vector2 Stick;

            public bool IsKeyDown(Key key) { return Held.Contains(key); }
            public Vector2 GetMouseDelta() { return Vector2.Zero; }
            public Vector2 GetMoveAxis() { return Stick; }
        }

        private static Simulation createSimulation() {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(1234);
            return new Simulation(gameConfig);
        }

        [Fact]
        public void aKeyboardSourceHasNoStick() {
            InputService input = new InputService(new FakeInputSource());
            input.update();
            Assert.Equal(Vector2.Zero, input.getMoveAxis());
        }

        [Fact]
        public void theStickPushedForwardWalksWithoutTurning() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            StickSource source = new StickSource { Stick = new Vector2(0f, 1f) };
            InputService input = new InputService(source);
            PlayerInputController controller = new PlayerInputController();
            float yaw = player.getYaw();
            Vector3 start = player.getPosition();

            for (int i = 0; i < 30; i++) {
                input.update();
                controller.update(simulation, input);
                simulation.fixedUpdate(0.02f);
            }

            Assert.Equal(yaw, player.getYaw());
            Assert.True(Vector3.Distance(start, player.getPosition()) > 0.5f);
        }

        [Fact]
        public void theStickPushedRightTurnsRightByHowFarItIsPushed() {
            Simulation full = createSimulation();
            Simulation half = createSimulation();
            float fullTurn = turnAfterFrames(full, new Vector2(1f, 0f), 10);
            float halfTurn = turnAfterFrames(half, new Vector2(0.5f, 0f), 10);
            // right lowers yaw, as D does (TurnDirectionTests)
            Assert.True(fullTurn < 0f);
            Assert.Equal(fullTurn / 2f, halfTurn, 3);
        }

        [Fact]
        public void aHeldKeyWinsOverTheStick() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            StickSource source = new StickSource { Stick = new Vector2(1f, 0f) };
            source.Held.Add(KeyBindings.MoveLeft);
            float yaw = player.getYaw();
            InputService input = new InputService(source);
            PlayerInputController controller = new PlayerInputController();
            input.update();
            controller.update(simulation, input);
            simulation.fixedUpdate(0.02f);
            Assert.True(player.getYaw() > yaw);
        }

        private static float turnAfterFrames(Simulation simulation, Vector2 stick, int frames) {
            Player player = simulation.getPlayer();
            StickSource source = new StickSource { Stick = stick };
            InputService input = new InputService(source);
            PlayerInputController controller = new PlayerInputController();
            float yaw = player.getYaw();
            for (int i = 0; i < frames; i++) {
                input.update();
                controller.update(simulation, input);
                player.fixedUpdate();
            }
            return player.getYaw() - yaw;
        }
    }
}
