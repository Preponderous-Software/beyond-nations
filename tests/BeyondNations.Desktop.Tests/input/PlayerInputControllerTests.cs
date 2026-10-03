using Silk.NET.Input;
using Xunit;
using beyondnations;
using beyondnations.desktop.input;

namespace beyondnationstests.desktop.input {

    /**
    * Drives PlayerInputController against a real Simulation with a
    * FakeInputSource, so these tests exercise the same command wiring the
    * desktop host does every frame, just without a window. Held bindings are
    * asserted to keep applying every frame they are down; edge-triggered
    * bindings are asserted to fire exactly once per press even when the key
    * is held across several update() calls, which is the "held vs
    * edge-triggered must behave distinctly" acceptance criterion.
    */
    public class PlayerInputControllerTests {

        private static Simulation createSimulation() {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(1234);
            return new Simulation(gameConfig);
        }

        [Fact]
        public void turning_appliesEveryFrame_whileKeyIsHeld_andStops_onRelease() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            source.press(Key.D);
            inputService.update();
            controller.update(simulation, inputService);
            player.fixedUpdate();
            float yawAfterFirstStep = player.getYaw();
            Assert.NotEqual(0f, yawAfterFirstStep);

            inputService.update();
            controller.update(simulation, inputService);
            player.fixedUpdate();
            float yawAfterSecondStep = player.getYaw();
            Assert.True(yawAfterSecondStep > yawAfterFirstStep, "held turning should keep advancing yaw every frame it stays down");

            source.release(Key.D);
            inputService.update();
            controller.update(simulation, inputService);
            player.fixedUpdate();
            Assert.Equal(yawAfterSecondStep, player.getYaw());
        }

        [Fact]
        public void arrowKeyAlternates_alsoTurnThePlayer() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            source.press(Key.Left);
            inputService.update();
            controller.update(simulation, inputService);
            player.fixedUpdate();

            Assert.NotEqual(0f, player.getYaw());
        }

        [Fact]
        public void sprint_setsRunSpeed_whileHeld_andWalkSpeed_onRelease() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            Assert.Equal(simulation.getGameConfig().getPlayerWalkSpeed(), player.getCurrentSpeed());

            source.press(Key.ShiftLeft);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.Equal(simulation.getGameConfig().getPlayerRunSpeed(), player.getCurrentSpeed());

            source.release(Key.ShiftLeft);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.Equal(simulation.getGameConfig().getPlayerWalkSpeed(), player.getCurrentSpeed());
        }

        [Fact]
        public void jump_requestsExactlyOnce_perPress_evenWhenHeldAcrossFrames() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            source.press(Key.Space);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.True(player.consumeJumpRequest());

            // Held for two more frames -- requestJump() must not fire again
            // until Space is released and pressed again.
            inputService.update();
            controller.update(simulation, inputService);
            Assert.False(player.consumeJumpRequest());

            inputService.update();
            controller.update(simulation, inputService);
            Assert.False(player.consumeJumpRequest());
        }

        [Fact]
        public void createNewNation_firesOnce_perPress_evenWhenHeldAcrossFrames() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            source.press(Key.N);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.NotNull(player.getNationId());
            Assert.Equal(1, simulation.getNationRepository().getNumberOfNations());
            NationId nationId = player.getNationId();

            // N is still held; a second frame must not create a second nation.
            inputService.update();
            controller.update(simulation, inputService);
            Assert.Equal(nationId, player.getNationId());
            Assert.Equal(1, simulation.getNationRepository().getNumberOfNations());
        }

        [Fact]
        public void toggleAutoWalk_flipsOncePerPress() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            Assert.False(player.isAutoWalking());

            source.press(Key.Insert);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.True(player.isAutoWalking());

            // Held: should not flip back on its own.
            inputService.update();
            controller.update(simulation, inputService);
            Assert.True(player.isAutoWalking());

            // A genuine release-then-press cycle: the release must land in
            // its own update() so InputService actually observes an up frame
            // before the next down edge.
            source.release(Key.Insert);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.True(player.isAutoWalking());

            source.press(Key.Insert);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.False(player.isAutoWalking());
        }

        [Fact]
        public void renderDistance_increasesAndDecreases_oncePerPress() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();
            int initial = player.getRenderDistance();

            source.press(Key.PageUp);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.Equal(initial + 10, player.getRenderDistance());

            // Held for another frame: should not keep climbing.
            inputService.update();
            controller.update(simulation, inputService);
            Assert.Equal(initial + 10, player.getRenderDistance());

            source.release(Key.PageUp);
            source.press(Key.PageDown);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.Equal(initial, player.getRenderDistance());
        }

        [Fact]
        public void inventoryKey_isLeftToTheHost_andChangesNothingInTheSimulation() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();
            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);
            string statusBefore = player.getStatus().getStatus();

            // I changes screen, which Game.readInput() hands to ScreenState
            // (#181). Handled here as well, it would act a second time on the
            // same press.
            source.press(Key.I);
            inputService.update();
            controller.update(simulation, inputService);

            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(statusBefore, player.getStatus().getStatus());
        }

        [Fact]
        public void debugGatedCommands_areNoOps_untilDebugModeIsToggledOn() {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            int coinsBefore = player.getInventory().getNumItems(ItemType.COIN);
            string statusBefore = player.getStatus().getStatus();

            source.press(Key.F4);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.Equal(coinsBefore, player.getInventory().getNumItems(ItemType.COIN));
            Assert.False(controller.isDebugMode());

            source.release(Key.F4);
            inputService.update();
            controller.update(simulation, inputService);

            source.press(Key.F1);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.True(controller.isDebugMode());

            source.release(Key.F1);
            source.press(Key.F4);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.Equal(coinsBefore + 100, player.getInventory().getNumItems(ItemType.COIN));

            // Held for another frame: should not spawn a second batch of coins.
            inputService.update();
            controller.update(simulation, inputService);
            Assert.Equal(coinsBefore + 100, player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void generateNearbyLand_onlyRuns_whenDebugModeIsOn() {
            Simulation simulation = createSimulation();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();
            int chunksBefore = simulation.getEnvironment().getNumChunks();

            source.press(Key.F2);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.Equal(chunksBefore, simulation.getEnvironment().getNumChunks());

            source.release(Key.F2);
            source.press(Key.F1);
            inputService.update();
            controller.update(simulation, inputService);

            source.release(Key.F1);
            source.press(Key.F2);
            inputService.update();
            controller.update(simulation, inputService);
            Assert.True(simulation.getEnvironment().getNumChunks() > chunksBefore);
        }

        [Fact]
        public void mouseLook_accumulatesFromMouseDelta_everyFrame() {
            Simulation simulation = createSimulation();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            source.setMouseDelta(new System.Numerics.Vector2(10f, 0f));
            inputService.update();
            controller.update(simulation, inputService);
            float yawAfterFirst = controller.getMouseLook().getYawDegrees();
            Assert.NotEqual(0f, yawAfterFirst);

            inputService.update();
            controller.update(simulation, inputService);
            Assert.True(controller.getMouseLook().getYawDegrees() > yawAfterFirst, "mouse look should keep accumulating while the mouse keeps moving");
        }

        [Fact]
        public void interact_and_settlementAndNationCommands_fireExactlyOncePerPress_notPerHeldFrame() {
            // These commands mostly no-op with a status message in a fresh
            // simulation (no nearby entities, no nation, no settlement); what
            // is under test here is that the binding fires the command
            // exactly once per press regardless of how long the key is held,
            // proving out the wiring for keys not covered by a more specific
            // test above: E, J, L, F, P, H, B.
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();

            Key[] editingKeys = { Key.E, Key.J, Key.L, Key.F, Key.P, Key.H, Key.B };
            foreach (Key key in editingKeys) {
                source.press(key);
                inputService.update();
                controller.update(simulation, inputService);
                string statusAfterPress = player.getStatus().getStatus();
                Assert.False(string.IsNullOrEmpty(statusAfterPress));

                // Held for a second frame: pressing the same no-op command
                // again would bump the duplicate-message counter Status
                // tracks internally (see Status.update), so an unchanged
                // status string here proves the command did not fire twice.
                inputService.update();
                controller.update(simulation, inputService);
                Assert.Equal(statusAfterPress, player.getStatus().getStatus());

                source.release(key);
                inputService.update();
                controller.update(simulation, inputService);
            }
        }

        [Fact]
        public void takeScreenshot_isLeftEntirelyToTheHost() {
            // Capture needs a framebuffer, so Game.readInput() owns F12 and
            // reports its own status. This controller must not touch the
            // status line for that key: it used to claim "Screenshot capture
            // is not implemented yet." on the same frame the host had already
            // written the PNG, so the message the player read was the
            // opposite of what happened (#246).
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();
            string statusBeforePress = player.getStatus().getStatus();

            source.press(Key.F12);
            inputService.update();
            controller.update(simulation, inputService);

            Assert.Equal(statusBeforePress, player.getStatus().getStatus());
            Assert.DoesNotContain("Screenshot", player.getStatus().getStatus());
        }

        [Fact]
        public void toggleCameraView_isLeftEntirelyToTheHost() {
            // The camera is the host's, not the simulation's, so Game.readInput()
            // owns V and reports the new view itself. Handling it here as well
            // would toggle nothing and would overwrite that report (#173).
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();
            string statusBeforePress = player.getStatus().getStatus();

            source.press(Key.V);
            inputService.update();
            controller.update(simulation, inputService);

            Assert.Equal(statusBeforePress, player.getStatus().getStatus());
        }
    }
}
