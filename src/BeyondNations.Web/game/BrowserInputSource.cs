using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Input;
using beyondnations.desktop.input;

namespace beyondnations.web {

    /**
    * The browser's IInputSource, the counterpart of SilkInputSource.
    *
    * Keys arrive as events rather than being polled, so "down now" is a set
    * the events keep up to date. A key that goes down and up again between
    * two frames is still reported down for one frame, through the same
    * PressLatch SilkInputSource uses (#290), so a quick tap is never lost.
    *
    * Looking around follows #291: only while the right mouse button is held.
    * The page turns a right-button drag into pointer lock and sends the
    * relative movement here; with the button up nothing arrives, so the
    * delta is zero just as MouseDeltaTracker makes it on the desktop.
    */
    public class BrowserInputSource : IInputSource {
        private readonly HashSet<Key> down = new HashSet<Key>();
        private readonly PressLatch<Key> presses = new PressLatch<Key>();
        private Vector2 lookDelta;

        public void key(Key key, bool isDown) {
            if (key == Key.Unknown) {
                return;
            }
            if (isDown) {
                if (down.Add(key)) {
                    presses.recordPress(key);
                }
            } else {
                down.Remove(key);
            }
        }

        public void look(float dx, float dy) {
            lookDelta += new Vector2(dx, dy);
        }

        /** Focus left the page: nothing stays held (no stuck W after alt-tab). */
        public void releaseAll() {
            down.Clear();
            lookDelta = Vector2.Zero;
        }

        public bool IsKeyDown(Key key) {
            return presses.wasPressed(key) || down.Contains(key);
        }

        public Vector2 GetMouseDelta() {
            Vector2 delta = lookDelta;
            lookDelta = Vector2.Zero;
            return delta;
        }

        public void EndFrame() {
            presses.clear();
        }
    }
}
