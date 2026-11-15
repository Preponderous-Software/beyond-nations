using System;
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

        [Fact]
        public void testOrbit_CirclesTheCentreOnce_LookingAtIt() {
            Vector3 centre = new Vector3(10f, 2f, -5f);
            FlyoverDirector orbit = FlyoverDirector.orbit(centre, 40);
            Assert.True(orbit.isOrbit());
            for (int f = 0; f < 40; f++) {
                Vector3 ground = orbit.getGroundPoint(f);
                float dx = ground.X - centre.X, dz = ground.Z - centre.Z;
                Assert.Equal(FlyoverDirector.OrbitRadius, MathF.Sqrt(dx * dx + dz * dz), 3);
                Assert.Equal(centre.Y + FlyoverDirector.OrbitHeight, orbit.getEye(f).Y, 3);
                assertClose(centre + new Vector3(0f, 3f, 0f), orbit.getTarget(f));
            }
            // a quarter of the way round is a quarter turn
            assertClose(centre + new Vector3(0f, 0f, FlyoverDirector.OrbitRadius), orbit.getGroundPoint(10));
        }

        [Fact]
        public void testThickestStand_IsTheTreeWithTheMostNeighbours() {
            List<Vector3> trees = new List<Vector3> {
                new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), new Vector3(104f, 0f, 0f),
                new Vector3(100f, 0f, 5f), new Vector3(200f, 0f, 0f),
            };
            Assert.Equal(new Vector3(100f, 0f, 0f), FlyoverDirector.thickestStand(trees, 10f, Vector3.Zero));
            Assert.Equal(new Vector3(7f, 0f, 7f), FlyoverDirector.thickestStand(new List<Vector3>(), 10f, new Vector3(7f, 0f, 7f)));
        }
    }
}
