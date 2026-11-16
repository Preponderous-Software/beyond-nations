using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * A world the host fast-forwards before the player takes control
    * (--season, --advance-ticks) starts the player as a fresh world would:
    * fed, and with their years still ahead of them (#490).
    */
    public class TestFastForwardStart {
        private const float Step = 1f / 60f;

        private readonly DiplomacyFixture world = new DiplomacyFixture();
        private Realm realm { get { return world.simulation.getRealm(); } }

        /** The realm's own clock, as TestSuccession runs it. */
        private void pass(int ticks) {
            TickCounter counter = world.simulation.getTickCounter();
            for (int i = 0; i < ticks; i++) {
                counter.increment();
                realm.update(counter.getTotalTicks());
            }
        }

        [Fact]
        public void testAPlayerAway_IsKeptFed_AndDoesNotDie() {
            world.player.setEnergy(0.5f);
            for (int i = 0; i < 600; i++) {
                world.simulation.fixedUpdateWithPlayerAway(Step);
            }
            Assert.Equal(0, world.simulation.getNumPlayerDeaths());
            Assert.True(world.player.getEnergy() > Player.MaxEnergy - 1f, "energy " + world.player.getEnergy());
        }

        [Fact]
        public void testAFreshStart_FillsTheEnergyBar() {
            world.player.setEnergy(1.1f);
            world.simulation.startPlayerFresh();
            Assert.Equal(Player.MaxEnergy, world.player.getEnergy());
        }

        [Fact]
        public void testAfterTheSeasonJump_TheReignStartsWhenThePlayerDoes() {
            world.simulation.getSeasonCalendar().advanceClockTo(world.simulation.getTickCounter(), Season.AUTUMN);
            int start = world.simulation.getTickCounter().getTotalTicks();
            Assert.True(start >= Realm.OLD_AGE_TICKS, "the jump alone is a lifetime: " + start);

            world.simulation.startPlayerFresh();
            Assert.Equal(start, realm.getReignStartTick());
            Assert.Equal(0, realm.getReignTicks());

            world.playerNation(3);
            Assert.Equal(Realm.OLD_AGE_TICKS, realm.getTicksUntilOldAge());
            // no "growing old" in the first days, as in a fresh world
            pass(Realm.OLD_AGE_TICKS - Realm.OLD_AGE_WARNING_TICKS - Realm.LAW_ROUND_TICKS);
            Assert.DoesNotContain("growing old", world.player.getStatus().getStatus());
            Assert.Empty(realm.getLineage());
        }

        [Fact]
        public void testWithoutAFreshStart_TheSkippedDaysCountAsTheReign() {
            // what #490 saw: the founder grew old within minutes of play
            world.simulation.getSeasonCalendar().advanceClockTo(world.simulation.getTickCounter(), Season.AUTUMN);
            world.playerNation(3);
            pass(Realm.LAW_ROUND_TICKS);
            Assert.Contains("growing old", world.player.getStatus().getStatus());
        }
    }
}
