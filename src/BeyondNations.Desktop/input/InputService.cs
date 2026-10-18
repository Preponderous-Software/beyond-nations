using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Input;

namespace beyondnations.desktop.input {

    /**
    * Turns a single-frame IInputSource into the two query styles the old
    * Unity code relied on: Input.GetKey (held, every frame it is down) and
    * Input.GetKeyDown (pressed, only the one frame it transitions from up to
    * down). Both are preserved because both are used -- held for movement and
    * sprint, pressed for interact, nation actions, toggles and the rest.
    *
    * update() must be called exactly once per frame, before any query. It
    * takes a snapshot of every key from the current IInputSource call and
    * compares it against the previous snapshot; nothing here touches
    * Silk.NET, so it can be driven by a FakeInputSource in tests with no
    * window at all.
    */
    public class InputService {
        private static readonly Key[] TrackedKeys = BuildTrackedKeys();

        private readonly IInputSource source;
        private HashSet<Key> heldLastFrame = new HashSet<Key>();
        private HashSet<Key> heldThisFrame = new HashSet<Key>();
        private Vector2 mouseDelta;

        public InputService(IInputSource source) {
            this.source = source;
        }

        public void update() {
            HashSet<Key> swap = heldLastFrame;
            heldLastFrame = heldThisFrame;
            heldThisFrame = swap;
            heldThisFrame.Clear();

            foreach (Key key in TrackedKeys) {
                if (source.IsKeyDown(key)) {
                    heldThisFrame.Add(key);
                }
            }

            mouseDelta = source.GetMouseDelta();
        }

        /**
        * True every frame the key is down. Equivalent to Unity's
        * Input.GetKey.
        */
        public bool isHeld(Key key) {
            return heldThisFrame.Contains(key);
        }

        /**
        * True only on the frame the key transitions from up to down.
        * Equivalent to Unity's Input.GetKeyDown.
        */
        public bool wasPressedThisFrame(Key key) {
            return heldThisFrame.Contains(key) && !heldLastFrame.Contains(key);
        }

        /**
        * Mouse movement since the previous update(), for camera look. Zero on
        * a frame with no mouse or no prior position to diff against.
        */
        public Vector2 getMouseDelta() {
            return mouseDelta;
        }

        private static Key[] BuildTrackedKeys() {
            List<Key> keys = new List<Key>();
            foreach (Key key in Enum.GetValues(typeof(Key))) {
                if (key != Key.Unknown) {
                    keys.Add(key);
                }
            }
            return keys.ToArray();
        }
    }
}
