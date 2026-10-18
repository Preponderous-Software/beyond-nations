using System;
using System.Collections.Generic;

namespace beyondnations.desktop.input {

    /**
    * Remembers which keys or buttons went down since the last frame, from
    * the device's press events, so that a press shorter than one frame is not
    * lost.
    *
    * Silk.NET's IsKeyPressed and IsButtonPressed report the state at the
    * moment they are asked. A key or button that goes down and comes back up
    * between two frames -- a quick tap at a low frame rate, or a touchpad
    * tap-to-click, which arrives as down and up a few milliseconds apart --
    * was never seen down at all, so the press did nothing. Holding on to the
    * press event until the frame has read it closes that gap.
    */
    public class PressLatch<T> {
        private readonly HashSet<T> pressed = new HashSet<T>();

        public void recordPress(T item) {
            pressed.Add(item);
        }

        public bool wasPressed(T item) {
            return pressed.Contains(item);
        }

        public void clear() {
            pressed.Clear();
        }

        /**
        * The presses that are already over: went down since the last frame
        * and are up again now. A press that is still held is left to the
        * ordinary polled state. Clears the latch.
        */
        public List<T> takeReleasedSinceLastFrame(Func<T, bool> isDownNow) {
            List<T> released = new List<T>();
            foreach (T item in pressed) {
                if (!isDownNow(item)) {
                    released.Add(item);
                }
            }
            pressed.Clear();
            return released;
        }
    }
}
