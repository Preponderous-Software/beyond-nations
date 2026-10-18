using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Input;
using Xunit;
using beyondnations.desktop.input;

namespace beyondnationstests.desktop.input {

    /**
    * A press that goes down and back up between two frames used to be lost:
    * Silk.NET's polled state never showed it down. The latch keeps it.
    */
    public class PressLatchTests {

        [Fact]
        public void tapShorterThanAFrame_isReleasedSinceLastFrame() {
            PressLatch<MouseButton> latch = new PressLatch<MouseButton>();
            latch.recordPress(MouseButton.Left);

            List<MouseButton> released = latch.takeReleasedSinceLastFrame(_ => false);

            Assert.Equal(new[] { MouseButton.Left }, released);
            Assert.False(latch.wasPressed(MouseButton.Left));
        }

        [Fact]
        public void pressStillHeld_isLeftToThePolledState() {
            PressLatch<MouseButton> latch = new PressLatch<MouseButton>();
            latch.recordPress(MouseButton.Left);

            Assert.Empty(latch.takeReleasedSinceLastFrame(_ => true));
        }

        [Fact]
        public void tapShorterThanAFrame_reachesInputService_asOnePress() {
            LatchingFakeSource source = new LatchingFakeSource();
            InputService inputService = new InputService(source);
            inputService.update();

            // down and up both between this frame and the last
            source.tap(Key.E);
            inputService.update();
            Assert.True(inputService.wasPressedThisFrame(Key.E));
            Assert.True(inputService.wasAnyKeyPressedThisFrame());

            inputService.update();
            Assert.False(inputService.isHeld(Key.E));
            Assert.False(inputService.wasPressedThisFrame(Key.E));
        }

        /**
        * Live state is always "up"; presses only arrive as events, the way
        * a sub-frame tap reaches SilkInputSource.
        */
        private class LatchingFakeSource : IInputSource {
            private readonly PressLatch<Key> presses = new PressLatch<Key>();

            public void tap(Key key) {
                presses.recordPress(key);
            }

            public bool IsKeyDown(Key key) {
                return presses.wasPressed(key);
            }

            public Vector2 GetMouseDelta() {
                return Vector2.Zero;
            }

            public void EndFrame() {
                presses.clear();
            }
        }
    }
}
