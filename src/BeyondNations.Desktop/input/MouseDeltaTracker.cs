using System.Numerics;

namespace beyondnations.desktop.input {

    /**
    * Turns successive cursor positions into the movement that drives camera
    * look, but only while the look button is held.
    *
    * The cursor is never captured, because the menus and the world command
    * bar are clicked with it. Feeding every movement into the camera meant
    * that reaching for a command bar button swung the view down at the
    * ground, and moving out of the window and back in at another edge spun
    * it. Holding the right mouse button to look keeps the cursor free for
    * clicking. The position is tracked even while the button is up, so
    * pressing it never produces a jump from wherever the cursor last was.
    */
    public class MouseDeltaTracker {
        private Vector2 lastPosition;
        private bool hasLastPosition;

        public Vector2 next(Vector2 position, bool lookHeld) {
            if (!hasLastPosition) {
                lastPosition = position;
                hasLastPosition = true;
                return Vector2.Zero;
            }

            Vector2 delta = position - lastPosition;
            lastPosition = position;
            return lookHeld ? delta : Vector2.Zero;
        }
    }
}
