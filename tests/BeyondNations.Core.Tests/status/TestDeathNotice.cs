using Xunit;
using beyondnations;

namespace beyondnationstests {

    /**
    * Pawn deaths on the status line: a run of them (a hard winter) is one
    * counting line rather than one line per pawn.
    */
    public class TestDeathNotice {

        [Fact]
        public void testOneDeath_NamesThePawn() {
            Assert.Equal("Alder has died.", DeathNotice.combine("", "Alder"));
            Assert.Equal("Alder has died.", DeathNotice.combine("Winter has come.", "Alder"));
        }

        [Fact]
        public void testDeathsWhileTheNoticeShows_AreCountedOnOneLine() {
            string status = DeathNotice.combine("", "Alder");
            status = DeathNotice.combine(status, "Birch");
            Assert.Equal("Alder and Birch have died.", status);
            status = DeathNotice.combine(status, "Cedar");
            Assert.Equal("3 pawns have died.", status);
            status = DeathNotice.combine(status, "Dogwood");
            Assert.Equal("4 pawns have died.", status);
        }

        [Fact]
        public void testThePawnDyingTwice_IsStillCounted() {
            Assert.Equal("2 pawns have died.", DeathNotice.combine("Alder has died.", "Alder"));
        }

        [Fact]
        public void testAnotherMessageInBetween_StartsANewCount() {
            Assert.Equal("Elm has died.", DeathNotice.combine("Spring has come. Saplings grow, chickens hatch.", "Elm"));
            // a status that only ends like a death notice is not one
            Assert.Equal("Elm has died.", DeathNotice.combine("Player 3 has died. (x2)", "Elm"));
        }

        [Fact]
        public void testAHardWinter_ShowsOneCountingLine_NotOneLinePerDeath() {
            TickCounter ticks = new TickCounter();
            Status status = new Status(ticks, 100);
            string[] names = { "Alder", "Birch", "Cedar", "Dogwood", "Elm", "Fir" };
            foreach (string name in names) {
                status.update(DeathNotice.combine(status.getStatus(), name));
                ticks.increment();
                status.clearStatusIfExpired();
            }
            Assert.Equal("6 pawns have died.", status.getStatus());

            // once the line has gone, the next death is named again
            for (int i = 0; i < 101; i++) {
                ticks.increment();
            }
            status.clearStatusIfExpired();
            status.update(DeathNotice.combine(status.getStatus(), "Gum"));
            Assert.Equal("Gum has died.", status.getStatus());
        }
    }
}
