using Xunit;

using beyondnations.desktop.ui;

namespace beyondnationstests.desktop.ui {

    /**
    * A command bar button that has just appeared or moved takes no click
    * for a moment (ButtonSettle): in the 0.6.3 playtest a click on Talk
    * bought a pickaxe for 299 coins when the pawn walked off and Buy Pickaxe
    * dropped into its place.
    */
    public class TestButtonSettle {

        private static bool frame(ButtonSettle settle, string label, float x, float y, double now) {
            bool settled = settle.isSettled(label, x, y, now);
            settle.endFrame();
            return settled;
        }

        [Fact]
        public void aNewButton_WaitsBeforeTakingAClick() {
            ButtonSettle settle = new ButtonSettle();
            Assert.False(frame(settle, "Buy Pickaxe (299)", 10, 400, 1.0));
            Assert.False(frame(settle, "Buy Pickaxe (299)", 10, 400, 1.0 + ButtonSettle.SettleSeconds / 2));
            Assert.True(frame(settle, "Buy Pickaxe (299)", 10, 400, 1.0 + ButtonSettle.SettleSeconds));
        }

        [Fact]
        public void aButtonThatMoves_WaitsAgain() {
            ButtonSettle settle = new ButtonSettle();
            frame(settle, "Buy Pickaxe (299)", 10, 400, 0.0);
            Assert.True(frame(settle, "Buy Pickaxe (299)", 10, 400, 5.0));
            // the pawn row above it went, and it dropped into Talk's place
            Assert.False(frame(settle, "Buy Pickaxe (299)", 10, 430, 5.1));
            Assert.True(frame(settle, "Buy Pickaxe (299)", 10, 430, 5.1 + ButtonSettle.SettleSeconds));
        }

        [Fact]
        public void aButtonThatGoesAndComesBack_WaitsAgain() {
            ButtonSettle settle = new ButtonSettle();
            frame(settle, "Withdraw Funds", 10, 470, 0.0);
            Assert.True(frame(settle, "Withdraw Funds", 10, 470, 2.0));
            settle.endFrame(); // a frame without it: the player left the settlement
            Assert.False(frame(settle, "Withdraw Funds", 10, 470, 2.1));
        }

        [Fact]
        public void aChangingCount_IsTheSameButton() {
            ButtonSettle settle = new ButtonSettle();
            frame(settle, "Collect Stockpile (12)", 130, 470, 0.0);
            Assert.True(frame(settle, "Collect Stockpile (13)", 130, 470, 1.0));
            Assert.Equal(ButtonSettle.keyOf("Feed Residents (3 hungry)"), ButtonSettle.keyOf("Feed Residents (10 hungry)"));
        }

        [Fact]
        public void anotherPawn_IsANewButton() {
            ButtonSettle settle = new ButtonSettle();
            frame(settle, "Talk to Ann Lee (Stranger)", 10, 414, 0.0);
            Assert.True(frame(settle, "Talk to Ann Lee (Stranger)", 10, 414, 1.0));
            Assert.False(frame(settle, "Talk to Bob Ray (Stranger)", 10, 414, 1.1));
        }
    }
}
