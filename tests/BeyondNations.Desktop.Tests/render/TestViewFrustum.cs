using System;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.render;

namespace beyondnationstests.desktop {

    /**
    * Plane extraction is the part of culling that is easy to get subtly wrong
    * and impossible to notice: a sign error in one plane hides a strip of the
    * world along one edge of the screen, and the only symptom is that things
    * disappear when nobody is looking at them.
    *
    * So the frustum is checked against a camera whose geometry is known by
    * hand. A ninety-degree field of view at an aspect of one means the visible
    * region at ten units away is a square twenty units across, and every case
    * below is a point placed relative to that square.
    */
    public class TestViewFrustum {

        private const float NearPlane = 1f;
        private const float FarPlane = 100f;

        /**
        * At the origin, looking down -Z, which is where a view matrix points by
        * convention.
        */
        private static Matrix4x4 knownViewProjection() {
            Matrix4x4 view = Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(0f, 0f, -1f), Vector3.UnitY);
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI / 2f, 1f, NearPlane, FarPlane);
            return view * projection;
        }

        private static ViewFrustum knownFrustum() {
            ViewFrustum frustum = new ViewFrustum();
            frustum.update(knownViewProjection());
            return frustum;
        }

        [Fact]
        public void testEveryPlaneNormalIsUnitLength() {
            // setup
            ViewFrustum frustum = knownFrustum();

            for (int i = 0; i < ViewFrustum.PlaneCount; i++) {
                // run
                Vector4 plane = frustum.getPlane(i);
                float length = new Vector3(plane.X, plane.Y, plane.Z).Length();

                // verify
                Assert.Equal(1f, length, 4);
            }
        }

        [Fact]
        public void testPlaneNormalsPointIntoTheVolume() {
            // setup
            ViewFrustum frustum = knownFrustum();
            Vector3 straightAhead = new Vector3(0f, 0f, -10f);

            for (int i = 0; i < ViewFrustum.PlaneCount; i++) {
                // verify: a point in the middle of the volume is in front of all six
                Assert.True(frustum.distanceTo(i, straightAhead) > 0f, "plane " + i + " faces the wrong way");
            }
        }

        [Fact]
        public void testTheNearAndFarPlanesAreWhereTheyWereAskedToBe() {
            // setup
            ViewFrustum frustum = knownFrustum();

            // run, verify: distance to the near plane is distance past it
            Assert.Equal(9f, frustum.distanceTo(ViewFrustum.Near, new Vector3(0f, 0f, -10f)), 3);
            Assert.Equal(90f, frustum.distanceTo(ViewFrustum.Far, new Vector3(0f, 0f, -10f)), 3);
        }

        [Fact]
        public void testAPointStraightAheadIsInside() {
            // setup
            ViewFrustum frustum = knownFrustum();

            // run, verify
            Assert.True(frustum.containsPoint(new Vector3(0f, 0f, -10f)));
        }

        [Fact]
        public void testAPointBehindTheCameraIsOutside() {
            // setup
            ViewFrustum frustum = knownFrustum();

            // run, verify
            Assert.False(frustum.containsPoint(new Vector3(0f, 0f, 10f)));
        }

        [Fact]
        public void testAPointNearerThanTheNearPlaneIsOutside() {
            // setup
            ViewFrustum frustum = knownFrustum();

            // run, verify
            Assert.False(frustum.containsPoint(new Vector3(0f, 0f, -0.5f)));
        }

        [Fact]
        public void testAPointBeyondTheFarPlaneIsOutside() {
            // setup
            ViewFrustum frustum = knownFrustum();

            // run, verify
            Assert.True(frustum.containsPoint(new Vector3(0f, 0f, -99f)));
            Assert.False(frustum.containsPoint(new Vector3(0f, 0f, -101f)));
        }

        [Fact]
        public void testTheSideEdgesAreWhereNinetyDegreesPutsThem() {
            // setup: at ten units out, the visible half-width is also ten
            ViewFrustum frustum = knownFrustum();

            // run, verify
            Assert.True(frustum.containsPoint(new Vector3(9f, 0f, -10f)));
            Assert.False(frustum.containsPoint(new Vector3(11f, 0f, -10f)));
            Assert.True(frustum.containsPoint(new Vector3(-9f, 0f, -10f)));
            Assert.False(frustum.containsPoint(new Vector3(-11f, 0f, -10f)));
        }

        [Fact]
        public void testTheTopAndBottomEdgesAreWhereTheAspectPutsThem() {
            // setup
            ViewFrustum frustum = knownFrustum();

            // run, verify
            Assert.True(frustum.containsPoint(new Vector3(0f, 9f, -10f)));
            Assert.False(frustum.containsPoint(new Vector3(0f, 11f, -10f)));
            Assert.True(frustum.containsPoint(new Vector3(0f, -9f, -10f)));
            Assert.False(frustum.containsPoint(new Vector3(0f, -11f, -10f)));
        }

        [Fact]
        public void testASphereIsKeptWhileAnyOfItIsStillInside() {
            // setup
            ViewFrustum frustum = knownFrustum();
            Vector3 justOutside = new Vector3(12f, 0f, -10f);

            // run, verify: the centre is out but a big enough sphere is not
            Assert.False(frustum.containsSphere(justOutside, 0f));
            Assert.True(frustum.containsSphere(justOutside, 5f));
            Assert.False(frustum.containsSphere(justOutside, 0.5f));
        }

        [Fact]
        public void testAWiderAspectSeesMoreSideways() {
            // setup
            Matrix4x4 view = Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(0f, 0f, -1f), Vector3.UnitY);
            ViewFrustum wide = new ViewFrustum();
            wide.update(view * Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 2f, NearPlane, FarPlane));
            ViewFrustum square = knownFrustum();
            Vector3 offToTheSide = new Vector3(15f, 0f, -10f);

            // run, verify
            Assert.False(square.containsPoint(offToTheSide));
            Assert.True(wide.containsPoint(offToTheSide));
        }

        [Fact]
        public void testTheFrustumMovesWithTheCamera() {
            // setup
            PlayerCamera camera = new PlayerCamera();
            camera.setAspectRatio(1000, 1000);
            camera.setRenderDistance(100f);
            ViewFrustum frustum = new ViewFrustum();

            // run: the player stands at the origin facing +Z, so the camera
            // trails at -Z and everything ahead of the player is in view
            camera.follow(Vector3.Zero, 0f, 0f, 0f);
            frustum.update(camera.getViewProjectionMatrix());

            // verify
            Assert.True(frustum.containsSphere(new Vector3(0f, 0f, 20f), 1f));
            Assert.False(frustum.containsSphere(new Vector3(0f, 0f, -60f), 1f));

            // run: turn the player around and the same two points swap over
            camera.follow(Vector3.Zero, MathF.PI, 0f, 0f);
            frustum.update(camera.getViewProjectionMatrix());

            // verify
            Assert.False(frustum.containsSphere(new Vector3(0f, 0f, 20f), 1f));
            Assert.True(frustum.containsSphere(new Vector3(0f, 0f, -60f), 1f));
        }
    }
}
