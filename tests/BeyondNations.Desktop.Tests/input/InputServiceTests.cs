using System.Numerics;
using Silk.NET.Input;
using Xunit;
using beyondnations.desktop.input;

namespace beyondnationstests.desktop.input {

    public class InputServiceTests {

        [Fact]
        public void isHeld_isFalse_whenNothingPressed() {
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);

            inputService.update();

            Assert.False(inputService.isHeld(Key.W));
        }

        [Fact]
        public void isHeld_isTrue_everyFrameTheKeyIsDown() {
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);
            source.press(Key.W);

            inputService.update();
            Assert.True(inputService.isHeld(Key.W));

            inputService.update();
            Assert.True(inputService.isHeld(Key.W));

            inputService.update();
            Assert.True(inputService.isHeld(Key.W));
        }

        [Fact]
        public void wasPressedThisFrame_isTrue_onlyOnTheFrameTheKeyGoesDown() {
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);

            inputService.update();
            Assert.False(inputService.wasPressedThisFrame(Key.Space));

            source.press(Key.Space);
            inputService.update();
            Assert.True(inputService.wasPressedThisFrame(Key.Space));

            // Still held on the next frame: this is the crux of the edge vs
            // held distinction the issue calls out. Held stays true; pressed
            // does not fire again.
            inputService.update();
            Assert.True(inputService.isHeld(Key.Space));
            Assert.False(inputService.wasPressedThisFrame(Key.Space));

            inputService.update();
            Assert.True(inputService.isHeld(Key.Space));
            Assert.False(inputService.wasPressedThisFrame(Key.Space));
        }

        [Fact]
        public void wasPressedThisFrame_firesAgain_afterAReleaseAndRepress() {
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);

            source.press(Key.E);
            inputService.update();
            Assert.True(inputService.wasPressedThisFrame(Key.E));

            inputService.update();
            Assert.False(inputService.wasPressedThisFrame(Key.E));

            source.release(Key.E);
            inputService.update();
            Assert.False(inputService.wasPressedThisFrame(Key.E));
            Assert.False(inputService.isHeld(Key.E));

            source.press(Key.E);
            inputService.update();
            Assert.True(inputService.wasPressedThisFrame(Key.E));
        }

        [Fact]
        public void isHeld_and_wasPressedThisFrame_areIndependentPerKey() {
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);

            source.press(Key.ShiftLeft);
            inputService.update();
            source.press(Key.W);
            inputService.update();

            // Shift has been held for two frames now, so it should no longer
            // read as "just pressed"; W was pressed this frame only.
            Assert.True(inputService.isHeld(Key.ShiftLeft));
            Assert.False(inputService.wasPressedThisFrame(Key.ShiftLeft));
            Assert.True(inputService.isHeld(Key.W));
            Assert.True(inputService.wasPressedThisFrame(Key.W));
        }

        [Fact]
        public void getMouseDelta_reportsWhatTheSourceProvidesForThatFrame() {
            FakeInputSource source = new FakeInputSource();
            InputService inputService = new InputService(source);

            source.setMouseDelta(new Vector2(3.5f, -2f));
            inputService.update();
            Assert.Equal(new Vector2(3.5f, -2f), inputService.getMouseDelta());

            source.setMouseDelta(Vector2.Zero);
            inputService.update();
            Assert.Equal(Vector2.Zero, inputService.getMouseDelta());
        }
    }
}
