using System.Numerics;
using Silk.NET.Input;

namespace beyondnations.desktop.input {

    /**
    * The real IInputSource: polls whatever keyboards and mice Silk.NET
    * reports through the window's IInputContext. This is the only file in
    * the input layer that talks to Silk.NET.Input directly; everything else
    * (InputService, KeyBindings, MouseLook, PlayerInputController) works
    * against the IInputSource seam and does not know Silk.NET exists.
    */
    public class SilkInputSource : IInputSource {
        private readonly IInputContext context;
        private readonly PressLatch<Key> keyPresses = new PressLatch<Key>();
        private Vector2 lastMousePosition;
        private bool hasLastMousePosition;

        public SilkInputSource(IInputContext context) {
            this.context = context;
            foreach (IKeyboard keyboard in context.Keyboards) {
                keyboard.KeyDown += (_, key, _) => keyPresses.recordPress(key);
            }
        }

        /**
        * Down now, or went down since the last frame. The second half is what
        * keeps a tap shorter than a frame from being lost (see PressLatch).
        */
        public bool IsKeyDown(Key key) {
            if (keyPresses.wasPressed(key)) {
                return true;
            }
            foreach (IKeyboard keyboard in context.Keyboards) {
                if (keyboard.IsKeyPressed(key)) {
                    return true;
                }
            }
            return false;
        }

        public void EndFrame() {
            keyPresses.clear();
        }

        /**
        * Delta since the last call, in whatever units Silk.NET reports mouse
        * position in (pixels). The first call after a mouse becomes available
        * has nothing to diff against, so it reports zero rather than a spurious
        * jump from the origin.
        */
        public Vector2 GetMouseDelta() {
            if (context.Mice.Count == 0) {
                return Vector2.Zero;
            }

            Vector2 position = context.Mice[0].Position;
            if (!hasLastMousePosition) {
                lastMousePosition = position;
                hasLastMousePosition = true;
                return Vector2.Zero;
            }

            Vector2 delta = position - lastMousePosition;
            lastMousePosition = position;
            return delta;
        }
    }
}
