using System.Numerics;
using Silk.NET.Input;
using Xunit;
using beyondnations;
using beyondnations.desktop.input;
using beyondnations.desktop.render;

namespace beyondnationstests.desktop.input {

    /**
    * Right must turn the view to the right as seen on screen. The sign of
    * yaw cannot show that by itself: Unity's world was left-handed and the
    * renderer's is right-handed, which mirrored both A/D and mouse look after
    * the port. These tests read the direction off the camera's own view
    * matrix, where +X in view space is the right-hand side of the screen.
    */
    public class TurnDirectionTests {

        private static Simulation createSimulation() {
            GameConfig gameConfig = new GameConfig();
            gameConfig.setWorldSeed(1234);
            return new Simulation(gameConfig);
        }

        private static float screenRightComponent(Matrix4x4 view, Vector3 worldDirection) {
            return Vector3.TransformNormal(worldDirection, view).X;
        }

        [Theory]
        [InlineData(Key.D, 1f)]
        [InlineData(Key.Right, 1f)]
        [InlineData(Key.A, -1f)]
        [InlineData(Key.Left, -1f)]
        public void turnKeys_turnTowardTheMatchingSideOfTheScreen(Key key, float expectedSide) {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            PlayerInputController controller = new PlayerInputController();
            PlayerCamera camera = new PlayerCamera();
            camera.follow(player.getPosition(), player.getYaw(), 0f, 0f);
            Matrix4x4 viewBefore = camera.getViewMatrix();

            source.press(key);
            for (int i = 0; i < 10; i++) {
                inputService.update();
                controller.update(simulation, inputService);
                player.fixedUpdate();
            }

            float side = screenRightComponent(viewBefore, player.getForward());
            Assert.True(side * expectedSide > 0f, key + " turned the player toward the wrong side of the screen (view-space x = " + side + ")");
        }

        [Theory]
        [InlineData(40f, 1f)]
        [InlineData(-40f, -1f)]
        public void mouseLook_turnsTheViewTheWayTheMouseMoves(float mouseDeltaX, float expectedSide) {
            Simulation simulation = createSimulation();
            Player player = simulation.getPlayer();
            MouseLook mouseLook = new MouseLook();
            PlayerCamera camera = new PlayerCamera();
            camera.follow(player.getPosition(), player.getYaw(), 0f, 0f);
            Matrix4x4 viewBefore = camera.getViewMatrix();

            mouseLook.apply(new Vector2(mouseDeltaX, 0f));
            camera.follow(player.getPosition(), player.getYaw(), mouseLook.getYawDegrees(), mouseLook.getPitchDegrees());

            float side = screenRightComponent(viewBefore, camera.getForward());
            Assert.True(side * expectedSide > 0f, "mouse x " + mouseDeltaX + " turned the view toward the wrong side (view-space x = " + side + ")");
        }
    }
}
