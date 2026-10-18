using System;
using beyondnations;
using Xunit;

namespace beyondnationstests {

    public class TestTimeOfDay {

        private static TickCounter ticked(int ticks) {
            TickCounter counter = new TickCounter();
            for (int i = 0; i < ticks; i++) {
                counter.increment();
            }
            return counter;
        }

        [Fact]
        public void StartsAtTheStartFractionOnDayZero() {
            TimeOfDay time = new TimeOfDay(new TickCounter());
            Assert.Equal(TimeOfDay.DefaultStartFraction, time.getFraction(), 6);
            Assert.Equal(0, time.getDay());
        }

        [Fact]
        public void AdvancesWithTheTotalTickCount() {
            TimeOfDay time = new TimeOfDay(ticked(250), 1000, 0.25f);
            Assert.Equal(0.5f, time.getFraction(), 6);
        }

        [Fact]
        public void WrapsAfterOneDayAndCountsDays() {
            Assert.Equal(0.2f, TimeOfDay.fractionAt(900, 1000, 0.3f), 5);
            Assert.Equal(1, TimeOfDay.dayAt(900, 1000, 0.3f));
            Assert.Equal(0.3f, TimeOfDay.fractionAt(5000, 1000, 0.3f), 5);
            Assert.Equal(5, TimeOfDay.dayAt(5000, 1000, 0.3f));
            Assert.Equal(0, TimeOfDay.dayAt(699, 1000, 0.3f));
        }

        [Fact]
        public void StaysInsideTheHalfOpenRange() {
            for (long tick = 0; tick < 3000; tick += 7) {
                float f = TimeOfDay.fractionAt(tick, 977, 0.999f);
                Assert.InRange(f, 0f, 0.99999994f);
            }
        }

        [Fact]
        public void IsAPureFunctionOfTheTickCount() {
            TimeOfDay a = new TimeOfDay(ticked(1234), 500, 0.1f);
            TimeOfDay b = new TimeOfDay(ticked(1234), 500, 0.1f);
            Assert.Equal(a.getFraction(), b.getFraction());
            Assert.Equal(TimeOfDay.fractionAt(1234, 500, 0.1f), a.getFraction());
        }

        [Fact]
        public void TheStartFractionCanBeMovedButNotOutOfRange() {
            TimeOfDay time = new TimeOfDay(new TickCounter());
            time.setStartFraction(0.9f);
            Assert.Equal(0.9f, time.getFraction(), 6);
            Assert.Throws<ArgumentOutOfRangeException>(() => time.setStartFraction(1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => time.setStartFraction(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => time.setStartFraction(float.NaN));
        }

        [Fact]
        public void RejectsBadConstructorArguments() {
            Assert.Throws<ArgumentNullException>(() => new TimeOfDay(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TimeOfDay(new TickCounter(), 0));
        }

        [Fact]
        public void TheSimulationExposesOneDrivenByItsTickCounter() {
            Simulation simulation = new Simulation(new GameConfig());
            TimeOfDay time = simulation.getTimeOfDay();
            float before = time.getFraction();
            simulation.getTickCounter().increment();
            Assert.True(time.getFraction() > before);
        }
    }
}
