using System;
using System.Numerics;
using Xunit;

using beyondnations.desktop.text;

namespace beyondnationstests {

    /**
    * The decisions behind world-space nametags, checked without a GL context.
    * Everything here follows from a position, a view matrix and a projection
    * matrix, which is why it can be tested at all.
    */
    public class TestLabelCulling {

        private static Matrix4x4 viewAtOriginLookingDownNegativeZ() {
            return Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(0, 0, -1), Vector3.UnitY);
        }

        private static Matrix4x4 perspective() {
            return Matrix4x4.CreatePerspectiveFieldOfView(60f * MathF.PI / 180f, 4f / 3f, 0.1f, 1000f);
        }

        [Fact]
        public void testLabelInFrontIsAccepted() {
            LabelCulling culling = new LabelCulling();
            float alpha;

            bool accepted = culling.tryAccept(new Vector3(0, 0, -10), viewAtOriginLookingDownNegativeZ(), perspective(), out alpha);

            Assert.True(accepted);
            Assert.Equal(1f, alpha, 3);
        }

        [Fact]
        public void testLabelBehindTheCameraIsRejected() {
            LabelCulling culling = new LabelCulling();
            float alpha;

            // Directly behind the eye. Without this test a label here would be
            // projected back through the eye and drawn mirrored on screen.
            bool accepted = culling.tryAccept(new Vector3(0, 0, 10), viewAtOriginLookingDownNegativeZ(), perspective(), out alpha);

            Assert.False(accepted);
            Assert.Equal(0f, alpha);
        }

        [Fact]
        public void testLabelExactlyOnTheEyePlaneIsRejected() {
            LabelCulling culling = new LabelCulling();
            float alpha;

            // The projection divides by zero here.
            bool accepted = culling.tryAccept(Vector3.Zero, viewAtOriginLookingDownNegativeZ(), perspective(), out alpha);

            Assert.False(accepted);
        }

        [Fact]
        public void testLabelBeyondCullDistanceIsRejected() {
            LabelCulling culling = new LabelCulling();
            culling.setCullDistance(100f);
            float alpha;

            bool accepted = culling.tryAccept(new Vector3(0, 0, -150), viewAtOriginLookingDownNegativeZ(), perspective(), out alpha);

            Assert.False(accepted);
            Assert.Equal(0f, alpha);
        }

        [Fact]
        public void testLabelOutsideTheFrustumSidesIsRejected() {
            LabelCulling culling = new LabelCulling();
            float alpha;

            // Far off to the left, but only a short way ahead, so it is well
            // outside the horizontal field of view.
            bool accepted = culling.tryAccept(new Vector3(-500, 0, -5), viewAtOriginLookingDownNegativeZ(), perspective(), out alpha);

            Assert.False(accepted);
        }

        [Fact]
        public void testAlphaIsFullyOpaqueUpToTheFadeDistance() {
            LabelCulling culling = new LabelCulling();
            culling.setFadeDistance(50f);
            culling.setCullDistance(100f);

            Assert.Equal(1f, culling.getAlphaForDistance(0f), 3);
            Assert.Equal(1f, culling.getAlphaForDistance(25f), 3);
            Assert.Equal(1f, culling.getAlphaForDistance(50f), 3);
        }

        [Fact]
        public void testAlphaRampsToNothingBetweenFadeAndCull() {
            LabelCulling culling = new LabelCulling();
            culling.setFadeDistance(50f);
            culling.setCullDistance(100f);

            // Half way along the ramp.
            Assert.Equal(0.5f, culling.getAlphaForDistance(75f), 3);
            Assert.Equal(0f, culling.getAlphaForDistance(100f), 3);
            Assert.Equal(0f, culling.getAlphaForDistance(200f), 3);
        }

        [Fact]
        public void testAlphaDecreasesMonotonicallyAcrossTheRamp() {
            LabelCulling culling = new LabelCulling();
            culling.setFadeDistance(50f);
            culling.setCullDistance(100f);

            float previous = culling.getAlphaForDistance(50f);
            for (float d = 55f; d <= 100f; d += 5f) {
                float current = culling.getAlphaForDistance(d);
                Assert.True(current <= previous, "alpha rose from " + previous + " to " + current + " at distance " + d);
                previous = current;
            }
        }
    }

    /**
    * The billboarding maths: a label faces the camera because it is built from
    * the camera's own right and up axes, so it needs no rotation of its own.
    */
    public class TestLabelBillboard {

        private static Matrix4x4 viewFrom(Vector3 eye, Vector3 target) {
            return Matrix4x4.CreateLookAt(eye, target, Vector3.UnitY);
        }

        [Fact]
        public void testEyePositionIsRecoveredFromTheViewMatrix() {
            Vector3 eye = new Vector3(12f, 34f, -56f);

            Vector3 recovered = LabelBillboard.getEyePosition(viewFrom(eye, Vector3.Zero));

            Assert.Equal(eye.X, recovered.X, 3);
            Assert.Equal(eye.Y, recovered.Y, 3);
            Assert.Equal(eye.Z, recovered.Z, 3);
        }

        [Fact]
        public void testForwardPointsFromTheEyeTowardTheTarget() {
            Vector3 eye = new Vector3(0, 0, 10);
            Vector3 target = Vector3.Zero;

            Vector3 forward = LabelBillboard.getForward(viewFrom(eye, target));
            Vector3 expected = Vector3.Normalize(target - eye);

            Assert.Equal(expected.X, forward.X, 3);
            Assert.Equal(expected.Y, forward.Y, 3);
            Assert.Equal(expected.Z, forward.Z, 3);
        }

        [Fact]
        public void testRightUpAndForwardAreMutuallyPerpendicularUnitVectors() {
            Matrix4x4 view = viewFrom(new Vector3(3, 7, 11), new Vector3(-4, 1, 2));

            Vector3 right = LabelBillboard.getRight(view);
            Vector3 up = LabelBillboard.getUp(view);
            Vector3 forward = LabelBillboard.getForward(view);

            Assert.Equal(1f, right.Length(), 3);
            Assert.Equal(1f, up.Length(), 3);
            Assert.Equal(1f, forward.Length(), 3);

            Assert.Equal(0f, Vector3.Dot(right, up), 3);
            Assert.Equal(0f, Vector3.Dot(right, forward), 3);
            Assert.Equal(0f, Vector3.Dot(up, forward), 3);
        }

        [Fact]
        public void testQuadCornersLieInThePlaneFacingTheCamera() {
            Matrix4x4 view = viewFrom(new Vector3(0, 0, 10), Vector3.Zero);
            Vector3 anchor = Vector3.Zero;
            Vector3 forward = LabelBillboard.getForward(view);

            // Two corners offset in the label's own 2D space must differ only
            // within the plane perpendicular to the view direction.
            Vector3 right = LabelBillboard.getRight(view);
            Vector3 up = LabelBillboard.getUp(view);
            Vector3 a = LabelBillboard.toWorld(anchor, right, up, -1f, -1f, 1f);
            Vector3 b = LabelBillboard.toWorld(anchor, right, up, 1f, 1f, 1f);

            Assert.Equal(0f, Vector3.Dot(b - a, forward), 3);
        }

        [Fact]
        public void testCenteringOffsetHalvesTheMeasuredSize() {
            Vector2 offset = LabelBillboard.getCenteringOffset(new Vector2(40f, 10f));

            // Centred horizontally, and lifted so the label sits above its
            // anchor rather than hanging off it.
            Assert.Equal(-20f, offset.X, 3);
            Assert.Equal(-10f, offset.Y, 3);
        }
    }
}
