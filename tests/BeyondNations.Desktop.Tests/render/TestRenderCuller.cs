using System;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.render;

namespace beyondnationstests.desktop {

    /**
    * The culling decision itself: given a camera and a thing, is the thing
    * submitted for drawing.
    *
    * This is where the acceptance criterion of #219 actually lives. A frame
    * that draws fewer instances is evidence, but only these tests say why: that
    * something past the render distance is rejected for being past it, that
    * something behind the camera is rejected for being behind it, and that
    * neither rejection can quietly swallow something that was in view.
    */
    public class TestRenderCuller {

        private static PlayerCamera cameraAtOrigin(float renderDistance) {
            PlayerCamera camera = new PlayerCamera();
            camera.setAspectRatio(1000, 1000);
            camera.setRenderDistance(renderDistance);
            camera.setMode(CameraMode.FirstPerson);
            // Facing +Z from the origin, which is where the player starts.
            camera.follow(Vector3.Zero, 0f, 0f, 0f);
            return camera;
        }

        private static RenderCuller cullerAtOrigin(float renderDistance) {
            RenderCuller culler = new RenderCuller();
            culler.beginFrame(cameraAtOrigin(renderDistance));
            return culler;
        }

        [Fact]
        public void testSomethingInFrontAndCloseIsDrawn() {
            // setup
            RenderCuller culler = cullerAtOrigin(200f);

            // run
            bool drawn = culler.shouldDraw(new Vector3(0f, 0f, 50f), Vector3.One);

            // verify
            Assert.True(drawn);
            Assert.Equal(0, culler.getCulled());
            Assert.Equal(1, culler.getConsidered());
        }

        [Fact]
        public void testSomethingBeyondTheRenderDistanceIsRejected() {
            // setup
            RenderCuller culler = cullerAtOrigin(200f);

            // run
            bool near = culler.shouldDraw(new Vector3(0f, 0f, 150f), Vector3.One);
            bool far = culler.shouldDraw(new Vector3(0f, 0f, 400f), Vector3.One);

            // verify
            Assert.True(near);
            Assert.False(far);
            Assert.Equal(1, culler.getCulledByDistance());
            Assert.Equal(0, culler.getCulledByFrustum());
        }

        [Fact]
        public void testTheSameThingIsKeptOrRejectedAsTheRenderDistanceMoves() {
            // setup
            Vector3 threeHundredAway = new Vector3(0f, 0f, 300f);

            // run
            RenderCuller shortSighted = cullerAtOrigin(50f);
            RenderCuller farSighted = cullerAtOrigin(1000f);

            // verify: this is Page Down and Page Up, in one assertion
            Assert.False(shortSighted.shouldDraw(threeHundredAway, Vector3.One));
            Assert.True(farSighted.shouldDraw(threeHundredAway, Vector3.One));
        }

        [Fact]
        public void testSomethingBehindTheCameraIsRejectedByTheFrustum() {
            // setup
            RenderCuller culler = cullerAtOrigin(200f);

            // run
            bool drawn = culler.shouldDraw(new Vector3(0f, 0f, -50f), Vector3.One);

            // verify
            Assert.False(drawn);
            Assert.Equal(0, culler.getCulledByDistance());
            Assert.Equal(1, culler.getCulledByFrustum());
        }

        [Fact]
        public void testSomethingOffToTheSideIsRejectedByTheFrustum() {
            // setup
            RenderCuller culler = cullerAtOrigin(200f);

            // run
            bool drawn = culler.shouldDraw(new Vector3(150f, 0f, 5f), Vector3.One);

            // verify
            Assert.False(drawn);
            Assert.Equal(1, culler.getCulledByFrustum());
        }

        [Fact]
        public void testAGroundTileIsMeasuredByItsCornersNotItsCentre() {
            // setup: a ground tile is fifteen units across and one thick
            Vector3 tileScale = new Vector3(15f, 1f, 15f);
            float radius = RenderCuller.boundingRadius(tileScale);
            RenderCuller culler = cullerAtOrigin(100f);

            // run: a tile whose centre is just past the render distance but
            // whose near corner is not
            bool drawn = culler.shouldDraw(new Vector3(0f, 0f, 100f + radius - 1f), tileScale);

            // verify
            Assert.True(radius > 10f);
            Assert.True(drawn);
        }

        [Fact]
        public void testTurningCullingOffDrawsEverything() {
            // setup
            RenderCuller culler = new RenderCuller();
            culler.beginFrame(cameraAtOrigin(50f));
            culler.setEnabled(false);

            // run
            bool behind = culler.shouldDraw(new Vector3(0f, 0f, -5000f), Vector3.One);
            bool distant = culler.shouldDraw(new Vector3(0f, 0f, 5000f), Vector3.One);

            // verify
            Assert.True(behind);
            Assert.True(distant);
            Assert.Equal(0, culler.getCulled());
        }

        [Fact]
        public void testCountersAreResetEveryFrame() {
            // setup
            RenderCuller culler = cullerAtOrigin(50f);
            culler.shouldDraw(new Vector3(0f, 0f, 5000f), Vector3.One);
            Assert.Equal(1, culler.getCulled());

            // run
            culler.beginFrame(cameraAtOrigin(50f));

            // verify
            Assert.Equal(0, culler.getCulled());
            Assert.Equal(0, culler.getConsidered());
        }

        [Fact]
        public void testRenderItemsAreJudgedByPositionAndScale() {
            // setup
            RenderCuller culler = cullerAtOrigin(200f);
            RenderItem visible;
            visible.kind = PrimitiveKind.Cube;
            visible.position = new Vector3(0f, 0f, 30f);
            visible.scale = Vector3.One;
            visible.color = Rgba.White;
            RenderItem hidden = visible;
            hidden.position = new Vector3(0f, 0f, -30f);

            // run, verify
            Assert.True(culler.shouldDraw(ref visible));
            Assert.False(culler.shouldDraw(ref hidden));
        }

        [Fact]
        public void testAThirdPersonCameraStillSeesThePlayerItFollows() {
            // setup: the one thing culling must never remove
            PlayerCamera camera = new PlayerCamera();
            camera.setAspectRatio(1280, 720);
            camera.setRenderDistance(50f);
            RenderCuller culler = new RenderCuller();

            for (int degrees = 0; degrees < 360; degrees += 15) {
                Vector3 playerPosition = new Vector3(degrees, 2f, -degrees);
                camera.follow(playerPosition, degrees * MathF.PI / 180f, 0f, 0f);
                culler.beginFrame(camera);

                // run, verify
                Assert.True(culler.shouldDraw(playerPosition, Vector3.One),
                    "the player was culled at yaw " + degrees);
            }
        }

        [Fact]
        public void testMostOfALargeWorldIsCulledFromOnePointOfView() {
            // setup: a grid of tiles like the ground the world generator lays
            // down, seen from a camera that can only look one way
            RenderCuller culler = cullerAtOrigin(200f);
            Vector3 tileScale = new Vector3(15f, 1f, 15f);
            int drawn = 0;
            int total = 0;

            // run
            for (int x = -600; x <= 600; x += 15) {
                for (int z = -600; z <= 600; z += 15) {
                    total++;
                    if (culler.shouldDraw(new Vector3(x, 0f, z), tileScale)) {
                        drawn++;
                    }
                }
            }

            // verify
            Assert.True(drawn > 0, "the culler drew nothing at all");
            Assert.True(drawn < total / 10, "only " + drawn + " of " + total + " tiles were expected to survive");
            Assert.Equal(total, drawn + culler.getCulled());
        }
    }
}
