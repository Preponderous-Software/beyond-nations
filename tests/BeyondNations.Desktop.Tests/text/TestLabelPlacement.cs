using System;
using System.IO;
using System.Numerics;
using Xunit;

using beyondnations.desktop.text;

namespace beyondnationstests {

    /**
    * A settlement's floating name keeps below the HUD's top band instead of
    * running off the top of the screen or under the status line and
    * chronicle (LabelPlacement), and is capped in size up close like a
    * nametag, only larger.
    */
    [Collection("LabelPlacement")]
    public class TestLabelPlacement {

        private static Matrix4x4 view() {
            return Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(0, 0, -1), Vector3.UnitY);
        }

        private static Matrix4x4 perspective() {
            return Matrix4x4.CreatePerspectiveFieldOfView(60f * MathF.PI / 180f, 9f / 16f, 0.1f, 1000f);
        }

        /** Where a world point lands, as a fraction of the screen's height from the top. */
        private static float fromTop(Vector3 point) {
            Vector4 clip = Vector4.Transform(new Vector4(point, 1f), view() * perspective());
            return (1f - clip.Y / clip.W) * 0.5f;
        }

        [Fact]
        public void aLabelBelowTheBandStaysWhereItIs() {
            // centred on screen, one unit tall, ten units away
            Assert.Equal(0f, LabelPlacement.getDrop(new Vector3(0, 0, -10), 1f, view(), perspective(), 0.3f));
        }

        [Fact]
        public void aLabelRunningOffTheTopIsLoweredUntilItsTopIsInsideTheScreen() {
            Vector3 anchor = new Vector3(0, 5.5f, -10);
            float height = 1f;
            Assert.True(fromTop(anchor + Vector3.UnitY * height) < 0f, "the label starts above the screen");

            float drop = LabelPlacement.getDrop(anchor, height, view(), perspective(), 0f);

            Assert.True(drop > 0f);
            float top = fromTop(anchor - Vector3.UnitY * drop + Vector3.UnitY * height);
            Assert.Equal(LabelPlacement.TopMargin, top, 3);
        }

        [Fact]
        public void aLabelUnderTheHudsTopBandIsLoweredBelowIt() {
            Vector3 anchor = new Vector3(0, 2f, -10);
            float height = 1f;
            float band = 0.3f;
            Assert.True(fromTop(anchor + Vector3.UnitY * height) < band, "the label starts inside the band");

            float drop = LabelPlacement.getDrop(anchor, height, view(), perspective(), band);

            float top = fromTop(anchor - Vector3.UnitY * drop + Vector3.UnitY * height);
            Assert.Equal(band + LabelPlacement.TopMargin, top, 3);
        }

        [Fact]
        public void theReserveIsAFractionOfTheScreen() {
            try {
                LabelPlacement.setTopReserve(1.4f);
                Assert.Equal(1f, LabelPlacement.getTopReserve());
                LabelPlacement.setTopReserve(float.NaN);
                Assert.Equal(0f, LabelPlacement.getTopReserve());
            }
            finally {
                LabelPlacement.setTopReserve(0f);
            }
        }

        [Fact]
        public void underAShallowBandALabelIsPlacedAndLowered() {
            Assert.True(LabelPlacement.tryPlace(new Vector3(0, 2f, -10), 1f, view(), perspective(), 0.3f, out float drop));
            Assert.True(drop > 0f);
        }

        /** A phone held upright: the HUD runs most of the way down, so a name that would rise into it is left out. */
        [Fact]
        public void underABandDeeperThanTheMaximumALabelThatWouldRiseIntoItIsLeftOut() {
            Assert.False(LabelPlacement.tryPlace(new Vector3(0, 2f, -10), 1f, view(), perspective(), 0.8f, out float drop));
            Assert.Equal(0f, drop);
            // one already below the band is drawn where it is
            Assert.True(LabelPlacement.tryPlace(new Vector3(0, -5f, -10), 0.5f, view(), perspective(), 0.8f, out drop));
            Assert.Equal(0f, drop);
        }

        [Fact]
        public void aLabelBehindTheCameraIsNotMoved() {
            Assert.Equal(0f, LabelPlacement.getDrop(new Vector3(0, 5f, 10), 1f, view(), perspective(), 0.3f));
        }

        [Fact]
        public void onlyLargeLabelsKeepClearOfTheTop() {
            Assert.True(LabelPlacement.keepsClearOfTop(2f));
            Assert.False(LabelPlacement.keepsClearOfTop(1f));
            Assert.False(LabelPlacement.keepsClearOfTop(0f));
        }

        [Fact]
        public void aLargeLabelIsCappedUpCloseAtItsScaleTimesANametagsShare() {
            LabelCulling culling = new LabelCulling();
            Matrix4x4 projection = perspective();
            Vector3 close = new Vector3(0, 0, -3);
            float nametag = culling.getLineHeight(0.6f, close, view(), projection);
            float town = culling.getLineHeight(0.6f * 2f, close, view(), projection, 2f);

            // up close both are capped, the town's name at twice the share
            Assert.True(town < 1.2f);
            Assert.Equal(nametag * 2f, town, 4);
            float share = town * projection.M22 / (2f * 3f);
            Assert.Equal(culling.getMaxScreenFraction() * 2f, share, 4);

            // far away neither is
            Vector3 far = new Vector3(0, 0, -80);
            Assert.Equal(1.2f, culling.getLineHeight(1.2f, far, view(), projection, 2f), 4);
        }

        /** Both renderers size labels with the cap and place large ones with LabelPlacement; #304 overwrote the cap. */
        [Theory]
        [InlineData("src/BeyondNations.Desktop/text/WorldLabelRenderer.cs")]
        [InlineData("src/BeyondNations.Web/game/WebWorldLabelRenderer.cs")]
        public void bothRenderersUseTheCapAndThePlacement(string path) {
            string code = File.ReadAllText(Path.Combine(repositoryRoot(), path));
            Assert.Contains("culling.getLineHeight(LineHeightInWorldUnits * labelScale", code);
            Assert.DoesNotContain("currentWorldUnitsPerPixel = LineHeightInWorldUnits * labelScale", code);
            Assert.Contains("LabelPlacement.tryPlace(", code);
        }

        private static string repositoryRoot() {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BeyondNations.sln"))) {
                dir = dir.Parent;
            }
            Assert.NotNull(dir);
            return dir.FullName;
        }
    }
}
