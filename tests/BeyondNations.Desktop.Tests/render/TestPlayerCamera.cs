using System;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.render;

namespace beyondnationstests.desktop {

    /**
    * The camera is placed by arithmetic on vectors, so where it ends up can be
    * checked exactly, without a window and without anybody looking at one.
    *
    * The framing under test is the one the Unity build had: a camera parented
    * to the player at the local offset (0, 5, -10), pointed back at it. If
    * these numbers move, the game looks different from the one being ported.
    */
    public class TestPlayerCamera {

        private const float Tolerance = 1e-4f;

        private static void assertClose(Vector3 expected, Vector3 actual) {
            Assert.True(
                Vector3.Distance(expected, actual) < 1e-3f,
                "expected " + expected + " but was " + actual);
        }

        [Fact]
        public void testThirdPersonSitsAtTheUnityOffsetWhenFacingForward() {
            // setup
            PlayerCamera camera = new PlayerCamera();

            // run
            camera.follow(Vector3.Zero, 0f, 0f, 0f);

            // verify
            assertClose(new Vector3(0f, 5f, -10f), camera.getEyePosition());
            assertClose(Vector3.Zero, camera.getTargetPosition());
        }

        [Fact]
        public void testThirdPersonFollowsThePlayerPosition() {
            // setup
            PlayerCamera camera = new PlayerCamera();
            Vector3 playerPosition = new Vector3(120f, 2f, -37f);

            // run
            camera.follow(playerPosition, 0f, 0f, 0f);

            // verify
            assertClose(playerPosition + PlayerCamera.ThirdPersonOffset, camera.getEyePosition());
            assertClose(playerPosition, camera.getTargetPosition());
        }

        [Fact]
        public void testThirdPersonTrailsThePlayerAroundAsItTurns() {
            // setup
            PlayerCamera camera = new PlayerCamera();
            float yaw = MathF.PI / 2f;

            // run
            camera.follow(Vector3.Zero, yaw, 0f, 0f);

            // verify: facing +X, so the eye is ten units back along -X
            assertClose(new Vector3(-10f, 5f, 0f), camera.getEyePosition());
        }

        [Fact]
        public void testThirdPersonEyeStaysTenUnitsBehindWhateverTheYaw() {
            // setup
            PlayerCamera camera = new PlayerCamera();
            Vector3 playerPosition = new Vector3(5f, 1f, 5f);

            for (int degrees = 0; degrees < 360; degrees += 15) {
                float yaw = degrees * MathF.PI / 180f;

                // run
                camera.follow(playerPosition, yaw, 0f, 0f);

                // verify: the eye is directly opposite the player's forward
                Vector3 offset = camera.getEyePosition() - playerPosition;
                Vector3 forward = new Vector3(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
                assertClose(new Vector3(0f, 5f, 0f) - forward * 10f, offset);
            }
        }

        [Fact]
        public void testMouseYawSwingsTheCameraWithoutMovingThePlayer() {
            // setup
            PlayerCamera camera = new PlayerCamera();

            // run
            camera.follow(Vector3.Zero, 0f, 90f, 0f);

            // verify: ninety degrees of mouse yaw is the same as ninety of player yaw
            assertClose(new Vector3(-10f, 5f, 0f), camera.getEyePosition());
            assertClose(Vector3.Zero, camera.getTargetPosition());
        }

        [Fact]
        public void testLookingUpLowersTheThirdPersonEye() {
            // setup
            PlayerCamera camera = new PlayerCamera();
            camera.follow(Vector3.Zero, 0f, 0f, 0f);
            float levelHeight = camera.getEyePosition().Y;

            // run
            camera.follow(Vector3.Zero, 0f, 0f, 30f);

            // verify
            Assert.True(camera.getEyePosition().Y < levelHeight);
        }

        [Fact]
        public void testExtremePitchNeverProducesAnUnusableViewMatrix() {
            // setup
            PlayerCamera camera = new PlayerCamera();

            for (float pitch = -89f; pitch <= 89f; pitch += 1f) {
                foreach (CameraMode mode in Enum.GetValues(typeof(CameraMode))) {
                    camera.setMode(mode);

                    // run
                    camera.follow(Vector3.Zero, 0.3f, 40f, pitch);
                    Matrix4x4 view = camera.getViewMatrix();

                    // verify
                    Assert.False(float.IsNaN(view.M11 + view.M22 + view.M33 + view.M44),
                        mode + " at pitch " + pitch + " gave a view matrix of NaN");
                }
            }
        }

        [Fact]
        public void testFirstPersonPutsTheEyeAtThePlayerAndLooksWhereItFaces() {
            // setup
            PlayerCamera camera = new PlayerCamera();
            camera.setMode(CameraMode.FirstPerson);
            Vector3 playerPosition = new Vector3(3f, 2f, 4f);

            // run
            camera.follow(playerPosition, 0f, 0f, 0f);

            // verify
            assertClose(playerPosition + new Vector3(0f, PlayerCamera.FirstPersonEyeHeight, 0f), camera.getEyePosition());
            assertClose(Vector3.UnitZ, camera.getForward());
        }

        [Fact]
        public void testFirstPersonPitchLooksUpAndDown() {
            // setup
            PlayerCamera camera = new PlayerCamera();
            camera.setMode(CameraMode.FirstPerson);

            // run
            camera.follow(Vector3.Zero, 0f, 0f, 45f);
            float lookingUp = camera.getForward().Y;
            camera.follow(Vector3.Zero, 0f, 0f, -45f);
            float lookingDown = camera.getForward().Y;

            // verify
            Assert.True(lookingUp > 0.7f);
            Assert.True(lookingDown < -0.7f);
        }

        [Fact]
        public void testToggleModeSwitchesBetweenTheTwoViews() {
            // setup
            PlayerCamera camera = new PlayerCamera();

            // verify
            Assert.Equal(CameraMode.ThirdPerson, camera.getMode());

            // run
            camera.toggleMode();
            Assert.Equal(CameraMode.FirstPerson, camera.getMode());
            camera.toggleMode();
            Assert.Equal(CameraMode.ThirdPerson, camera.getMode());
        }

        [Fact]
        public void testRenderDistanceBecomesTheFarPlane() {
            // setup
            PlayerCamera camera = new PlayerCamera();
            camera.setAspectRatio(1600, 900);

            // run
            camera.setRenderDistance(50f);
            Matrix4x4 shallow = camera.getProjectionMatrix();
            camera.setRenderDistance(1000f);
            Matrix4x4 deep = camera.getProjectionMatrix();

            // verify: only the depth terms of the projection move with it
            Assert.Equal(1000f, camera.getRenderDistance());
            Assert.NotEqual(shallow.M33, deep.M33);
            Assert.Equal(shallow.M11, deep.M11, 4);
        }

        [Fact]
        public void testADegenerateRenderDistanceIsRefused() {
            // setup
            PlayerCamera camera = new PlayerCamera();
            camera.setRenderDistance(200f);

            // run
            camera.setRenderDistance(0f);
            camera.setRenderDistance(-1f);

            // verify
            Assert.Equal(200f, camera.getRenderDistance());
        }

        [Fact]
        public void testAspectRatioFollowsTheFramebufferAndIgnoresAMinimisedWindow() {
            // setup
            PlayerCamera camera = new PlayerCamera();

            // run
            camera.setAspectRatio(1600, 800);
            float wide = camera.getAspectRatio();
            camera.setAspectRatio(0, 0);

            // verify
            Assert.Equal(2f, wide, 4);
            Assert.Equal(wide, camera.getAspectRatio());
        }

        [Fact]
        public void testTheViewMatrixPutsThePlayerInFrontOfTheCamera() {
            // setup
            PlayerCamera camera = new PlayerCamera();
            Vector3 playerPosition = new Vector3(10f, 0f, 10f);
            camera.follow(playerPosition, 0.9f, 0f, 0f);

            // run
            Vector3 inViewSpace = Vector3.Transform(playerPosition, camera.getViewMatrix());

            // verify: the view looks down -Z, and the player is ten units away
            Assert.True(inViewSpace.Z < 0f);
            Assert.True(MathF.Abs(inViewSpace.X) < 1e-3f);
            Assert.Equal(MathF.Sqrt(125f), inViewSpace.Length(), 2);
        }
    }
}
