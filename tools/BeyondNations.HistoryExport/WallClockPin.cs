using System;
using System.Linq.Expressions;
using System.Reflection;

namespace beyondnations.historyexport {

    /**
    * Replaces the simulation's two wall-clock reads with simulated time, so a
    * headless run of a seed is reproducible. The simulation itself is not
    * modified; private fields are set before each tick so that the existing
    * DateTime.Now comparisons come out as they would in the game running at
    * exactly TicksPerSecond.
    *
    * 1. TickCounter (src/BeyondNations.Core/tick/TickCounter.cs) resets
    *    getTick() to zero whenever a real second has passed, and
    *    Simulation.fixedUpdate decides when pawns recompute and execute their
    *    behaviour from getTick(). Here the reset fires on every
    *    TicksPerSecond-th tick and never otherwise, which is what the game
    *    does at its default 50 ticks per second (Desktop's GameOptions).
    *
    * 2. Sapling.isGrown() (src/BeyondNations.Core/entity/entities/Sapling.cs)
    *    compares DateTime.Now with the time it was planted. Here each sapling
    *    is grown once (ticks since planting) / TicksPerSecond exceeds its
    *    growTime, in simulated seconds.
    *
    * A headless run executes thousands of ticks per real second, so without
    * this the same seed gives a different history on every run (#272).
    *
    * This reaches into private fields, which is why it fails loudly if one is
    * renamed rather than silently recording a wall-clock history. Once Core
    * stops reading the wall clock (#272) this can go.
    */
    public sealed class WallClockPin {
        // A sapling's planted time, once pinned, encodes its planting tick as
        // DateTime.MaxValue minus (tick + 1) ticks: still in the future, so
        // Core sees it as not grown, and recognisably not a real DateTime.Now.
        private static readonly long PinnedFloor = DateTime.MaxValue.Ticks - (long) int.MaxValue - 1;

        private readonly Simulation simulation;
        private readonly TickCounter tickCounter;
        private readonly int ticksPerSecond;
        private readonly Action<TickCounter, DateTime> setLastTickUpdate;
        private readonly Func<Sapling, DateTime> getPlanted;
        private readonly Action<Sapling, DateTime> setPlanted;
        private readonly Func<Sapling, int> getGrowTime;

        public WallClockPin(Simulation simulation, int ticksPerSecond) {
            if (ticksPerSecond <= 0) {
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), "must be positive");
            }
            this.simulation = simulation;
            this.tickCounter = simulation.getTickCounter();
            this.ticksPerSecond = ticksPerSecond;
            setLastTickUpdate = setter<TickCounter, DateTime>("lastTickUpdate");
            getPlanted = getter<Sapling, DateTime>("planted");
            setPlanted = setter<Sapling, DateTime>("planted");
            getGrowTime = getter<Sapling, int>("growTime");
        }

        /**
        * Call immediately before Simulation.fixedUpdate.
        */
        public void beforeTick() {
            // The tick about to run is getTotalTicks() + 1.
            int completed = tickCounter.getTotalTicks();
            int upcoming = completed + 1;
            bool secondElapses = upcoming % ticksPerSecond == 0;
            // MinValue: "a second has certainly passed"; MaxValue: "it never has".
            setLastTickUpdate(tickCounter, secondElapses ? DateTime.MinValue : DateTime.MaxValue);

            EntityRepository entities = simulation.getEntityRepository();
            int count = entities.getEntityCount();
            for (int i = 0; i < count; i++) {
                Sapling sapling = entities.getEntityAt(i) as Sapling;
                if (sapling == null) {
                    continue;
                }
                long planted = getPlanted(sapling).Ticks;
                if (planted == DateTime.MinValue.Ticks) {
                    continue; // already grown; the simulation replaces it this tick
                }
                int plantedTick;
                if (planted < PinnedFloor) {
                    // New since the last tick: it was planted during that tick,
                    // and is first checked by the one about to run.
                    plantedTick = completed;
                    setPlanted(sapling, new DateTime(DateTime.MaxValue.Ticks - plantedTick - 1));
                } else {
                    plantedTick = (int) (DateTime.MaxValue.Ticks - planted - 1);
                }
                double simulatedSeconds = (double) (upcoming - plantedTick) / ticksPerSecond;
                if (simulatedSeconds > getGrowTime(sapling)) {
                    setPlanted(sapling, DateTime.MinValue);
                }
            }
        }

        private static FieldInfo field<TOwner, TValue>(string name) {
            FieldInfo info = typeof(TOwner).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null || info.FieldType != typeof(TValue)) {
                throw new InvalidOperationException(
                    typeof(TOwner).Name + " no longer has a private " + typeof(TValue).Name + " '" + name + "'; "
                    + "the history exporter cannot replace its wall-clock read with simulated time. "
                    + "Update WallClockPin to match.");
            }
            return info;
        }

        // Compiled accessors: this runs for every sapling on every tick.
        private static Func<TOwner, TValue> getter<TOwner, TValue>(string name) {
            ParameterExpression owner = Expression.Parameter(typeof(TOwner));
            return Expression.Lambda<Func<TOwner, TValue>>(Expression.Field(owner, field<TOwner, TValue>(name)), owner).Compile();
        }

        private static Action<TOwner, TValue> setter<TOwner, TValue>(string name) {
            ParameterExpression owner = Expression.Parameter(typeof(TOwner));
            ParameterExpression value = Expression.Parameter(typeof(TValue));
            return Expression.Lambda<Action<TOwner, TValue>>(
                Expression.Assign(Expression.Field(owner, field<TOwner, TValue>(name)), value), owner, value).Compile();
        }
    }
}
