using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Input;
using beyondnations.desktop.input;

namespace beyondnationstests.desktop.input {

    /**
    * A window-free IInputSource for tests. The held set and mouse delta are
    * set directly by the test, then InputService.update() is called to take
    * a snapshot -- exactly what SilkInputSource would report from a real
    * keyboard and mouse, without a window ever existing.
    */
    public class FakeInputSource : IInputSource {
        private readonly HashSet<Key> held = new HashSet<Key>();
        private Vector2 mouseDelta = Vector2.Zero;

        public void press(Key key) {
            held.Add(key);
        }

        public void release(Key key) {
            held.Remove(key);
        }

        public void setMouseDelta(Vector2 delta) {
            mouseDelta = delta;
        }

        public bool IsKeyDown(Key key) {
            return held.Contains(key);
        }

        public Vector2 GetMouseDelta() {
            return mouseDelta;
        }
    }
}
