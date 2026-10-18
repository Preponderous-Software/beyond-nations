using System.Numerics;
using Xunit;
using beyondnations.desktop.input;

namespace beyondnationstests.desktop.input {

    public class MouseLookTests {

        [Fact]
        public void apply_accumulatesYawFromHorizontalDelta() {
            MouseLook mouseLook = new MouseLook(sensitivityDegreesPerPixel: 1f);

            mouseLook.apply(new Vector2(10f, 0f));
            Assert.Equal(10f, mouseLook.getYawDegrees());

            mouseLook.apply(new Vector2(5f, 0f));
            Assert.Equal(15f, mouseLook.getYawDegrees());
        }

        [Fact]
        public void apply_accumulatesPitchInvertedFromVerticalDelta() {
            MouseLook mouseLook = new MouseLook(sensitivityDegreesPerPixel: 1f);

            // Moving the mouse up (negative Y delta) should look up (positive pitch).
            mouseLook.apply(new Vector2(0f, -10f));
            Assert.Equal(10f, mouseLook.getPitchDegrees());
        }

        [Fact]
        public void apply_clampsPitchToConfiguredRange() {
            MouseLook mouseLook = new MouseLook(sensitivityDegreesPerPixel: 1f, minPitchDegrees: -30f, maxPitchDegrees: 30f);

            mouseLook.apply(new Vector2(0f, -1000f));
            Assert.Equal(30f, mouseLook.getPitchDegrees());

            mouseLook.apply(new Vector2(0f, 2000f));
            Assert.Equal(-30f, mouseLook.getPitchDegrees());
        }

        [Fact]
        public void apply_withZeroDelta_leavesYawAndPitchUnchanged() {
            MouseLook mouseLook = new MouseLook(sensitivityDegreesPerPixel: 1f);

            mouseLook.apply(new Vector2(4f, -4f));
            float yawAfterFirst = mouseLook.getYawDegrees();
            float pitchAfterFirst = mouseLook.getPitchDegrees();

            mouseLook.apply(Vector2.Zero);
            Assert.Equal(yawAfterFirst, mouseLook.getYawDegrees());
            Assert.Equal(pitchAfterFirst, mouseLook.getPitchDegrees());
        }
    }
}
