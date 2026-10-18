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
        private Vector2 lastMousePosition;
        private bool hasLastMousePosition;

        public SilkInputSource(IInputContext context) {
            this.context = context;
        }

        public bool IsKeyDown(Key key) {
            foreach (IKeyboard keyboard in context.Keyboards) {
                if (keyboard.IsKeyPressed(key)) {
                    return true;
                }
            }
            return false;
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
