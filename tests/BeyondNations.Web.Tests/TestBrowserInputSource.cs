using System.Numerics;
using beyondnations.desktop.input;
using beyondnations.web;
using Silk.NET.Input;
using Xunit;

namespace beyondnationstests.web {

    public class TestBrowserInputSource {

        /** #290: a key that goes down and up between two frames is still one press. */
        [Fact]
        public void aTapBetweenFramesIsPressedForOneFrame() {
            BrowserInputSource source = new BrowserInputSource();
            InputService input = new InputService(source);
            input.update();

            source.key(Key.E, true);
            source.key(Key.E, false);
            input.update();
            Assert.True(input.wasPressedThisFrame(Key.E));

            input.update();
            Assert.False(input.wasPressedThisFrame(Key.E));
            Assert.False(input.isHeld(Key.E));
        }

        [Fact]
        public void aHeldKeyIsHeldUntilReleased() {
            BrowserInputSource source = new BrowserInputSource();
            InputService input = new InputService(source);
            source.key(Key.W, true);
            input.update();
            Assert.True(input.wasPressedThisFrame(Key.W));
            input.update();
            Assert.True(input.isHeld(Key.W));
            Assert.False(input.wasPressedThisFrame(Key.W));
            source.key(Key.W, false);
            input.update();
            Assert.False(input.isHeld(Key.W));
        }

        /** Auto-repeat sends keydown again while held; that is not a new press. */
        [Fact]
        public void repeatedKeyDownsAreNotNewPresses() {
            BrowserInputSource source = new BrowserInputSource();
            InputService input = new InputService(source);
            source.key(Key.N, true);
            input.update();
            source.key(Key.N, true);
            input.update();
            Assert.False(input.wasPressedThisFrame(Key.N));
        }

        [Fact]
        public void lookMovementIsReportedOnceAndAccumulates() {
            BrowserInputSource source = new BrowserInputSource();
            InputService input = new InputService(source);
            source.look(10, -2);
            source.look(5, 1);
            input.update();
            Assert.Equal(new Vector2(15, -1), input.getMouseDelta());
            input.update();
            Assert.Equal(Vector2.Zero, input.getMouseDelta());
        }

        [Fact]
        public void losingFocusReleasesEverything() {
            BrowserInputSource source = new BrowserInputSource();
            InputService input = new InputService(source);
            source.key(Key.W, true);
            source.look(30, 0);
            input.update();
            source.releaseAll();
            input.update();
            input.update();
            Assert.False(input.isHeld(Key.W));
            Assert.Equal(Vector2.Zero, input.getMouseDelta());
        }

        /** The on-screen stick: kept within the unit circle, and let go of when focus is lost. */
        [Fact]
        public void theStickIsClampedAndReleasedWithFocus() {
            BrowserInputSource source = new BrowserInputSource();
            InputService input = new InputService(source);
            source.move(0.5f, -0.25f);
            input.update();
            Assert.Equal(new Vector2(0.5f, -0.25f), input.getMoveAxis());

            source.move(3f, 3f);
            input.update();
            Assert.Equal(1f, input.getMoveAxis().Length(), 3);

            source.move(float.NaN, 1f);
            input.update();
            Assert.Equal(Vector2.Zero, input.getMoveAxis());

            source.move(0f, 1f);
            source.releaseAll();
            input.update();
            Assert.Equal(Vector2.Zero, input.getMoveAxis());
        }

        [Fact]
        public void unknownKeysAreIgnored() {
            BrowserInputSource source = new BrowserInputSource();
            source.key(Key.Unknown, true);
            Assert.False(source.IsKeyDown(Key.Unknown));
        }
    }
}
