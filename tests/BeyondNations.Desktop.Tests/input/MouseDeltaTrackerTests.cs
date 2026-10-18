using System.Numerics;
using Xunit;
using beyondnations.desktop.input;

namespace beyondnationstests.desktop.input {

    /**
    * Moving the free cursor to click a command bar button used to swing the
    * camera; look now follows the mouse only while the right button is held.
    */
    public class MouseDeltaTrackerTests {

        [Fact]
        public void cursorMovedWithoutLookButton_doesNotTurnTheCamera() {
            MouseDeltaTracker tracker = new MouseDeltaTracker();
            tracker.next(new Vector2(640, 360), false);

            Assert.Equal(Vector2.Zero, tracker.next(new Vector2(170, 750), false));
        }

        [Fact]
        public void cursorMovedWithLookButtonHeld_reportsTheMovement() {
            MouseDeltaTracker tracker = new MouseDeltaTracker();
            tracker.next(new Vector2(100, 100), true);

            Assert.Equal(new Vector2(15, -5), tracker.next(new Vector2(115, 95), true));
        }

        [Fact]
        public void pressingLookButtonAfterMovingFreely_doesNotJump() {
            MouseDeltaTracker tracker = new MouseDeltaTracker();
            tracker.next(new Vector2(0, 0), false);
            tracker.next(new Vector2(900, 600), false);

            Assert.Equal(new Vector2(2, 1), tracker.next(new Vector2(902, 601), true));
        }

        [Fact]
        public void firstSample_isZero() {
            MouseDeltaTracker tracker = new MouseDeltaTracker();

            Assert.Equal(Vector2.Zero, tracker.next(new Vector2(500, 500), true));
        }
    }
}
