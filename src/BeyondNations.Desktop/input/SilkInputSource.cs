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
        private readonly MouseDeltaTracker mouseDelta = new MouseDeltaTracker();

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
        * Movement since the last call, in pixels, while the right mouse
        * button is held, and zero otherwise; see MouseDeltaTracker for why
        * looking is gated on the button.
        */
        public Vector2 GetMouseDelta() {
            if (context.Mice.Count == 0) {
                return Vector2.Zero;
            }

            IMouse mouse = context.Mice[0];
            return mouseDelta.next(mouse.Position, mouse.IsButtonPressed(MouseButton.Right));
        }
    }
}
