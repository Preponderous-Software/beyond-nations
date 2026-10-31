using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations.desktop.render;

namespace beyondnationstests.desktop {

    /**
    * The README flyover's camera path (--flyover-out). It is arithmetic on
    * settlement positions, so where it goes is checked without a window.
    */
    public class TestFlyoverDirector {

        private static void assertClose(Vector3 expected, Vector3 actual) {
            Assert.True(Vector3.Distance(expected, actual) < 1e-3f, "expected " + expected + " but was " + actual);
        }

        [Fact]
        public void testGlidesBetweenTheClosestSettlementsOfDifferentNations() {
            // setup: 0 and 1 are closest but share a nation; 1 and 2 are the closest pair across a border
            List<Vector3> settlements = new List<Vector3> {
                new Vector3(0f, 1.5f, 0f),
                new Vector3(50f, 1.5f, 0f),
                new Vector3(50f, 1.5f, 200f),
                new Vector3(1000f, 1.5f, 1000f),
            };
            List<int> nations = new List<int> { 0, 0, 1, 2 };

            // run
            FlyoverDirector flyover = FlyoverDirector.between(settlements, nations, Vector3.Zero, 10);

            // verify: from behind settlement 1, heading +Z, to short of settlement 2
            assertClose(new Vector3(50f, 1.5f, -FlyoverDirector.LeadIn), flyover.getStart());
            assertClose(new Vector3(50f, 1.5f, 200f - FlyoverDirector.StopShort), flyover.getEnd());
            assertClose(flyover.getStart() + new Vector3(0f, FlyoverDirector.Height, 0f), flyover.getEye(0));
            assertClose(flyover.getEnd() + new Vector3(0f, FlyoverDirector.Height, 0f), flyover.getEye(9));
            assertClose(flyover.getEnd() + new Vector3(0f, 0f, FlyoverDirector.LookAhead), flyover.getTarget(9));
        }

        [Fact]
        public void testTheGlideEasesInAndOut() {
            FlyoverDirector flyover = new FlyoverDirector(Vector3.Zero, new Vector3(0f, 0f, 300f), 101);

            float firstStep = flyover.getGroundPoint(1).Z - flyover.getGroundPoint(0).Z;
            float middleStep = flyover.getGroundPoint(51).Z - flyover.getGroundPoint(50).Z;

            Assert.True(firstStep < middleStep / 10f, "starts slow: " + firstStep + " vs " + middleStep);
            assertClose(flyover.getEnd(), flyover.getGroundPoint(500));
        }

        [Fact]
        public void testWithNoBorderToCrossItGlidesOutFromTheFallback() {
            List<Vector3> settlements = new List<Vector3> { new Vector3(10f, 0f, 10f), new Vector3(30f, 0f, 10f) };
            List<int> nations = new List<int> { 4, 4 };
            Vector3 player = new Vector3(5f, 1f, 5f);

            FlyoverDirector flyover = FlyoverDirector.between(settlements, nations, player, 10);

            assertClose(player - new Vector3(0f, 0f, FlyoverDirector.LeadIn), flyover.getStart());
        }
    }
}
