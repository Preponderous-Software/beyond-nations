using System.Numerics;
using Silk.NET.Input;

namespace beyondnations.desktop.input {

    /**
    * A single-frame snapshot of raw input device state.
    *
    * This is deliberately dumb: it answers "is this key down right now" and
    * "how far did the mouse move since the last question", nothing more.
    * Held-vs-pressed-this-frame is a stateful distinction that requires
    * remembering the previous frame, and that logic lives in InputService so
    * it can be unit tested without a window. SilkInputSource is the only
    * implementation that touches Silk.NET.Input; FakeInputSource (in the test
    * project) drives InputService in tests.
    */
    public interface IInputSource {
        bool IsKeyDown(Key key);

        Vector2 GetMouseDelta();
    }
}
